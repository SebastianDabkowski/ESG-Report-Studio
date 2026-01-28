import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { WarningCircle, CheckCircle } from '@phosphor-icons/react'
import type { DataPoint, MissingReasonCategory } from '@/lib/types'

interface MissingDataFlagProps {
  dataPoint: DataPoint
  currentUserId: string
  onFlagged: (updatedDataPoint: DataPoint) => void
  onUnflagged: (updatedDataPoint: DataPoint) => void
  onCancel?: () => void
}

export default function MissingDataFlag({ 
  dataPoint, 
  currentUserId,
  onFlagged,
  onUnflagged,
  onCancel 
}: MissingDataFlagProps) {
  const [category, setCategory] = useState<MissingReasonCategory>('not-measured')
  const [reason, setReason] = useState('')
  const [changeNote, setChangeNote] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleFlag = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    if (!reason.trim()) {
      setError('Please provide a reason for flagging this data as missing.')
      setIsSubmitting(false)
      return
    }

    try {
      const response = await fetch(`/api/data-points/${dataPoint.id}/flag-missing`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          flaggedBy: currentUserId,
          missingReasonCategory: category,
          missingReason: reason
        })
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.error || 'Failed to flag data as missing')
      }

      const updatedDataPoint = await response.json()
      onFlagged(updatedDataPoint)
    } catch (err: any) {
      setError(err.message || 'Failed to flag data as missing')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleUnflag = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const response = await fetch(`/api/data-points/${dataPoint.id}/unflag-missing`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          unflaggedBy: currentUserId,
          changeNote: changeNote || undefined
        })
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.error || 'Failed to unflag data')
      }

      const updatedDataPoint = await response.json()
      onUnflagged(updatedDataPoint)
    } catch (err: any) {
      setError(err.message || 'Failed to unflag data')
    } finally {
      setIsSubmitting(false)
    }
  }

  const categoryOptions: { value: MissingReasonCategory; label: string; description: string }[] = [
    { 
      value: 'not-measured', 
      label: 'Not Measured', 
      description: 'Data was not measured or tracked during the reporting period' 
    },
    { 
      value: 'not-applicable', 
      label: 'Not Applicable', 
      description: 'This data point does not apply to our organization or operations' 
    },
    { 
      value: 'unavailable-from-supplier', 
      label: 'Unavailable from Supplier', 
      description: 'Required data is not available from suppliers or third parties' 
    },
    { 
      value: 'data-quality-issue', 
      label: 'Data Quality Issue', 
      description: 'Data exists but quality is insufficient for reporting' 
    },
    { 
      value: 'system-limitation', 
      label: 'System Limitation', 
      description: 'Technical or process limitations prevent data collection' 
    },
    { 
      value: 'other', 
      label: 'Other', 
      description: 'Another reason not covered by the above categories' 
    }
  ]

  const selectedOption = categoryOptions.find(opt => opt.value === category)

  // If already flagged, show unflag form
  if (dataPoint.isMissing) {
    return (
      <form onSubmit={handleUnflag} className="space-y-4">
        <Alert>
          <CheckCircle className="h-4 w-4" />
          <AlertDescription>
            <div className="font-medium mb-2">Currently Flagged as Missing</div>
            <div className="text-sm">
              <strong>Category:</strong> {selectedOption?.label || dataPoint.missingReasonCategory}
            </div>
            <div className="text-sm">
              <strong>Reason:</strong> {dataPoint.missingReason}
            </div>
            {dataPoint.missingFlaggedBy && (
              <div className="text-sm text-muted-foreground mt-1">
                Flagged on {dataPoint.missingFlaggedAt ? new Date(dataPoint.missingFlaggedAt).toLocaleDateString() : 'unknown date'}
              </div>
            )}
          </AlertDescription>
        </Alert>

        <div className="space-y-2">
          <Label htmlFor="changeNote">Change Note (Optional)</Label>
          <Textarea
            id="changeNote"
            value={changeNote}
            onChange={(e) => setChangeNote(e.target.value)}
            placeholder="Explain why this data is now available..."
            rows={3}
          />
        </div>

        {error && (
          <Alert variant="destructive">
            <WarningCircle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <div className="flex gap-2 justify-end">
          {onCancel && (
            <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
              Cancel
            </Button>
          )}
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Unflagging...' : 'Unflag as Missing'}
          </Button>
        </div>
      </form>
    )
  }

  // Otherwise, show flag form
  return (
    <form onSubmit={handleFlag} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="category">Missing Data Category</Label>
        <Select value={category} onValueChange={(value) => setCategory(value as MissingReasonCategory)}>
          <SelectTrigger id="category">
            <SelectValue placeholder="Select category" />
          </SelectTrigger>
          <SelectContent>
            {categoryOptions.map((option) => (
              <SelectItem key={option.value} value={option.value}>
                <div>
                  <div className="font-medium">{option.label}</div>
                  <div className="text-xs text-muted-foreground">{option.description}</div>
                </div>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {selectedOption && (
          <p className="text-sm text-muted-foreground">{selectedOption.description}</p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="reason">Detailed Reason *</Label>
        <Textarea
          id="reason"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="Provide specific details about why this data is missing..."
          rows={4}
          required
        />
        <p className="text-xs text-muted-foreground">
          Explain the specific circumstances and any plans to address this gap.
        </p>
      </div>

      {error && (
        <Alert variant="destructive">
          <WarningCircle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <div className="flex gap-2 justify-end">
        {onCancel && (
          <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </Button>
        )}
        <Button type="submit" disabled={isSubmitting || !reason.trim()}>
          {isSubmitting ? 'Flagging...' : 'Flag as Missing'}
        </Button>
      </div>
    </form>
  )
}
