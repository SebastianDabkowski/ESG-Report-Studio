# Remediation Plan Feature Implementation

## Overview
This implementation adds comprehensive remediation plan functionality to the ESG Report Studio, allowing report managers to create structured plans for addressing missing or estimated data items in future reporting periods.

## Acceptance Criteria Met

### ✅ Criterion 1: Create Remediation Plan with Complete Details
**Requirement**: Given an item is missing or estimated, when I create a remediation plan, then I can set target period, owner, and actions.

**Implementation**:
- **RemediationPlan Model** includes:
  - `Title` and `Description`: Clear identification and explanation
  - `TargetPeriod`: When the gap should be resolved (e.g., "Q1 2026", "FY 2026")
  - `OwnerId` and `OwnerName`: Assigned responsibility
  - `Priority`: low, medium, high for prioritization
  - `Status`: planned, in-progress, completed, cancelled
  - Optional links to `GapId`, `AssumptionId`, or `DataPointId` for full traceability

- **RemediationAction Model** enables detailed action tracking:
  - `Title` and `Description`: Specific task details
  - `OwnerId` and `OwnerName`: Action responsibility
  - `DueDate`: Deadline for action completion
  - `Status`: pending, in-progress, completed, cancelled
  - `EvidenceIds`: Attachments supporting completion

### ✅ Criterion 2: Highlight Overdue and Upcoming Actions
**Requirement**: Given a remediation plan exists, when the target period is approaching, then the system highlights overdue or upcoming actions.

**Implementation**:
- **Overdue Badge**: Red badge displayed when action due date has passed
- **Due Soon Badge**: Yellow badge for actions within 14 days of due date
- **Date Normalization**: Timezone-safe comparison by normalizing dates to start of day
- **Visual Indicators**: Clear color-coded badges make status immediately visible
- **Status Filtering**: Completed and cancelled actions excluded from overdue checks

### ✅ Criterion 3: Mark Actions Done with Evidence and Completion Date
**Requirement**: Given a plan action is completed, when I mark it done, then completion date and evidence can be stored.

**Implementation**:
- **Complete Action Endpoint**: `POST /api/remediation-plans/actions/{id}/complete`
- Captures:
  - `CompletedBy`: User who completed the action
  - `CompletedAt`: Automatic ISO 8601 timestamp
  - `CompletionNotes`: Optional context about completion
  - `EvidenceIds`: Array of evidence IDs (invoices, meter readings, HR exports, etc.)
- **Evidence Validation**: Backend validates all evidence IDs exist before acceptance
- **Audit Trail**: Full tracking of who completed what and when

## Architecture

### Backend (.NET 9)

#### Models (`ReportingModels.cs`)
**RemediationPlan** (lines 1360-1424):
- Core fields: Id, SectionId, Title, Description, TargetPeriod
- Ownership: OwnerId, OwnerName
- Status tracking: Priority, Status, CompletedAt, CompletedBy
- Linkage: Optional GapId, AssumptionId, DataPointId
- Audit: CreatedBy, CreatedAt, UpdatedBy, UpdatedAt

**RemediationAction** (lines 1426-1492):
- Core fields: Id, RemediationPlanId, Title, Description
- Ownership: OwnerId, OwnerName, DueDate
- Completion: Status, CompletedAt, CompletedBy, CompletionNotes
- Evidence: EvidenceIds array
- Audit: CreatedBy, CreatedAt, UpdatedBy, UpdatedAt

**Request DTOs** (lines 1494-1615):
- CreateRemediationPlanRequest
- UpdateRemediationPlanRequest
- CompleteRemediationPlanRequest
- CreateRemediationActionRequest
- UpdateRemediationActionRequest
- CompleteRemediationActionRequest

#### Data Store (`InMemoryReportStore.cs`)
**Storage** (lines 26-27):
- `List<RemediationPlan> _remediationPlans`
- `List<RemediationAction> _remediationActions`

