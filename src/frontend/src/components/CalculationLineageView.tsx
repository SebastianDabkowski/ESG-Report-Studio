import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, Calculator, RefreshCw, TrendingUp, CheckCircle, Info } from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card'
import { Badge } from './ui/badge'
import { Button } from './ui/button'
import { Alert, AlertDescription } from './ui/alert'
import type { CalculationLineageResponse, RecalculateDataPointRequest } from '../lib/types'

interface CalculationLineageViewProps {
  dataPointId: string
}

export function CalculationLineageView({ dataPointId }: CalculationLineageViewProps) {
  const queryClient = useQueryClient()

  // Fetch calculation lineage
  const { data: lineage, isLoading, error } = useQuery<CalculationLineageResponse>({
    queryKey: ['calculation-lineage', dataPointId],
    queryFn: async () => {
      const response = await fetch(`/api/data-points/${dataPointId}/lineage`)
      if (!response.ok) {
        if (response.status === 404) {
          throw new Error('This data point is not a calculated value')
        }
        throw new Error('Failed to fetch calculation lineage')
      }
      return response.json()
    },
  })

  // Mutation for recalculating
  const recalculateMutation = useMutation({
    mutationFn: async () => {
      const request: RecalculateDataPointRequest = {
        calculatedBy: 'user-1', // TODO: Get from auth context
        changeNote: 'Manual recalculation triggered from UI',
      }
      const response = await fetch(`/api/data-points/${dataPointId}/recalculate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      })
      if (!response.ok) {
        throw new Error('Failed to recalculate data point')
      }
      return response.json()
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['calculation-lineage', dataPointId] })
      queryClient.invalidateQueries({ queryKey: ['data-point', dataPointId] })
    },
  })

  if (isLoading) {
    return (
      <Card>
        <CardContent className="pt-6">
          <div className="text-center text-muted-foreground">Loading lineage...</div>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card>
        <CardContent className="pt-6">
          <Alert>
            <Info className="h-4 w-4" />
            <AlertDescription>
              {error instanceof Error ? error.message : 'Unable to load calculation lineage'}
            </AlertDescription>
          </Alert>
        </CardContent>
      </Card>
    )
  }

  if (!lineage) {
    return null
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Calculator className="h-5 w-5" />
              Calculation Lineage
            </CardTitle>
            <CardDescription>
              Track the inputs and formula used to calculate this value
            </CardDescription>
          </div>
          <div className="flex items-center gap-2">
            <Badge variant="outline">Version {lineage.version}</Badge>
            {lineage.needsRecalculation && (
              <Badge variant="destructive" className="flex items-center gap-1">
                <AlertTriangle className="h-3 w-3" />
                Needs Recalculation
              </Badge>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Recalculation Warning */}
        {lineage.needsRecalculation && lineage.recalculationReason && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>
              <div className="font-medium">This calculation is out of date</div>
              <div className="text-sm mt-1">{lineage.recalculationReason}</div>
              <Button
                size="sm"
                className="mt-2"
                onClick={() => recalculateMutation.mutate()}
                disabled={recalculateMutation.isPending}
              >
                <RefreshCw className={`h-4 w-4 mr-2 ${recalculateMutation.isPending ? 'animate-spin' : ''}`} />
                {recalculateMutation.isPending ? 'Recalculating...' : 'Recalculate Now'}
              </Button>
            </AlertDescription>
          </Alert>
        )}

        {/* Formula */}
        {lineage.formula && (
          <div>
            <h4 className="text-sm font-medium mb-2">Formula</h4>
            <div className="bg-muted p-3 rounded-md font-mono text-sm">
              {lineage.formula}
            </div>
          </div>
        )}

        {/* Calculation Metadata */}
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <span className="text-muted-foreground">Calculated At:</span>
            <div className="font-medium">
              {lineage.calculatedAt ? new Date(lineage.calculatedAt).toLocaleString() : 'N/A'}
            </div>
          </div>
          <div>
            <span className="text-muted-foreground">Calculated By:</span>
            <div className="font-medium">{lineage.calculatedBy || 'System'}</div>
          </div>
        </div>

        {/* Input Data Points */}
        <div>
          <h4 className="text-sm font-medium mb-3 flex items-center gap-2">
            <TrendingUp className="h-4 w-4" />
            Input Data Points ({lineage.inputs.length})
          </h4>
          <div className="space-y-2">
            {lineage.inputs.map((input) => (
              <div
                key={input.dataPointId}
                className={`border rounded-lg p-3 ${
                  input.hasChanged ? 'border-orange-500 bg-orange-50 dark:bg-orange-950' : ''
                }`}
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <div className="font-medium">{input.title}</div>
                      {input.hasChanged && (
                        <Badge variant="outline" className="text-orange-600 border-orange-600">
                          Value Changed
                        </Badge>
                      )}
                      {!input.hasChanged && (
                        <CheckCircle className="h-4 w-4 text-green-600" />
                      )}
                    </div>
                    <div className="grid grid-cols-2 gap-x-4 gap-y-1 mt-2 text-sm">
                      <div>
                        <span className="text-muted-foreground">Current Value:</span>
                        <div className="font-medium">
                          {input.currentValue || 'N/A'}
                          {input.unit && ` ${input.unit}`}
                        </div>
                      </div>
                      {input.hasChanged && input.valueAtCalculation && (
                        <div>
                          <span className="text-muted-foreground">Value at Calculation:</span>
                          <div className="font-medium text-muted-foreground line-through">
                            {input.valueAtCalculation}
                            {input.unit && ` ${input.unit}`}
                          </div>
                        </div>
                      )}
                      <div className="col-span-2">
                        <span className="text-muted-foreground">Last Updated:</span>
                        <div className="text-xs">
                          {input.lastUpdated ? new Date(input.lastUpdated).toLocaleString() : 'N/A'}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Recalculate Button (when not flagged) */}
        {!lineage.needsRecalculation && (
          <div className="pt-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => recalculateMutation.mutate()}
              disabled={recalculateMutation.isPending}
            >
              <RefreshCw className={`h-4 w-4 mr-2 ${recalculateMutation.isPending ? 'animate-spin' : ''}`} />
              Recalculate Manually
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
