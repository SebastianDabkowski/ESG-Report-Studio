import { useState } from 'react'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Lock, LockOpen, CheckCircle, XCircle, FileText } from '@phosphor-icons/react'
import type { SectionSummary, User } from '@/lib/types'
import { submitSectionForApproval, approveSection, requestSectionChanges, createSectionRevision } from '@/lib/api'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { isSectionLocked, getSectionLockReason } from '@/lib/helpers'

interface SectionStatusManagerProps {
  section: SectionSummary
  currentUser: User
  onSectionUpdated: (section: SectionSummary) => void
}

export default function SectionStatusManager({ section, currentUser, onSectionUpdated }: SectionStatusManagerProps) {
  const [isSubmitDialogOpen, setIsSubmitDialogOpen] = useState(false)
  const [isApproveDialogOpen, setIsApproveDialogOpen] = useState(false)
  const [isRequestChangesDialogOpen, setIsRequestChangesDialogOpen] = useState(false)
  const [isCreateRevisionDialogOpen, setIsCreateRevisionDialogOpen] = useState(false)
  
  const [submissionNote, setSubmissionNote] = useState('')
  const [approvalNote, setApprovalNote] = useState('')
  const [changeNote, setChangeNote] = useState('')
  const [revisionNote, setRevisionNote] = useState('')
  const [error, setError] = useState<string | null>(null)

  const queryClient = useQueryClient()

  // Can submit if draft or changes-requested
  const canSubmit = section.status === 'draft' || section.status === 'changes-requested'
  
  // Can approve/request changes if submitted-for-approval and user is reviewer
  const canReview = section.status === 'submitted-for-approval' && 
    (currentUser.role === 'report-owner' || currentUser.role === 'admin')
  
  // Can create revision if approved
  const canRevise = section.status === 'approved'

  const submitMutation = useMutation({
    mutationFn: () => submitSectionForApproval(section.id, {
      submittedBy: currentUser.id,
      submittedByName: currentUser.name,
      submissionNote: submissionNote || undefined
    }),
    onSuccess: (updatedSection) => {
      onSectionUpdated(updatedSection as SectionSummary)
      queryClient.invalidateQueries({ queryKey: ['audit-log'] })
      setIsSubmitDialogOpen(false)
      setSubmissionNote('')
      setError(null)
    },
    onError: (error: Error) => {
      setError(error.message || 'Failed to submit section for approval')
    }
  })

  const approveMutation = useMutation({
    mutationFn: () => approveSection(section.id, {
      approvedBy: currentUser.id,
      approvedByName: currentUser.name,
      approvalNote: approvalNote || undefined
    }),
    onSuccess: (updatedSection) => {
      onSectionUpdated(updatedSection as SectionSummary)
      queryClient.invalidateQueries({ queryKey: ['audit-log'] })
      setIsApproveDialogOpen(false)
      setApprovalNote('')
      setError(null)
    },
    onError: (error: Error) => {
      setError(error.message || 'Failed to approve section')
    }
  })

  const requestChangesMutation = useMutation({
    mutationFn: () => requestSectionChanges(section.id, {
      requestedBy: currentUser.id,
      requestedByName: currentUser.name,
      changeNote
    }),
    onSuccess: (updatedSection) => {
      onSectionUpdated(updatedSection as SectionSummary)
      queryClient.invalidateQueries({ queryKey: ['audit-log'] })
      setIsRequestChangesDialogOpen(false)
      setChangeNote('')
      setError(null)
    },
    onError: (error: Error) => {
      setError(error.message || 'Failed to request changes')
    }
  })

  const createRevisionMutation = useMutation({
    mutationFn: () => createSectionRevision(section.id, {
      createdBy: currentUser.id,
      createdByName: currentUser.name,
      revisionNote: revisionNote || undefined
    }),
    onSuccess: (updatedSection) => {
      onSectionUpdated(updatedSection as SectionSummary)
      queryClient.invalidateQueries({ queryKey: ['audit-log'] })
      setIsCreateRevisionDialogOpen(false)
      setRevisionNote('')
      setError(null)
    },
    onError: (error: Error) => {
      setError(error.message || 'Failed to create revision')
    }
  })

  const isLocked = isSectionLocked(section.status)
  const lockReason = getSectionLockReason(section.status, section.submittedByName, section.submittedForApprovalAt)

  return (
    <div className="space-y-3">
      {/* Lock indicator */}
      {isLocked && (
        <Alert className="bg-amber-50 border-amber-200">
          <Lock className="h-4 w-4 text-amber-600" />
          <AlertDescription className="text-sm text-amber-800">
            {lockReason}
          </AlertDescription>
        </Alert>
      )}

      {/* Action buttons */}
      <div className="flex gap-2 flex-wrap">
        {canSubmit && (
          <Button
            size="sm"
            variant="outline"
            onClick={() => setIsSubmitDialogOpen(true)}
            className="gap-2"
          >
            <Lock className="h-4 w-4" />
            Submit for Approval
          </Button>
        )}

        {canReview && (
          <>
            <Button
              size="sm"
              variant="default"
              onClick={() => setIsApproveDialogOpen(true)}
              className="gap-2 bg-green-600 hover:bg-green-700"
            >
              <CheckCircle className="h-4 w-4" />
              Approve
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => setIsRequestChangesDialogOpen(true)}
              className="gap-2 text-orange-600 border-orange-300 hover:bg-orange-50"
            >
              <XCircle className="h-4 w-4" />
              Request Changes
            </Button>
          </>
        )}

        {canRevise && (
          <Button
            size="sm"
            variant="outline"
            onClick={() => setIsCreateRevisionDialogOpen(true)}
            className="gap-2"
          >
            <FileText className="h-4 w-4" />
            Create New Revision
          </Button>
        )}
      </div>

      {/* Submit for Approval Dialog */}
      <Dialog open={isSubmitDialogOpen} onOpenChange={setIsSubmitDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Submit Section for Approval</DialogTitle>
            <DialogDescription>
              Submit this section for review. Once submitted, the section will be locked and cannot be edited until it's approved or changes are requested.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <Label htmlFor="submission-note">Note to Reviewer (Optional)</Label>
              <Textarea
                id="submission-note"
                placeholder="Add any context or notes for the reviewer..."
                value={submissionNote}
                onChange={(e) => setSubmissionNote(e.target.value)}
                rows={3}
              />
            </div>

            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsSubmitDialogOpen(false)}>
              Cancel
            </Button>
            <Button 
              onClick={() => submitMutation.mutate()}
              disabled={submitMutation.isPending}
            >
              {submitMutation.isPending ? 'Submitting...' : 'Submit for Approval'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Approve Dialog */}
      <Dialog open={isApproveDialogOpen} onOpenChange={setIsApproveDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Approve Section</DialogTitle>
            <DialogDescription>
              Approve this section. A version snapshot will be created for auditability. The section will remain locked until a new revision is created.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <Label htmlFor="approval-note">Approval Note (Optional)</Label>
              <Textarea
                id="approval-note"
                placeholder="Add any approval comments..."
                value={approvalNote}
                onChange={(e) => setApprovalNote(e.target.value)}
                rows={3}
              />
            </div>

            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsApproveDialogOpen(false)}>
              Cancel
            </Button>
            <Button 
              onClick={() => approveMutation.mutate()}
              disabled={approveMutation.isPending}
              className="bg-green-600 hover:bg-green-700"
            >
              {approveMutation.isPending ? 'Approving...' : 'Approve Section'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Request Changes Dialog */}
      <Dialog open={isRequestChangesDialogOpen} onOpenChange={setIsRequestChangesDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Request Changes</DialogTitle>
            <DialogDescription>
              Request changes to this section. The section will be unlocked so the contributor can make edits.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <Label htmlFor="change-note">Change Request Note (Required)</Label>
              <Textarea
                id="change-note"
                placeholder="Explain what changes are needed..."
                value={changeNote}
                onChange={(e) => setChangeNote(e.target.value)}
                rows={4}
                required
              />
            </div>

            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsRequestChangesDialogOpen(false)}>
              Cancel
            </Button>
            <Button 
              onClick={() => requestChangesMutation.mutate()}
              disabled={requestChangesMutation.isPending || !changeNote.trim()}
              variant="outline"
              className="text-orange-600 border-orange-300 hover:bg-orange-50"
            >
              {requestChangesMutation.isPending ? 'Requesting...' : 'Request Changes'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Create Revision Dialog */}
      <Dialog open={isCreateRevisionDialogOpen} onOpenChange={setIsCreateRevisionDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create New Revision</DialogTitle>
            <DialogDescription>
              Create a new draft revision of this approved section. The approved version will remain unchanged for audit purposes.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <Label htmlFor="revision-note">Revision Note (Optional)</Label>
              <Textarea
                id="revision-note"
                placeholder="Explain the reason for this revision..."
                value={revisionNote}
                onChange={(e) => setRevisionNote(e.target.value)}
                rows={3}
              />
            </div>

            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsCreateRevisionDialogOpen(false)}>
              Cancel
            </Button>
            <Button 
              onClick={() => createRevisionMutation.mutate()}
              disabled={createRevisionMutation.isPending}
            >
              {createRevisionMutation.isPending ? 'Creating...' : 'Create Revision'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