**Remediation Plan Methods** (lines 4013-4253):
- `GetRemediationPlans(sectionId?)`: List all or filter by section
- `GetRemediationPlanById(id)`: Retrieve single plan
- `CreateRemediationPlan(...)`: Create with validation
- `UpdateRemediationPlan(...)`: Update with validation
- `CompleteRemediationPlan(id, completedBy)`: Mark as complete
- `DeleteRemediationPlan(id)`: Delete plan and all actions

**Remediation Action Methods** (lines 4255-4472):
- `GetRemediationActions(remediationPlanId)`: List actions for a plan
- `GetRemediationActionById(id)`: Retrieve single action
- `CreateRemediationAction(...)`: Create with validation
- `UpdateRemediationAction(...)`: Update with validation
- `CompleteRemediationAction(id, completedBy, notes, evidenceIds)`: Mark complete with evidence
- `DeleteRemediationAction(id)`: Delete action

**Validation Rules**:
- Title, description, target period/due date required
- Owner ID and name required
- Priority must be low/medium/high
- Status must be valid enum value
- Due dates validated as valid date format
- Evidence IDs validated to exist
- References (gap, assumption, data point) validated if provided

#### Controller (`RemediationPlansController.cs`)
**Endpoints**:
- `GET /api/remediation-plans?sectionId={id}`: List plans
- `GET /api/remediation-plans/{id}`: Get specific plan
- `POST /api/remediation-plans`: Create plan
- `PUT /api/remediation-plans/{id}`: Update plan
- `POST /api/remediation-plans/{id}/complete`: Mark plan complete
- `DELETE /api/remediation-plans/{id}`: Delete plan
- `GET /api/remediation-plans/{planId}/actions`: List actions
- `GET /api/remediation-plans/actions/{id}`: Get specific action
- `POST /api/remediation-plans/actions`: Create action
- `PUT /api/remediation-plans/actions/{id}`: Update action
- `POST /api/remediation-plans/actions/{id}/complete`: Mark action complete
- `DELETE /api/remediation-plans/actions/{id}`: Delete action

### Frontend (React 19 + TypeScript)

#### Types (`types.ts`)
**RemediationPlan Interface**:
```typescript
interface RemediationPlan {
  id: string
  sectionId: string
  title: string
  description: string
  targetPeriod: string
  ownerId: string
  ownerName: string
  priority: 'low' | 'medium' | 'high'
  status: 'planned' | 'in-progress' | 'completed' | 'cancelled'
  gapId?: string
  assumptionId?: string
  dataPointId?: string
  completedAt?: string
  completedBy?: string
  createdBy: string
  createdAt: string
  updatedBy?: string
  updatedAt?: string
}
```

**RemediationAction Interface**:
```typescript
interface RemediationAction {
  id: string
  remediationPlanId: string
  title: string
  description: string
  ownerId: string
  ownerName: string
  dueDate: string
  status: 'pending' | 'in-progress' | 'completed' | 'cancelled'
  completedAt?: string
  completedBy?: string
  evidenceIds: string[]
  completionNotes?: string
  createdBy: string
  createdAt: string
  updatedBy?: string
  updatedAt?: string
}
```

#### API Layer (`api.ts`)
Complete set of API methods matching backend endpoints:
- Plan CRUD: create, update, complete, delete
- Action CRUD: create, update, complete, delete
- Proper TypeScript typing with payload interfaces
- Error handling and type safety

#### Components

**RemediationPlanForm** (`RemediationPlanForm.tsx`):
- Create and edit plans
- Owner selection dropdown from user list
- Target period input
- Priority selection (low/medium/high)
- Status selection (planned/in-progress/completed/cancelled) for edit mode
- React Hook Form + Zod validation
- Error handling

**RemediationActionForm** (`RemediationActionForm.tsx`):
- Create and edit actions
- Owner selection dropdown
- Due date picker
- Status selection for edit mode
- React Hook Form + Zod validation
- Date conversion to ISO format

