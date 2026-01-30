import { useState, useEffect } from 'react'
import { 
  ExportYoYAnnexRequest, 
  ReportingPeriod, 
  ReportSection, 
  YoYAnnexExportRecord 
} from '@/lib/types'
import { exportYoYAnnex, getYoYAnnexExports } from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { Loader2, Download, FileText, Calendar, CheckCircle2, AlertCircle } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Textarea } from '@/components/ui/textarea'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

interface YoYAnnexExportDialogProps {
  currentPeriod: ReportingPeriod
  periods: ReportingPeriod[]
  sections?: ReportSection[]
  currentUserId: string
  onClose: () => void
}

export default function YoYAnnexExportDialog({
  currentPeriod,
  periods,
  sections = [],
  currentUserId,
  onClose
}: YoYAnnexExportDialogProps) {
  const [priorPeriodId, setPriorPeriodId] = useState<string>('')
  const [selectedSectionIds, setSelectedSectionIds] = useState<string[]>([])
  const [includeVarianceExplanations, setIncludeVarianceExplanations] = useState(true)
  const [includeEvidenceReferences, setIncludeEvidenceReferences] = useState(true)
  const [includeNarrativeDiffs, setIncludeNarrativeDiffs] = useState(true)
  const [exportNote, setExportNote] = useState('')
  const [isExporting, setIsExporting] = useState(false)
  const [exportError, setExportError] = useState<string | null>(null)
  const [exportHistory, setExportHistory] = useState<YoYAnnexExportRecord[]>([])
  const [loadingHistory, setLoadingHistory] = useState(true)

  // Get available prior periods (periods before the current period)
  const availablePriorPeriods = periods.filter(p => 
    p.id !== currentPeriod.id && 
    new Date(p.startDate) < new Date(currentPeriod.startDate)
  ).sort((a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime())

  // Set default prior period to the most recent one
  useEffect(() => {
    if (availablePriorPeriods.length > 0 && !priorPeriodId) {
      setPriorPeriodId(availablePriorPeriods[0].id)
    }
  }, [availablePriorPeriods, priorPeriodId])

  // Load export history
  useEffect(() => {
    const loadHistory = async () => {
      try {
        const history = await getYoYAnnexExports(currentPeriod.id)
        setExportHistory(history)
      } catch (error) {
        console.error('Failed to load export history:', error)
      } finally {
        setLoadingHistory(false)
      }
    }
    
    loadHistory()
  }, [currentPeriod.id])

  const handleExport = async () => {
    if (!priorPeriodId) {
      setExportError('Please select a prior period to compare against.')
      return
    }

    setIsExporting(true)
    setExportError(null)

    try {
      const request: ExportYoYAnnexRequest = {
        currentPeriodId: currentPeriod.id,
        priorPeriodId,
        sectionIds: selectedSectionIds.length > 0 ? selectedSectionIds : undefined,
        includeVarianceExplanations,
        includeEvidenceReferences,
        includeNarrativeDiffs,
        exportedBy: currentUserId,
        exportNote: exportNote || undefined
      }

      const blob = await exportYoYAnnex(request)
      
      // Create download link
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      
      const priorPeriod = periods.find(p => p.id === priorPeriodId)
      const filename = `yoy-annex-${priorPeriod?.name.replace(/\s+/g, '-')}-to-${currentPeriod.name.replace(/\s+/g, '-')}-${new Date().toISOString().split('T')[0]}.zip`
      a.download = filename
      
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      window.URL.revokeObjectURL(url)

      // Refresh export history
      const history = await getYoYAnnexExports(currentPeriod.id)
      setExportHistory(history)
      
      // Show success message and close after a delay
      setTimeout(() => {
        onClose()
      }, 1500)
    } catch (error) {
      console.error('Export failed:', error)
      setExportError(error instanceof Error ? error.message : 'Failed to export YoY annex')
    } finally {
      setIsExporting(false)
    }
  }

  const toggleSection = (sectionId: string) => {
    setSelectedSectionIds(prev => 
      prev.includes(sectionId) 
        ? prev.filter(id => id !== sectionId)
        : [...prev, sectionId]
    )
  }

  const selectAllSections = () => {
    setSelectedSectionIds(sections.map(s => s.id))
  }

  const clearAllSections = () => {
    setSelectedSectionIds([])
  }

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 Bytes'
    const k = 1024
    const sizes = ['Bytes', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString()
  }

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Export Year-over-Year Annex</DialogTitle>
          <DialogDescription>
            Generate a comprehensive annex for auditors with metric deltas, variance explanations, and evidence references.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          {/* Period Selection */}
          <div className="space-y-2">
            <Label>Comparison Periods</Label>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label className="text-sm text-muted-foreground">Current Period</Label>
                <div className="flex items-center gap-2 p-3 border rounded-md bg-muted/50">
                  <Calendar className="h-4 w-4 text-muted-foreground" />
                  <div>
                    <div className="font-medium">{currentPeriod.name}</div>
                    <div className="text-xs text-muted-foreground">
                      {currentPeriod.startDate} to {currentPeriod.endDate}
                    </div>
                  </div>
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="prior-period" className="text-sm text-muted-foreground">
                  Prior Period *
                </Label>
                <Select value={priorPeriodId} onValueChange={setPriorPeriodId}>
                  <SelectTrigger id="prior-period">
                    <SelectValue placeholder="Select prior period" />
                  </SelectTrigger>
                  <SelectContent>
                    {availablePriorPeriods.map(period => (
                      <SelectItem key={period.id} value={period.id}>
                        {period.name} ({period.startDate} to {period.endDate})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>

          {/* Section Selection */}
          {sections.length > 0 && (
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label>Sections to Include</Label>
                <div className="flex gap-2">
                  <Button variant="ghost" size="sm" onClick={selectAllSections}>
                    Select All
                  </Button>
                  <Button variant="ghost" size="sm" onClick={clearAllSections}>
                    Clear All
                  </Button>
                </div>
              </div>
              <div className="border rounded-md p-4 space-y-2 max-h-48 overflow-y-auto">
                {selectedSectionIds.length === 0 && (
                  <div className="text-sm text-muted-foreground italic">
                    All sections will be included by default
                  </div>
                )}
                {sections.map(section => (
                  <div key={section.id} className="flex items-center gap-2">
                    <Checkbox
                      id={`section-${section.id}`}
                      checked={selectedSectionIds.includes(section.id)}
                      onCheckedChange={() => toggleSection(section.id)}
                    />
                    <Label
                      htmlFor={`section-${section.id}`}
                      className="flex-1 cursor-pointer"
                    >
                      <div className="flex items-center gap-2">
                        <span>{section.title}</span>
                        <Badge variant="outline" className="text-xs">
                          {section.category}
                        </Badge>
                      </div>
                    </Label>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Export Options */}
          <div className="space-y-3">
            <Label>Export Options</Label>
            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <Checkbox
                  id="include-variance"
                  checked={includeVarianceExplanations}
                  onCheckedChange={(checked) => setIncludeVarianceExplanations(checked === true)}
                />
                <Label htmlFor="include-variance" className="cursor-pointer font-normal">
                  Include variance explanations
                </Label>
              </div>
              <div className="flex items-center gap-2">
                <Checkbox
                  id="include-evidence"
                  checked={includeEvidenceReferences}
                  onCheckedChange={(checked) => setIncludeEvidenceReferences(checked === true)}
                />
                <Label htmlFor="include-evidence" className="cursor-pointer font-normal">
                  Include evidence references
                </Label>
              </div>
              <div className="flex items-center gap-2">
                <Checkbox
                  id="include-narratives"
                  checked={includeNarrativeDiffs}
                  onCheckedChange={(checked) => setIncludeNarrativeDiffs(checked === true)}
                />
                <Label htmlFor="include-narratives" className="cursor-pointer font-normal">
                  Include narrative diffs summary
                </Label>
              </div>
            </div>
          </div>

          {/* Export Note */}
          <div className="space-y-2">
            <Label htmlFor="export-note">Export Note (Optional)</Label>
            <Textarea
              id="export-note"
              placeholder="Add a note about this export (e.g., 'Q4 2024 audit package')"
              value={exportNote}
              onChange={(e) => setExportNote(e.target.value)}
              rows={3}
            />
          </div>

          {/* Export History */}
          <div className="space-y-2">
            <Label>Recent Exports</Label>
            <div className="border rounded-md">
              {loadingHistory ? (
                <div className="p-4 text-center text-sm text-muted-foreground">
                  Loading export history...
                </div>
              ) : exportHistory.length === 0 ? (
                <div className="p-4 text-center text-sm text-muted-foreground">
                  No previous exports found
                </div>
              ) : (
                <div className="divide-y max-h-48 overflow-y-auto">
                  {exportHistory.slice(0, 5).map(record => (
                    <div key={record.id} className="p-3 hover:bg-muted/50 transition-colors">
                      <div className="flex items-start justify-between gap-2">
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2">
                            <FileText className="h-4 w-4 text-muted-foreground shrink-0" />
                            <span className="text-sm font-medium truncate">
                              {record.priorPeriodName} → {record.currentPeriodName}
                            </span>
                          </div>
                          <div className="text-xs text-muted-foreground mt-1">
                            {formatDate(record.exportedAt)} by {record.exportedByName}
                          </div>
                          <div className="flex items-center gap-3 mt-1 text-xs text-muted-foreground">
                            <span>{record.metricRowCount} metrics</span>
                            <span>•</span>
                            <span>{record.varianceExplanationCount} explanations</span>
                            <span>•</span>
                            <span>{formatBytes(record.packageSize)}</span>
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Error Display */}
          {exportError && (
            <div className="flex items-start gap-2 p-3 border border-destructive rounded-md bg-destructive/10">
              <AlertCircle className="h-4 w-4 text-destructive mt-0.5" />
              <div className="flex-1 text-sm text-destructive">{exportError}</div>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={isExporting}>
            Cancel
          </Button>
          <Button onClick={handleExport} disabled={isExporting || !priorPeriodId}>
            {isExporting ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Generating Export...
              </>
            ) : (
              <>
                <Download className="mr-2 h-4 w-4" />
                Export YoY Annex
              </>
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
