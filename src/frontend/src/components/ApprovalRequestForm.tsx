import React, { useState } from 'react';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { Textarea } from './ui/textarea';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from './ui/card';
import { Alert, AlertDescription } from './ui/alert';

interface ApprovalRequestFormProps {
  periodId: string;
  periodName: string;
  currentUserId: string;
  availableApprovers: Array<{ id: string; name: string; email: string }>;
  onSubmit: (request: CreateApprovalRequestRequest) => Promise<void>;
  onCancel: () => void;
}

interface CreateApprovalRequestRequest {
  periodId: string;
  requestedBy: string;
  approverIds: string[];
  requestMessage?: string;
  approvalDeadline?: string;
}

export function ApprovalRequestForm({
  periodId,
  periodName,
  currentUserId,
  availableApprovers,
  onSubmit,
  onCancel
}: ApprovalRequestFormProps) {
  const [selectedApprovers, setSelectedApprovers] = useState<string[]>([]);
  const [requestMessage, setRequestMessage] = useState('');
  const [approvalDeadline, setApprovalDeadline] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleApproverToggle = (approverId: string) => {
    setSelectedApprovers(prev =>
      prev.includes(approverId)
        ? prev.filter(id => id !== approverId)
        : [...prev, approverId]
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (selectedApprovers.length === 0) {
      setError('Please select at least one approver');
      return;
    }

    const request: CreateApprovalRequestRequest = {
      periodId,
      requestedBy: currentUserId,
      approverIds: selectedApprovers,
      requestMessage: requestMessage || undefined,
      approvalDeadline: approvalDeadline || undefined
    };

    setIsSubmitting(true);
    try {
      await onSubmit(request);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit approval request');
      setIsSubmitting(false);
    }
  };

  return (
    <Card className="w-full max-w-2xl">
      <CardHeader>
        <CardTitle>Request Approval</CardTitle>
        <CardDescription>
          Request approval for report period: {periodName}
        </CardDescription>
      </CardHeader>
      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-6">
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          <div className="space-y-2">
            <Label>Select Approvers *</Label>
            <div className="border rounded-md p-4 space-y-2 max-h-64 overflow-y-auto">
              {availableApprovers.length === 0 ? (
                <p className="text-sm text-muted-foreground">No approvers available</p>
              ) : (
                availableApprovers.map(approver => (
                  <div key={approver.id} className="flex items-center space-x-2">
                    <input
                      type="checkbox"
                      id={`approver-${approver.id}`}
                      checked={selectedApprovers.includes(approver.id)}
                      onChange={() => handleApproverToggle(approver.id)}
                      className="h-4 w-4 rounded border-gray-300"
                    />
                    <label
                      htmlFor={`approver-${approver.id}`}
                      className="text-sm font-medium leading-none cursor-pointer peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                    >
                      {approver.name} ({approver.email})
                    </label>
                  </div>
                ))
              )}
            </div>
            <p className="text-sm text-muted-foreground">
              Selected: {selectedApprovers.length} approver(s)
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="requestMessage">Message to Approvers (Optional)</Label>
            <Textarea
              id="requestMessage"
              value={requestMessage}
              onChange={(e) => setRequestMessage(e.target.value)}
              placeholder="Add any context or notes for the approvers..."
              rows={4}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="approvalDeadline">Approval Deadline (Optional)</Label>
            <Input
              id="approvalDeadline"
              type="datetime-local"
              value={approvalDeadline}
              onChange={(e) => setApprovalDeadline(e.target.value)}
            />
          </div>
        </CardContent>
        <CardFooter className="flex justify-end space-x-2">
          <Button
            type="button"
            variant="outline"
            onClick={onCancel}
            disabled={isSubmitting}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting || selectedApprovers.length === 0}>
            {isSubmitting ? 'Requesting...' : 'Request Approval'}
          </Button>
        </CardFooter>
      </form>
    </Card>
  );
}
