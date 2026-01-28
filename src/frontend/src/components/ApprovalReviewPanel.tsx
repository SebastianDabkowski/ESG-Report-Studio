import React, { useState } from 'react';
import { Button } from './ui/button';
import { Label } from './ui/label';
import { Textarea } from './ui/textarea';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from './ui/card';
import { Alert, AlertDescription } from './ui/alert';
import { Badge } from './ui/badge';
import { CheckCircle, XCircle, Clock } from 'lucide-react';

interface ApprovalRecord {
  id: string;
  approvalRequestId: string;
  approverId: string;
  approverName: string;
  status: string;
  decision?: string;
  decidedAt?: string;
  comment?: string;
}

interface ApprovalRequest {
  id: string;
  periodId: string;
  requestedBy: string;
  requestedAt: string;
  requestMessage?: string;
  approvalDeadline?: string;
  status: string;
  approvals: ApprovalRecord[];
}

interface ApprovalReviewPanelProps {
  approvalRequest: ApprovalRequest;
  periodName: string;
  currentUserId: string;
  onSubmitDecision: (recordId: string, decision: 'approve' | 'reject', comment?: string) => Promise<void>;
}

export function ApprovalReviewPanel({
  approvalRequest,
  periodName,
  currentUserId,
  onSubmitDecision
}: ApprovalReviewPanelProps) {
  const [comment, setComment] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Find the current user's approval record
  const myApproval = approvalRequest.approvals.find(a => a.approverId === currentUserId);
  const canDecide = myApproval && myApproval.status === 'pending';

  const handleDecision = async (decision: 'approve' | 'reject') => {
    if (!myApproval) return;

    setError(null);
    setIsSubmitting(true);

    try {
      await onSubmitDecision(myApproval.id, decision, comment || undefined);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit decision');
      setIsSubmitting(false);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'approved':
        return <CheckCircle className="h-4 w-4 text-green-500" />;
      case 'rejected':
        return <XCircle className="h-4 w-4 text-red-500" />;
      default:
        return <Clock className="h-4 w-4 text-yellow-500" />;
    }
  };

  const getStatusBadge = (status: string) => {
    const variant = status === 'approved' ? 'default' : status === 'rejected' ? 'destructive' : 'secondary';
    return (
      <Badge variant={variant}>
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </Badge>
    );
  };

  return (
    <Card className="w-full">
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle>Approval Request</CardTitle>
            <CardDescription>
              Report Period: {periodName}
            </CardDescription>
          </div>
          {getStatusBadge(approvalRequest.status)}
        </div>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="space-y-2">
          <div className="text-sm">
            <span className="font-medium">Requested: </span>
            {new Date(approvalRequest.requestedAt).toLocaleString()}
          </div>
          {approvalRequest.approvalDeadline && (
            <div className="text-sm">
              <span className="font-medium">Deadline: </span>
              {new Date(approvalRequest.approvalDeadline).toLocaleString()}
            </div>
          )}
          {approvalRequest.requestMessage && (
            <div className="mt-4">
              <Label>Message from Requester</Label>
              <div className="mt-2 p-3 bg-muted rounded-md text-sm">
                {approvalRequest.requestMessage}
              </div>
            </div>
          )}
        </div>

        <div className="space-y-2">
          <Label>Approval Status</Label>
          <div className="border rounded-md divide-y">
            {approvalRequest.approvals.map(approval => (
              <div key={approval.id} className="p-3 flex items-start justify-between">
                <div className="flex items-start space-x-2 flex-1">
                  {getStatusIcon(approval.status)}
                  <div className="flex-1">
                    <div className="font-medium text-sm">{approval.approverName}</div>
                    {approval.decidedAt && (
                      <div className="text-xs text-muted-foreground">
                        Decided: {new Date(approval.decidedAt).toLocaleString()}
                      </div>
                    )}
                    {approval.comment && (
                      <div className="mt-2 text-sm text-muted-foreground italic">
                        "{approval.comment}"
                      </div>
                    )}
                  </div>
                </div>
                {getStatusBadge(approval.status)}
              </div>
            ))}
          </div>
        </div>

        {canDecide && (
          <div className="space-y-4 pt-4 border-t">
            <div className="bg-blue-50 border border-blue-200 rounded-md p-4">
              <p className="text-sm font-medium text-blue-900">
                Your approval is required
              </p>
              <p className="text-sm text-blue-700 mt-1">
                Please review the report and provide your decision.
              </p>
            </div>

            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="comment">Comment (Optional)</Label>
              <Textarea
                id="comment"
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                placeholder="Add any comments about your decision..."
                rows={3}
                disabled={isSubmitting}
              />
            </div>

            <div className="flex space-x-2">
              <Button
                onClick={() => handleDecision('approve')}
                disabled={isSubmitting}
                className="flex-1"
              >
                {isSubmitting ? 'Submitting...' : 'Approve'}
              </Button>
              <Button
                onClick={() => handleDecision('reject')}
                disabled={isSubmitting}
                variant="destructive"
                className="flex-1"
              >
                {isSubmitting ? 'Submitting...' : 'Reject'}
              </Button>
            </div>
          </div>
        )}

        {myApproval && myApproval.status !== 'pending' && (
          <div className="bg-muted rounded-md p-4">
            <p className="text-sm font-medium">
              You have {myApproval.status} this request
            </p>
            {myApproval.decidedAt && (
              <p className="text-sm text-muted-foreground mt-1">
                on {new Date(myApproval.decidedAt).toLocaleString()}
              </p>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
