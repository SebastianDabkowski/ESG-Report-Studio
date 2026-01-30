import type { 
  ReportingPeriod, 
  ReportSection, 
  SectionSummary, 
  Organization, 
  OrganizationalUnit, 
  User, 
  CompletenessStats, 
  UpdateDataPointStatusRequest, 
  StatusValidationError, 
  DataPointNote, 
  CreateDataPointNoteRequest, 
  ResponsibilityMatrix, 
  ReadinessReport, 
  RolloverRequest, 
  RolloverResult, 
  RolloverAuditLog,
  DataTypeRolloverRule,
  SaveDataTypeRolloverRuleRequest,
  RolloverRuleHistory,
  MetricComparisonResponse,
  TextDisclosureComparisonResponse
} from '@/lib/types'

export interface ReportingDataSnapshot {
  organization: Organization | null
  periods: ReportingPeriod[]
  sections: ReportSection[]
  sectionSummaries: SectionSummary[]
  organizationalUnits: OrganizationalUnit[]
}

export interface CreateReportingPeriodPayload {
  name: string
  startDate: string
  endDate: string
  reportingMode: 'simplified' | 'extended'
  reportScope: 'single-company' | 'group'
  ownerId: string
  ownerName: string
  organizationId?: string
  copyOwnershipFromPeriodId?: string
  carryForwardGapsAndAssumptions?: boolean
}

export interface UpdateReportingPeriodPayload {
  name: string
  startDate: string
  endDate: string
  reportingMode: 'simplified' | 'extended'
  reportScope: 'single-company' | 'group'
}

const baseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? '/api'

function buildUrl(path: string): string {
  const trimmedBase = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return `${trimmedBase}${normalizedPath}`
}

async function requestJson<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(buildUrl(path), {
    headers: {
      'Content-Type': 'application/json',
      ...(options?.headers ?? {})
    },
    ...options
  })

  if (!response.ok) {
    let errorMessage = 'Request failed'
    let structuredError: any = null
    
    // Clone the response to allow multiple reads
    const clonedResponse = response.clone()
    
    try {
      const errorData = await response.json()
      // Check if it's a structured error with 'error' field
      if (errorData.error) {
        // Check if the error is itself a structured validation error
        if (typeof errorData.error === 'object' && errorData.error.message && errorData.error.missingFields) {
          structuredError = errorData.error
          errorMessage = errorData.error.message
        } else {
          errorMessage = errorData.error
        }
      } else if (errorData.message && errorData.missingFields) {
        // Direct StatusValidationError structure
        structuredError = errorData
        errorMessage = errorData.message
      } else if (typeof errorData === 'string') {
        errorMessage = errorData
      }
    } catch {
      // If JSON parsing fails, try to get text from cloned response
      try {
        const text = await clonedResponse.text()
        if (text) {
          errorMessage = text
        }
      } catch {
        // Ignore and use default message
      }
    }
    
    // Create error with structured data if available
    const error = new Error(errorMessage) as any
    if (structuredError) {
      error.validationError = structuredError
    }
    throw error
  }

  return response.json() as Promise<T>
}

export function getReportingData(): Promise<ReportingDataSnapshot> {
  return requestJson<ReportingDataSnapshot>('reporting-data')
}

