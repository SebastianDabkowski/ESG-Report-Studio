import { CompletenessLevel, SectionStatus, Classification, UserRole } from './types'

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
