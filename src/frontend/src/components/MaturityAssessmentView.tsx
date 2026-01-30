import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getCurrentMaturityAssessment,
  getMaturityAssessmentHistory,
  calculateMaturityAssessment,
  getActiveMaturityModel,
  getPeriods
} from '@/lib/api'
import type { ReportingPeriod, MaturityAssessment, User } from '@/lib/types'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { CheckCircle2, XCircle, AlertCircle, TrendingUp, BarChart3, FileText } from 'lucide-react'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

interface MaturityAssessmentViewProps {
  currentUser: User
}

export function MaturityAssessmentView({ currentUser }: MaturityAssessmentViewProps) {
  const queryClient = useQueryClient()
  const [showHistory, setShowHistory] = useState(false)
  const [selectedPeriodId, setSelectedPeriodId] = useState<string | null>(null)

  // Query for periods
  const { data: periods } = useQuery({
    queryKey: ['periods'],
    queryFn: getPeriods
  })

  // Auto-select first period
  useEffect(() => {
    if (periods && periods.length > 0 && !selectedPeriodId) {
      setSelectedPeriodId(periods[0].id)
    }
  }, [periods, selectedPeriodId])

  const selectedPeriod = periods?.find(p => p.id === selectedPeriodId)

  // Query for current assessment
  const { data: currentAssessment, isLoading: isLoadingCurrent } = useQuery({
    queryKey: ['maturity-assessment', selectedPeriodId, 'current'],
    queryFn: () => getCurrentMaturityAssessment(selectedPeriodId!),
    enabled: !!selectedPeriodId,
    retry: false
  })

  // Query for assessment history
  const { data: assessmentHistory } = useQuery({
    queryKey: ['maturity-assessment', selectedPeriodId, 'history'],
    queryFn: () => getMaturityAssessmentHistory(selectedPeriodId!),
    enabled: showHistory && !!selectedPeriodId
  })

  // Query for active maturity model
  const { data: activeModel } = useQuery({
    queryKey: ['maturity-model', 'active'],
    queryFn: () => getActiveMaturityModel(),
    retry: false
  })

  // Mutation for calculating assessment
  const calculateMutation = useMutation({
    mutationFn: () =>
      calculateMaturityAssessment({
        periodId: selectedPeriodId!,
        calculatedBy: currentUser.id,
        calculatedByName: currentUser.name
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['maturity-assessment', selectedPeriodId] })
    }
  })

  if (!selectedPeriodId || !selectedPeriod) {
    return (
      <Alert>
        <AlertCircle className="h-4 w-4" />
        <AlertDescription>
          No reporting period found. Please create a reporting period first.
        </AlertDescription>
      </Alert>
    )
  }

  if (isLoadingCurrent) {
    return <div className="p-4">Loading assessment...</div>
  }

  if (!activeModel) {
    return (
      <Alert>
        <AlertCircle className="h-4 w-4" />
        <AlertDescription>
          No active maturity model found. Please create a maturity model first.
        </AlertDescription>
      </Alert>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header with Period Selector */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">Maturity Assessment</h2>
          <p className="text-muted-foreground">
            Track reporting maturity progress
          </p>
        </div>
        <div className="flex items-center gap-4">
          <Select value={selectedPeriodId} onValueChange={setSelectedPeriodId}>
            <SelectTrigger className="w-[250px]">
              <SelectValue placeholder="Select period" />
            </SelectTrigger>
            <SelectContent>
              {periods?.map(period => (
                <SelectItem key={period.id} value={period.id}>
                  {period.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button
            onClick={() => calculateMutation.mutate()}
            disabled={calculateMutation.isPending}
          >
            {calculateMutation.isPending ? 'Calculating...' : 'Calculate Maturity Score'}
          </Button>
        </div>
      </div>

      {/* Current Assessment */}
      {currentAssessment ? (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <BarChart3 className="h-5 w-5" />
              Current Maturity Score
            </CardTitle>
            <CardDescription>
              Calculated {new Date(currentAssessment.calculatedAt).toLocaleDateString()} by{' '}
              {currentAssessment.calculatedByName}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {/* Overall Score */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-sm font-medium">Overall Score</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="text-4xl font-bold">{currentAssessment.overallScore.toFixed(1)}%</div>
                  <p className="text-sm text-muted-foreground mt-1">
                    {currentAssessment.stats.passedCriteria} of {currentAssessment.stats.totalCriteria} criteria passed
                  </p>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-sm font-medium">Achieved Level</CardTitle>
                </CardHeader>
                <CardContent>
                  {currentAssessment.achievedLevelName ? (
                    <>
                      <div className="text-2xl font-bold">{currentAssessment.achievedLevelName}</div>
                      <p className="text-sm text-muted-foreground mt-1">
                        Level {currentAssessment.achievedLevelOrder}
                      </p>
                    </>
                  ) : (
                    <div className="text-sm text-muted-foreground">No level achieved yet</div>
                  )}
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-sm font-medium">Data Quality</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    <div className="flex items-center justify-between">
                      <span className="text-sm">Completeness</span>
                      <span className="font-medium">
                        {currentAssessment.stats.dataCompletenessPercentage.toFixed(1)}%
                      </span>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="text-sm">Evidence</span>
                      <span className="font-medium">
                        {currentAssessment.stats.evidenceQualityPercentage.toFixed(1)}%
                      </span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Criteria Results */}
            <div>
              <h3 className="text-lg font-semibold mb-4">Criteria Results</h3>
              <div className="space-y-4">
                {/* Group by level */}
                {Array.from(new Set(currentAssessment.criterionResults.map(r => r.levelOrder)))
                  .sort((a, b) => a - b)
                  .map(levelOrder => {
                    const levelResults = currentAssessment.criterionResults.filter(
                      r => r.levelOrder === levelOrder
                    )
                    const levelName = levelResults[0]?.levelName || `Level ${levelOrder}`
                    const allPassed = levelResults.every(r => r.passed)
                    const somePassed = levelResults.some(r => r.passed)

                    return (
                      <Card key={levelOrder}>
                        <CardHeader className="pb-3">
                          <div className="flex items-center justify-between">
                            <CardTitle className="text-base">
                              {levelName} (Level {levelOrder})
                            </CardTitle>
                            <Badge variant={allPassed ? 'default' : somePassed ? 'secondary' : 'outline'}>
                              {allPassed ? 'Achieved' : somePassed ? 'Partial' : 'Not Achieved'}
                            </Badge>
                          </div>
                        </CardHeader>
                        <CardContent>
                          <div className="space-y-3">
                            {levelResults.map(result => (
                              <div
                                key={result.criterionId}
                                className="flex items-start gap-3 p-3 rounded-lg border"
                              >
                                <div className="mt-0.5">
                                  {result.passed ? (
                                    <CheckCircle2 className="h-5 w-5 text-green-600" />
                                  ) : result.status === 'incomplete-data' ? (
                                    <AlertCircle className="h-5 w-5 text-yellow-600" />
                                  ) : (
                                    <XCircle className="h-5 w-5 text-red-600" />
                                  )}
                                </div>
                                <div className="flex-1">
                                  <div className="flex items-center justify-between mb-1">
                                    <h4 className="font-medium">{result.criterionName}</h4>
                                    <Badge variant={result.isMandatory ? 'default' : 'secondary'} className="text-xs">
                                      {result.isMandatory ? 'Mandatory' : 'Optional'}
                                    </Badge>
                                  </div>
                                  <div className="grid grid-cols-3 gap-2 text-sm mb-2">
                                    <div>
                                      <span className="text-muted-foreground">Type:</span>{' '}
                                      <span className="capitalize">{result.criterionType.replace('-', ' ')}</span>
                                    </div>
                                    <div>
                                      <span className="text-muted-foreground">Target:</span>{' '}
                                      <span className="font-medium">
                                        {result.targetValue} {result.unit}
                                      </span>
                                    </div>
                                    <div>
                                      <span className="text-muted-foreground">Actual:</span>{' '}
                                      <span className="font-medium">
                                        {result.actualValue} {result.unit}
                                      </span>
                                    </div>
                                  </div>
                                  {result.failureReason && (
                                    <Alert variant="destructive" className="mt-2">
                                      <AlertDescription className="text-sm">
                                        {result.failureReason}
                                      </AlertDescription>
                                    </Alert>
                                  )}
                                </div>
                              </div>
                            ))}
                          </div>
                        </CardContent>
                      </Card>
                    )
                  })}
              </div>
            </div>
          </CardContent>
        </Card>
      ) : (
        <Alert>
          <TrendingUp className="h-4 w-4" />
          <AlertDescription>
            No assessment calculated yet. Click "Calculate Maturity Score" to generate your first assessment.
          </AlertDescription>
        </Alert>
      )}

      {/* History Section */}
      <div>
        <Button
          variant="outline"
          onClick={() => setShowHistory(!showHistory)}
          className="mb-4"
        >
          <FileText className="h-4 w-4 mr-2" />
          {showHistory ? 'Hide' : 'Show'} Assessment History
        </Button>

        {showHistory && assessmentHistory && assessmentHistory.length > 0 && (
          <div className="space-y-3">
            {assessmentHistory.map(assessment => (
              <Card key={assessment.id}>
                <CardHeader className="py-3">
                  <div className="flex items-center justify-between">
                    <div>
                      <CardTitle className="text-sm">
                        {new Date(assessment.calculatedAt).toLocaleString()}
                      </CardTitle>
                      <CardDescription className="text-xs">
                        By {assessment.calculatedByName}
                      </CardDescription>
                    </div>
                    <div className="text-right">
                      <div className="text-lg font-bold">{assessment.overallScore.toFixed(1)}%</div>
                      {assessment.achievedLevelName && (
                        <div className="text-xs text-muted-foreground">
                          {assessment.achievedLevelName}
                        </div>
                      )}
                    </div>
                  </div>
                </CardHeader>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
