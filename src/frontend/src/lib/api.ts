import type { ReportingPeriod, ReportSection, SectionSummary, Organization, OrganizationalUnit, User, CompletenessStats, UpdateDataPointStatusRequest, StatusValidationError, DataPointNote, CreateDataPointNoteRequest, ResponsibilityMatrix, ReadinessReport } from '@/lib/types'

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
