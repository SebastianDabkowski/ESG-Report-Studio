export type UserRole = 'admin' | 'report-owner' | 'contributor' | 'auditor'

export type SectionStatus = 'draft' | 'in-review' | 'approved'

export type ProgressStatus = 'not-started' | 'in-progress' | 'blocked' | 'completed'

export type ReviewStatus = 'draft' | 'ready-for-review' | 'approved' | 'changes-requested'

export type CompletenessLevel = 'empty' | 'partial' | 'complete'

export type CompletenessStatus = 'missing' | 'incomplete' | 'complete' | 'not applicable'

export type Classification = 'fact' | 'declaration' | 'plan'

export type ContentType = 'narrative' | 'metric' | 'evidence' | 'assumption' | 'gap'

export type ReportingMode = 'simplified' | 'extended'

export type ReportScope = 'single-company' | 'group'

export interface User {
  id: string
  name: string
  email: string
  role: UserRole
  avatarUrl?: string
}

export interface Organization {
  id: string
  name: string
  legalForm: string
  country: string
  identifier: string
  createdAt: string
  createdBy: string
  coverageType: 'full' | 'limited'
  coverageJustification?: string
}

export interface ReportingPeriod {
  id: string
  name: string
  startDate: string
  endDate: string
  reportingMode: ReportingMode
  reportScope: ReportScope
  status: 'active' | 'closed'
  createdAt: string
  ownerId: string
}

export interface ReportSection {
  id: string
  periodId: string
  title: string
  category: 'environmental' | 'social' | 'governance'
  description: string
  ownerId: string
  status: SectionStatus
  completeness: CompletenessLevel
  approvedAt?: string
  approvedBy?: string
  order: number
}

export interface DataPoint {
  id: string
  sectionId: string
  type: ContentType
  classification?: Classification
  title: string
  content: string
  value?: string | number
  unit?: string
  ownerId: string
  contributorIds: string[]
  source: string
  informationType: string
  assumptions?: string
  completenessStatus: CompletenessStatus
  reviewStatus: ReviewStatus
  reviewedBy?: string
  reviewedAt?: string
  reviewComments?: string
  createdAt: string
  updatedAt: string
  evidenceIds: string[]
  deadline?: string
  isBlocked: boolean
  blockerReason?: string
  blockerDueDate?: string
}

export interface DataPointNote {
  id: string
  dataPointId: string
  content: string
  createdBy: string
  createdByName: string
  createdAt: string
  updatedAt: string
}

export interface CreateDataPointNoteRequest {
  content: string
  createdBy: string
}

export interface Evidence {
  id: string
  sectionId: string
  title: string
  description?: string
  fileUrl?: string
  fileName?: string
  sourceUrl?: string
  uploadedBy: string
  uploadedAt: string
  linkedDataPoints: string[]
}

export interface Assumption {
  id: string
  sectionId: string
  dataPointId?: string
  description: string
  methodology: string
  limitations: string
  createdBy: string
  createdAt: string
}

export interface Gap {
  id: string
  sectionId: string
  title: string
  description: string
  impact: 'low' | 'medium' | 'high'
  improvementPlan?: string
  targetDate?: string
  createdBy: string
  createdAt: string
  resolved: boolean
}

export interface AuditLogEntry {
  id: string
  timestamp: string
  userId: string
  userName: string
  action: string
  entityType: string
  entityId: string
  changeNote?: string
  changes: {
    field: string
    oldValue: string
    newValue: string
  }[]
}

export interface MissingFieldDetail {
  field: string
  reason: string
}

export interface StatusValidationError {
  message: string
  missingFields: MissingFieldDetail[]
}

export interface UpdateDataPointStatusRequest {
  completenessStatus: CompletenessStatus
  updatedBy: string
  changeNote?: string
}

export interface SectionSummary extends ReportSection {
  dataPointCount: number
  evidenceCount: number
  gapCount: number
  assumptionCount: number
  completenessPercentage: number
  ownerName: string
  progressStatus: ProgressStatus
}

export interface OrganizationalUnit {
  id: string
  name: string
  parentId?: string
  description: string
  createdAt: string
  createdBy: string
}

export interface CompletenessBreakdown {
  id: string
  name: string
  missingCount: number
  incompleteCount: number
  completeCount: number
  notApplicableCount: number
  totalCount: number
  completePercentage: number
}

export interface CompletenessStats {
  overall: CompletenessBreakdown
  byCategory: CompletenessBreakdown[]
  byOrganizationalUnit: CompletenessBreakdown[]
}
