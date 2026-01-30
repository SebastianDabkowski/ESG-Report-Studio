export type UserRole = 'admin' | 'report-owner' | 'contributor' | 'auditor'

export type SectionStatus = 'draft' | 'in-review' | 'approved'

export type ProgressStatus = 'not-started' | 'in-progress' | 'blocked' | 'completed'

export type ReviewStatus = 'draft' | 'ready-for-review' | 'approved' | 'changes-requested'

export type CompletenessLevel = 'empty' | 'partial' | 'complete'

export type CompletenessStatus = 'missing' | 'incomplete' | 'complete' | 'not applicable'

export type EstimateType = 'point' | 'range' | 'proxy-based' | 'extrapolated'

export type ConfidenceLevel = 'low' | 'medium' | 'high'

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
  catalogCode?: string
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
  isMissing: boolean
  missingReason?: string
  missingReasonCategory?: string
  missingFlaggedBy?: string
  missingFlaggedAt?: string
  estimateType?: EstimateType
  estimateMethod?: string
  confidenceLevel?: ConfidenceLevel
  estimateInputSources?: EstimateInputSource[]
  estimateInputs?: string
  estimateAuthor?: string
  estimateCreatedAt?: string
  sourceReferences?: NarrativeSourceReference[]
  publicationSourceHash?: string
  provenanceLastVerified?: string
  provenanceNeedsReview?: boolean
  provenanceReviewReason?: string
  provenanceFlaggedBy?: string
  provenanceFlaggedAt?: string
  // Calculation Lineage fields
  isCalculated?: boolean
  calculationFormula?: string
  calculationInputIds?: string[]
  calculationInputSnapshot?: string
  calculationVersion?: number
  calculatedAt?: string
  calculatedBy?: string
  calculationNeedsRecalculation?: boolean
  recalculationReason?: string
  recalculationFlaggedAt?: string
}

export interface EstimateInputSource {
  sourceType: string
  sourceReference: string
  description: string
}

