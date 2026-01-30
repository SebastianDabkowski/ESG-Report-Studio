import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useKV } from '@github/spark/hooks'
import {
  TrendUp,
  TrendDown,
  Equals,
  WarningCircle,
  CheckCircle,
  Info,
  ArrowLeft
} from '@phosphor-icons/react'
import type {
  ReportingPeriod,
  CompletenessComparison,
  CompletenessBreakdownComparison
} from '@/lib/types'
import { getCompletenessComparison } from '@/lib/api'

interface CompletenessComparisonViewProps {
  onBack?: () => void
}

export default function CompletenessComparisonView({ onBack }: CompletenessComparisonViewProps) {
  const [periods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  
  const [currentPeriodId, setCurrentPeriodId] = useState<string>('')
  const [priorPeriodId, setPriorPeriodId] = useState<string>('')
  const [comparison, setComparison] = useState<CompletenessComparison | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Auto-select the two most recent periods
  useEffect(() => {
    if (periods.length >= 2) {
      const sorted = [...periods].sort((a, b) => 
        new Date(b.startDate).getTime() - new Date(a.startDate).getTime()
      )
      setCurrentPeriodId(sorted[0].id)
      setPriorPeriodId(sorted[1].id)
    }
  }, [periods])

  // Fetch comparison when both periods are selected
  useEffect(() => {
    if (!currentPeriodId || !priorPeriodId) return

    const fetchComparison = async () => {
      setIsLoading(true)
      setError(null)
      try {
        const data = await getCompletenessComparison({
          currentPeriodId,
          priorPeriodId
        })
        setComparison(data)
      } catch (err) {
        console.error('Failed to fetch completeness comparison:', err)
        setError('Failed to load completeness comparison. Please try again.')
      } finally {
        setIsLoading(false)
      }
    }

    fetchComparison()
  }, [currentPeriodId, priorPeriodId])

  const renderChangeIndicator = (change: number | null, isRegression: boolean) => {
    if (change === null || change === 0) {
      return (
        <Badge variant="outline" className="gap-1">
          <Equals className="h-3 w-3" />
          No Change
        </Badge>
      )
    }

    if (isRegression) {
      return (
        <Badge variant="destructive" className="gap-1">
          <TrendDown className="h-3 w-3" />
          {change.toFixed(1)}%
        </Badge>
      )
    }

    return (
      <Badge variant="success" className="gap-1">
        <TrendUp className="h-3 w-3" />
        +{change.toFixed(1)}%
      </Badge>
    )
  }

  const renderBreakdownCard = (item: CompletenessBreakdownComparison, showOwner: boolean = false) => (
    <Card key={item.id} className="mb-3">
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <CardTitle className="text-sm font-medium">{item.name}</CardTitle>
            {showOwner && item.ownerName && (
              <p className="text-xs text-muted-foreground mt-1">Owner: {item.ownerName}</p>
            )}
          </div>
          <div className="flex items-center gap-2">
            {!item.existsInBothPeriods ? (
              <Badge variant="outline" className="gap-1">
                <Info className="h-3 w-3" />
                {item.notApplicableReason}
              </Badge>
            ) : (
              renderChangeIndicator(item.percentagePointChange, item.isRegression)
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent className="pt-0">
        {item.existsInBothPeriods && item.priorPeriod && (
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <p className="text-muted-foreground mb-1">{comparison?.priorPeriod.name}</p>
              <div className="space-y-0.5">
                <p className="font-medium">{item.priorPeriod.completePercentage.toFixed(1)}% complete</p>
                <p className="text-xs text-muted-foreground">
                  {item.priorPeriod.completeCount} of {item.priorPeriod.totalCount} items
                </p>
              </div>
            </div>
            <div>
              <p className="text-muted-foreground mb-1">{comparison?.currentPeriod.name}</p>
              <div className="space-y-0.5">
                <p className="font-medium">{item.currentPeriod.completePercentage.toFixed(1)}% complete</p>
                <p className="text-xs text-muted-foreground">
                  {item.currentPeriod.completeCount} of {item.currentPeriod.totalCount} items
                </p>
              </div>
            </div>
          </div>
        )}
        {!item.existsInBothPeriods && (
          <p className="text-sm text-muted-foreground">
            This section is not comparable between periods due to structural changes.
          </p>
        )}
      </CardContent>
    </Card>
  )

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            {onBack && (
              <Button variant="ghost" size="sm" onClick={onBack} aria-label="Go back">
                <ArrowLeft className="h-4 w-4" />
              </Button>
            )}
            <h1 className="text-2xl font-bold">Completeness Comparison</h1>
          </div>
          <p className="text-muted-foreground mt-1">
            Compare data completeness between reporting periods
          </p>
        </div>
      </div>

      {/* Period Selection */}
      <Card>
        <CardHeader>
          <CardTitle>Select Periods to Compare</CardTitle>
          <CardDescription>
            Choose the current and prior reporting periods for comparison
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label htmlFor="current-period-select" className="text-sm font-medium mb-2 block">Current Period</label>
              <Select value={currentPeriodId} onValueChange={setCurrentPeriodId}>
                <SelectTrigger id="current-period-select">
                  <SelectValue placeholder="Select current period" />
                </SelectTrigger>
                <SelectContent>
                  {periods.map(period => (
                    <SelectItem key={period.id} value={period.id}>
                      {period.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <label htmlFor="prior-period-select" className="text-sm font-medium mb-2 block">Prior Period</label>
              <Select value={priorPeriodId} onValueChange={setPriorPeriodId}>
                <SelectTrigger id="prior-period-select">
                  <SelectValue placeholder="Select prior period" />
                </SelectTrigger>
                <SelectContent>
                  {periods.map(period => (
                    <SelectItem 
                      key={period.id} 
                      value={period.id}
                      disabled={period.id === currentPeriodId}
                    >
                      {period.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Loading and Error States */}
      {isLoading && (
        <Card>
          <CardContent className="py-8 text-center text-muted-foreground">
            Loading comparison...
          </CardContent>
        </Card>
      )}

      {error && (
        <Card className="border-destructive">
          <CardContent className="py-8 text-center">
            <WarningCircle className="h-12 w-12 text-destructive mx-auto mb-2" />
            <p className="text-destructive">{error}</p>
          </CardContent>
        </Card>
      )}

      {/* Comparison Results */}
      {comparison && !isLoading && !error && (
        <>
          {/* Summary */}
          <Card>
            <CardHeader>
              <CardTitle>Summary</CardTitle>
              <CardDescription>
                Comparison between {comparison.priorPeriod.name} and {comparison.currentPeriod.name}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-5 gap-4">
                <div className="text-center">
                  <div className="flex items-center justify-center gap-1 text-destructive mb-1">
                    <TrendDown className="h-4 w-4" />
                    <span className="text-2xl font-bold">{comparison.summary.regressionCount}</span>
                  </div>
                  <p className="text-xs text-muted-foreground">Regressions</p>
                </div>
                <div className="text-center">
                  <div className="flex items-center justify-center gap-1 text-green-600 mb-1">
                    <TrendUp className="h-4 w-4" />
                    <span className="text-2xl font-bold">{comparison.summary.improvementCount}</span>
                  </div>
                  <p className="text-xs text-muted-foreground">Improvements</p>
                </div>
                <div className="text-center">
                  <div className="flex items-center justify-center gap-1 mb-1">
                    <Equals className="h-4 w-4" />
                    <span className="text-2xl font-bold">{comparison.summary.unchangedCount}</span>
                  </div>
                  <p className="text-xs text-muted-foreground">Unchanged</p>
                </div>
                <div className="text-center">
                  <div className="flex items-center justify-center gap-1 text-blue-600 mb-1">
                    <CheckCircle className="h-4 w-4" />
                    <span className="text-2xl font-bold">{comparison.summary.addedSectionCount}</span>
                  </div>
                  <p className="text-xs text-muted-foreground">Added</p>
                </div>
                <div className="text-center">
                  <div className="flex items-center justify-center gap-1 text-gray-500 mb-1">
                    <Info className="h-4 w-4" />
                    <span className="text-2xl font-bold">{comparison.summary.removedSectionCount}</span>
                  </div>
                  <p className="text-xs text-muted-foreground">Removed</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Overall Comparison */}
          <Card>
            <CardHeader>
              <CardTitle>Overall Completeness</CardTitle>
            </CardHeader>
            <CardContent>
              {renderBreakdownCard(comparison.overall)}
            </CardContent>
          </Card>

          {/* Category Comparison */}
          <Card>
            <CardHeader>
              <CardTitle>By Category (E/S/G)</CardTitle>
            </CardHeader>
            <CardContent>
              {comparison.byCategory.map(cat => renderBreakdownCard(cat))}
            </CardContent>
          </Card>

          {/* Regressions */}
          {comparison.regressions.length > 0 && (
            <Card className="border-destructive">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <WarningCircle className="h-5 w-5 text-destructive" />
                  Regressions ({comparison.regressions.length})
                </CardTitle>
                <CardDescription>
                  Sections with decreased completeness that require attention
                </CardDescription>
              </CardHeader>
              <CardContent>
                {comparison.regressions.map(reg => renderBreakdownCard(reg, true))}
              </CardContent>
            </Card>
          )}

          {/* Improvements */}
          {comparison.improvements.length > 0 && (
            <Card className="border-green-200">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <CheckCircle className="h-5 w-5 text-green-600" />
                  Improvements ({comparison.improvements.length})
                </CardTitle>
                <CardDescription>
                  Sections with increased completeness
                </CardDescription>
              </CardHeader>
              <CardContent>
                {comparison.improvements.map(imp => renderBreakdownCard(imp, true))}
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  )
}
