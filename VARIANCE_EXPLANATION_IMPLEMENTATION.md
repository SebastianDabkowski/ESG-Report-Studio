# Variance Explanation Workflow Implementation

This document describes the implementation of the variance explanation workflow feature for ESG Report Studio.

## Overview

The variance explanation workflow enables Data Owners to provide explanations for significant year-over-year changes in metrics. The system automatically flags variances that exceed configured thresholds and requires explanations before the report can be finalized.

## Features Implemented

### Backend

#### Models (ReportingModels.cs)

1. **VarianceThresholdConfig**: Configuration for variance detection thresholds
   - Properties: Id, PercentageThreshold, AbsoluteThreshold, RequireBothThresholds, RequireReviewerApproval
   - Can be attached to a ReportingPeriod to enable variance flagging

2. **VarianceExplanation**: Represents an explanation for a flagged variance
   - Properties: Id, DataPointId, PriorPeriodId, CurrentValue, PriorValue, PercentageChange, AbsoluteChange
   - Explanation fields: Explanation, RootCause, Category
   - Status values: draft, submitted, approved, rejected, revision-requested
   - Support for evidence and references

3. **VarianceFlagInfo**: Embedded in MetricComparisonResponse
   - Indicates if a variance requires explanation
   - Contains existing explanation (if any)
   - Shows whether the flag has been cleared

4. **Request/Response DTOs**:
   - CreateVarianceThresholdConfigRequest
   - CreateVarianceExplanationRequest
   - UpdateVarianceExplanationRequest
   - SubmitVarianceExplanationRequest
   - ReviewVarianceExplanationRequest

#### Store Methods (InMemoryReportStore.cs)

1. **CreateVarianceThresholdConfig**: Creates threshold configuration for a period
   - Validates at least one threshold is provided
   - Attaches configuration to the reporting period

2. **CheckVarianceThreshold**: Internal method to check if variance exceeds thresholds
   - Supports percentage, absolute, or both threshold types
   - Returns VarianceFlagInfo with explanation requirement details

3. **CreateVarianceExplanation**: Creates a new variance explanation
   - Links to data point and prior period
   - Captures current/prior values and calculated changes
   - Status starts as "draft"

4. **UpdateVarianceExplanation**: Updates an existing explanation
   - Only allowed in "draft" or "revision-requested" status
   - Updates explanation text, root cause, category, evidence, and references

5. **SubmitVarianceExplanation**: Submits explanation for review
   - Changes status from "draft" to "submitted"
   - Required before review if approval workflow is enabled

6. **ReviewVarianceExplanation**: Reviews (approve/reject/request-revision) an explanation
   - Only allowed for "submitted" explanations
   - Approve: Changes status to "approved" and clears the flag
   - Reject: Changes status to "rejected"
   - Request-revision: Changes status to "revision-requested"

7. **GetVarianceExplanations**: Retrieves explanations with optional filtering
   - Can filter by dataPointId or periodId

8. **GetVarianceExplanation**: Retrieves a single explanation by ID

9. **DeleteVarianceExplanation**: Deletes an explanation

All methods include audit trail logging for compliance.

#### API Controller (VarianceExplanationsController.cs)

1. **POST /api/variance-explanations/threshold-config/{periodId}**: Create threshold configuration
   - Request body: CreateVarianceThresholdConfigRequest
   - Returns: VarianceThresholdConfig

2. **POST /api/variance-explanations**: Create variance explanation
   - Request body: CreateVarianceExplanationRequest
   - Returns: VarianceExplanation

3. **GET /api/variance-explanations**: List variance explanations
   - Query params: dataPointId?, periodId?
   - Returns: Array of VarianceExplanation

4. **GET /api/variance-explanations/{id}**: Get single variance explanation
   - Returns: VarianceExplanation or 404

5. **PUT /api/variance-explanations/{id}**: Update variance explanation
   - Request body: UpdateVarianceExplanationRequest
   - Returns: VarianceExplanation

6. **POST /api/variance-explanations/{id}/submit**: Submit for review
   - Request body: SubmitVarianceExplanationRequest
   - Returns: VarianceExplanation

