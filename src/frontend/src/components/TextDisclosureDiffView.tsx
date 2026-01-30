import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { ArrowLeft, GitCompare, Info, WarningCircle } from '@phosphor-icons/react'
import { useState, useEffect, useCallback } from 'react'
import { compareTextDisclosures } from '@/lib/api'
import type { TextDisclosureComparisonResponse, ReportingPeriod } from '@/lib/types'
import { formatDateTime } from '@/lib/helpers'

interface TextDisclosureDiffViewProps {
  dataPointId: string
  dataPointTitle: string
  availablePeriods?: ReportingPeriod[]
  onClose?: () => void
}

export default function TextDisclosureDiffView({
  dataPointId,
  dataPointTitle,
  availablePeriods = [],
  onClose
}: TextDisclosureDiffViewProps) {
  const [comparison, setComparison] = useState<TextDisclosureComparisonResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedPeriodId, setSelectedPeriodId] = useState<string | undefined>(undefined)
  const [granularity, setGranularity] = useState<'word' | 'sentence'>('word')

  const loadComparison = useCallback(async () => {
    try {
      setLoading(true)
      const data = await compareTextDisclosures(dataPointId, selectedPeriodId, granularity)
      setComparison(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load comparison')
    } finally {
      setLoading(false)
    }
  }, [dataPointId, selectedPeriodId, granularity])

  useEffect(() => {
    loadComparison()
  }, [loadComparison])

  function getChangeTypeColor(changeType: string): string {
    switch (changeType) {
      case 'added': return 'bg-green-100 text-green-800'
      case 'removed': return 'bg-red-100 text-red-800 line-through'
      case 'modified': return 'bg-blue-100 text-blue-800'
      case 'unchanged': return 'bg-gray-50 text-gray-800'
      default: return 'bg-gray-100 text-gray-800'
    }
  }

  function renderTextWithHighlights() {
    if (!comparison || !comparison.segments || comparison.segments.length === 0) {
      return <p className="text-gray-500">No content to display</p>
    }

    return (
      <div className="prose max-w-none">
        {comparison.segments.map((segment, index) => {
          if (segment.changeType === 'unchanged') {
            return <span key={index}>{segment.text}</span>
          }
          return (
            <span
              key={index}
              className={`${getChangeTypeColor(segment.changeType)} px-0.5 rounded`}
              title={segment.changeType}
            >
              {segment.text}
            </span>
          )
        })}
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-2">
              <GitCompare size={24} weight="duotone" />
              <div>
                <CardTitle>Year-over-Year Text Comparison</CardTitle>
                <CardDescription>{dataPointTitle}</CardDescription>
              </div>
            </div>
            {onClose && (
              <Button onClick={onClose} variant="outline" size="sm">
                <ArrowLeft className="mr-2" size={16} />
                Back
              </Button>
            )}
          </div>
        </CardHeader>

        <CardContent>
          {/* Controls */}
          <div className="flex gap-4 mb-6">
            {availablePeriods.length > 0 && (
              <div className="flex-1">
                <label className="text-sm font-medium mb-2 block">Compare with Period:</label>
                <Select
                  value={selectedPeriodId || 'auto'}
                  onValueChange={(value) => setSelectedPeriodId(value === 'auto' ? undefined : value)}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select period" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="auto">Auto (use rollover lineage)</SelectItem>
                    {availablePeriods.map((period) => (
                      <SelectItem key={period.id} value={period.id}>
                        {period.name} ({period.startDate} to {period.endDate})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="w-48">
              <label className="text-sm font-medium mb-2 block">Diff Level:</label>
              <Select
                value={granularity}
                onValueChange={(value) => setGranularity(value as 'word' | 'sentence')}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="word">Word-level</SelectItem>
                  <SelectItem value="sentence">Sentence-level</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {loading && (
            <div className="text-center py-8 text-gray-500">
              Loading comparison...
            </div>
          )}

          {error && (
            <Alert variant="destructive">
              <WarningCircle className="h-4 w-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {!loading && !error && comparison && (
            <>
              {/* Draft Copy Alert */}
              {comparison.isDraftCopy && !comparison.hasBeenEdited && (
                <Alert className="mb-4">
                  <Info className="h-4 w-4" />
                  <AlertDescription>
                    This disclosure was copied from a previous period and has not yet been edited. 
                    No changes are shown.
                  </AlertDescription>
                </Alert>
              )}

              {/* Period Information */}
              <div className="grid grid-cols-2 gap-4 mb-6">
                <Card className="bg-gray-50">
                  <CardHeader className="pb-3">
                    <CardTitle className="text-sm">Current Period</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-1 text-sm">
                    <div>
                      <span className="font-medium">Period:</span> {comparison.currentDataPoint.periodName}
                    </div>
                    <div>
                      <span className="font-medium">Status:</span>{' '}
                      <Badge variant="outline">{comparison.currentDataPoint.reviewStatus}</Badge>
                    </div>
                    <div>
                      <span className="font-medium">Updated:</span>{' '}
                      {formatDateTime(comparison.currentDataPoint.updatedAt)}
                    </div>
                  </CardContent>
                </Card>

                {comparison.previousDataPoint ? (
                  <Card className="bg-gray-50">
                    <CardHeader className="pb-3">
                      <CardTitle className="text-sm">Previous Period</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-1 text-sm">
                      <div>
                        <span className="font-medium">Period:</span> {comparison.previousDataPoint.periodName}
                      </div>
                      <div>
                        <span className="font-medium">Status:</span>{' '}
                        <Badge variant="outline">{comparison.previousDataPoint.reviewStatus}</Badge>
                      </div>
                      <div>
                        <span className="font-medium">Updated:</span>{' '}
                        {formatDateTime(comparison.previousDataPoint.updatedAt)}
                      </div>
                    </CardContent>
                  </Card>
                ) : (
                  <Card className="bg-gray-50">
                    <CardHeader className="pb-3">
                      <CardTitle className="text-sm">Previous Period</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <p className="text-sm text-gray-500">No previous version found</p>
                    </CardContent>
                  </Card>
                )}
              </div>

              {/* Summary Statistics */}
              <Card className="mb-6">
                <CardHeader className="pb-3">
                  <CardTitle className="text-sm">Change Summary</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-4 gap-4 text-center">
                    <div>
                      <div className="text-2xl font-bold text-green-600">{comparison.summary.addedSegments}</div>
                      <div className="text-xs text-gray-600">Added</div>
                    </div>
                    <div>
                      <div className="text-2xl font-bold text-red-600">{comparison.summary.removedSegments}</div>
                      <div className="text-xs text-gray-600">Removed</div>
                    </div>
                    <div>
                      <div className="text-2xl font-bold text-blue-600">{comparison.summary.modifiedSegments}</div>
                      <div className="text-xs text-gray-600">Modified</div>
                    </div>
                    <div>
                      <div className="text-2xl font-bold text-gray-600">{comparison.summary.unchangedSegments}</div>
                      <div className="text-xs text-gray-600">Unchanged</div>
                    </div>
                  </div>
                  {comparison.summary.hasChanges ? (
                    <p className="text-sm text-gray-600 mt-4">
                      Changed from {comparison.summary.oldTextLength} to {comparison.summary.newTextLength} characters
                    </p>
                  ) : (
                    <p className="text-sm text-gray-600 mt-4">No changes detected</p>
                  )}
                </CardContent>
              </Card>

              {/* Diff View */}
              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-sm">Text Comparison</CardTitle>
                  <div className="flex gap-2 mt-2">
                    <Badge className="bg-green-100 text-green-800">Added</Badge>
                    <Badge className="bg-red-100 text-red-800">Removed</Badge>
                    <Badge className="bg-blue-100 text-blue-800">Modified</Badge>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="p-4 bg-white border rounded-lg">
                    {renderTextWithHighlights()}
                  </div>
                </CardContent>
              </Card>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
