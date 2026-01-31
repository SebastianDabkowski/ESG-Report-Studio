# Periodic Access Review Implementation

## Overview

This document describes the implementation of the periodic access review feature for the ESG Report Studio. This feature enables Compliance Officers to periodically review user access, roles, and section scopes to ensure role assignments remain appropriate over time.

## Feature Components

### 1. Data Models (ReportingModels.cs)

#### AccessReview
Represents a complete access review session.
- **Id**: Unique identifier
- **Title**: Name of the review (e.g., "Q1 2024 Access Review")
- **Description**: Optional purpose description
- **Status**: "in-progress", "completed", or "exported"
- **StartedAt/StartedBy**: Audit trail for review initiation
- **CompletedAt/CompletedBy**: Audit trail for review completion
- **Entries**: List of user access review entries
- **Summary**: Statistics about the review

#### AccessReviewEntry
Represents a single user's access assignment in the review.
- **UserId/UserName/UserEmail**: User identification
- **IsActive**: User status
- **RoleIds/RoleNames**: Assigned roles
- **SectionScopes**: Section access (owned + explicit grants)
- **OwnedPeriodIds**: Reporting periods owned by user
- **Decision**: "pending", "retain", or "revoke"
- **DecisionAt/DecisionBy**: Audit trail for decision

#### AccessReviewSectionScope
Details about section-level access in the review.
- **SectionId/SectionTitle**: Section identification
- **AccessType**: "owner" or "explicit-grant"
- **GrantedAt**: When access was granted

#### AccessReviewLogEntry
Audit trail entry for review actions.
- **Action**: "review-started", "decision-recorded", "access-revoked", "review-completed"
- **Timestamp/UserId/UserName**: Who and when
- **Details**: Action description
- **RelatedUserId**: User affected by action
- **Decision/Note**: Additional context

### 2. Store Methods (InMemoryReportStore.cs)

#### StartAccessReview(StartAccessReviewRequest)
Initiates a new access review by:
1. Creating entries for all users in the system
2. Capturing current roles and permissions
3. Listing section scopes (owned + explicit grants)
4. Identifying owned reporting periods
5. Logging the review start

#### RecordReviewDecision(reviewId, RecordReviewDecisionRequest)
Records a review decision for a user:
- For "retain": Updates decision metadata
- For "revoke":
  - Removes all user roles
  - Removes all section access grants
  - Deactivates the user
  - Logs the revocation with details
  - Updates summary statistics

#### CompleteAccessReview(reviewId, CompleteAccessReviewRequest)
Finalizes the review:
- Marks status as "completed"
- Records completion timestamp and reviewer
- Prevents further modifications
- Logs the completion

#### GetAccessReviewLog(reviewId)
Retrieves the complete audit trail for a review.

### 3. API Endpoints (AccessReviewController.cs)

#### POST /api/access-reviews
Start a new access review.

**Request:**
```json
{
  "title": "Q1 2024 Access Review",
  "description": "Quarterly access review",
  "startedBy": "compliance-officer-id",
  "startedByName": "Compliance Officer"
}
```

**Response:** AccessReview object with all user entries

#### GET /api/access-reviews
List all access reviews (newest first).

#### GET /api/access-reviews/{id}
Get details of a specific review including all entries.

#### POST /api/access-reviews/{id}/decisions
Record a review decision.

**Request:**
```json
{
  "entryId": "entry-guid",
  "decision": "retain" or "revoke",
  "decisionNote": "Optional justification",
  "decisionBy": "reviewer-id",
  "decisionByName": "Reviewer Name"
}
```

#### POST /api/access-reviews/{id}/complete
Complete the review (prevents further changes).

**Request:**
```json
{
  "completedBy": "compliance-officer-id",
  "completedByName": "Compliance Officer"
}
```

#### GET /api/access-reviews/{id}/log
Get audit trail for the review (all actions with timestamps).

