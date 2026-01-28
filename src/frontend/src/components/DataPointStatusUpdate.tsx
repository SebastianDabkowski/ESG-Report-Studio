import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { WarningCircle, CheckCircle } from '@phosphor-icons/react'
import { updateDataPointStatus } from '@/lib/api'
import type { DataPoint, CompletenessStatus, StatusValidationError, MissingFieldDetail } from '@/lib/types'

interface DataPointStatusUpdateProps {
  dataPoint: DataPoint
  currentUserId: string
  onStatusUpdated: (updatedDataPoint: DataPoint) => void
  onCancel?: () => void
}

export default function DataPointStatusUpdate({ 
  dataPoint, 
  currentUserId,
  onStatusUpdated,
  onCancel 
}: DataPointStatusUpdateProps) {
  const [selectedStatus, setSelectedStatus] = useState<CompletenessStatus>(dataPoint.completenessStatus)
  const [changeNote, setChangeNote] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [validationError, setValidationError] = useState<StatusValidationError | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setValidationError(null)
    setIsSubmitting(true)

    try {
      const updatedDataPoint = await updateDataPointStatus(dataPoint.id, {
        completenessStatus: selectedStatus,
        updatedBy: currentUserId,
        changeNote: changeNote || undefined
      })
      
      onStatusUpdated(updatedDataPoint)
    } catch (err: any) {
      // Check if the error is a validation error with missing fields
      if (err.message && typeof err.message === 'object' && 'missingFields' in err.message) {
        setValidationError(err.message as StatusValidationError)
      } else if (err instanceof Error) {
        setError(err.message)
      } else {
        setError('Failed to update status')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  const statusOptions: { value: CompletenessStatus; label: string; description: string }[] = [
    { 
      value: 'missing', 
      label: 'Missing', 
      description: 'No data available' 
    },
    { 
      value: 'incomplete', 
      label: 'Incomplete', 
      description: 'Data collection in progress' 
    },
    { 
      value: 'complete', 
      label: 'Complete', 
      description: 'All required data collected and verified' 
    },
    { 
      value: 'not applicable', 
      label: 'Not Applicable', 
      description: 'Not relevant to this reporting period' 
    }
  ]

  const selectedOption = statusOptions.find(opt => opt.value === selectedStatus)

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="status">Completion Status</Label>
        <Select value={selectedStatus} onValueChange={(value) => setSelectedStatus(value as CompletenessStatus)}>
          <SelectTrigger id="status">
            <SelectValue placeholder="Select status" />
          </SelectTrigger>
          <SelectContent>
            {statusOptions.map((option) => (
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
        <Label htmlFor="changeNote">Change Note (Optional)</Label>
        <Textarea
          id="changeNote"
          value={changeNote}
          onChange={(e) => setChangeNote(e.target.value)}
          placeholder="Explain the reason for this status change..."
          rows={3}
        />
      </div>

      {error && (
        <Alert variant="destructive">
          <WarningCircle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {validationError && (
        <Alert variant="destructive">
          <WarningCircle className="h-4 w-4" />
          <AlertDescription>
            <div className="font-medium mb-2">{validationError.message}</div>
            {validationError.missingFields.length > 0 && (
              <div className="space-y-1">
                <div className="text-sm font-medium">Missing required fields:</div>
                <ul className="list-disc list-inside space-y-1">
                  {validationError.missingFields.map((field: MissingFieldDetail, index: number) => (
                    <li key={index} className="text-sm">
                      <span className="font-medium">{field.field}:</span> {field.reason}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </AlertDescription>
        </Alert>
      )}

      {selectedStatus === 'complete' && !validationError && (
        <Alert>
          <CheckCircle className="h-4 w-4" />
          <AlertDescription>
            Marking as complete requires: value, period/deadline, methodology/source, and owner assignment.
          </AlertDescription>
        </Alert>
      )}

      <div className="flex gap-2 justify-end">
        {onCancel && (
          <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </Button>
        )}
        <Button 
          type="submit" 
          disabled={isSubmitting || selectedStatus === dataPoint.completenessStatus}
        >
          {isSubmitting ? 'Updating...' : 'Update Status'}
        </Button>
      </div>
    </form>
  )
}