export interface NarrativeSourceReference {
  sourceType: string
  sourceReference: string
  description: string
  originSystem?: string
  ownerId?: string
  ownerName?: string
  lastUpdated?: string
  valueSnapshot?: string
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

export interface CalculationLineageResponse {
  dataPointId: string
  formula?: string
  version: number
  calculatedAt?: string
  calculatedBy?: string
  inputs: LineageInput[]
  inputSnapshot?: string
  needsRecalculation: boolean
  recalculationReason?: string
}

export interface LineageInput {
  dataPointId: string
  title: string
  currentValue?: string
  unit?: string
  valueAtCalculation?: string
  lastUpdated?: string
  hasChanged: boolean
}

export interface RecalculateDataPointRequest {
  calculatedBy: string
  changeNote?: string
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
  // Chain-of-custody metadata
  fileSize?: number
  checksum?: string
  contentType?: string
  integrityStatus: string // 'valid', 'failed', 'not-checked'
}

export interface EvidenceAccessLog {
  id: string
  evidenceId: string
  userId: string
  userName: string
  accessedAt: string
  action: string // 'download', 'view', 'validate'
  purpose?: string
}

export interface AssumptionSource {
  sourceType: string
  sourceReference: string
  description: string
}

export interface Assumption {
  id: string
  sectionId: string
  dataPointId?: string
  title: string
  description: string
  scope: string
  validityStartDate: string
  validityEndDate: string
  methodology: string
  limitations: string
  rationale?: string
  sources: AssumptionSource[]
  status: 'active' | 'deprecated' | 'invalid'
  replacementAssumptionId?: string
  deprecationJustification?: string
  version: number
  updatedBy?: string
  updatedAt?: string
  createdBy: string
  createdAt: string
  linkedDataPointIds: string[]
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

export interface Simplification {
  id: string
  sectionId: string
  title: string
  description: string
  affectedEntities: string[]
  affectedSites: string[]
  affectedProcesses: string[]
  impactLevel: 'low' | 'medium' | 'high'
  impactNotes?: string
  status: 'active' | 'removed'
  createdBy: string
  createdAt: string
  updatedBy?: string
  updatedAt?: string
}

export interface RemediationPlan {
  id: string
  sectionId: string
  title: string
  description: string
  targetPeriod: string
  ownerId: string
  ownerName: string
  priority: 'low' | 'medium' | 'high'
  status: 'planned' | 'in-progress' | 'completed' | 'cancelled'
  gapId?: string
  assumptionId?: string
  dataPointId?: string
  completedAt?: string
  completedBy?: string
  createdBy: string
  createdAt: string
  updatedBy?: string
  updatedAt?: string
}

export interface RemediationAction {
  id: string
  remediationPlanId: string
  title: string
  description: string
  ownerId: string
  ownerName: string
  dueDate: string
  status: 'pending' | 'in-progress' | 'completed' | 'cancelled'
  completedAt?: string
  completedBy?: string
  evidenceIds: string[]
  completionNotes?: string
  createdBy: string
  createdAt: string
  updatedBy?: string
  updatedAt?: string
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

export interface FlagMissingDataRequest {
  flaggedBy: string
  missingReasonCategory: MissingReasonCategory
  missingReason: string
}

export interface UnflagMissingDataRequest {
  unflaggedBy: string
  changeNote?: string
}

export type MissingReasonCategory =
  | 'not-measured'
  | 'not-applicable'
  | 'unavailable-from-supplier'
  | 'data-quality-issue'
  | 'system-limitation'
  | 'other'

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

export interface OwnerAssignment {
  ownerId: string
  ownerName: string
  ownerEmail: string
  sections: SectionSummary[]
  totalDataPoints: number
}

export interface ResponsibilityMatrix {
  assignments: OwnerAssignment[]
  totalSections: number
  unassignedSections: number
  periodId?: string
}

export interface ReadinessMetrics {
  ownershipPercentage: number
  completionPercentage: number
  blockedCount: number
  overdueCount: number
  totalItems: number
  itemsWithOwners: number
  completedItems: number
}

export interface ReadinessItem {
  id: string
  type: 'section' | 'datapoint'
  title: string
  category: 'environmental' | 'social' | 'governance'
  ownerId: string
  ownerName: string
  progressStatus: ProgressStatus
  isBlocked: boolean
  isOverdue: boolean
  deadline?: string
  completenessPercentage: number
}

export interface ReadinessReport {
  periodId?: string
  metrics: ReadinessMetrics
  items: ReadinessItem[]
}

export type ExceptionType = 'missing-data' | 'estimated-data' | 'simplified-scope' | 'other'
export type ExceptionStatus = 'pending' | 'accepted' | 'rejected'

export interface CompletionException {
  id: string
  sectionId: string
  dataPointId?: string
  title: string
  exceptionType: ExceptionType
  justification: string
  status: ExceptionStatus
  requestedBy: string
  requestedAt: string
  approvedBy?: string
  approvedAt?: string
  rejectedBy?: string
  rejectedAt?: string
  reviewComments?: string
  expiresAt?: string
}

export interface CreateCompletionExceptionRequest {
  sectionId: string
  dataPointId?: string
  title: string
  exceptionType: ExceptionType
  justification: string
  requestedBy: string
  expiresAt?: string
}

export interface ApproveCompletionExceptionRequest {
  approvedBy: string
  reviewComments?: string
}

export interface RejectCompletionExceptionRequest {
  rejectedBy: string
  reviewComments: string
}

export interface DataPointSummary {
  id: string
  title: string
  completenessStatus: string
  missingReason?: string
  estimateType?: string
  confidenceLevel?: string
}

export interface SectionCompletenessDetail {
  sectionId: string
  sectionTitle: string
  category: string
  missingItems: DataPointSummary[]
  estimatedItems: DataPointSummary[]
  simplifiedItems: DataPointSummary[]
  acceptedExceptions: CompletionException[]
}

export interface CompletenessValidationSummary {
  totalSections: number
  totalDataPoints: number
  missingCount: number
  estimatedCount: number
  simplifiedCount: number
  acceptedExceptionsCount: number
  pendingExceptionsCount: number
  completenessPercentage: number
  completenessWithExceptionsPercentage: number
}

export interface CompletenessValidationReport {
  periodId: string
  sections: SectionCompletenessDetail[]
  summary: CompletenessValidationSummary
}

// Gaps and Improvements Report Types
export interface GapsAndImprovementsReport {
  periodId?: string
  summary: GapsAndImprovementsSummary
  sections: SectionGapsAndImprovements[]
  autoGeneratedNarrative: string
  manualNarrative?: string
  generatedAt: string
  generatedBy: string
}

export interface GapsAndImprovementsSummary {
  totalGaps: number
  resolvedGaps: number
  unresolvedGaps: number
  totalAssumptions: number
  activeAssumptions: number
  deprecatedAssumptions: number
  totalSimplifications: number
  activeSimplifications: number
  totalRemediationPlans: number
  completedRemediationPlans: number
  inProgressRemediationPlans: number
  totalRemediationActions: number
  completedActions: number
  overdueActions: number
}

export interface SectionGapsAndImprovements {
  sectionId: string
  sectionTitle: string
  category: string
  gaps: GapWithRemediation[]
  assumptions: AssumptionReference[]
  simplifications: SimplificationReference[]
  remediationPlans: RemediationPlanWithActions[]
}

export interface GapWithRemediation {
  gap: Gap
  remediationPlan?: RemediationPlanWithActions
}

export interface AssumptionReference {
  assumption: Assumption
  linkedDataPointTitles: string[]
}

export interface SimplificationReference {
  simplification: Simplification
}

export interface RemediationPlanWithActions {
  plan: RemediationPlan
  actions: RemediationAction[]
}

// Gap Dashboard Types
export interface GapDashboardItem {
  gap: Gap
  sectionTitle: string
  category: string
  ownerName?: string
  ownerId?: string
  duePeriod?: string
  status: 'open' | 'resolved'
  remediationPlanId?: string
  remediationPlanStatus?: string
}

export interface GapDashboardResponse {
  gaps: GapDashboardItem[]
  summary: GapDashboardSummary
  totalCount: number
}

export interface GapDashboardSummary {
  totalGaps: number
  openGaps: number
  resolvedGaps: number
  highRiskGaps: number
  mediumRiskGaps: number
  lowRiskGaps: number
  withRemediationPlan: number
  withoutRemediationPlan: number
}

// Decision Log Types
export interface Decision {
  id: string
  sectionId?: string
  title: string
  context: string
  decisionText: string
  alternatives: string
  consequences: string
  status: 'active' | 'superseded' | 'deprecated'
  version: number
  referencedByFragmentIds: string[]
  createdBy: string
  createdAt: string
  updatedBy?: string
  updatedAt?: string
  changeNote?: string
}

export interface DecisionVersion {
  id: string
  decisionId: string
  version: number
  title: string
  context: string
  decisionText: string
  alternatives: string
  consequences: string
  status: string
  createdBy: string
  createdAt: string
  changeNote?: string
}

export interface CreateDecisionRequest {
  sectionId?: string
  title: string
  context: string
  decisionText: string
  alternatives: string
  consequences: string
}

export interface UpdateDecisionRequest {
  title: string
  context: string
  decisionText: string
  alternatives: string
  consequences: string
  changeNote: string
}

export interface LinkDecisionRequest {
  fragmentId: string
}

export interface UnlinkDecisionRequest {
  fragmentId: string
}

export interface DeprecateDecisionRequest {
  reason: string
}

// Fragment Audit Types
export interface FragmentSectionInfo {
  sectionId: string
  sectionTitle: string
  sectionCategory: string
  catalogCode?: string
}

export interface LinkedSource {
  sourceType: string
  sourceReference: string
  description: string
  navigationUrl?: string
  originSystem?: string
  ownerId?: string
  ownerName?: string
  lastUpdated?: string
}

export interface LinkedEvidence {
  evidenceId: string
  fileName: string
  description?: string
  uploadedBy: string
  uploadedAt: string
  fileUrl?: string
  checksum?: string
  integrityStatus: string
}

export interface LinkedDecision {
  decisionId: string
  title: string
  decisionText: string
  status: string
  version: number
  decisionBy: string
  decisionDate: string
}

export interface LinkedAssumption {
  assumptionId: string
  title: string
  description: string
  status: string
  version: number
  methodology?: string
  createdBy: string
  createdAt: string
}

export interface LinkedGap {
  gapId: string
  title: string
  description: string
  impact: string
  resolved: boolean
  improvementPlan?: string
}

export interface ProvenanceWarning {
  missingLinkType: string
  message: string
  severity: 'info' | 'warning' | 'error'
  recommendation?: string
}

export interface FragmentAuditView {
  fragmentType: string
  fragmentId: string
  stableFragmentIdentifier: string
  fragmentTitle: string
  fragmentContent: string
  sectionInfo?: FragmentSectionInfo
  linkedSources: LinkedSource[]
  linkedEvidenceFiles: LinkedEvidence[]
  linkedDecisions: LinkedDecision[]
  linkedAssumptions: LinkedAssumption[]
  linkedGaps: LinkedGap[]
  provenanceWarnings: ProvenanceWarning[]
  hasCompleteProvenance: boolean
  auditTrail: AuditLogEntry[]
}

export interface ExportFragmentMapping {
  exportId: string
  periodId: string
  exportFormat: string
  exportedAt: string
  exportedBy: string
  mappings: FragmentMapping[]
}

export interface FragmentMapping {
  stableFragmentIdentifier: string
  fragmentType: string
  fragmentId: string
  pageNumber?: number
  paragraphNumber?: string
  sectionHeading?: string
}

export interface RolloverOptions {
  copyStructure: boolean
  copyDisclosures: boolean
  copyDataValues: boolean
  copyAttachments: boolean
}

export interface RolloverRequest {
  sourcePeriodId: string
  targetPeriodName: string
  targetPeriodStartDate: string
  targetPeriodEndDate: string
  targetReportingMode?: ReportingMode
  targetReportScope?: ReportScope
  options: RolloverOptions
  performedBy: string
  ruleOverrides?: RolloverRuleOverride[]
  manualMappings?: ManualSectionMapping[]
}

export interface RolloverAuditLog {
  id: string
  sourcePeriodId: string
  sourcePeriodName: string
  targetPeriodId: string
  targetPeriodName: string
  performedBy: string
  performedByName: string
  performedAt: string
  options: RolloverOptions
  sectionsCopied: number
  dataPointsCopied: number
  gapsCopied: number
  assumptionsCopied: number
  remediationPlansCopied: number
  evidenceCopied: number
}

export interface RolloverResult {
  success: boolean
  errorMessage?: string
  targetPeriod?: ReportingPeriod
  auditLog?: RolloverAuditLog
  reconciliation?: RolloverReconciliation
}

export interface RolloverReconciliation {
  totalSourceSections: number
  mappedSections: number
  unmappedSections: number
  mappedItems: MappedSection[]
  unmappedItems: UnmappedSection[]
}

export interface MappedSection {
  sourceCatalogCode: string
  sourceTitle: string
  targetCatalogCode: string
  targetTitle: string
  mappingType: 'automatic' | 'manual'
  dataPointsCopied: number
}

export interface UnmappedSection {
  sourceCatalogCode?: string
  sourceTitle: string
  sourceSectionId: string
  reason: string
  suggestedActions: string[]
  affectedDataPoints: number
}

export interface ManualSectionMapping {
  sourceCatalogCode: string
  targetCatalogCode: string
}

// Rollover Rules Types
export type DataTypeRolloverRuleType = 'Copy' | 'Reset' | 'CopyAsDraft'

export interface DataTypeRolloverRule {
  id: string
  dataType: string
  ruleType: DataTypeRolloverRuleType
  description?: string
  createdAt: string
  createdBy: string
  updatedAt?: string
  updatedBy?: string
  version: number
}

export interface RolloverRuleHistory {
  id: string
  ruleId: string
  dataType: string
  ruleType: DataTypeRolloverRuleType
  description?: string
  version: number
  changedAt: string
  changedBy: string
  changedByName: string
  changeType: 'created' | 'updated' | 'deleted'
}

export interface SaveDataTypeRolloverRuleRequest {
  dataType: string
  ruleType: string
  description?: string
  savedBy: string
}

export interface RolloverRuleOverride {
  dataType: string
  ruleType: DataTypeRolloverRuleType
}