#### GET /api/access-reviews/{id}/export/csv
Export review results as CSV with:
- User ID, Name, Email
- Active status
- Assigned roles
- Section count
- Owned periods
- Decision, timestamp, reviewer, note

#### GET /api/access-reviews/{id}/export/json
Export complete review data including audit log as JSON.

## Acceptance Criteria Compliance

### AC1: Review Initiation
✅ **Given** an access review period is configured, **when** the review starts, **then** the system lists all active users, roles, and report/section scopes.

**Implementation**: `StartAccessReview` creates entries for all users, capturing:
- User details (ID, name, email, active status)
- All assigned roles (RoleIds and RoleNames)
- Section scopes (owned sections + explicit grants)
- Owned reporting periods

### AC2: Access Revocation
✅ **Given** I revoke access during review, **when** I confirm, **then** permissions are removed and recorded in an access-review log.

**Implementation**: `RecordReviewDecision` with decision="revoke":
- Removes all user roles from RoleIds
- Removes all section access grants
- Deactivates the user (IsActive = false)
- Logs to AccessReviewLog with action="access-revoked"
- Includes details about removed roles and grants

### AC3: Export Results
✅ **Given** the review ends, **when** I export the review results, **then** the export includes decisions, reviewers, and timestamps.

**Implementation**: Export endpoints provide:
- CSV format: All user entries with decisions, decision timestamps, reviewer names, and notes
- JSON format: Complete review object with all entries and full audit log
- Audit log endpoint: Separate access to timestamped action log with reviewers

## Usage Example

### 1. Start Review
```bash
POST /api/access-reviews
{
  "title": "Q1 2024 Access Review",
  "startedBy": "compliance-officer",
  "startedByName": "Jane Compliance"
}
```

### 2. Review Users and Make Decisions
```bash
# Retain access
POST /api/access-reviews/{reviewId}/decisions
{
  "entryId": "entry-1",
  "decision": "retain",
  "decisionNote": "User still needs access for reporting",
  "decisionBy": "compliance-officer",
  "decisionByName": "Jane Compliance"
}

# Revoke access
POST /api/access-reviews/{reviewId}/decisions
{
  "entryId": "entry-2",
  "decision": "revoke",
  "decisionNote": "User left organization",
  "decisionBy": "compliance-officer",
  "decisionByName": "Jane Compliance"
}
```

### 3. Complete Review
```bash
POST /api/access-reviews/{reviewId}/complete
{
  "completedBy": "compliance-officer",
  "completedByName": "Jane Compliance"
}
```

### 4. Export Results
```bash
# CSV export
GET /api/access-reviews/{reviewId}/export/csv

# JSON export with audit log
GET /api/access-reviews/{reviewId}/export/json

# Audit trail only
GET /api/access-reviews/{reviewId}/log
```

## Testing

All functionality is covered by unit tests in `AccessReviewTests.cs`:
- ✅ Review initiation with user/role/scope capture
- ✅ Recording retain decisions
- ✅ Recording revoke decisions with access removal
- ✅ User deactivation on revocation
- ✅ Invalid decision rejection
- ✅ Review completion
- ✅ Preventing decisions on completed reviews
- ✅ Audit log generation
- ✅ Summary statistics calculation

**Test Results**: 11/11 passing

## Notes

### MVP Scope
As per requirements, this implementation supports:
- ✅ Manual review initiation
- ✅ Manual decision recording
- ✅ Export functionality
- ❌ Automated reminders (can be added later)
- ❌ Scheduled reviews (can be added later)

### Security Considerations
- All actions are logged with user ID and timestamp
- Access revocation is immediate and logged
- Review completion prevents further modifications
- Audit trail is immutable (append-only)

### Future Enhancements
Potential additions outside MVP scope:
1. Automated reminder emails for pending reviews
2. Scheduled review creation
3. Review templates with predefined criteria
4. Bulk decision operations
5. Review analytics and trends
6. Integration with external identity providers
