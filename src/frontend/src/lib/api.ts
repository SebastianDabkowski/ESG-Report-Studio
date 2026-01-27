import type { ReportingPeriod, ReportSection, SectionSummary, Organization, OrganizationalUnit } from '@/lib/types'

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
  variant: 'simplified' | 'extended'
  reportScope: 'single-company' | 'group'
  ownerId: string
  ownerName: string
  organizationId?: string
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

