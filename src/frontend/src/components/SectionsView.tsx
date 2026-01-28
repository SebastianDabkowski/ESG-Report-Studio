import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Progress } from '@/components/ui/progress'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { useKV } from '@github/spark/hooks'
import { Plus, CheckCircle, WarningCircle, Target, Article, Lightbulb, FileText, PaperclipHorizontal } from '@phosphor-icons/react'
import type { User, ReportingPeriod, SectionSummary, DataPoint, Gap, Classification, ContentType } from '@/lib/types'
import { getStatusColor, getStatusBorderColor, getClassificationColor, getCompletenessStatusColor, canApproveSection, canEditSection, generateId, calculateCompleteness } from '@/lib/helpers'

interface SectionsViewProps {
  currentUser: User
}

export default function SectionsView({ currentUser }: SectionsViewProps) {
  const [periods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  const [sections, setSections] = useKV<SectionSummary[]>('section-summaries', [])
  const [dataPoints, setDataPoints] = useKV<DataPoint[]>('data-points', [])
  const [gaps, setGaps] = useKV<Gap[]>('gaps', [])
  
  const [selectedSection, setSelectedSection] = useState<SectionSummary | null>(null)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [isAddDataOpen, setIsAddDataOpen] = useState(false)
  const [isAddGapOpen, setIsAddGapOpen] = useState(false)
  
  const [dataTitle, setDataTitle] = useState('')
  const [dataContent, setDataContent] = useState('')
  const [dataType, setDataType] = useState<ContentType>('narrative')
  const [dataClassification, setDataClassification] = useState<Classification>('fact')
  
  const [gapTitle, setGapTitle] = useState('')
  const [gapDescription, setGapDescription] = useState('')
  const [gapImpact, setGapImpact] = useState<'low' | 'medium' | 'high'>('medium')

  const activePeriod = periods?.find(p => p.status === 'active')
  const activeSections = sections?.filter(s => activePeriod && s.periodId === activePeriod.id) || []
  
  const sectionDataPoints = selectedSection 
    ? dataPoints?.filter(d => d.sectionId === selectedSection.id) || []
    : []
  
  const sectionGaps = selectedSection
    ? gaps?.filter(g => g.sectionId === selectedSection.id) || []
    : []

  const handleOpenDetail = (section: SectionSummary) => {
    setSelectedSection(section)
    setIsDetailOpen(true)
  }

  const handleAddData = () => {
    if (!selectedSection) return

    const newDataPoint: DataPoint = {
      id: generateId(),
      sectionId: selectedSection.id,
      type: dataType,
      classification: dataClassification,
      title: dataTitle,
      content: dataContent,
      ownerId: currentUser.id,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      evidenceIds: []
    }

    setDataPoints((current) => [...(current || []), newDataPoint])

    setSections((current) => {
      const updated = current || []
      return updated.map(s => {
        if (s.id === selectedSection.id) {
          const newCount = s.dataPointCount + 1
          const { level, percentage } = calculateCompleteness(newCount, s.evidenceCount, s.gapCount)
          return { ...s, dataPointCount: newCount, completeness: level, completenessPercentage: percentage }
        }
        return s
      })
    })

    setIsAddDataOpen(false)
    setDataTitle('')
    setDataContent('')
    setDataType('narrative')
    setDataClassification('fact')
  }

  const handleAddGap = () => {
    if (!selectedSection) return

    const newGap: Gap = {
      id: generateId(),
      sectionId: selectedSection.id,
      title: gapTitle,
      description: gapDescription,
      impact: gapImpact,
      createdBy: currentUser.id,
      createdAt: new Date().toISOString(),
      resolved: false
    }

    setGaps((current) => [...(current || []), newGap])

    setSections((current) => {
      const updated = current || []
      return updated.map(s => {
        if (s.id === selectedSection.id) {
          const newGapCount = s.gapCount + 1
          const { level, percentage } = calculateCompleteness(s.dataPointCount, s.evidenceCount, newGapCount)
          return { ...s, gapCount: newGapCount, completeness: level, completenessPercentage: percentage }
        }
        return s
      })
    })

    setIsAddGapOpen(false)
    setGapTitle('')
    setGapDescription('')
    setGapImpact('medium')
  }

  const handleApprove = (sectionId: string) => {
    setSections((current) => {
      const updated = current || []
      return updated.map(s => 
        s.id === sectionId 
          ? { ...s, status: 'approved' as const, approvedAt: new Date().toISOString(), approvedBy: currentUser.id }
          : s
      )
    })
  }

  const handleSubmitForReview = (sectionId: string) => {
    setSections((current) => {
      const updated = current || []
      return updated.map(s => 
        s.id === sectionId 
          ? { ...s, status: 'in-review' as const }
          : s
      )
    })
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold tracking-tight text-foreground">
          Report Sections
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          Manage section content, data points, and evidence
        </p>
      </div>

      {activePeriod ? (
        <div className="grid gap-4">
          {activeSections.map(section => (
            <Card 
              key={section.id} 
              className={`border-l-4 ${getStatusBorderColor(section.status)} cursor-pointer hover:shadow-md transition-shadow`}
              onClick={() => handleOpenDetail(section)}
            >
              <CardHeader>
                <div className="flex items-start justify-between gap-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <CardTitle className="text-lg">{section.title}</CardTitle>
                      <Badge className={getStatusColor(section.status)} variant="secondary">
                        {section.status}
                      </Badge>
                      <Badge variant="outline" className="capitalize">
                        {section.category}
                      </Badge>
                    </div>
                    <CardDescription>{section.description}</CardDescription>
                  </div>
                  
                  <div className="text-right flex-shrink-0">
                    <div className="text-sm font-semibold font-mono mb-1">
                      {section.completenessPercentage}%
                    </div>
                    <Progress value={section.completenessPercentage} className="h-2 w-24" />
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex items-center gap-4 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1">
                    <Target size={14} />
                    {section.dataPointCount} data points
                  </span>
                  <span className="flex items-center gap-1">
                    <PaperclipHorizontal size={14} />
                    {section.evidenceCount} evidence
                  </span>
                  <span className="flex items-center gap-1">
                    <WarningCircle size={14} />
                    {section.gapCount} gaps
                  </span>
                  <span className="font-medium">Owner: {section.ownerName}</span>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <FileText size={48} weight="duotone" className="text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No Active Period</h3>
            <p className="text-sm text-muted-foreground text-center max-w-md">
              Create a reporting period to view and manage sections.
            </p>
          </CardContent>
        </Card>
      )}

      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-3xl max-h-[80vh] overflow-y-auto">
          {selectedSection && (
            <>
              <DialogHeader>
                <DialogTitle className="flex items-center gap-2">
                  {selectedSection.title}
                  <Badge className={getStatusColor(selectedSection.status)}>
                    {selectedSection.status}
                  </Badge>
                </DialogTitle>
                <DialogDescription>{selectedSection.description}</DialogDescription>
              </DialogHeader>

              <div className="space-y-6 py-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium">Completeness</span>
                    <span className="text-sm font-mono font-semibold">{selectedSection.completenessPercentage}%</span>
                  </div>
                  <Progress value={selectedSection.completenessPercentage} className="h-3 w-48" />
                </div>

                {selectedSection.completenessPercentage < 70 && (
                  <Alert>
                    <WarningCircle size={16} />
                    <AlertDescription>
                      This section needs more data and evidence before it can be approved.
                    </AlertDescription>
                  </Alert>
                )}

                <div>
                  <div className="flex items-center justify-between mb-3">
                    <h4 className="font-semibold flex items-center gap-2">
                      <Target size={18} />
                      Data Points ({sectionDataPoints.length})
                    </h4>
                    {canEditSection(currentUser.role) && selectedSection.status !== 'approved' && (
                      <Button size="sm" onClick={() => setIsAddDataOpen(true)} className="gap-2">
                        <Plus size={14} weight="bold" />
                        Add Data
                      </Button>
                    )}
                  </div>
                  
                  {sectionDataPoints.length > 0 ? (
                    <div className="space-y-2">
                      {sectionDataPoints.map(dp => (
                        <div key={dp.id} className="border border-border rounded-lg p-3">
                          <div className="flex items-start justify-between gap-2 mb-2">
                            <h5 className="font-medium text-sm">{dp.title}</h5>
                            <div className="flex gap-1 flex-shrink-0">
                              <Badge variant="outline" className="text-xs capitalize">{dp.type}</Badge>
                              {dp.classification && (
                                <Badge className={`${getClassificationColor(dp.classification)} text-xs`}>
                                  {dp.classification}
                                </Badge>
                              )}
                              {dp.completenessStatus && (
                                <Badge className={`${getCompletenessStatusColor(dp.completenessStatus)} text-xs capitalize border`}>
                                  {dp.completenessStatus}
                                </Badge>
                              )}
                            </div>
                          </div>
                          <p className="text-xs text-muted-foreground">{dp.content}</p>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground">No data points added yet.</p>
                  )}
                </div>

                <div>
                  <div className="flex items-center justify-between mb-3">
                    <h4 className="font-semibold flex items-center gap-2">
                      <WarningCircle size={18} />
                      Gaps ({sectionGaps.filter(g => !g.resolved).length})
                    </h4>
                    {canEditSection(currentUser.role) && selectedSection.status !== 'approved' && (
                      <Button size="sm" variant="outline" onClick={() => setIsAddGapOpen(true)} className="gap-2">
                        <Plus size={14} weight="bold" />
                        Document Gap
                      </Button>
                    )}
                  </div>
                  
                  {sectionGaps.filter(g => !g.resolved).length > 0 ? (
                    <div className="space-y-2">
                      {sectionGaps.filter(g => !g.resolved).map(gap => (
                        <div key={gap.id} className="border border-border rounded-lg p-3">
                          <div className="flex items-start justify-between gap-2 mb-1">
                            <h5 className="font-medium text-sm">{gap.title}</h5>
                            <Badge 
                              variant={gap.impact === 'high' ? 'destructive' : gap.impact === 'medium' ? 'default' : 'secondary'}
                              className="text-xs"
                            >
                              {gap.impact} impact
                            </Badge>
                          </div>
                          <p className="text-xs text-muted-foreground">{gap.description}</p>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground">No unresolved gaps.</p>
                  )}
                </div>
              </div>

              <DialogFooter>
                {canEditSection(currentUser.role) && selectedSection.status === 'draft' && (
                  <Button onClick={() => handleSubmitForReview(selectedSection.id)}>
                    Submit for Review
                  </Button>
                )}
                {canApproveSection(currentUser.role) && selectedSection.status === 'in-review' && (
                  <Button onClick={() => handleApprove(selectedSection.id)}>
                    <CheckCircle size={16} weight="fill" className="mr-2" />
                    Approve Section
                  </Button>
                )}
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={isAddDataOpen} onOpenChange={setIsAddDataOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Data Point</DialogTitle>
            <DialogDescription>Add narrative, metric, or other content to this section.</DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="data-type">Type</Label>
              <Select value={dataType} onValueChange={(v) => setDataType(v as ContentType)}>
                <SelectTrigger id="data-type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="narrative">Narrative</SelectItem>
                  <SelectItem value="metric">Metric</SelectItem>
                  <SelectItem value="evidence">Evidence Reference</SelectItem>
                  <SelectItem value="assumption">Assumption</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="data-classification">Classification</Label>
              <Select value={dataClassification} onValueChange={(v) => setDataClassification(v as Classification)}>
                <SelectTrigger id="data-classification">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="fact">Fact (verified data)</SelectItem>
                  <SelectItem value="declaration">Declaration (policy or commitment)</SelectItem>
                  <SelectItem value="plan">Plan (future intention)</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="data-title">Title</Label>
              <Input
                id="data-title"
                placeholder="e.g., Total GHG Emissions"
                value={dataTitle}
                onChange={(e) => setDataTitle(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="data-content">Content</Label>
              <Textarea
                id="data-content"
                placeholder="Enter the data point content..."
                value={dataContent}
                onChange={(e) => setDataContent(e.target.value)}
                rows={4}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddDataOpen(false)}>Cancel</Button>
            <Button onClick={handleAddData} disabled={!dataTitle || !dataContent}>
              Add Data Point
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isAddGapOpen} onOpenChange={setIsAddGapOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Document Gap</DialogTitle>
            <DialogDescription>
              Explicitly record missing data or information limitations.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="gap-title">Gap Title</Label>
              <Input
                id="gap-title"
                placeholder="e.g., Missing Scope 3 emissions data"
                value={gapTitle}
                onChange={(e) => setGapTitle(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="gap-description">Description</Label>
              <Textarea
                id="gap-description"
                placeholder="Describe what's missing and why..."
                value={gapDescription}
                onChange={(e) => setGapDescription(e.target.value)}
                rows={3}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="gap-impact">Impact Level</Label>
              <Select value={gapImpact} onValueChange={(v) => setGapImpact(v as 'low' | 'medium' | 'high')}>
                <SelectTrigger id="gap-impact">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="low">Low - Minor omission</SelectItem>
                  <SelectItem value="medium">Medium - Notable gap</SelectItem>
                  <SelectItem value="high">High - Critical missing data</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddGapOpen(false)}>Cancel</Button>
            <Button onClick={handleAddGap} disabled={!gapTitle || !gapDescription}>
              Document Gap
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}