import { CompletenessLevel, CompletenessStatus, SectionStatus, ProgressStatus, Classification, UserRole } from './types'

export function getStatusColor(status: SectionStatus): string {
  switch (status) {
    case 'approved':
      return 'bg-success text-success-foreground'
    case 'in-review':
      return 'bg-warning text-warning-foreground'
    case 'draft':
      return 'bg-muted text-muted-foreground'
  }
}

export function getStatusBorderColor(status: SectionStatus): string {
  switch (status) {
    case 'approved':
      return 'border-l-success'
    case 'in-review':
      return 'border-l-warning'
    case 'draft':
      return 'border-l-muted-foreground'
  }
}

export function getCompletenessColor(level: CompletenessLevel): string {
  switch (level) {
    case 'complete':
      return 'bg-success'
    case 'partial':
      return 'bg-warning'
    case 'empty':
      return 'bg-muted-foreground'
  }
}

export function getCompletenessStatusColor(status: CompletenessStatus): string {
  switch (status) {
    case 'complete':
      return 'bg-green-100 text-green-800 border-green-300'
    case 'incomplete':
      return 'bg-amber-100 text-amber-800 border-amber-300'
    case 'missing':
      return 'bg-red-100 text-red-800 border-red-300'
    case 'not applicable':
      return 'bg-gray-100 text-gray-800 border-gray-300'
  }
}

export function getProgressStatusColor(status: ProgressStatus): string {
  switch (status) {
    case 'completed':
      return 'bg-success text-success-foreground'
    case 'in-progress':
      return 'bg-info text-info-foreground'
    case 'blocked':
      return 'bg-alert text-alert-foreground'
    case 'not-started':
      return 'bg-muted text-muted-foreground'
  }
}

export function getProgressStatusLabel(status: ProgressStatus): string {
  switch (status) {
    case 'completed':
      return 'Completed'
    case 'in-progress':
      return 'In Progress'
    case 'blocked':
      return 'Blocked'
    case 'not-started':
      return 'Not Started'
  }
}

export function getClassificationColor(classification: Classification): string {
  switch (classification) {
    case 'fact':
      return 'bg-fact text-white'
    case 'declaration':
      return 'bg-declaration text-white'
    case 'plan':
      return 'bg-plan text-white'
  }
}

export function calculateCompleteness(
  dataPointCount: number,
  evidenceCount: number,
  gapCount: number
): { level: CompletenessLevel; percentage: number } {
  const totalExpected = 10
  const actual = Math.min(dataPointCount * 0.6 + evidenceCount * 0.3, totalExpected)
  const gapPenalty = gapCount * 0.5
  const score = Math.max(0, actual - gapPenalty)
  const percentage = Math.round((score / totalExpected) * 100)

  let level: CompletenessLevel
  if (percentage === 0) level = 'empty'
  else if (percentage < 70) level = 'partial'
  else level = 'complete'

  return { level, percentage }
}

export function getRoleIcon(role: UserRole): string {
  switch (role) {
    case 'admin':
      return 'UserGear'
    case 'report-owner':
      return 'Crown'
    case 'contributor':
      return 'User'
    case 'auditor':
      return 'Eye'
  }
}

export function getRoleLabel(role: UserRole): string {
  switch (role) {
    case 'admin':
      return 'Admin'
    case 'report-owner':
      return 'Report Owner'
    case 'contributor':
      return 'Contributor'
    case 'auditor':
      return 'Auditor'
  }
}

export function formatDate(dateString: string): string {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  }).format(date)
}

export function formatDateTime(dateString: string): string {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date)
}

export function generateId(): string {
  return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
}

export function canApproveSection(userRole: UserRole): boolean {
  return userRole === 'admin' || userRole === 'report-owner'
}

export function canEditSection(userRole: UserRole): boolean {
  return userRole === 'admin' || userRole === 'report-owner' || userRole === 'contributor'
}

export function isReadOnly(userRole: UserRole): boolean {
  return userRole === 'auditor'
}

/**
 * Check if a user has permission to export reports.
 * Permission is granted if:
 * - User has canExport flag set to true (global permission), OR
 * - User is the owner of the specific reporting period (owner-based permission)
 */
export function canUserExport(user: { canExport?: boolean; id: string }, periodOwnerId?: string): boolean {
  // Check global export permission
  if (user.canExport === true) {
    return true
  }
  
  // Check owner-based permission for specific period
  if (periodOwnerId && user.id === periodOwnerId) {
    return true
  }
  
  return false
}
