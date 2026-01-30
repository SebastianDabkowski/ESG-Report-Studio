import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { ArrowLeft, CheckCircle, Warning } from '@phosphor-icons/react'
import { useState } from 'react'
import { createVarianceExplanation, updateVarianceExplanation, submitVarianceExplanation } from '@/lib/api'
import type { VarianceExplanation, CreateVarianceExplanationRequest, UpdateVarianceExplanationRequest } from '@/lib/types'

interface VarianceExplanationFormProps {
  dataPointId: string
  priorPeriodId: string
  currentValue: string
  priorValue: string
  percentageChange?: number
  absoluteChange?: number
  existingExplanation?: VarianceExplanation
  onSave?: (explanation: VarianceExplanation) => void
  onCancel?: () => void
}

const VARIANCE_CATEGORIES = [
  { value: 'operational-change', label: 'Operational Change' },
  { value: 'methodology-change', label: 'Methodology Change' },
  { value: 'business-expansion', label: 'Business Expansion' },
  { value: 'business-contraction', label: 'Business Contraction' },
  { value: 'market-conditions', label: 'Market Conditions' },
  { value: 'regulatory-change', label: 'Regulatory Change' },
  { value: 'data-quality-improvement', label: 'Data Quality Improvement' },
  { value: 'other', label: 'Other' }
]

