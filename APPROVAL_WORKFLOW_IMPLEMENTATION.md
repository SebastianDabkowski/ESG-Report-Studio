# Approval Workflow Implementation

This document describes the implementation of the review and sign-off approval workflow feature for ESG Report Studio.

## Overview

The approval workflow enables Report Owners to request formal approval for report versions, with traceable approval records from designated approvers.

## Features Implemented

### Backend

#### Models (ReportingModels.cs)

1. **ApprovalRequest**: Represents an approval request workflow for a reporting period
   - Properties: Id, PeriodId, RequestedBy, RequestedAt, RequestMessage, ApprovalDeadline, Status, Approvals
   - Status values: pending, approved, rejected, cancelled

2. **ApprovalRecord**: Individual approval record from one approver
   - Properties: Id, ApprovalRequestId, ApproverId, ApproverName, Status, Decision, DecidedAt, Comment
   - Status values: pending, approved, rejected

3. **CreateApprovalRequestRequest**: Request DTO for creating an approval request
   - Specifies periodId, requestedBy, approverIds, optional requestMessage and approvalDeadline

4. **SubmitApprovalDecisionRequest**: Request DTO for submitting an approval decision
   - Specifies approvalRecordId, decision (approve/reject), optional comment, decidedBy

5. **PublishReportResult**: Extended to include ApprovalRequest reference
   - Links publication to the approval record for traceability

#### Store Methods (InMemoryReportStore.cs)

1. **CreateApprovalRequest**: Creates a new approval request with records for each approver
   - Validates period and approvers exist
   - Creates approval records for each approver
   - Logs action to audit trail

2. **SubmitApprovalDecision**: Records an approval decision
   - Validates approver authorization
   - Updates approval record with decision and timestamp
   - Updates overall approval request status when all approvers have responded
   - Logs action to audit trail

3. **GetApprovalRequests**: Retrieves approval requests with optional filtering by period or approver

4. **GetApprovalRequest**: Retrieves a specific approval request by ID

#### Notifications (INotificationService.cs, OwnerAssignmentNotificationService.cs)

1. **SendApprovalRequestNotificationAsync**: Sends notifications to all approvers when approval is requested
   - Includes request message, deadline, and requester information
   - Creates in-app notifications and sends emails

2. **SendApprovalDecisionNotificationAsync**: Notifies requester when an approval decision is made
   - Includes approver's decision, comment, and overall status
   - Shows progress (e.g., "2 of 3 approvers have responded")

#### API Controller (ApprovalsController.cs)

1. **POST /api/approvals/requests**: Create a new approval request
   - Validates request and sends notifications to approvers
   - Returns created ApprovalRequest

2. **POST /api/approvals/decisions**: Submit an approval decision
   - Validates authorization and records decision
   - Sends notification to requester
   - Returns updated ApprovalRecord

3. **GET /api/approvals/requests**: Get all approval requests
   - Optional filters: periodId, approverId
   - Returns list of ApprovalRequest

4. **GET /api/approvals/requests/{id}**: Get specific approval request
   - Returns single ApprovalRequest or 404

### Frontend

#### Components

1. **ApprovalRequestForm.tsx**: Form for requesting approval
   - Select multiple approvers from available users
   - Add optional message to approvers
   - Set optional approval deadline
   - Validates at least one approver is selected

2. **ApprovalReviewPanel.tsx**: Panel for approvers to review and decide
   - Shows approval request details and message
   - Lists all approvers and their status
   - Allows pending approvers to approve or reject with optional comment
   - Shows decision confirmation for completed approvals

3. **ApprovalHistoryView.tsx**: View of all approval requests
   - Lists all approval requests for a period
   - Shows status summary (approved/rejected/pending counts)
   - Displays individual approver decisions
   - Supports click-through to detailed view

## User Flows

### Requesting Approval

1. Report Owner reviews the report and determines it's ready for approval
2. Report Owner navigates to approval section
3. Report Owner fills out ApprovalRequestForm:
   - Selects one or more approvers
   - Adds context message (optional)
   - Sets deadline (optional)
4. System creates approval request and sends notifications to all approvers
5. Approvers receive email and in-app notifications

### Approving/Rejecting

1. Approver receives notification about approval request
2. Approver navigates to approval review panel
3. Approver reviews:
   - Report period information
   - Request message from Report Owner
   - Current approval status from other approvers
4. Approver adds optional comment
5. Approver clicks "Approve" or "Reject"
6. System records decision with timestamp
7. System sends notification to Report Owner
8. System updates overall approval status if all approvers have responded

### Publishing with Approval

1. Report Owner initiates publication
2. System checks for approved ApprovalRequest
3. PublishReportResult includes reference to ApprovalRequest
4. Published artifact is linked to approval record for audit trail

## Audit Trail

All approval actions are logged to the audit trail:

- Approval request creation: Records who requested, when, and for which period
- Approval decisions: Records who approved/rejected, when, and with what comment
- Each entry includes userId, userName, timestamp, and change note

## Notification Types

New notification types added:

- `approval-requested`: Sent to approvers when approval is requested
- `approval-approved`: Sent to requester when an approver approves
- `approval-rejected`: Sent to requester when an approver rejects

## API Endpoints

### Create Approval Request
```
POST /api/approvals/requests
Content-Type: application/json

{
  "periodId": "string",
  "requestedBy": "string",
  "approverIds": ["string"],
  "requestMessage": "string (optional)",
  "approvalDeadline": "ISO 8601 datetime (optional)"
}

Response: 200 OK with ApprovalRequest
```

### Submit Approval Decision
```
POST /api/approvals/decisions
Content-Type: application/json

{
  "approvalRecordId": "string",
  "decision": "approve" | "reject",
  "comment": "string (optional)",
  "decidedBy": "string"
}

Response: 200 OK with ApprovalRecord
```

### Get Approval Requests
```
GET /api/approvals/requests?periodId={id}&approverId={id}

Response: 200 OK with ApprovalRequest[]
```

### Get Approval Request
```
GET /api/approvals/requests/{id}

Response: 200 OK with ApprovalRequest or 404 Not Found
```

## Acceptance Criteria Met

✅ **Given a report version is ready, when I request approval, then assigned approvers receive a notification and a review task.**
- Implemented via CreateApprovalRequest and SendApprovalRequestNotificationAsync
- Approvers receive both in-app notifications and emails

✅ **Given an approver, when they approve or reject, then the decision is recorded with timestamp and optional comment.**
- Implemented via SubmitApprovalDecision
- Records decision, timestamp (DecidedAt), and optional comment
- Logged to audit trail

✅ **Given all required approvals are collected, when I publish, then the published artifact references the approval record.**
- PublishReportResult extended to include ApprovalRequest property
- Provides traceability from publication to approval decisions

## MVP Features

The implementation supports a simple approval workflow:
- Parallel approvals (all approvers notified at once)
- Overall status becomes "approved" only when all approve
- Overall status becomes "rejected" if any approver rejects
- Individual approvers can approve/reject independently

## Future Enhancements

Potential improvements for future iterations:
- Sequential approval chains (approve in specific order)
- Conditional approval paths (different approvers based on conditions)
- Approval delegation (approver can delegate to another user)
- Approval reminders (automatic reminders for pending approvals)
- Approval withdrawal (requester can cancel pending approval request)
- Re-approval when content changes after approval
