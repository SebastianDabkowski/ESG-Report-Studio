import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { TrendUp, TrendDown, Minus, ArrowLeft, WarningCircle } from '@phosphor-icons/react'
import { useState, useEffect } from 'react'
import { compareMetrics } from '@/lib/api'
import type { MetricComparisonResponse } from '@/lib/types'

interface MetricComparisonViewProps {
  dataPointId: string
  dataPointTitle: string
  onClose?: () => void
}

export default function MetricComparisonView({
  dataPointId,
  dataPointTitle,
  onClose
}: MetricComparisonViewProps) {
  const [comparison, setComparison] = useState<MetricComparisonResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedPeriodId, setSelectedPeriodId] = useState<string | undefined>(undefined)

  useEffect(() => {
    loadComparison(selectedPeriodId)
  }, [dataPointId, selectedPeriodId]) // Added dataPointId to dependency array

  async function loadComparison(priorPeriodId?: string) {
    try {
      setLoading(true)
      const data = await compareMetrics(dataPointId, priorPeriodId)
      setComparison(data)
      
      // If selectedPeriodId is not set and we have baselines, set it to the first one
      if (!selectedPeriodId && data.availableBaselines.length > 0) {
        setSelectedPeriodId(data.availableBaselines[0].periodId)
      }
      
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load comparison')
    } finally {
      setLoading(false)
    }
  }

  function formatValue(value?: string, numericValue?: number, unit?: string): string {
    if (value && unit) {
      return `${value} ${unit}`
    } else if (value) {
      return value
    } else if (numericValue !== undefined && numericValue !== null) {
      return unit ? `${numericValue} ${unit}` : `${numericValue}`
    }
    return 'N/A'
  }

  function formatPercentage(value?: number): string {
    if (value === undefined || value === null) return 'N/A'
    const sign = value > 0 ? '+' : ''
    return `${sign}${value.toFixed(2)}%`
  }

  function getTrendIcon(percentageChange?: number) {
    if (percentageChange === undefined || percentageChange === null) {
      return <Minus size={24} weight="bold" className="text-gray-400" />
    }
    
    // Use neutral colors - interpretation depends on metric type
    if (percentageChange > 0) {
      return <TrendUp size={24} weight="bold" className="text-blue-600" />
    } else if (percentageChange < 0) {
      return <TrendDown size={24} weight="bold" className="text-purple-600" />
    }
    
    return <Minus size={24} weight="bold" className="text-gray-400" />
  }

  function getTrendColor(percentageChange?: number): string {
    if (percentageChange === undefined || percentageChange === null) return 'text-gray-600'
    // Use neutral blue colors since increase/decrease interpretation depends on metric type
    // (e.g., emissions increase is bad, revenue increase is good)
    if (percentageChange > 0) return 'text-blue-600'
    if (percentageChange < 0) return 'text-purple-600'
    return 'text-gray-600'
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-2">
              <div>
                <CardTitle>Year-over-Year Comparison</CardTitle>
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
          {loading && (
            <div className="text-center py-8 text-gray-500">
              Loading comparison...
            </div>
          )}

          {error && (
            <div className="text-center py-8">
              <p className="text-red-600">{error}</p>
              <Button onClick={() => loadComparison(selectedPeriodId)} className="mt-4" variant="outline">
                Retry
              </Button>
            </div>
          )}

          {!loading && !error && comparison && (
            <div className="space-y-6">
              {/* Baseline Period Selector */}
              {comparison.availableBaselines.length > 0 && (
                <div className="space-y-2">
                  <label className="text-sm font-medium text-gray-700">
                    Compare Against
                  </label>
                  <Select
                    value={selectedPeriodId || comparison.availableBaselines[0].periodId}
                    onValueChange={(value) => setSelectedPeriodId(value)}
                  >
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Select baseline period" />
                    </SelectTrigger>
                    <SelectContent>
                      {comparison.availableBaselines.map((baseline) => (
                        <SelectItem key={baseline.periodId} value={baseline.periodId}>
                          {baseline.label} - {baseline.periodName}
                          {!baseline.hasData && ' (No data)'}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}

              {/* Comparison Not Available Warning */}
              {!comparison.isComparisonAvailable && (
                <Alert>
                  <WarningCircle size={20} weight="fill" className="text-yellow-600" />
                  <AlertDescription>
                    <strong>Comparison Unavailable:</strong> {comparison.unavailableReason}
                  </AlertDescription>
                </Alert>
              )}

              {/* Unit Compatibility Warning */}
              {!comparison.unitsCompatible && comparison.unitWarning && (
                <Alert>
                  <WarningCircle size={20} weight="fill" className="text-orange-600" />
                  <AlertDescription>
                    <strong>Unit Mismatch:</strong> {comparison.unitWarning}
                  </AlertDescription>
                </Alert>
              )}

              {/* Comparison Summary */}
              {comparison.isComparisonAvailable && (
                <Card className="bg-blue-50 border-blue-200">
                  <CardContent className="pt-6">
                    <div className="flex items-center justify-between">
                      <div className="text-center flex-1">
                        <div className="text-sm text-gray-600 mb-1">Change</div>
                        <div className="flex items-center justify-center gap-2">
                          {getTrendIcon(comparison.percentageChange)}
                          <span className={`text-3xl font-bold ${getTrendColor(comparison.percentageChange)}`}>
                            {formatPercentage(comparison.percentageChange)}
                          </span>
                        </div>
                        {comparison.absoluteChange !== undefined && comparison.absoluteChange !== null && (
                          <div className="text-sm text-gray-600 mt-1">
                            ({comparison.absoluteChange > 0 ? '+' : ''}{comparison.absoluteChange.toFixed(2)} {comparison.currentPeriod.unit || ''})
                          </div>
                        )}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Period Values */}
              <div className="grid grid-cols-2 gap-4">
                {/* Current Period */}
                <Card className="bg-blue-50">
                  <CardHeader className="pb-3">
                    <CardTitle className="text-sm">Current Period</CardTitle>
                    <CardDescription className="text-xs">
                      {comparison.currentPeriod.periodName}
                      <br />
                      {comparison.currentPeriod.startDate} to {comparison.currentPeriod.endDate}
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <div>
                      <div className="text-xs text-gray-600 mb-1">Value</div>
                      <div className="text-2xl font-bold text-blue-900">
                        {formatValue(comparison.currentPeriod.value, comparison.currentPeriod.numericValue, comparison.currentPeriod.unit)}
                      </div>
                    </div>
                    <div className="grid grid-cols-2 gap-2 text-xs">
                      <div>
                        <span className="text-gray-600">Type:</span>
                        <Badge variant="outline" className="ml-1 text-xs">
                          {comparison.currentPeriod.informationType}
                        </Badge>
                      </div>
                      <div>
                        <span className="text-gray-600">Evidence:</span>
                        <span className="ml-1 font-medium">{comparison.currentPeriod.evidenceCount}</span>
                      </div>
                    </div>
                    <div className="text-xs">
                      <span className="text-gray-600">Owner:</span>
                      <span className="ml-1">{comparison.currentPeriod.ownerName}</span>
                    </div>
                  </CardContent>
                </Card>

                {/* Prior Period */}
                <Card className={comparison.priorPeriod ? 'bg-gray-50' : 'bg-gray-100'}>
                  <CardHeader className="pb-3">
                    <CardTitle className="text-sm">Prior Period</CardTitle>
                    {comparison.priorPeriod && (
                      <CardDescription className="text-xs">
                        {comparison.priorPeriod.periodName}
                        <br />
                        {comparison.priorPeriod.startDate} to {comparison.priorPeriod.endDate}
                      </CardDescription>
                    )}
                  </CardHeader>
                  <CardContent className="space-y-3">
                    {comparison.priorPeriod ? (
                      <>
                        <div>
                          <div className="text-xs text-gray-600 mb-1">Value</div>
                          <div className="text-2xl font-bold text-gray-900">
                            {formatValue(comparison.priorPeriod.value, comparison.priorPeriod.numericValue, comparison.priorPeriod.unit)}
                          </div>
                        </div>
                        <div className="grid grid-cols-2 gap-2 text-xs">
                          <div>
                            <span className="text-gray-600">Type:</span>
                            <Badge variant="outline" className="ml-1 text-xs">
                              {comparison.priorPeriod.informationType}
                            </Badge>
                          </div>
                          <div>
                            <span className="text-gray-600">Evidence:</span>
                            <span className="ml-1 font-medium">{comparison.priorPeriod.evidenceCount}</span>
                          </div>
                        </div>
                        <div className="text-xs">
                          <span className="text-gray-600">Owner:</span>
                          <span className="ml-1">{comparison.priorPeriod.ownerName}</span>
                        </div>
                      </>
                    ) : (
                      <div className="text-center py-4 text-gray-500 text-sm">
                        No prior period data available
                      </div>
                    )}
                  </CardContent>
                </Card>
              </div>

              {/* Additional Context */}
              {comparison.availableBaselines.length === 0 && !comparison.priorPeriod && (
                <Alert>
                  <AlertDescription>
                    This is the first reporting period for this metric. No historical data is available for comparison.
                  </AlertDescription>
                </Alert>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