export function createReportingPeriod(payload: CreateReportingPeriodPayload): Promise<ReportingDataSnapshot> {
  return requestJson<ReportingDataSnapshot>('periods', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export function updateReportingPeriod(id: string, payload: UpdateReportingPeriodPayload): Promise<ReportingPeriod> {
  return requestJson<ReportingPeriod>(`periods/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })
}

export function hasReportingStarted(id: string): Promise<boolean> {
  return requestJson<boolean>(`periods/${id}/has-started`)
}

export interface CreateOrganizationPayload {
  name: string
  legalForm: string
  country: string
  identifier: string
  createdBy: string
  coverageType: 'full' | 'limited'
  coverageJustification?: string
}

export interface UpdateOrganizationPayload {
  name: string
  legalForm: string
  country: string
  identifier: string
  coverageType: 'full' | 'limited'
  coverageJustification?: string
}

export function getOrganization(): Promise<Organization> {
  return requestJson<Organization>('organization')
}

export function createOrganization(payload: CreateOrganizationPayload): Promise<Organization> {
  return requestJson<Organization>('organization', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export function updateOrganization(id: string, payload: UpdateOrganizationPayload): Promise<Organization> {
  return requestJson<Organization>(`organization/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })
}

export interface CreateOrganizationalUnitPayload {
  name: string
  parentId?: string
  description: string
  createdBy: string
}

export interface UpdateOrganizationalUnitPayload {
  name: string
  parentId?: string
  description: string
}

export function getOrganizationalUnits(): Promise<OrganizationalUnit[]> {
  return requestJson<OrganizationalUnit[]>('organizational-units')
}

export function getOrganizationalUnit(id: string): Promise<OrganizationalUnit> {
  return requestJson<OrganizationalUnit>(`organizational-units/${id}`)
}

export function createOrganizationalUnit(payload: CreateOrganizationalUnitPayload): Promise<OrganizationalUnit> {
  return requestJson<OrganizationalUnit>('organizational-units', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export function updateOrganizationalUnit(id: string, payload: UpdateOrganizationalUnitPayload): Promise<OrganizationalUnit> {
  return requestJson<OrganizationalUnit>(`organizational-units/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })
}

export function deleteOrganizationalUnit(id: string): Promise<void> {
  return requestJson<void>(`organizational-units/${id}`, {
    method: 'DELETE'
  })
}

// Section Owner API methods
export interface UpdateSectionOwnerPayload {
  ownerId: string
  updatedBy: string
  changeNote?: string
}

export function updateSectionOwner(sectionId: string, payload: UpdateSectionOwnerPayload): Promise<ReportSection> {
  return requestJson<ReportSection>(`sections/${sectionId}/owner`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })
}

export interface BulkUpdateSectionOwnerPayload {
  sectionIds: string[]
  ownerId: string
  updatedBy: string
  changeNote?: string
}

export interface BulkUpdateFailure {
  sectionId: string
  reason: string
}

export interface BulkUpdateSectionOwnerResult {
  updatedSections: ReportSection[]
  skippedSections: BulkUpdateFailure[]
}

export function bulkUpdateSectionOwner(payload: BulkUpdateSectionOwnerPayload): Promise<BulkUpdateSectionOwnerResult> {
  return requestJson<BulkUpdateSectionOwnerResult>('sections/bulk-owner', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

// User API methods
export function getUsers(): Promise<User[]> {
  return requestJson<User[]>('users')
}

export function getUser(id: string): Promise<User> {
  return requestJson<User>(`users/${id}`)
}

// Data Points API methods
export async function getDataPoints(sectionId?: string, assignedUserId?: string): Promise<any[]> {
  const params = new URLSearchParams()
  if (sectionId) params.append('sectionId', sectionId)
  if (assignedUserId) params.append('assignedUserId', assignedUserId)
  
  const queryString = params.toString()
  const path = queryString ? `data-points?${queryString}` : 'data-points'
  
  return requestJson<any[]>(path)
}

export interface CreateDataPointPayload {
  sectionId: string
  type: string
  classification?: string
  title: string
  content: string
  value?: string
  unit?: string
  ownerId: string
  contributorIds: string[]
  source: string
  informationType: string
  assumptions?: string
  completenessStatus: string
  reviewStatus?: string
  estimateType?: string
  estimateMethod?: string
  confidenceLevel?: string
  estimateInputSources?: import('@/lib/types').EstimateInputSource[]
  estimateInputs?: string
}

export interface UpdateDataPointPayload {
  type: string
  classification?: string
  title: string
  content: string
  value?: string
  unit?: string
  ownerId: string
  contributorIds: string[]
  source: string
  informationType: string
  assumptions?: string
  completenessStatus: string
  reviewStatus?: string
  changeNote?: string
  updatedBy?: string
  estimateType?: string
  estimateMethod?: string
  confidenceLevel?: string
  estimateInputSources?: import('@/lib/types').EstimateInputSource[]
  estimateInputs?: string
}

export interface ApproveDataPointPayload {
  reviewedBy: string
  reviewComments?: string
}

export interface RequestChangesPayload {
  reviewedBy: string
  reviewComments: string
}

export async function createDataPoint(payload: CreateDataPointPayload): Promise<any> {
  return requestJson<any>('data-points', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function updateDataPoint(id: string, payload: UpdateDataPointPayload): Promise<any> {
  return requestJson<any>(`data-points/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })
}

export async function deleteDataPoint(id: string): Promise<void> {
  await requestJson<void>(`data-points/${id}`, {
    method: 'DELETE'
  })
}

export async function approveDataPoint(id: string, payload: ApproveDataPointPayload): Promise<any> {
  return requestJson<any>(`data-points/${id}/approve`, {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function requestChangesOnDataPoint(id: string, payload: RequestChangesPayload): Promise<any> {
  return requestJson<any>(`data-points/${id}/request-changes`, {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function updateDataPointStatus(id: string, payload: UpdateDataPointStatusRequest): Promise<any> {
  return requestJson<any>(`data-points/${id}/status`, {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

// Data Point Notes API methods
export async function getDataPointNotes(dataPointId: string): Promise<DataPointNote[]> {
  return requestJson<DataPointNote[]>(`data-points/${dataPointId}/notes`)
}

export async function createDataPointNote(dataPointId: string, payload: CreateDataPointNoteRequest): Promise<DataPointNote> {
  return requestJson<DataPointNote>(`data-points/${dataPointId}/notes`, {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

// Audit Log API methods
export interface AuditLogFilters {
  entityType?: string
  entityId?: string
  userId?: string
  action?: string
  startDate?: string
  endDate?: string
}

function buildAuditLogQueryParams(filters?: AuditLogFilters): URLSearchParams {
  const params = new URLSearchParams()
  if (filters?.entityType) params.append('entityType', filters.entityType)
  if (filters?.entityId) params.append('entityId', filters.entityId)
  if (filters?.userId) params.append('userId', filters.userId)
  if (filters?.action) params.append('action', filters.action)
  if (filters?.startDate) params.append('startDate', filters.startDate)
  if (filters?.endDate) params.append('endDate', filters.endDate)
  return params
}

async function downloadFile(url: string, filename: string): Promise<void> {
  const response = await fetch(url)
  if (!response.ok) {
    const errorText = await response.text().catch(() => 'Unknown error')
    throw new Error(`Export failed: ${response.status} ${response.statusText}. ${errorText}`)
  }
  
  const blob = await response.blob()
  const blobUrl = window.URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = blobUrl
  a.download = filename
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  window.URL.revokeObjectURL(blobUrl)
}

export async function getAuditLog(filters?: AuditLogFilters): Promise<any[]> {
  const params = buildAuditLogQueryParams(filters)
  const queryString = params.toString()
  const path = queryString ? `audit-log?${queryString}` : 'audit-log'
  
  return requestJson<any[]>(path)
}

export async function exportAuditLogCsv(filters?: AuditLogFilters): Promise<void> {
  const params = buildAuditLogQueryParams(filters)
  const queryString = params.toString()
  const path = queryString ? `audit-log/export/csv?${queryString}` : 'audit-log/export/csv'
  const filename = `audit-log-${new Date().toISOString().split('T')[0]}.csv`
  
  await downloadFile(buildUrl(path), filename)
}

export async function exportAuditLogJson(filters?: AuditLogFilters): Promise<void> {
  const params = buildAuditLogQueryParams(filters)
  const queryString = params.toString()
  const path = queryString ? `audit-log/export/json?${queryString}` : 'audit-log/export/json'
  const filename = `audit-log-${new Date().toISOString().split('T')[0]}.json`
  
  await downloadFile(buildUrl(path), filename)
}

export interface EntityTimeline {
  entityType: string
  entityId: string
  totalChanges: number
  metadata: any
  timeline: TimelineEntry[]
}

export interface TimelineEntry {
  id: string
  timestamp: string
  userId: string
  userName: string
  action: string
  changeNote?: string
  changes: {
    field: string
    before: string | null
    after: string | null
  }[]
}

export async function getEntityTimeline(entityType: string, entityId: string): Promise<EntityTimeline> {
  return requestJson<EntityTimeline>(`audit-log/timeline/${entityType}/${entityId}`)
}

export interface VersionComparison {
  entityType: string
  entityId: string
  fromVersion: {
    id: string
    timestamp: string
    userId: string
    userName: string
    action: string
    changeNote?: string
  }
  toVersion: {
    id: string
    timestamp: string
    userId: string
    userName: string
    action: string
    changeNote?: string
  }
  metadata: any
  differences: {
    field: string
    fromValue: string | null
    toValue: string | null
    changeType: 'added' | 'removed' | 'modified'
  }[]
}

export async function compareVersions(
  entityType: string, 
  entityId: string, 
  fromVersion: string, 
  toVersion: string
): Promise<VersionComparison> {
  const params = new URLSearchParams()
  params.append('fromVersion', fromVersion)
  params.append('toVersion', toVersion)
  return requestJson<VersionComparison>(`audit-log/compare/${entityType}/${entityId}?${params}`)
}

// Dashboard API
export interface CompletenessStatsParams {
  periodId?: string
  category?: string
  organizationalUnitId?: string
}

export async function getCompletenessStats(params?: CompletenessStatsParams): Promise<CompletenessStats> {
  const queryParams = new URLSearchParams()
  if (params?.periodId) queryParams.append('periodId', params.periodId)
  if (params?.category) queryParams.append('category', params.category)
  if (params?.organizationalUnitId) queryParams.append('organizationalUnitId', params.organizationalUnitId)
  
  const queryString = queryParams.toString()
  return requestJson<CompletenessStats>(`dashboard/completeness-stats${queryString ? `?${queryString}` : ''}`)
}

// Responsibility Matrix API
export interface ResponsibilityMatrixParams {
  periodId?: string
  ownerFilter?: string
}

export async function getResponsibilityMatrix(params?: ResponsibilityMatrixParams): Promise<ResponsibilityMatrix> {
  const queryParams = new URLSearchParams()
  if (params?.periodId) queryParams.append('periodId', params.periodId)
  if (params?.ownerFilter) queryParams.append('ownerFilter', params.ownerFilter)
  
  const queryString = queryParams.toString()
  return requestJson<ResponsibilityMatrix>(`responsibility-matrix${queryString ? `?${queryString}` : ''}`)
}

// Readiness Report API
export interface ReadinessReportParams {
  periodId?: string
  sectionId?: string
  ownerId?: string
  category?: string
}

export async function getReadinessReport(params?: ReadinessReportParams): Promise<ReadinessReport> {
  const queryParams = new URLSearchParams()
  if (params?.periodId) queryParams.append('periodId', params.periodId)
  if (params?.sectionId) queryParams.append('sectionId', params.sectionId)
  if (params?.ownerId) queryParams.append('ownerId', params.ownerId)
  if (params?.category) queryParams.append('category', params.category)
  
  const queryString = queryParams.toString()
  return requestJson<ReadinessReport>(`readiness/report${queryString ? `?${queryString}` : ''}`)
}

// Assumptions API
export interface CreateAssumptionPayload {
  sectionId: string
  title: string
  description: string
  scope: string
  validityStartDate: string
  validityEndDate: string
  methodology: string
  limitations: string
  linkedDataPointIds: string[]
  rationale?: string
  sources?: import('@/lib/types').AssumptionSource[]
}

export interface UpdateAssumptionPayload {
  title: string
  description: string
  scope: string
  validityStartDate: string
  validityEndDate: string
  methodology: string
  limitations: string
  linkedDataPointIds: string[]
  rationale?: string
  sources?: import('@/lib/types').AssumptionSource[]
}

export interface DeprecateAssumptionPayload {
  replacementAssumptionId?: string
  justification?: string
}

export interface LinkAssumptionPayload {
  dataPointId: string
}

export async function getAssumptions(sectionId?: string): Promise<import('@/lib/types').Assumption[]> {
  const queryString = sectionId ? `?sectionId=${encodeURIComponent(sectionId)}` : ''
  return requestJson<import('@/lib/types').Assumption[]>(`assumptions${queryString}`)
}

export async function getAssumptionById(id: string): Promise<import('@/lib/types').Assumption> {
  return requestJson<import('@/lib/types').Assumption>(`assumptions/${id}`)
}

export async function createAssumption(payload: CreateAssumptionPayload): Promise<import('@/lib/types').Assumption> {
  return requestJson<import('@/lib/types').Assumption>('assumptions', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function updateAssumption(id: string, payload: UpdateAssumptionPayload): Promise<import('@/lib/types').Assumption> {
  return requestJson<import('@/lib/types').Assumption>(`assumptions/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })
}

export async function deprecateAssumption(id: string, payload: DeprecateAssumptionPayload): Promise<void> {
  await requestJson<void>(`assumptions/${id}/deprecate`, {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function linkAssumptionToDataPoint(assumptionId: string, dataPointId: string): Promise<void> {
  await requestJson<void>(`assumptions/${assumptionId}/link`, {
    method: 'POST',
    body: JSON.stringify({ dataPointId })
  })
}

export async function unlinkAssumptionFromDataPoint(assumptionId: string, dataPointId: string): Promise<void> {
  await requestJson<void>(`assumptions/${assumptionId}/unlink`, {
    method: 'POST',
    body: JSON.stringify({ dataPointId })
  })
}

export async function deleteAssumption(id: string): Promise<void> {
  await requestJson<void>(`assumptions/${id}`, {
    method: 'DELETE'
  })
}

// Simplifications API
export interface CreateSimplificationPayload {
  sectionId: string
  title: string
  description: string
  affectedEntities: string[]
  affectedSites: string[]
  affectedProcesses: string[]
  impactLevel: 'low' | 'medium' | 'high'
  impactNotes?: string
}

export interface UpdateSimplificationPayload {
  title: string
  description: string
  affectedEntities: string[]
  affectedSites: string[]
  affectedProcesses: string[]
  impactLevel: 'low' | 'medium' | 'high'
  impactNotes?: string
}

export async function getSimplifications(sectionId?: string): Promise<import('@/lib/types').Simplification[]> {
  const queryString = sectionId ? `?sectionId=${encodeURIComponent(sectionId)}` : ''
  return requestJson<import('@/lib/types').Simplification[]>(`simplifications${queryString}`)
}

export async function getSimplificationById(id: string): Promise<import('@/lib/types').Simplification> {
  return requestJson<import('@/lib/types').Simplification>(`simplifications/${id}`)
}

export async function createSimplification(payload: CreateSimplificationPayload): Promise<import('@/lib/types').Simplification> {
  return requestJson<import('@/lib/types').Simplification>('simplifications', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function updateSimplification(id: string, payload: UpdateSimplificationPayload): Promise<import('@/lib/types').Simplification> {
  return requestJson<import('@/lib/types').Simplification>(`simplifications/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })
}

export async function deleteSimplification(id: string): Promise<void> {
  await requestJson<void>(`simplifications/${id}`, {
    method: 'DELETE'
  })
}

// Remediation Plans API

export async function getRemediationPlans(sectionId?: string): Promise<import('@/lib/types').RemediationPlan[]> {
  const url = sectionId ? `remediation-plans?sectionId=${sectionId}` : 'remediation-plans'
  return requestJson<import('@/lib/types').RemediationPlan[]>(url)
}

export async function getRemediationPlanById(id: string): Promise<import('@/lib/types').RemediationPlan> {
  return requestJson<import('@/lib/types').RemediationPlan>(`remediation-plans/${id}`)
}

export interface CreateRemediationPlanPayload {
  sectionId: string
  title: string
  description: string
  targetPeriod: string
  ownerId: string
  ownerName: string
  priority: 'low' | 'medium' | 'high'
  gapId?: string
  assumptionId?: string
  dataPointId?: string
}

export async function createRemediationPlan(payload: CreateRemediationPlanPayload): Promise<import('@/lib/types').RemediationPlan> {
  return requestJson<import('@/lib/types').RemediationPlan>('remediation-plans', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export interface UpdateRemediationPlanPayload {
  title: string
  description: string
  targetPeriod: string
  ownerId: string
  ownerName: string
  priority: 'low' | 'medium' | 'high'
  status: 'planned' | 'in-progress' | 'completed' | 'cancelled'
}

export async function updateRemediationPlan(id: string, payload: UpdateRemediationPlanPayload): Promise<import('@/lib/types').RemediationPlan> {
  return requestJson<import('@/lib/types').RemediationPlan>(`remediation-plans/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })
}

export async function completeRemediationPlan(id: string, completedBy: string): Promise<import('@/lib/types').RemediationPlan> {
  return requestJson<import('@/lib/types').RemediationPlan>(`remediation-plans/${id}/complete`, {
    method: 'POST',
    body: JSON.stringify({ completedBy })
  })
}

export async function deleteRemediationPlan(id: string): Promise<void> {
  await requestJson<void>(`remediation-plans/${id}`, {
    method: 'DELETE'
  })
}

// Remediation Actions API

export async function getRemediationActions(planId: string): Promise<import('@/lib/types').RemediationAction[]> {
  return requestJson<import('@/lib/types').RemediationAction[]>(`remediation-plans/${planId}/actions`)
}

export async function getRemediationActionById(id: string): Promise<import('@/lib/types').RemediationAction> {
  return requestJson<import('@/lib/types').RemediationAction>(`remediation-plans/actions/${id}`)
}

export interface CreateRemediationActionPayload {
  remediationPlanId: string
  title: string
  description: string
  ownerId: string
  ownerName: string
  dueDate: string
}

export async function createRemediationAction(payload: CreateRemediationActionPayload): Promise<import('@/lib/types').RemediationAction> {
  return requestJson<import('@/lib/types').RemediationAction>('remediation-plans/actions', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export interface UpdateRemediationActionPayload {
  title: string
  description: string
  ownerId: string
  ownerName: string
  dueDate: string
  status: 'pending' | 'in-progress' | 'completed' | 'cancelled'
}

export async function updateRemediationAction(id: string, payload: UpdateRemediationActionPayload): Promise<import('@/lib/types').RemediationAction> {
  return requestJson<import('@/lib/types').RemediationAction>(`remediation-plans/actions/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })
}

export interface CompleteRemediationActionPayload {
  completedBy: string
  completionNotes?: string
  evidenceIds: string[]
}

export async function completeRemediationAction(id: string, payload: CompleteRemediationActionPayload): Promise<import('@/lib/types').RemediationAction> {
  return requestJson<import('@/lib/types').RemediationAction>(`remediation-plans/actions/${id}/complete`, {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function deleteRemediationAction(id: string): Promise<void> {
  await requestJson<void>(`remediation-plans/actions/${id}`, {
    method: 'DELETE'
  })
}

// ==================== Completion Exceptions ====================

export async function getCompletionExceptions(
  sectionId?: string,
  status?: string
): Promise<import('@/lib/types').CompletionException[]> {
  const params = new URLSearchParams()
  if (sectionId) params.append('sectionId', sectionId)
  if (status) params.append('status', status)
  
  const url = params.toString() 
    ? `completion-exceptions?${params.toString()}` 
    : 'completion-exceptions'
  
  return requestJson<import('@/lib/types').CompletionException[]>(url)
}

export async function getCompletionException(id: string): Promise<import('@/lib/types').CompletionException> {
  return requestJson<import('@/lib/types').CompletionException>(`completion-exceptions/${id}`)
}

export async function createCompletionException(
  payload: import('@/lib/types').CreateCompletionExceptionRequest
): Promise<import('@/lib/types').CompletionException> {
  return requestJson<import('@/lib/types').CompletionException>('completion-exceptions', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function approveCompletionException(
  id: string,
  payload: import('@/lib/types').ApproveCompletionExceptionRequest
): Promise<import('@/lib/types').CompletionException> {
  return requestJson<import('@/lib/types').CompletionException>(`completion-exceptions/${id}/approve`, {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function rejectCompletionException(
  id: string,
  payload: import('@/lib/types').RejectCompletionExceptionRequest
): Promise<import('@/lib/types').CompletionException> {
  return requestJson<import('@/lib/types').CompletionException>(`completion-exceptions/${id}/reject`, {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function deleteCompletionException(id: string): Promise<void> {
  await requestJson<void>(`completion-exceptions/${id}`, {
    method: 'DELETE'
  })
}

export async function getCompletenessValidationReport(
  periodId: string
): Promise<import('@/lib/types').CompletenessValidationReport> {
  return requestJson<import('@/lib/types').CompletenessValidationReport>(
    `completion-exceptions/validation-report?periodId=${periodId}`
  )
}

// ==================== Gaps and Improvements ====================

export async function getGapsAndImprovementsReport(
  periodId?: string,
  sectionId?: string
): Promise<import('@/lib/types').GapsAndImprovementsReport> {
  const params = new URLSearchParams()
  if (periodId) params.append('periodId', periodId)
  if (sectionId) params.append('sectionId', sectionId)
  
  const url = params.toString() 
    ? `gaps-and-improvements/report?${params.toString()}`
    : 'gaps-and-improvements/report'
  
  return requestJson<import('@/lib/types').GapsAndImprovementsReport>(url)
}

export interface UpdateGapsNarrativePayload {
  manualNarrative?: string
}

export async function updateGapsNarrative(payload: UpdateGapsNarrativePayload): Promise<{ message: string }> {
  return requestJson<{ message: string }>('gaps-and-improvements/narrative', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export interface GetGapsDashboardParams {
  periodId?: string
  status?: 'open' | 'resolved' | 'all'
  sectionId?: string
  ownerId?: string
  duePeriod?: string
  sortBy?: 'risk' | 'dueDate' | 'section'
  sortOrder?: 'asc' | 'desc'
}

export async function getGapsDashboard(
  params: GetGapsDashboardParams = {}
): Promise<import('@/lib/types').GapDashboardResponse> {
  const searchParams = new URLSearchParams()
  
  if (params.periodId) searchParams.append('periodId', params.periodId)
  if (params.status) searchParams.append('status', params.status)
  if (params.sectionId) searchParams.append('sectionId', params.sectionId)
  if (params.ownerId) searchParams.append('ownerId', params.ownerId)
  if (params.duePeriod) searchParams.append('duePeriod', params.duePeriod)
  if (params.sortBy) searchParams.append('sortBy', params.sortBy)
  if (params.sortOrder) searchParams.append('sortOrder', params.sortOrder)
  
  const url = searchParams.toString()
    ? `gaps-and-improvements/dashboard?${searchParams.toString()}`
    : 'gaps-and-improvements/dashboard'
  
  return requestJson<import('@/lib/types').GapDashboardResponse>(url)
}

// ==================== Decisions ====================

export async function getDecisions(sectionId?: string): Promise<import('@/lib/types').Decision[]> {
  const url = sectionId ? `decisions?sectionId=${sectionId}` : 'decisions'
  return requestJson<import('@/lib/types').Decision[]>(url)
}

export async function getDecisionById(id: string): Promise<import('@/lib/types').Decision> {
  return requestJson<import('@/lib/types').Decision>(`decisions/${id}`)
}

export async function getDecisionVersionHistory(id: string): Promise<import('@/lib/types').DecisionVersion[]> {
  return requestJson<import('@/lib/types').DecisionVersion[]>(`decisions/${id}/versions`)
}

export async function createDecision(
  payload: import('@/lib/types').CreateDecisionRequest
): Promise<import('@/lib/types').Decision> {
  return requestJson<import('@/lib/types').Decision>('decisions', {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function updateDecision(
  id: string,
  payload: import('@/lib/types').UpdateDecisionRequest
): Promise<import('@/lib/types').Decision> {
  return requestJson<import('@/lib/types').Decision>(`decisions/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })
}

export async function deprecateDecision(
  id: string,
  payload: import('@/lib/types').DeprecateDecisionRequest
): Promise<import('@/lib/types').Decision> {
  return requestJson<import('@/lib/types').Decision>(`decisions/${id}/deprecate`, {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function linkDecisionToFragment(
  id: string,
  payload: import('@/lib/types').LinkDecisionRequest
): Promise<{ message: string }> {
  return requestJson<{ message: string }>(`decisions/${id}/link`, {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function unlinkDecisionFromFragment(
  id: string,
  payload: import('@/lib/types').UnlinkDecisionRequest
): Promise<{ message: string }> {
  return requestJson<{ message: string }>(`decisions/${id}/unlink`, {
    method: 'POST',
    body: JSON.stringify(payload)
  })
}

export async function getDecisionsByFragment(fragmentId: string): Promise<import('@/lib/types').Decision[]> {
  return requestJson<import('@/lib/types').Decision[]>(`decisions/fragment/${fragmentId}`)
}

export async function deleteDecision(id: string): Promise<{ message: string }> {
  return requestJson<{ message: string }>(`decisions/${id}`, {
    method: 'DELETE'
  })
}

// Fragment Audit API
export async function getFragmentAuditView(
  fragmentType: string,
  fragmentId: string
): Promise<import('@/lib/types').FragmentAuditView> {
  return requestJson<import('@/lib/types').FragmentAuditView>(`fragment-audit/${fragmentType}/${fragmentId}`)
}

export async function getStableFragmentIdentifier(
  fragmentType: string,
  fragmentId: string
): Promise<{ stableFragmentIdentifier: string }> {
  return requestJson<{ stableFragmentIdentifier: string }>(`fragment-audit/${fragmentType}/${fragmentId}/stable-identifier`)
}

export async function rolloverPeriod(request: RolloverRequest): Promise<RolloverResult> {
  return requestJson<RolloverResult>('periods/rollover', {
    method: 'POST',
    body: JSON.stringify(request)
  })
}

export async function getRolloverAuditLogs(periodId: string): Promise<RolloverAuditLog[]> {
  return requestJson<RolloverAuditLog[]>(`periods/${periodId}/rollover-audit`)
}

// Rollover Rules API
export async function getRolloverRules(dataType?: string): Promise<DataTypeRolloverRule[]> {
  const params = dataType ? `?dataType=${encodeURIComponent(dataType)}` : ''
  return requestJson<DataTypeRolloverRule[]>(`rollover-rules${params}`)
}

export async function getRolloverRuleForDataType(dataType: string): Promise<DataTypeRolloverRule> {
  return requestJson<DataTypeRolloverRule>(`rollover-rules/${encodeURIComponent(dataType)}`)
}

export async function saveRolloverRule(request: SaveDataTypeRolloverRuleRequest): Promise<DataTypeRolloverRule> {
  return requestJson<DataTypeRolloverRule>('rollover-rules', {
    method: 'POST',
    body: JSON.stringify(request)
  })
}

export async function deleteRolloverRule(dataType: string, deletedBy: string): Promise<void> {
  await requestJson<void>(`rollover-rules/${encodeURIComponent(dataType)}?deletedBy=${encodeURIComponent(deletedBy)}`, {
    method: 'DELETE'
  })
}

export async function getRolloverRuleHistory(dataType: string): Promise<RolloverRuleHistory[]> {
  return requestJson<RolloverRuleHistory[]>(`rollover-rules/${encodeURIComponent(dataType)}/history`)
}

// Metric Comparison API
export async function compareMetrics(dataPointId: string, priorPeriodId?: string): Promise<MetricComparisonResponse> {
  const params = priorPeriodId ? `?priorPeriodId=${encodeURIComponent(priorPeriodId)}` : ''
  return requestJson<MetricComparisonResponse>(`data-points/${dataPointId}/compare-periods${params}`)
}

export async function compareTextDisclosures(
  dataPointId: string, 
  previousPeriodId?: string,
  granularity: 'word' | 'sentence' = 'word'
): Promise<TextDisclosureComparisonResponse> {
  const params = new URLSearchParams()
  if (previousPeriodId) {
    params.append('previousPeriodId', previousPeriodId)
  }
  params.append('granularity', granularity)
  
  const queryString = params.toString()
  return requestJson<TextDisclosureComparisonResponse>(`data-points/${dataPointId}/compare-text${queryString ? '?' + queryString : ''}`)
}

// ==================== Variance Explanations ====================

export async function createVarianceThresholdConfig(
  periodId: string,
  request: CreateVarianceThresholdConfigRequest
): Promise<VarianceThresholdConfig> {
  return requestJson<VarianceThresholdConfig>(`variance-explanations/threshold-config/${periodId}`, {
    method: 'POST',
    body: JSON.stringify(request)
  })
}

export async function createVarianceExplanation(
  request: CreateVarianceExplanationRequest
): Promise<VarianceExplanation> {
  return requestJson<VarianceExplanation>('variance-explanations', {
    method: 'POST',
    body: JSON.stringify(request)
  })
}

export async function getVarianceExplanations(
  dataPointId?: string,
  periodId?: string
): Promise<VarianceExplanation[]> {
  const params = new URLSearchParams()
  if (dataPointId) params.append('dataPointId', dataPointId)
  if (periodId) params.append('periodId', periodId)
  
  const queryString = params.toString()
  return requestJson<VarianceExplanation[]>(`variance-explanations${queryString ? '?' + queryString : ''}`)
}

export async function getVarianceExplanation(id: string): Promise<VarianceExplanation> {
  return requestJson<VarianceExplanation>(`variance-explanations/${id}`)
}

export async function updateVarianceExplanation(
  id: string,
  request: UpdateVarianceExplanationRequest
): Promise<VarianceExplanation> {
  return requestJson<VarianceExplanation>(`variance-explanations/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request)
  })
}

export async function submitVarianceExplanation(
  id: string,
  request: SubmitVarianceExplanationRequest
): Promise<VarianceExplanation> {
  return requestJson<VarianceExplanation>(`variance-explanations/${id}/submit`, {
    method: 'POST',
    body: JSON.stringify(request)
  })
}

export async function reviewVarianceExplanation(
  id: string,
  request: ReviewVarianceExplanationRequest
): Promise<VarianceExplanation> {
  return requestJson<VarianceExplanation>(`variance-explanations/${id}/review`, {
    method: 'POST',
    body: JSON.stringify(request)
  })
}

export async function deleteVarianceExplanation(
  id: string,
  deletedBy: string
): Promise<void> {
  await requestJson<void>(`variance-explanations/${id}?deletedBy=${encodeURIComponent(deletedBy)}`, {
    method: 'DELETE'
  })
}
