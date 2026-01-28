# Validation Rules API

This document describes the API endpoints for managing validation rules that are applied to data points during creation and updates.

## Overview

Validation rules are configurable constraints that ensure data quality and compliance. When a user attempts to save a data point that violates an active validation rule, the system blocks the save operation and returns the configured error message.

## Supported Rule Types

- **non-negative**: Ensures numeric values are not negative
- **required-unit**: Requires a unit to be specified when a value is provided
- **allowed-units**: Restricts units to a predefined list
- **value-within-period**: Ensures date values fall within the reporting period

## Endpoints

### List Validation Rules

Get all validation rules, optionally filtered by section.

**Request:**
```
GET /api/validation-rules?sectionId={sectionId}
```

**Query Parameters:**
- `sectionId` (optional): Filter rules by section ID

**Response:**
```json
[
  {
    "id": "rule-123",
    "sectionId": "section-456",
    "ruleType": "non-negative",
    "targetField": "value",
    "parameters": null,
    "errorMessage": "Energy consumption cannot be negative.",
    "isActive": true,
    "createdBy": "user-1",
    "createdAt": "2024-01-15T10:30:00Z"
  }
]
```

### Get Validation Rule

Get a specific validation rule by ID.

**Request:**
```
GET /api/validation-rules/{id}
```

**Response:**
```json
{
  "id": "rule-123",
  "sectionId": "section-456",
  "ruleType": "non-negative",
  "targetField": "value",
  "parameters": null,
  "errorMessage": "Energy consumption cannot be negative.",
  "isActive": true,
  "createdBy": "user-1",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Error Response (404):**
```json
{
  "error": "ValidationRule with ID 'rule-123' not found."
}
```

### Create Validation Rule

Create a new validation rule for a section.

**Request:**
```
POST /api/validation-rules
Content-Type: application/json

{
  "sectionId": "section-456",
  "ruleType": "non-negative",
  "targetField": "value",
  "errorMessage": "Energy consumption cannot be negative.",
  "createdBy": "user-1"
}
```

**Request Body Fields:**
- `sectionId` (required): ID of the section this rule applies to
- `ruleType` (required): Type of validation rule (see supported types above)
- `targetField` (optional): Field to validate (e.g., "value", "unit")
- `parameters` (optional): JSON-encoded parameters for the rule (e.g., allowed units list)
- `errorMessage` (required): Message to display when validation fails
- `createdBy` (required): ID of the user creating the rule

**Response (201 Created):**
```json
{
  "id": "rule-123",
  "sectionId": "section-456",
  "ruleType": "non-negative",
  "targetField": "value",
  "parameters": null,
  "errorMessage": "Energy consumption cannot be negative.",
  "isActive": true,
  "createdBy": "user-1",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Error Response (400):**
```json
{
  "error": "SectionId is required."
}
```

### Update Validation Rule

Update an existing validation rule.

**Request:**
```
PUT /api/validation-rules/{id}
Content-Type: application/json

{
  "ruleType": "non-negative",
  "targetField": "value",
  "errorMessage": "Updated error message: Value cannot be negative.",
  "isActive": true
}
```

**Request Body Fields:**
- `ruleType` (required): Type of validation rule
- `targetField` (optional): Field to validate
- `parameters` (optional): JSON-encoded parameters
- `errorMessage` (required): Error message to display
- `isActive` (required): Whether the rule is active

**Response (200 OK):**
```json
{
  "id": "rule-123",
  "sectionId": "section-456",
  "ruleType": "non-negative",
  "targetField": "value",
  "parameters": null,
  "errorMessage": "Updated error message: Value cannot be negative.",
  "isActive": true,
  "createdBy": "user-1",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Error Response (404):**
```json
{
  "error": "ValidationRule not found."
}
```

### Delete Validation Rule

Delete a validation rule.

**Request:**
```
DELETE /api/validation-rules/{id}
```

**Response (204 No Content):**
```
(empty body)
```

**Error Response (404):**
```json
{
  "error": "ValidationRule with ID 'rule-123' not found."
}
```

## Rule Type Examples

### Non-Negative

Validates that numeric values are not negative.

```json
{
  "sectionId": "section-456",
  "ruleType": "non-negative",
  "targetField": "value",
  "errorMessage": "Energy consumption cannot be negative.",
  "createdBy": "user-1"
}
```

### Required Unit

Ensures that when a value is provided, a unit must also be specified.

```json
{
  "sectionId": "section-456",
  "ruleType": "required-unit",
  "targetField": "unit",
  "errorMessage": "Unit is required when providing a numeric value.",
  "createdBy": "user-1"
}
```

### Allowed Units

Restricts units to a predefined list.

```json
{
  "sectionId": "section-456",
  "ruleType": "allowed-units",
  "targetField": "unit",
  "parameters": "[\"MWh\", \"kWh\", \"GJ\"]",
  "errorMessage": "Unit must be one of: MWh, kWh, GJ.",
  "createdBy": "user-1"
}
```

### Value Within Period

Ensures date values fall within the reporting period.

```json
{
  "sectionId": "section-456",
  "ruleType": "value-within-period",
  "targetField": "value",
  "errorMessage": "Date must be within the reporting period (2024-01-01 to 2024-12-31).",
  "createdBy": "user-1"
}
```

## Data Point Validation

When creating or updating a data point, all active validation rules for that section are evaluated. If any rule fails, the save operation is blocked.

**Example - Create Data Point (Blocked by Validation):**

**Request:**
```
POST /api/data-points
Content-Type: application/json

{
  "sectionId": "section-456",
  "title": "Energy Consumption",
  "content": "Total energy consumption",
  "value": "-100",
  "unit": "MWh",
  "ownerId": "user-1",
  "source": "Energy System",
  "informationType": "fact",
  "completenessStatus": "complete"
}
```

**Response (400 Bad Request):**
```json
{
  "error": "Energy consumption cannot be negative."
}
```

## Rule Updates

When validation rules are updated, the new rules are immediately applied to subsequent save operations. Existing data points are not retroactively validated.

## Best Practices

1. **Clear Error Messages**: Write error messages that help users understand what's wrong and how to fix it.
2. **Rule Scope**: Create rules at the section level to ensure consistency within each reporting area.
3. **Active/Inactive**: Use the `isActive` flag to temporarily disable rules without deleting them.
4. **Parameters**: Use the `parameters` field for configurable rules (e.g., allowed units, date ranges).
5. **Testing**: Test validation rules with both valid and invalid data before activating them.