**RemediationActionsList** (`RemediationActionsList.tsx`):
- Display all actions for a plan
- Status badges with color coding
- Overdue badge (red) for past-due actions
- Due Soon badge (yellow) for actions within 14 days
- Completed date display
- Completion notes display
- Mark complete, edit, delete actions
- Timezone-safe date comparisons
- Dialog integration for add/edit forms

**RemediationPlansList** (`RemediationPlansList.tsx`):
- Display all plans for a section
- Status and priority badges
- Expandable action lists
- Mark complete, edit, delete plans
- Dialog integration for add/edit forms
- Empty state handling

#### Integration (`DataCollectionWorkspace.tsx`)
- Added after SimplificationsList in section cards
- Passes section ID and user list
- Consistent UI pattern with other section components

## Key Features

### Flexible Traceability
- Plans can link to Gaps, Assumptions, or DataPoints
- Full audit trail from issue identification to resolution
- Optional references allow standalone plans

### Status Management
- Independent status tracking for plans and actions
- Clear status transitions with validation
- Visual status indicators throughout UI

### Evidence Support
- Evidence IDs attached to completed actions
- Reuses existing Evidence model for consistency
- Backend validation ensures evidence exists

### Due Date Tracking
- Clear visual indicators for overdue actions
- Upcoming actions highlighted 14 days in advance
- Timezone-safe date comparisons

### User Management
- Owner assignment from existing user list
- Denormalized names for performance
- Audit trail captures all actors

## Validation Rules

### Backend Validation
1. Title, description, target period required
2. Owner ID and name required
3. Priority must be low/medium/high
4. Status must be valid enum value
5. Due dates must be valid ISO format
6. Evidence IDs must exist
7. Cannot complete already completed items
8. References validated if provided

### Frontend Validation
- Zod schemas mirror backend rules
- Real-time form validation
- Owner selection ensures valid user
- Date picker ensures valid date format

## Migration Path

If moving from in-memory to database:
1. Create EF Core migrations for RemediationPlan and RemediationAction tables
2. Add indexes on SectionId, Status, DueDate
3. Add foreign key constraints to Gap, Assumption, DataPoint (nullable)
4. Update InMemoryReportStore methods to use DbContext
5. No changes needed to controller or frontend

## Future Enhancements

1. **Notifications**: Email/in-app alerts for approaching due dates
2. **Templates**: Common remediation action templates
3. **Recurrence**: Recurring remediation tasks
4. **Bulk Operations**: Assign multiple actions at once
5. **Analytics**: Remediation plan completion metrics
6. **Export**: Include remediation plans in report exports
7. **Workflow**: Approval workflow for plan changes
8. **Integration**: Auto-create plans from gap analysis

## Testing Results

### Backend
- ✅ All 234 existing tests pass
- ✅ Build succeeds with no errors
- ✅ Comprehensive validation prevents invalid states

### Frontend
- ✅ Build succeeds with no errors
- ✅ TypeScript strict mode compliance
- ✅ React hooks best practices followed
- ✅ No linting errors

## Security Considerations

### Input Validation
- All user input validated on both client and server
- XSS protection through React's built-in escaping
- No SQL injection risk (in-memory store)

### Authorization
- Controller uses User.Identity.Name for audit fields
- Ready for role-based access control integration
- Owner validation ensures users exist

### Data Integrity
- Referential integrity maintained through validation
- Cascade delete for actions when plan deleted
- Evidence validation prevents orphaned references
- Audit trail preserved

## Conclusion

This implementation provides a complete, production-ready remediation plan management system that meets all acceptance criteria. It enables report managers to:

1. Create structured plans for addressing missing data
2. Track actions with clear due dates and ownership
3. Monitor progress with visual indicators for overdue/upcoming tasks
4. Document completion with evidence and notes
5. Maintain full audit trail for accountability

The feature integrates seamlessly with existing gap and assumption management, providing a comprehensive approach to ESG data quality improvement.