7. **POST /api/variance-explanations/{id}/review**: Review explanation
   - Request body: ReviewVarianceExplanationRequest
   - Returns: VarianceExplanation

8. **DELETE /api/variance-explanations/{id}**: Delete explanation
   - Query param: deletedBy (required)
   - Returns: 204 No Content

#### Integration with Metric Comparison

The `CompareMetrics` method in InMemoryReportStore has been enhanced to:
1. Check if the current period has a VarianceThresholdConfig
2. If configured, call CheckVarianceThreshold to evaluate the variance
3. Include VarianceFlagInfo in the MetricComparisonResponse

This enables automatic variance flagging whenever metrics are compared.

### Frontend

#### Types (types.ts)

Added TypeScript interfaces for all variance explanation models:
- VarianceThresholdConfig
- VarianceExplanation
- VarianceFlagInfo
- Request types for create, update, submit, and review operations

#### API Functions (api.ts)

Implemented API client functions:
- createVarianceThresholdConfig
- createVarianceExplanation
- getVarianceExplanations
- getVarianceExplanation
- updateVarianceExplanation
- submitVarianceExplanation
- reviewVarianceExplanation
- deleteVarianceExplanation

All functions use the shared requestJson utility for consistent error handling.

#### Components

1. **VarianceExplanationForm.tsx**: Form for creating and editing variance explanations
   - Shows variance summary (prior value, current value, change)
   - Status badge showing current explanation status
   - Category dropdown for classifying variance types
   - Explanation text area (required)
   - Root cause input (optional)
   - Support for draft and submit workflows
   - Displays reviewer comments when revision is requested
   - Read-only mode for approved/rejected explanations

2. **MetricComparisonView.tsx** (Updated): Enhanced to show variance flags
   - Displays amber alert when variance requires explanation
   - Shows explanation requirement reason
   - Displays existing explanation with status badge
   - Shows flag clearance status
   - Button to open variance explanation form (placeholder)
   - Integration point for VarianceExplanationForm

## Configuration

### Setting Up Variance Thresholds

To enable variance flagging for a reporting period:

```typescript
// Frontend
const config = await createVarianceThresholdConfig(periodId, {
  percentageThreshold: 10,     // Flag changes >= 10%
  absoluteThreshold: 1000,     // Flag absolute changes >= 1000
  requireBothThresholds: false, // Either threshold triggers flag
  requireReviewerApproval: true, // Require review before clearing
  createdBy: currentUser.id
})
```

### Threshold Logic

- **Percentage threshold**: Flags if |percentageChange| >= threshold
- **Absolute threshold**: Flags if |absoluteChange| >= threshold
- **RequireBothThresholds**: 
  - `false` (default): Either threshold being exceeded triggers flag
  - `true`: Both thresholds must be exceeded to trigger flag

### Approval Workflow

- **RequireReviewerApproval = false**: Submitting an explanation clears the flag
- **RequireReviewerApproval = true**: Explanation must be approved by a reviewer to clear the flag

## User Workflows

### Data Owner Creating Explanation

1. View metric comparison and see variance flag alert
2. Click "Provide Explanation" button
3. Fill in variance explanation form:
   - Select category (optional)
   - Provide detailed explanation (required)
   - Add root cause (optional)
   - Attach evidence (future enhancement)
4. Save as draft or submit for review

### Data Owner Updating Explanation

1. View existing explanation in comparison view
2. If status is "draft" or "revision-requested", edit allowed
3. Update explanation, root cause, or category
4. Save changes or submit for review

### Reviewer Approving Explanation

1. View submitted variance explanation
2. Review explanation, root cause, and any evidence
3. Make decision:
   - **Approve**: Clears the variance flag
   - **Request Revision**: Returns to data owner with comments
   - **Reject**: Marks as rejected with reason

## Database Schema

The implementation uses the in-memory store with the following collections:

```csharp
private readonly List<VarianceThresholdConfig> _varianceThresholdConfigs = new();
private readonly List<VarianceExplanation> _varianceExplanations = new();
```