export default function VarianceExplanationForm({
  dataPointId,
  priorPeriodId,
  currentValue,
  priorValue,
  percentageChange,
  absoluteChange,
  existingExplanation,
  onSave,
  onCancel
}: VarianceExplanationFormProps) {
  const [explanation, setExplanation] = useState(existingExplanation?.explanation || '')
  const [rootCause, setRootCause] = useState(existingExplanation?.rootCause || '')
  const [category, setCategory] = useState(existingExplanation?.category || '')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  const isEditing = !!existingExplanation
  const canEdit = !existingExplanation || existingExplanation.status === 'draft' || existingExplanation.status === 'revision-requested'

  async function handleSubmit(submitForReview: boolean = false) {
    if (!explanation.trim()) {
      setError('Explanation is required')
      return
    }

    try {
      setLoading(true)
      setError(null)

      let result: VarianceExplanation

      if (isEditing) {
        const updateRequest: UpdateVarianceExplanationRequest = {
          explanation,
          rootCause: rootCause || undefined,
          category: category || undefined,
          updatedBy: 'current-user' // TODO: Get from auth context
        }
        result = await updateVarianceExplanation(existingExplanation.id, updateRequest)
      } else {
        const createRequest: CreateVarianceExplanationRequest = {
          dataPointId,
          priorPeriodId,
          explanation,
          rootCause: rootCause || undefined,
          category: category || undefined,
          createdBy: 'current-user' // TODO: Get from auth context
        }
        result = await createVarianceExplanation(createRequest)
      }

      // If submitForReview is true, submit the explanation
      if (submitForReview) {
        result = await submitVarianceExplanation(result.id, {
          submittedBy: 'current-user' // TODO: Get from auth context
        })
      }

      setSuccess(true)
      setTimeout(() => {
        if (onSave) {
          onSave(result)
        }
      }, 1000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save variance explanation')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle>Variance Explanation</CardTitle>
            <CardDescription>
              Explain the significant year-over-year change in this metric
            </CardDescription>
          </div>
          {onCancel && (
            <Button onClick={onCancel} variant="outline" size="sm">
              <ArrowLeft className="mr-2" size={16} />
              Back
            </Button>
          )}
        </div>
      </CardHeader>

      <CardContent className="space-y-6">
        {/* Variance Summary */}
        <div className="grid grid-cols-3 gap-4 p-4 bg-gray-50 rounded-lg">
          <div>
            <div className="text-xs text-gray-600 mb-1">Prior Value</div>
            <div className="font-medium">{priorValue}</div>
          </div>
          <div>
            <div className="text-xs text-gray-600 mb-1">Current Value</div>
            <div className="font-medium">{currentValue}</div>
          </div>
          <div>
            <div className="text-xs text-gray-600 mb-1">Change</div>
            <div className="font-medium">
              {percentageChange !== undefined && percentageChange !== null ? (
                <span className={percentageChange > 0 ? 'text-blue-600' : 'text-purple-600'}>
                  {percentageChange > 0 ? '+' : ''}{percentageChange.toFixed(2)}%
                </span>
              ) : 'N/A'}
              {absoluteChange !== undefined && absoluteChange !== null && (
                <span className="text-xs text-gray-600 ml-1">
                  ({absoluteChange > 0 ? '+' : ''}{absoluteChange.toFixed(2)})
                </span>
              )}
            </div>
          </div>
        </div>

        {/* Status Badge */}
        {existingExplanation && (
          <div className="flex items-center gap-2">
            <span className="text-sm text-gray-600">Status:</span>
            <Badge variant={
              existingExplanation.status === 'approved' ? 'default' :
              existingExplanation.status === 'submitted' ? 'secondary' :
              existingExplanation.status === 'rejected' ? 'destructive' :
              'outline'
            }>
              {existingExplanation.status}
            </Badge>
          </div>
        )}

        {/* Review Comments (if rejected or revision requested) */}
        {existingExplanation?.reviewComments && existingExplanation.status !== 'approved' && (
          <Alert className="bg-yellow-50 border-yellow-300">
            <Warning size={20} weight="fill" className="text-yellow-600" />
            <AlertDescription>
              <strong>Reviewer Comments:</strong> {existingExplanation.reviewComments}
            </AlertDescription>
          </Alert>
        )}

        {/* Form Fields */}
        <div className="space-y-4">
          <div>
            <Label htmlFor="category">Category (Optional)</Label>
            <Select value={category} onValueChange={setCategory} disabled={!canEdit}>
              <SelectTrigger id="category">
                <SelectValue placeholder="Select a category" />
              </SelectTrigger>
              <SelectContent>
                {VARIANCE_CATEGORIES.map((cat) => (
                  <SelectItem key={cat.value} value={cat.value}>
                    {cat.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div>
            <Label htmlFor="explanation">Explanation *</Label>
            <Textarea
              id="explanation"
              value={explanation}
              onChange={(e) => setExplanation(e.target.value)}
              placeholder="Provide a detailed explanation for the variance..."
              rows={6}
              disabled={!canEdit}
              className="resize-none"
            />
            <p className="text-xs text-gray-500 mt-1">
              Explain the primary reason for the year-over-year change in this metric.
            </p>
          </div>

          <div>
            <Label htmlFor="rootCause">Root Cause (Optional)</Label>
            <Input
              id="rootCause"
              value={rootCause}
              onChange={(e) => setRootCause(e.target.value)}
              placeholder="e.g., Business expansion, Process improvement, Market conditions"
              disabled={!canEdit}
            />
            <p className="text-xs text-gray-500 mt-1">
              Briefly describe the underlying reason for the change.
            </p>
          </div>
        </div>

        {/* Error/Success Messages */}
        {error && (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        {success && (
          <Alert className="bg-green-50 border-green-300">
            <CheckCircle size={20} weight="fill" className="text-green-600" />
            <AlertDescription>
              Variance explanation saved successfully!
            </AlertDescription>
          </Alert>
        )}

        {/* Action Buttons */}
        {canEdit && (
          <div className="flex gap-2 justify-end">
            {onCancel && (
              <Button onClick={onCancel} variant="outline" disabled={loading}>
                Cancel
              </Button>
            )}
            <Button
              onClick={() => handleSubmit(false)}
              variant="outline"
              disabled={loading || !explanation.trim()}
            >
              Save Draft
            </Button>
            <Button
              onClick={() => handleSubmit(true)}
              disabled={loading || !explanation.trim()}
            >
              {loading ? 'Saving...' : 'Submit for Review'}
            </Button>
          </div>
        )}

        {/* Read-only message for approved/rejected */}
        {existingExplanation && existingExplanation.status === 'approved' && (
          <Alert className="bg-green-50 border-green-300">
            <CheckCircle size={20} weight="fill" className="text-green-600" />
            <AlertDescription>
              This explanation has been approved by {existingExplanation.reviewedBy} on{' '}
              {new Date(existingExplanation.reviewedAt!).toLocaleDateString()}.
            </AlertDescription>
          </Alert>
        )}

        {existingExplanation && existingExplanation.status === 'rejected' && (
          <Alert variant="destructive">
            <AlertDescription>
              This explanation was rejected. Please update it and resubmit.
            </AlertDescription>
          </Alert>
        )}
      </CardContent>
    </Card>
  )
}
