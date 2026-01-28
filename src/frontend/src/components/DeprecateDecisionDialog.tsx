import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import type { Decision } from '@/lib/types'
import { deprecateDecision } from '@/lib/api'

interface DeprecateDecisionDialogProps {
  decision: Decision
  onSuccess: () => void
  onCancel: () => void
}

export default function DeprecateDecisionDialog({
  decision,
  onSuccess,
  onCancel,
}: DeprecateDecisionDialogProps) {
  const [reason, setReason] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async () => {
    if (!reason.trim()) {
      setError('Deprecation reason is required')
      return
    }

    try {
      setLoading(true)
      setError(null)
      await deprecateDecision(decision.id, { reason: reason.trim() })
      onSuccess()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to deprecate decision')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={true} onOpenChange={onCancel}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Deprecate Decision</DialogTitle>
          <DialogDescription>
            Mark this decision as deprecated. The decision will be preserved but marked as no longer applicable.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <Label htmlFor="reason">Deprecation Reason *</Label>
            <Textarea
              id="reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Explain why this decision is being deprecated"
              rows={4}
              required
            />
            <p className="text-sm text-muted-foreground">
              This reason will be recorded in the decision's change history.
            </p>
          </div>

          {error && (
            <div className="bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded">
              {error}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={loading}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={loading}>
            {loading ? 'Deprecating...' : 'Deprecate Decision'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
