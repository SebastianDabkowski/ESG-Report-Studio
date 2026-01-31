import React, { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card'
import { Button } from './ui/button'
import { Badge } from './ui/badge'
import { Textarea } from './ui/textarea'
import { Label } from './ui/label'
import { Alert, AlertDescription } from './ui/alert'
import { getAccessRequests, approveAccessRequest, rejectAccessRequest } from '@/lib/api'
import type { AccessRequest, ReviewAccessRequestRequest } from '@/lib/types'

interface AccessRequestsManagerProps {
  currentUserId: string
  isAdmin: boolean
}

export function AccessRequestsManager({ currentUserId, isAdmin }: AccessRequestsManagerProps) {
  const [requests, setRequests] = useState<AccessRequest[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedRequest, setSelectedRequest] = useState<AccessRequest | null>(null)
  const [reviewComment, setReviewComment] = useState('')
  const [isReviewing, setIsReviewing] = useState(false)

  useEffect(() => {
    loadRequests()
  }, [])

  const loadRequests = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await getAccessRequests(
        undefined, // status - show all
        isAdmin ? undefined : currentUserId, // only show user's own if not admin
        undefined // resourceId
      )
      setRequests(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load access requests')
    } finally {
      setLoading(false)
    }
  }

  const handleReview = async (requestId: string, decision: 'approve' | 'reject') => {
    setIsReviewing(true)
    setError(null)
    
    const payload: ReviewAccessRequestRequest = {
      accessRequestId: requestId,
      decision,
      reviewedBy: currentUserId,
      reviewComment: reviewComment.trim() || undefined
    }

    try {
      if (decision === 'approve') {
        await approveAccessRequest(requestId, payload)
      } else {
        await rejectAccessRequest(requestId, payload)
      }
      
      // Reload requests to show updated status
      await loadRequests()
      setSelectedRequest(null)
      setReviewComment('')
    } catch (err) {
      setError(err instanceof Error ? err.message : `Failed to ${decision} request`)
    } finally {
      setIsReviewing(false)
    }
  }

  const getStatusBadgeVariant = (status: string) => {
    switch (status) {
      case 'pending':
        return 'secondary'
      case 'approved':
        return 'default'
      case 'rejected':
        return 'destructive'
      default:
        return 'outline'
    }
  }

  if (loading) {
    return (
      <Card>
        <CardContent className="pt-6">
          <p className="text-center text-muted-foreground">Loading access requests...</p>
        </CardContent>
      </Card>
    )
  }

  const pendingRequests = requests.filter(r => r.status === 'pending')
  const reviewedRequests = requests.filter(r => r.status !== 'pending')

  return (
    <div className="space-y-6">
      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {/* Pending Requests */}
      <Card>
        <CardHeader>
          <CardTitle>Pending Access Requests</CardTitle>
          <CardDescription>
            {isAdmin 
              ? 'Review and approve or reject access requests from users' 
              : 'Your pending access requests'}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {pendingRequests.length === 0 ? (
            <p className="text-center text-muted-foreground py-4">
              No pending access requests
            </p>
          ) : (
            <div className="space-y-4">
              {pendingRequests.map((request) => (
                <div
                  key={request.id}
                  className="border rounded-lg p-4 space-y-3"
                >
                  <div className="flex items-start justify-between">
                    <div className="space-y-1">
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{request.requestedByName}</span>
                        <Badge variant={getStatusBadgeVariant(request.status)}>
                          {request.status}
                        </Badge>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        Requested access to {request.resourceType}: <span className="font-medium">{request.resourceName}</span>
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {new Date(request.requestedAt).toLocaleString()}
                      </p>
                    </div>
                  </div>

                  <div className="space-y-2">
                    <p className="text-sm font-medium">Reason:</p>
                    <p className="text-sm text-muted-foreground bg-muted p-3 rounded">
                      {request.reason}
                    </p>
                  </div>

                  {isAdmin && request.status === 'pending' && (
                    <div className="space-y-3 pt-2">
                      {selectedRequest?.id === request.id ? (
                        <>
                          <div className="space-y-2">
                            <Label htmlFor={`comment-${request.id}`}>Review Comment (Optional)</Label>
                            <Textarea
                              id={`comment-${request.id}`}
                              placeholder="Add a comment explaining your decision..."
                              value={reviewComment}
                              onChange={(e) => setReviewComment(e.target.value)}
                              rows={2}
                              disabled={isReviewing}
                            />
                          </div>
                          <div className="flex gap-2">
                            <Button
                              size="sm"
                              onClick={() => handleReview(request.id, 'approve')}
                              disabled={isReviewing}
                            >
                              {isReviewing ? 'Processing...' : 'Approve'}
                            </Button>
                            <Button
                              size="sm"
                              variant="destructive"
                              onClick={() => handleReview(request.id, 'reject')}
                              disabled={isReviewing}
                            >
                              {isReviewing ? 'Processing...' : 'Reject'}
                            </Button>
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => {
                                setSelectedRequest(null)
                                setReviewComment('')
                              }}
                              disabled={isReviewing}
                            >
                              Cancel
                            </Button>
                          </div>
                        </>
                      ) : (
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => setSelectedRequest(request)}
                        >
                          Review Request
                        </Button>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Reviewed Requests */}
      {reviewedRequests.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Reviewed Requests</CardTitle>
            <CardDescription>Previously reviewed access requests</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {reviewedRequests.map((request) => (
                <div
                  key={request.id}
                  className="border rounded-lg p-4 space-y-2"
                >
                  <div className="flex items-start justify-between">
                    <div className="space-y-1">
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{request.requestedByName}</span>
                        <Badge variant={getStatusBadgeVariant(request.status)}>
                          {request.status}
                        </Badge>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        {request.resourceType}: {request.resourceName}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        Requested: {new Date(request.requestedAt).toLocaleDateString()}
                        {request.reviewedAt && 
                          ` • Reviewed: ${new Date(request.reviewedAt).toLocaleDateString()}`}
                      </p>
                    </div>
                  </div>

                  {request.reviewComment && (
                    <div className="text-sm">
                      <span className="font-medium">Review comment:</span>
                      <p className="text-muted-foreground mt-1">{request.reviewComment}</p>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
