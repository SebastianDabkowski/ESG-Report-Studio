# Assumption Management Implementation Summary

## Overview
This implementation adds comprehensive assumption management functionality to the ESG Report Studio, allowing users to record, track, and manage assumptions used in ESG calculations and narratives.

## Acceptance Criteria Met

### ✅ Criterion 1: Complete Assumption Data Model
**Requirement**: Given a disclosure item, when I add an assumption, then it includes title, description, scope, and validity period.

**Implementation**:
- Enhanced `Assumption` model with all required fields:
  - `Title`: Brief descriptive title
  - `Description`: Detailed explanation
  - `Scope`: Organizational/operational scope (e.g., "Company-wide", "Specific facility")
  - `ValidityStartDate` and `ValidityEndDate`: Define the temporal scope
  - `Methodology`: How the assumption was derived
  - `Limitations`: Known constraints or uncertainties
- Frontend form validates all required fields
- Backend enforces validation rules

### ✅ Criterion 2: Multi-Disclosure Linking with Version Tracking
**Requirement**: Given an assumption is linked to multiple disclosures, when I update the assumption, then all linked disclosures reflect the latest version reference.

**Implementation**:
- `LinkedDataPointIds` array tracks all data points using this assumption
- `Version` field automatically increments on each update
- `UpdatedBy` and `UpdatedAt` provide audit trail
- Single source of truth - updating one assumption updates all references
- No need for manual syncing across disclosures

### ✅ Criterion 3: Deprecation with Replacement or Justification
**Requirement**: Given an assumption is deprecated, when I mark it as 'Invalid', then the system requires a replacement or justification.

**Implementation**:
- `Status` field supports: 'active', 'deprecated', 'invalid'
- Deprecation workflow enforces business rules:
  - Must provide either `ReplacementAssumptionId` OR `DeprecationJustification`
  - Cannot update assumptions with status != 'active'
  - Cannot delete assumptions used as replacements
- Frontend provides dedicated deprecation dialog with validation
- Backend validates the deprecation request

## Architecture

### Backend (.NET 9)

#### Models
**File**: `src/backend/Application/ARP.ESG_ReportStudio.API/Reporting/ReportingModels.cs`
- `Assumption` class (lines 668-702): Complete data model
- `CreateAssumptionRequest` (lines 724-737): Creation payload
- `UpdateAssumptionRequest` (lines 742-754): Update payload
- `DeprecateAssumptionRequest` (lines 759-767): Deprecation payload

#### Data Store
**File**: `src/backend/Application/ARP.ESG_ReportStudio.API/Reporting/InMemoryReportStore.cs`
- `GetAssumptions(sectionId?)`: Retrieve all or filtered assumptions
- `GetAssumptionById(id)`: Retrieve single assumption
- `CreateAssumption(...)`: Create with validation
- `UpdateAssumption(...)`: Update with version increment
- `DeprecateAssumption(...)`: Deprecate with replacement/justification
- `LinkAssumptionToDataPoint(...)`: Link to data point
- `UnlinkAssumptionFromDataPoint(...)`: Unlink from data point
- `DeleteAssumption(id)`: Delete with protection

#### Controller
**File**: `src/backend/Application/ARP.ESG_ReportStudio.API/Controllers/AssumptionsController.cs`
- `GET /api/assumptions?sectionId={id}`: List assumptions
- `GET /api/assumptions/{id}`: Get specific assumption
- `POST /api/assumptions`: Create assumption
- `PUT /api/assumptions/{id}`: Update assumption
- `POST /api/assumptions/{id}/deprecate`: Deprecate assumption
- `POST /api/assumptions/{id}/link`: Link to data point
- `POST /api/assumptions/{id}/unlink`: Unlink from data point
- `DELETE /api/assumptions/{id}`: Delete assumption

### Frontend (React 19 + TypeScript)

#### Types
**File**: `src/frontend/src/lib/types.ts`
- Updated `Assumption` interface with all new fields
- Proper TypeScript typing for status literals

#### API Layer
**File**: `src/frontend/src/lib/api.ts`
- Complete set of API methods matching backend endpoints
- Proper error handling and type safety
- Request/response payload interfaces

#### Components

**AssumptionForm** (`src/frontend/src/components/AssumptionForm.tsx`)
- Create and edit assumptions
- Zod schema validation
- Date validation (end date must be after start date)
- Required field enforcement
- Success/error feedback

**AssumptionsList** (`src/frontend/src/components/AssumptionsList.tsx`)
- Display all assumptions for a section
- Status badges (active, deprecated, invalid)
- Version indicators
- Linked data point count
- Edit, deprecate, delete actions
- Empty state handling
- Error handling

