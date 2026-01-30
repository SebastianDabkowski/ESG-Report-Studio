import { useState, useEffect } from 'react'
import { GeneratedReport, ReportingPeriod, User } from '@/lib/types'
import { previewReport } from '@/lib/api'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { Loader2, FileText, Eye, AlertCircle, ChevronRight, CheckCircle, XCircle } from 'lucide-react'
import { formatDate } from '@/lib/helpers'

interface ReportPreviewDialogProps {
  period: ReportingPeriod
  currentUser: User
  onClose: () => void
}

export default function ReportPreviewDialog({
  period,
  currentUser,
  onClose
}: ReportPreviewDialogProps) {
  const [report, setReport] = useState<GeneratedReport | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedSectionId, setSelectedSectionId] = useState<string | null>(null)

  useEffect(() => {
    const loadPreview = async () => {
      setIsLoading(true)
      setError(null)
      
      try {
        const previewData = await previewReport(period.id, { userId: currentUser.id })
        setReport(previewData)
        
        // Auto-select first section if available
        if (previewData.sections.length > 0) {
          setSelectedSectionId(previewData.sections[0].section.id)
        }
      } catch (err) {
        console.error('Failed to load report preview:', err)
        setError(err instanceof Error ? err.message : 'Failed to load report preview')
      } finally {
        setIsLoading(false)
      }
    }

    loadPreview()
  }, [period.id, currentUser.id])

  const selectedSection = report?.sections.find(s => s.section.id === selectedSectionId)

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent className="max-w-7xl max-h-[90vh] p-0">
        <DialogHeader className="p-6 pb-4">
          <DialogTitle className="flex items-center gap-2">
            <Eye className="h-5 w-5" />
            Report Preview
          </DialogTitle>
          <DialogDescription>
            Preview report for {period.name} • {formatDate(period.startDate)} - {formatDate(period.endDate)}
          </DialogDescription>
        </DialogHeader>

        {isLoading && (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        )}

        {error && (
          <div className="p-6">
            <div className="flex items-center gap-2 text-destructive">
              <AlertCircle className="h-5 w-5" />
              <span>{error}</span>
            </div>
          </div>
        )}

        {!isLoading && !error && report && (
          <div className="flex h-[calc(90vh-140px)]">
            {/* Table of Contents Sidebar */}
            <div className="w-80 border-r">
              <ScrollArea className="h-full">
                <div className="p-4">
                  <h3 className="font-semibold text-sm mb-3">Table of Contents</h3>
                  <div className="space-y-1">
                    {report.sections.map((section, index) => (
                      <Button
                        key={section.section.id}
                        variant={selectedSectionId === section.section.id ? 'secondary' : 'ghost'}
                        className="w-full justify-start text-left h-auto py-2"
                        onClick={() => setSelectedSectionId(section.section.id)}
                      >
                        <div className="flex items-start gap-2 w-full">
                          <span className="text-xs text-muted-foreground mt-0.5">{index + 1}.</span>
                          <div className="flex-1 min-w-0">
                            <div className="text-sm font-medium truncate">
                              {section.section.title}
                            </div>
                            <div className="flex items-center gap-2 mt-1">
                              <Badge 
                                variant="outline" 
                                className="text-xs"
                              >
                                {section.section.category}
                              </Badge>
                              <span className="text-xs text-muted-foreground">
                                {section.dataPoints.length} data points
                              </span>
                            </div>
                          </div>
                          <ChevronRight className="h-4 w-4 flex-shrink-0 opacity-50" />
                        </div>
                      </Button>
                    ))}
                  </div>
                  
                  {report.sections.length === 0 && (
                    <div className="text-center py-8 text-muted-foreground">
                      <FileText className="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p className="text-sm">No sections available</p>
                    </div>
                  )}
                </div>
              </ScrollArea>
            </div>

            {/* Content Area */}
            <div className="flex-1">
              <ScrollArea className="h-full">
                <div className="p-6">
                  {selectedSection ? (
                    <div className="space-y-6">
                      {/* Section Header */}
                      <div>
                        <div className="flex items-center gap-2 mb-2">
                          <Badge variant="outline">{selectedSection.section.category}</Badge>
                          <Badge 
                            variant={selectedSection.section.status === 'approved' ? 'default' : 'secondary'}
                          >
                            {selectedSection.section.status}
                          </Badge>
                        </div>
                        <h2 className="text-2xl font-bold">{selectedSection.section.title}</h2>
                        {selectedSection.section.description && (
                          <p className="text-muted-foreground mt-2">
                            {selectedSection.section.description}
                          </p>
                        )}
                        {selectedSection.owner && (
                          <div className="mt-3 text-sm">
                            <span className="text-muted-foreground">Owner: </span>
                            <span className="font-medium">{selectedSection.owner.name}</span>
                          </div>
                        )}
                      </div>

                      <Separator />

                      {/* Data Points */}
                      {selectedSection.dataPoints.length > 0 && (
                        <div>
                          <h3 className="text-lg font-semibold mb-4">Data Points</h3>
                          <div className="space-y-4">
                            {selectedSection.dataPoints.map((dataPoint) => (
                              <Card key={dataPoint.id}>
                                <CardHeader className="pb-3">
                                  <CardTitle className="text-base flex items-center justify-between">
                                    <span>{dataPoint.title}</span>
                                    <Badge variant="outline" className="text-xs">
                                      {dataPoint.informationType}
                                    </Badge>
                                  </CardTitle>
                                </CardHeader>
                                <CardContent>
                                  <div className="space-y-2">
                                    <div className="flex items-baseline gap-2">
                                      <span className="text-lg font-semibold">
                                        {dataPoint.value || '—'}
                                      </span>
                                      {dataPoint.unit && (
                                        <span className="text-sm text-muted-foreground">
                                          {dataPoint.unit}
                                        </span>
                                      )}
                                    </div>
                                    
                                    <div className="flex items-center gap-4 text-sm text-muted-foreground">
                                      <div className="flex items-center gap-1">
                                        {dataPoint.status === 'complete' ? (
                                          <CheckCircle className="h-3.5 w-3.5 text-green-600" />
                                        ) : (
                                          <XCircle className="h-3.5 w-3.5 text-orange-600" />
                                        )}
                                        <span>{dataPoint.status}</span>
                                      </div>
                                      
                                      {dataPoint.evidenceCount > 0 && (
                                        <span>{dataPoint.evidenceCount} evidence file(s)</span>
                                      )}
                                      
                                      {dataPoint.hasAssumptions && (
                                        <span className="text-orange-600">Has assumptions</span>
                                      )}
                                    </div>
                                    
                                    <div className="text-xs text-muted-foreground">
                                      Owner: {dataPoint.ownerName}
                                      {dataPoint.lastUpdatedAt && (
                                        <> • Updated {formatDate(dataPoint.lastUpdatedAt)}</>
                                      )}
                                    </div>
                                  </div>
                                </CardContent>
                              </Card>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* Assumptions */}
                      {selectedSection.assumptions.length > 0 && (
                        <div>
                          <h3 className="text-lg font-semibold mb-4">Assumptions</h3>
                          <div className="space-y-3">
                            {selectedSection.assumptions.map((assumption) => (
                              <Card key={assumption.id} className="border-orange-200 bg-orange-50">
                                <CardContent className="pt-4">
                                  <div className="space-y-2">
                                    <p className="text-sm font-medium">{assumption.description}</p>
                                    {assumption.justification && (
                                      <p className="text-sm text-muted-foreground">
                                        {assumption.justification}
                                      </p>
                                    )}
                                    <div className="text-xs text-muted-foreground">
                                      Created {formatDate(assumption.createdAt)}
                                    </div>
                                  </div>
                                </CardContent>
                              </Card>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* Gaps */}
                      {selectedSection.gaps.length > 0 && (
                        <div>
                          <h3 className="text-lg font-semibold mb-4">Data Gaps</h3>
                          <div className="space-y-3">
                            {selectedSection.gaps.map((gap) => (
                              <Card key={gap.id} className="border-red-200 bg-red-50">
                                <CardContent className="pt-4">
                                  <div className="space-y-2">
                                    <p className="text-sm font-medium">{gap.description}</p>
                                    {gap.missingReason && (
                                      <p className="text-sm text-muted-foreground">
                                        Reason: {gap.missingReason}
                                      </p>
                                    )}
                                    <div className="text-xs text-muted-foreground">
                                      Created {formatDate(gap.createdAt)}
                                    </div>
                                  </div>
                                </CardContent>
                              </Card>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* Empty State */}
                      {selectedSection.dataPoints.length === 0 && 
                       selectedSection.assumptions.length === 0 && 
                       selectedSection.gaps.length === 0 && (
                        <div className="text-center py-12 text-muted-foreground">
                          <FileText className="h-12 w-12 mx-auto mb-3 opacity-50" />
                          <p>No data available for this section</p>
                        </div>
                      )}

                      {/* Pagination Indicator */}
                      <div className="mt-8 pt-6 border-t">
                        <div className="flex justify-between items-center text-sm text-muted-foreground">
                          <span>
                            Section {report.sections.findIndex(s => s.section.id === selectedSectionId) + 1} of {report.sections.length}
                          </span>
                          <span className="text-xs">
                            Preview generated at {formatDate(report.generatedAt)}
                          </span>
                        </div>
                      </div>
                    </div>
                  ) : (
                    <div className="text-center py-12 text-muted-foreground">
                      <FileText className="h-12 w-12 mx-auto mb-3 opacity-50" />
                      <p>Select a section from the table of contents to preview</p>
                    </div>
                  )}
                </div>
              </ScrollArea>
            </div>
          </div>
        )}

        <div className="p-6 pt-4 border-t">
          <div className="flex justify-between items-center">
            <div className="text-sm text-muted-foreground">
              {report && (
                <>
                  {report.sections.length} section(s) • 
                  {' '}{report.sections.reduce((sum, s) => sum + s.dataPoints.length, 0)} data point(s)
                </>
              )}
            </div>
            <Button onClick={onClose}>Close Preview</Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