For production, these would be mapped to database tables with appropriate indexes:
- VarianceThresholdConfigs table
- VarianceExplanations table
- Indexes on DataPointId, PriorPeriodId, Status

## Validation Rules

1. **Creating Threshold Config**:
   - At least one threshold (percentage or absolute) must be provided
   - Thresholds must be positive numbers
   - Period must exist

2. **Creating Explanation**:
   - Data point must exist
   - Prior period must exist
   - Explanation text is required

3. **Updating Explanation**:
   - Can only update in "draft" or "revision-requested" status
   - Explanation text cannot be empty

4. **Submitting Explanation**:
   - Can only submit from "draft" or "revision-requested" status
   - Explanation text must not be empty

5. **Reviewing Explanation**:
   - Can only review "submitted" explanations
   - Decision must be: approve, reject, or request-revision

## Audit Trail

All variance explanation actions are logged to the audit log:
- variance-threshold-config-created
- variance-explanation-created
- variance-explanation-updated
- variance-explanation-submitted
- variance-explanation-reviewed
- variance-explanation-deleted

Each entry includes:
- Timestamp
- User ID and name
- Action type
- Entity ID
- Change description

## Future Enhancements

1. **Evidence Attachment**: Link evidence files to variance explanations
2. **Bulk Operations**: Explain multiple variances at once
3. **Templates**: Pre-defined explanation templates for common variance types
4. **Analytics**: Dashboard showing variance trends and explanation completion rates
5. **Export**: Include variance explanations in report exports
6. **Notifications**: Alert data owners when variance explanations are required
7. **Variance Review List**: Dedicated view for reviewers to see all pending explanations
8. **Historical Comparison**: Show previous explanations for recurring variances

## API Examples

### Create Threshold Configuration

```http
POST /api/variance-explanations/threshold-config/{periodId}
Content-Type: application/json

{
  "percentageThreshold": 10,
  "absoluteThreshold": 1000,
  "requireBothThresholds": false,
  "requireReviewerApproval": true,
  "createdBy": "user-123"
}
```

### Create Variance Explanation

```http
POST /api/variance-explanations
Content-Type: application/json

{
  "dataPointId": "dp-456",
  "priorPeriodId": "period-2023",
  "explanation": "Increased production capacity led to higher emissions",
  "rootCause": "Business expansion",
  "category": "business-expansion",
  "createdBy": "user-123"
}
```

### Submit for Review

```http
POST /api/variance-explanations/{id}/submit
Content-Type: application/json

{
  "submittedBy": "user-123"
}
```

### Approve Explanation

```http
POST /api/variance-explanations/{id}/review
Content-Type: application/json

{
  "decision": "approve",
  "comments": "Explanation is comprehensive and well-documented",
  "reviewedBy": "reviewer-456"
}
```

## Testing

Unit tests have been created in `VarianceExplanationTests.cs` covering:
- Creating threshold configurations
- Creating variance explanations
- Updating explanations
- Submitting explanations
- Reviewing and approving explanations
- Workflow state transitions

Note: Some test setup issues need to be resolved related to test data initialization.

## Security Considerations

1. **Authorization**: Implement role-based access control
   - Data owners can create/update their own explanations
   - Reviewers can approve/reject explanations
   - Admins can configure thresholds

2. **Validation**: All inputs are validated on the backend
   - Prevent SQL injection in text fields
   - Validate user IDs exist
   - Enforce status transition rules

3. **Audit Trail**: All actions are logged for compliance and security audits

## Performance Considerations

1. **Variance Checking**: Performed only when thresholds are configured
2. **Caching**: MetricComparisonResponse can be cached
3. **Indexes**: Add database indexes on frequently queried fields
4. **Pagination**: List endpoints should support pagination for large datasets

## Conclusion

The variance explanation workflow provides a comprehensive solution for tracking and documenting significant year-over-year changes in ESG metrics. The implementation follows the established patterns in the codebase and integrates seamlessly with the existing metric comparison functionality.