**DeprecateAssumptionDialog** (`src/frontend/src/components/DeprecateAssumptionDialog.tsx`)
- Modal dialog for deprecation workflow
- Radio selection: provide replacement OR mark as invalid
- Dropdown of available replacement assumptions
- Justification textarea
- Validation ensures one or the other is provided
- Clear error messaging

#### Integration
**File**: `src/frontend/src/components/DataCollectionWorkspace.tsx`
- Integrated `AssumptionsList` into each section card
- Positioned after gaps section with border separator
- Automatically loads assumptions for each section

## Key Features

### Reusability
- Single assumption can be linked to multiple data points
- No duplication of assumption data
- Centralized management

### Version Tracking
- Automatic version increment on updates
- Audit trail with updatedBy and updatedAt
- All linked data points reference latest version automatically

### Validity Period
- Start and end dates define temporal scope
- Supports carry-over between reporting years
- Enables assumption lifecycle management

### Status Management
- **Active**: Normal operational state, can be edited and linked
- **Deprecated**: Replaced by another assumption, read-only
- **Invalid**: Marked as no longer valid with justification, read-only

### Data Integrity
- Cannot delete assumptions used as replacements
- Cannot edit non-active assumptions
- Required field validation on both frontend and backend
- Methodology field is now validated as required

## Validation Rules

### Backend Validation
1. Title, Description, Scope required (non-empty)
2. Validity start and end dates required (valid date format)
3. End date must be after start date
4. Methodology required (non-empty)
5. Linked data points must exist
6. Cannot update non-active assumptions
7. Deprecation requires replacement OR justification
8. Replacement assumption must be active
9. Cannot delete if used as replacement

### Frontend Validation
1. All backend validations mirrored in Zod schema
2. Date picker ensures valid date selection
3. Conditional validation based on deprecation choice
4. Trim whitespace from string inputs
5. Empty string vs undefined handling for optional fields

## Testing Results

### Backend
- ✅ All 234 existing tests pass
- ✅ Build succeeds with no errors
- ✅ Comprehensive validation prevents invalid states

### Frontend
- ✅ Build succeeds with no errors
- ✅ TypeScript strict mode compliance
- ✅ React hooks best practices followed
- ✅ No ESLint warnings

## Security Considerations

### Input Validation
- All user input validated on both client and server
- SQL injection not applicable (in-memory store)
- XSS protection through React's built-in escaping

### Authorization
- Controller uses User.Identity.Name for audit fields
- Ready for role-based access control integration

### Data Integrity
- Referential integrity maintained through validation
- Cannot orphan data points by deleting linked assumptions
- Audit trail preserved

## Future Enhancements

1. **Search and Filter**: Add search across assumptions, filter by status, scope
2. **Assumption Templates**: Pre-populate common assumptions
3. **Impact Analysis**: Show which data points would be affected before updating
4. **Notification**: Alert data point owners when linked assumptions change
5. **Export**: Include assumptions in report exports
6. **Analytics**: Track assumption usage patterns
7. **Approval Workflow**: Require approval for assumption changes

## Migration Path

If moving from in-memory to database:
1. Create EF Core migration for Assumption table
2. Add indexes on SectionId, Status, ValidityDates
3. Update InMemoryReportStore methods to use DbContext
4. No changes needed to controller or frontend

## Documentation for Users

### Creating an Assumption
1. Navigate to a section in Data Collection Workspace
2. Scroll to Assumptions section
3. Click "Add Assumption"
4. Fill in required fields:
   - Title: Brief name
   - Description: Detailed explanation
   - Scope: Where it applies
   - Validity period: When it's valid
   - Methodology: How derived
   - Limitations (optional): Known issues
5. Click "Create Assumption"

### Linking to Data Points
1. When creating/editing a data point, assumptions can reference the assumption ID
2. Or use the Link button in assumption management

### Updating an Assumption
1. Find the assumption in the list
2. Click the pencil (edit) icon
3. Make changes
4. Click "Update Assumption"
5. Version number increments automatically
6. All linked data points reference the new version

### Deprecating an Assumption
1. Find the active assumption
2. Click the warning icon
3. Choose:
   - **Provide Replacement**: Select another active assumption
   - **Mark as Invalid**: Provide justification
4. Click "Deprecate Assumption"
5. Status changes to deprecated or invalid

## Conclusion

This implementation provides a complete, production-ready assumption management system that meets all acceptance criteria and follows best practices for both backend and frontend development.
