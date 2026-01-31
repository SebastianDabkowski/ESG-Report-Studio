import React, { useState } from 'react'
import { Button } from './ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from './ui/dialog'
import { Label } from './ui/label'
import { Textarea } from './ui/textarea'
import { Alert, AlertDescription } from './ui/alert'
import { createAccessRequest } from '@/lib/api'
import type { CreateAccessRequestRequest } from '@/lib/types'

interface AccessRequestButtonProps {
  resourceType: 'section' | 'report'
  resourceId: string
  resourceName: string
  currentUserId: string
  onSuccess?: () => void
  variant?: 'default' | 'outline' | 'ghost'
  size?: 'default' | 'sm' | 'lg'
}

export function AccessRequestButton({
  resourceType,
  resourceId,
  resourceName,
  currentUserId,
  onSuccess,
  variant = 'outline',
  size = 'sm'
}: AccessRequestButtonProps) {
  const [isOpen, setIsOpen] = useState(false)
  const [reason, setReason] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSuccessMessage(null)

    if (!reason.trim()) {
      setError('Please provide a reason for requesting access')
      return
    }

    const payload: CreateAccessRequestRequest = {
      requestedBy: currentUserId,
      resourceType,
      resourceId,
      reason: reason.trim()
    }

    setIsSubmitting(true)
    try {
      await createAccessRequest(payload)
      setSuccessMessage('Access request submitted successfully. An administrator will review your request.')
      setReason('')
      
      // Close dialog after a short delay
      setTimeout(() => {
        setIsOpen(false)
        setSuccessMessage(null)
        onSuccess?.()
      }, 2000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit access request')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleOpenChange = (open: boolean) => {
    if (!isSubmitting) {
      setIsOpen(open)
      if (!open) {
        // Reset form when closing
        setReason('')
        setError(null)
        setSuccessMessage(null)
      }
    }
  }

  return (
    <>
      <Button variant={variant} size={size} onClick={() => setIsOpen(true)}>
        Request Access
      </Button>

      <Dialog open={isOpen} onOpenChange={handleOpenChange}>
        <DialogContent className="sm:max-w-[500px]">
          <form onSubmit={handleSubmit}>
            <DialogHeader>
              <DialogTitle>Request Access</DialogTitle>
              <DialogDescription>
                Request access to {resourceType} "{resourceName}"
              </DialogDescription>
            </DialogHeader>

            <div className="grid gap-4 py-4">
              {error && (
                <Alert variant="destructive">
                  <AlertDescription>{error}</AlertDescription>
                </Alert>
              )}

              {successMessage && (
                <Alert>
                  <AlertDescription>{successMessage}</AlertDescription>
                </Alert>
              )}

              <div className="grid gap-2">
                <Label htmlFor="reason">Reason for Access *</Label>
                <Textarea
                  id="reason"
                  placeholder="Please explain why you need access to this resource..."
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  rows={4}
                  disabled={isSubmitting}
                  required
                />
                <p className="text-xs text-muted-foreground">
                  Your request will be reviewed by an administrator.
                </p>
              </div>
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => handleOpenChange(false)}
                disabled={isSubmitting}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={isSubmitting || !!successMessage}>
                {isSubmitting ? 'Submitting...' : 'Submit Request'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  )
}
