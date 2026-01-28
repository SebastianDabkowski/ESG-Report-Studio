import type { MissingReasonCategory } from './types'

export const MISSING_REASON_CATEGORIES: { value: MissingReasonCategory; label: string; description: string }[] = [
  { 
    value: 'not-measured', 
    label: 'Not Measured', 
    description: 'Data was not measured or tracked during the reporting period' 
  },
  { 
    value: 'not-applicable', 
    label: 'Not Applicable', 
    description: 'This data point does not apply to our organization or operations' 
  },
  { 
    value: 'unavailable-from-supplier', 
    label: 'Unavailable from Supplier', 
    description: 'Required data is not available from suppliers or third parties' 
  },
  { 
    value: 'data-quality-issue', 
    label: 'Data Quality Issue', 
    description: 'Data exists but quality is insufficient for reporting' 
  },
  { 
    value: 'system-limitation', 
    label: 'System Limitation', 
    description: 'Technical or process limitations prevent data collection' 
  },
  { 
    value: 'other', 
    label: 'Other', 
    description: 'Another reason not covered by the above categories' 
  }
]

export const MISSING_REASON_CATEGORY_LABELS: Record<MissingReasonCategory, string> = {
  'not-measured': 'Not Measured',
  'not-applicable': 'Not Applicable',
  'unavailable-from-supplier': 'Unavailable from Supplier',
  'data-quality-issue': 'Data Quality Issue',
  'system-limitation': 'System Limitation',
  'other': 'Other'
}
