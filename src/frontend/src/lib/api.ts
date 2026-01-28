import type { ReportingPeriod, ReportSection, SectionSummary, Organization, OrganizationalUnit, User, CompletenessStats } from '@/lib/types'

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
    
    // Clone the response to allow multiple reads
    const clonedResponse = response.clone()
    
    try {
      const errorData = await response.json()
      // Check if it's a structured error with 'error' field
      if (errorData.error) {
        errorMessage = errorData.error
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
    throw new Error(errorMessage)
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

// Audit Log API methods
export interface AuditLogFilters {
  entityType?: string
  entityId?: string
  userId?: string
  startDate?: string
  endDate?: string
}

export async function getAuditLog(filters?: AuditLogFilters): Promise<any[]> {
  const params = new URLSearchParams()
  if (filters?.entityType) params.append('entityType', filters.entityType)
  if (filters?.entityId) params.append('entityId', filters.entityId)
  if (filters?.userId) params.append('userId', filters.userId)
  if (filters?.startDate) params.append('startDate', filters.startDate)
  if (filters?.endDate) params.append('endDate', filters.endDate)
  
  const queryString = params.toString()
  const path = queryString ? `audit-log?${queryString}` : 'audit-log'
  
  return requestJson<any[]>(path)
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
