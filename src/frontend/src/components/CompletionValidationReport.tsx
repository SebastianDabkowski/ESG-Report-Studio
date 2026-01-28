import { useState, useEffect } from 'react'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { 
  ChartBar,
  Warning,
  Info,
  CheckCircle,
  XCircle,
  Target,
  ShieldCheck
} from '@phosphor-icons/react'
import type { CompletenessValidationReport } from '@/lib/types'
import { getCompletenessValidationReport } from '@/lib/api'

interface CompletionValidationReportProps {
  periodId: string
}

export function CompletionValidationReport({ periodId }: CompletionValidationReportProps) {
  const [report, setReport] = useState<CompletenessValidationReport | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadReport = async () => {
      setLoading(true)
      setError(null)
      try {
        const data = await getCompletenessValidationReport(periodId)
        setReport(data)
      } catch (err) {
        console.error('Error loading validation report:', err)
        setError(err instanceof Error ? err.message : 'Failed to load validation report')
      } finally {
        setLoading(false)
      }
    }

    loadReport()
  }, [periodId])

  if (loading) {
    return <div className="text-center py-8 text-gray-500">Loading validation report...</div>
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <Warning className="h-4 w-4" />
        <AlertDescription>{error}</AlertDescription>
      </Alert>
    )
  }

  if (!report) {
    return null
  }

  const { summary, sections } = report

  return (
    <div className="space-y-6">
      {/* Summary Section */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ChartBar className="h-5 w-5" />
            Completeness Validation Summary
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="p-4 bg-blue-50 rounded-lg">
              <div className="text-2xl font-bold text-blue-900">{summary.totalSections}</div>
              <div className="text-sm text-blue-700">Total Sections</div>
            </div>
            <div className="p-4 bg-purple-50 rounded-lg">
              <div className="text-2xl font-bold text-purple-900">{summary.totalDataPoints}</div>
              <div className="text-sm text-purple-700">Total Data Points</div>
            </div>
            <div className="p-4 bg-green-50 rounded-lg">
              <div className="text-2xl font-bold text-green-900">{summary.completenessPercentage}%</div>
              <div className="text-sm text-green-700">Completeness</div>
            </div>
            <div className="p-4 bg-emerald-50 rounded-lg">
              <div className="text-2xl font-bold text-emerald-900">
                {summary.completenessWithExceptionsPercentage}%
              </div>
              <div className="text-sm text-emerald-700">With Exceptions</div>
            </div>
          </div>

          <div className="grid grid-cols-2 md:grid-cols-5 gap-4 mt-4">
            <div className="p-3 bg-red-50 rounded-lg border border-red-200">
              <div className="text-lg font-semibold text-red-900">{summary.missingCount}</div>
              <div className="text-xs text-red-700">Missing</div>
            </div>
            <div className="p-3 bg-yellow-50 rounded-lg border border-yellow-200">
              <div className="text-lg font-semibold text-yellow-900">{summary.estimatedCount}</div>
              <div className="text-xs text-yellow-700">Estimated</div>
            </div>
            <div className="p-3 bg-orange-50 rounded-lg border border-orange-200">
              <div className="text-lg font-semibold text-orange-900">{summary.simplifiedCount}</div>
              <div className="text-xs text-orange-700">Simplified</div>
            </div>
            <div className="p-3 bg-green-50 rounded-lg border border-green-200">
              <div className="text-lg font-semibold text-green-900">{summary.acceptedExceptionsCount}</div>
              <div className="text-xs text-green-700">Accepted Exceptions</div>
            </div>
            <div className="p-3 bg-gray-50 rounded-lg border border-gray-200">
              <div className="text-lg font-semibold text-gray-900">{summary.pendingExceptionsCount}</div>
              <div className="text-xs text-gray-700">Pending Exceptions</div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Sections Detail */}
      {sections.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>Sections Breakdown</CardTitle>
          </CardHeader>
          <CardContent>
            <Tabs defaultValue={sections[0].sectionId}>
              <TabsList className="w-full justify-start overflow-x-auto">
              {sections.map((section) => (
                <TabsTrigger key={section.sectionId} value={section.sectionId}>
                  {section.sectionTitle}
                  {(section.missingItems.length > 0 || 
                    section.estimatedItems.length > 0 || 
                    section.simplifiedItems.length > 0) && (
                    <Badge variant="outline" className="ml-2">
                      {section.missingItems.length + section.estimatedItems.length + section.simplifiedItems.length}
                    </Badge>
                  )}
                </TabsTrigger>
              ))}
            </TabsList>

            {sections.map((section) => (
              <TabsContent key={section.sectionId} value={section.sectionId} className="space-y-4">
                <div className="flex items-center gap-2 mb-4">
                  <Badge>{section.category}</Badge>
                  {section.acceptedExceptions.length > 0 && (
                    <Badge className="bg-green-100 text-green-800 border-green-200">
                      {section.acceptedExceptions.length} Accepted Exception(s)
                    </Badge>
                  )}
                </div>

                {/* Missing Items */}
                {section.missingItems.length > 0 && (
                  <div className="space-y-2">
                    <h4 className="font-semibold text-red-900 flex items-center gap-2">
                      <XCircle className="h-4 w-4" />
                      Missing Items ({section.missingItems.length})
                    </h4>
                    <div className="space-y-2">
                      {section.missingItems.map((item) => (
                        <div key={item.id} className="p-3 bg-red-50 border border-red-200 rounded-md">
                          <div className="font-medium text-sm">{item.title}</div>
                          {item.missingReason && (
                            <div className="text-xs text-gray-600 mt-1">
                              Reason: {item.missingReason}
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Estimated Items */}
                {section.estimatedItems.length > 0 && (
                  <div className="space-y-2">
                    <h4 className="font-semibold text-yellow-900 flex items-center gap-2">
                      <Target className="h-4 w-4" />
                      Estimated Items ({section.estimatedItems.length})
                    </h4>
                    <div className="space-y-2">
                      {section.estimatedItems.map((item) => (
                        <div key={item.id} className="p-3 bg-yellow-50 border border-yellow-200 rounded-md">
                          <div className="font-medium text-sm">{item.title}</div>
                          <div className="flex gap-2 mt-1">
                            {item.estimateType && (
                              <Badge variant="outline" className="text-xs">
                                {item.estimateType}
                              </Badge>
                            )}
                            {item.confidenceLevel && (
                              <Badge variant="outline" className="text-xs">
                                Confidence: {item.confidenceLevel}
                              </Badge>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Simplified Items */}
                {section.simplifiedItems.length > 0 && (
                  <div className="space-y-2">
                    <h4 className="font-semibold text-orange-900 flex items-center gap-2">
                      <Info className="h-4 w-4" />
                      Simplified Scope ({section.simplifiedItems.length})
                    </h4>
                    <div className="space-y-2">
                      {section.simplifiedItems.map((item) => (
                        <div key={item.id} className="p-3 bg-orange-50 border border-orange-200 rounded-md">
                          <div className="font-medium text-sm">{item.title}</div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Accepted Exceptions */}
                {section.acceptedExceptions.length > 0 && (
                  <div className="space-y-2">
                    <h4 className="font-semibold text-green-900 flex items-center gap-2">
                      <ShieldCheck className="h-4 w-4" />
                      Accepted Exceptions ({section.acceptedExceptions.length})
                    </h4>
                    <div className="space-y-2">
                      {section.acceptedExceptions.map((exception) => (
                        <div key={exception.id} className="p-3 bg-green-50 border border-green-200 rounded-md">
                          <div className="font-medium text-sm">{exception.title}</div>
                          <div className="text-xs text-gray-600 mt-1">{exception.justification}</div>
                          <div className="flex gap-2 mt-2">
                            <Badge variant="outline" className="text-xs">
                              {exception.exceptionType}
                            </Badge>
                            {exception.approvedAt && (
                              <span className="text-xs text-gray-500">
                                Approved: {new Date(exception.approvedAt).toLocaleDateString()}
                              </span>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {section.missingItems.length === 0 && 
                 section.estimatedItems.length === 0 && 
                 section.simplifiedItems.length === 0 && 
                 section.acceptedExceptions.length === 0 && (
                  <div className="text-center py-8 text-gray-500">
                    <CheckCircle className="h-12 w-12 mx-auto mb-2 opacity-30" />
                    <p>This section has no validation issues.</p>
                  </div>
                )}
              </TabsContent>
            ))}
          </Tabs>
        </CardContent>
      </Card>
      ) : (
        <Card>
          <CardContent className="py-12">
            <div className="text-center text-gray-500">
              <Info className="h-12 w-12 mx-auto mb-2 opacity-30" />
              <p>No sections found for this reporting period.</p>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
