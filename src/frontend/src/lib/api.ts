import type { ReportingPeriod, ReportSection, SectionSummary } from '@/lib/types'

export interface ReportingDataSnapshot {
  periods: ReportingPeriod[]
  sections: ReportSection[]
  sectionSummaries: SectionSummary[]
}

export interface CreateReportingPeriodPayload {
  name: string
  startDate: string
  endDate: string
  variant: 'simplified' | 'extended'
  ownerId: string
  ownerName: string
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
    const message = await response.text()
    throw new Error(message || 'Request failed')
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
