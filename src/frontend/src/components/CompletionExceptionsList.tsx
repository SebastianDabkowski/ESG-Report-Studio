import { useState, useEffect, useCallback } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { 
  ShieldCheck, 
  Plus, 
  Check, 
  X, 
  Warning,
  CalendarBlank,
  Info,
  Clock
} from '@phosphor-icons/react'
import type { CompletionException } from '@/lib/types'
import { 
  getCompletionExceptions, 
  approveCompletionException, 
  rejectCompletionException,
  deleteCompletionException 
} from '@/lib/api'
import { CompletionExceptionForm } from './CompletionExceptionForm'

interface CompletionExceptionsListProps {
  sectionId: string
  currentUserId: string
  currentUserRole: 'admin' | 'report-owner' | 'contributor' | 'auditor'
}

export function CompletionExceptionsList({ 
  sectionId, 
  currentUserId,
  currentUserRole 
}: CompletionExceptionsListProps) {
  const [exceptions, setExceptions] = useState<CompletionException[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [reviewingException, setReviewingException] = useState<CompletionException | undefined>()
  const [reviewComments, setReviewComments] = useState('')
  const [reviewAction, setReviewAction] = useState<'approve' | 'reject'>('approve')
  const [isSubmittingReview, setIsSubmittingReview] = useState(false)

  const canApprove = currentUserRole === 'admin' || currentUserRole === 'report-owner'

  const loadExceptions = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await getCompletionExceptions(sectionId)
      setExceptions(data)
    } catch (err) {
      console.error('Error loading exceptions:', err)
      setError(err instanceof Error ? err.message : 'Failed to load exceptions')
    } finally {
      setLoading(false)
    }
  }, [sectionId])

  useEffect(() => {
    loadExceptions()
  }, [loadExceptions])

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this exception?')) {
      return
    }

    try {
      await deleteCompletionException(id)
      await loadExceptions()
    } catch (err) {
      console.error('Error deleting exception:', err)
      setError(err instanceof Error ? err.message : 'Failed to delete exception')
    }
  }

  const handleReview = (exception: CompletionException, action: 'approve' | 'reject') => {
    setReviewingException(exception)
    setReviewAction(action)
    setReviewComments('')
  }

  const handleSubmitReview = async () => {
    if (!reviewingException) return

    if (reviewAction === 'reject' && !reviewComments.trim()) {
      setError('Review comments are required when rejecting an exception')
      return
    }

    setIsSubmittingReview(true)
    setError(null)

    try {
      if (reviewAction === 'approve') {
        await approveCompletionException(reviewingException.id, {
          approvedBy: currentUserId,
          reviewComments: reviewComments || undefined
        })
      } else {
        await rejectCompletionException(reviewingException.id, {
          rejectedBy: currentUserId,
          reviewComments: reviewComments
        })
      }
      
      setReviewingException(undefined)
      setReviewComments('')
      await loadExceptions()
    } catch (err) {
      console.error('Error reviewing exception:', err)
      setError(err instanceof Error ? err.message : 'Failed to review exception')
      // Keep dialog open to show error
    } finally {
      setIsSubmittingReview(false)
    }
  }

  const handleFormSuccess = async () => {
    setShowForm(false)
    await loadExceptions()
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'pending':
        return <Badge className="bg-yellow-100 text-yellow-800 border-yellow-200">Pending</Badge>
      case 'accepted':
        return <Badge className="bg-green-100 text-green-800 border-green-200">Accepted</Badge>
      case 'rejected':
        return <Badge className="bg-red-100 text-red-800 border-red-200">Rejected</Badge>
      default:
        return <Badge>{status}</Badge>
    }
  }

  const getExceptionTypeBadge = (type: string) => {
    const typeLabels: Record<string, string> = {
      'missing-data': 'Missing Data',
      'estimated-data': 'Estimated Data',
      'simplified-scope': 'Simplified Scope',
      'other': 'Other'
    }
    return <Badge variant="outline">{typeLabels[type] || type}</Badge>
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  }

  if (loading) {
    return <div className="text-center py-8 text-gray-500">Loading exceptions...</div>
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h3 className="text-lg font-semibold flex items-center gap-2">
            <ShieldCheck className="h-5 w-5" />
            Completion Exceptions
          </h3>
          <p className="text-sm text-gray-600 mt-1">
            Request and manage approved exceptions for completeness validation
          </p>
        </div>
        <Button onClick={() => setShowForm(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Request Exception
        </Button>
      </div>

      {error && (
        <Alert variant="destructive">
          <Warning className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {showForm && (
        <Card>
          <CardHeader>
            <CardTitle>Request Completion Exception</CardTitle>
          </CardHeader>
          <CardContent>
            <CompletionExceptionForm
              sectionId={sectionId}
              requestedBy={currentUserId}
              onSuccess={handleFormSuccess}
              onCancel={() => setShowForm(false)}
            />
          </CardContent>
        </Card>
      )}

      {exceptions.length === 0 && !showForm ? (
        <Card>
          <CardContent className="py-12">
            <div className="text-center text-gray-500">
              <ShieldCheck className="h-12 w-12 mx-auto mb-3 opacity-30" />
              <p>No completion exceptions found for this section.</p>
              <p className="text-sm mt-1">Request an exception to allow controlled gaps in the report.</p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {exceptions.map((exception) => (
            <Card key={exception.id}>
              <CardContent className="pt-6">
                <div className="space-y-4">
                  <div className="flex justify-between items-start">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-2">
                        <h4 className="font-semibold">{exception.title}</h4>
                        {getStatusBadge(exception.status)}
                        {getExceptionTypeBadge(exception.exceptionType)}
                      </div>
                      <p className="text-sm text-gray-700 mb-3">{exception.justification}</p>
                      
                      <div className="grid grid-cols-2 gap-4 text-sm">
                        <div className="flex items-center gap-2 text-gray-600">
                          <Clock className="h-4 w-4" />
                          <span>Requested: {formatDate(exception.requestedAt)}</span>
                        </div>
                        
                        {exception.expiresAt && (
                          <div className="flex items-center gap-2 text-gray-600">
                            <CalendarBlank className="h-4 w-4" />
                            <span>Expires: {formatDate(exception.expiresAt)}</span>
                          </div>
                        )}
                        
                        {exception.approvedAt && exception.approvedBy && (
                          <div className="flex items-center gap-2 text-green-600">
                            <Check className="h-4 w-4" />
                            <span>Approved: {formatDate(exception.approvedAt)}</span>
                          </div>
                        )}
                        
                        {exception.rejectedAt && exception.rejectedBy && (
                          <div className="flex items-center gap-2 text-red-600">
                            <X className="h-4 w-4" />
                            <span>Rejected: {formatDate(exception.rejectedAt)}</span>
                          </div>
                        )}
                      </div>
                      
                      {exception.reviewComments && (
                        <div className="mt-3 p-3 bg-gray-50 rounded-md">
                          <p className="text-sm font-medium text-gray-700 mb-1">Review Comments:</p>
                          <p className="text-sm text-gray-600">{exception.reviewComments}</p>
                        </div>
                      )}
                    </div>
                    
                    <div className="flex gap-2 ml-4">
                      {exception.status === 'pending' && canApprove && (
                        <>
                          <Button 
                            size="sm" 
                            variant="outline"
                            onClick={() => handleReview(exception, 'approve')}
                          >
                            <Check className="h-4 w-4 mr-1" />
                            Approve
                          </Button>
                          <Button 
                            size="sm" 
                            variant="outline"
                            onClick={() => handleReview(exception, 'reject')}
                          >
                            <X className="h-4 w-4 mr-1" />
                            Reject
                          </Button>
                        </>
                      )}
                      {exception.status === 'pending' && exception.requestedBy === currentUserId && (
                        <Button 
                          size="sm" 
                          variant="outline"
                          onClick={() => handleDelete(exception.id)}
                        >
                          Delete
                        </Button>
                      )}
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Review Dialog */}
      <Dialog open={!!reviewingException} onOpenChange={() => setReviewingException(undefined)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {reviewAction === 'approve' ? 'Approve' : 'Reject'} Exception
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <p className="text-sm text-gray-600 mb-2">
                Exception: <strong>{reviewingException?.title}</strong>
              </p>
              <p className="text-sm text-gray-600">
                {reviewingException?.justification}
              </p>
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="reviewComments">
                Review Comments {reviewAction === 'reject' && <span className="text-red-600">*</span>}
              </Label>
              <Textarea
                id="reviewComments"
                value={reviewComments}
                onChange={(e) => setReviewComments(e.target.value)}
                placeholder={reviewAction === 'approve' 
                  ? 'Optional comments about this approval...' 
                  : 'Explain why this exception is being rejected...'}
                rows={4}
              />
            </div>

            <div className="flex gap-3 justify-end">
              <Button 
                variant="outline" 
                onClick={() => setReviewingException(undefined)}
                disabled={isSubmittingReview}
              >
                Cancel
              </Button>
              <Button 
                onClick={handleSubmitReview}
                disabled={isSubmittingReview}
                variant={reviewAction === 'reject' ? 'destructive' : 'default'}
              >
                {isSubmittingReview ? 'Submitting...' : reviewAction === 'approve' ? 'Approve' : 'Reject'}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}
