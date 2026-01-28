import React from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card';
import { Badge } from './ui/badge';
import { CheckCircle, XCircle, Clock, FileCheck } from 'lucide-react';

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

interface ApprovalHistoryViewProps {
  approvalRequests: ApprovalRequest[];
  periodName: string;
  onSelectRequest?: (requestId: string) => void;
}

export function ApprovalHistoryView({
  approvalRequests,
  periodName,
  onSelectRequest
}: ApprovalHistoryViewProps) {
  const getStatusBadge = (status: string) => {
    const variant = status === 'approved' ? 'default' : status === 'rejected' ? 'destructive' : 'secondary';
    return (
      <Badge variant={variant}>
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </Badge>
    );
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'approved':
        return <CheckCircle className="h-5 w-5 text-green-500" />;
      case 'rejected':
        return <XCircle className="h-5 w-5 text-red-500" />;
      default:
        return <Clock className="h-5 w-5 text-yellow-500" />;
    }
  };

  const getApprovalSummary = (approvals: ApprovalRecord[]) => {
    const total = approvals.length;
    const approved = approvals.filter(a => a.status === 'approved').length;
    const rejected = approvals.filter(a => a.status === 'rejected').length;
    const pending = approvals.filter(a => a.status === 'pending').length;

    return { total, approved, rejected, pending };
  };

  if (approvalRequests.length === 0) {
    return (
      <Card className="w-full">
        <CardHeader>
          <CardTitle>Approval History</CardTitle>
          <CardDescription>Report Period: {periodName}</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <FileCheck className="h-12 w-12 text-muted-foreground mb-4" />
            <p className="text-lg font-medium text-muted-foreground">No approval requests yet</p>
            <p className="text-sm text-muted-foreground mt-2">
              Approval requests will appear here once created
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="w-full">
      <CardHeader>
        <CardTitle>Approval History</CardTitle>
        <CardDescription>
          Report Period: {periodName} • {approvalRequests.length} request(s)
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {approvalRequests.map(request => {
            const summary = getApprovalSummary(request.approvals);
            return (
              <div
                key={request.id}
                className={`border rounded-lg p-4 ${
                  onSelectRequest ? 'cursor-pointer hover:bg-muted/50 transition-colors' : ''
                }`}
                onClick={() => onSelectRequest?.(request.id)}
              >
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center space-x-3">
                    {getStatusIcon(request.status)}
                    <div>
                      <div className="font-medium">
                        Approval Request
                      </div>
                      <div className="text-sm text-muted-foreground">
                        Requested: {new Date(request.requestedAt).toLocaleString()}
                      </div>
                    </div>
                  </div>
                  {getStatusBadge(request.status)}
                </div>

                {request.requestMessage && (
                  <div className="mb-3 text-sm text-muted-foreground italic">
                    "{request.requestMessage}"
                  </div>
                )}

                <div className="flex items-center space-x-4 text-sm">
                  <div className="flex items-center space-x-1">
                    <span className="font-medium">Approvers:</span>
                    <span>{summary.total}</span>
                  </div>
                  {summary.approved > 0 && (
                    <div className="flex items-center space-x-1 text-green-600">
                      <CheckCircle className="h-4 w-4" />
                      <span>{summary.approved} Approved</span>
                    </div>
                  )}
                  {summary.rejected > 0 && (
                    <div className="flex items-center space-x-1 text-red-600">
                      <XCircle className="h-4 w-4" />
                      <span>{summary.rejected} Rejected</span>
                    </div>
                  )}
                  {summary.pending > 0 && (
                    <div className="flex items-center space-x-1 text-yellow-600">
                      <Clock className="h-4 w-4" />
                      <span>{summary.pending} Pending</span>
                    </div>
                  )}
                </div>

                {request.approvalDeadline && (
                  <div className="mt-2 text-sm text-muted-foreground">
                    Deadline: {new Date(request.approvalDeadline).toLocaleString()}
                  </div>
                )}

                {/* Show individual approvals */}
                <div className="mt-3 pt-3 border-t space-y-2">
                  {request.approvals.map(approval => (
                    <div key={approval.id} className="flex items-center justify-between text-sm">
                      <div className="flex items-center space-x-2">
                        {approval.status === 'approved' && <CheckCircle className="h-4 w-4 text-green-500" />}
                        {approval.status === 'rejected' && <XCircle className="h-4 w-4 text-red-500" />}
                        {approval.status === 'pending' && <Clock className="h-4 w-4 text-yellow-500" />}
                        <span>{approval.approverName}</span>
                      </div>
                      <div className="text-muted-foreground">
                        {approval.decidedAt 
                          ? new Date(approval.decidedAt).toLocaleDateString()
                          : 'Pending'}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
