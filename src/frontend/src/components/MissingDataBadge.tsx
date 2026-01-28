import { Warning } from '@phosphor-icons/react'
import { Badge } from '@/components/ui/badge'
import { MISSING_REASON_CATEGORY_LABELS } from '@/lib/missingDataConstants'
import type { DataPoint } from '@/lib/types'

interface MissingDataBadgeProps {
  dataPoint: DataPoint
  showReason?: boolean
}

export default function MissingDataBadge({ dataPoint, showReason = false }: MissingDataBadgeProps) {
  if (!dataPoint.isMissing) {
    return null
  }

  const categoryLabel = dataPoint.missingReasonCategory 
    ? MISSING_REASON_CATEGORY_LABELS[dataPoint.missingReasonCategory] || dataPoint.missingReasonCategory
    : 'Missing'

  return (
    <div className="flex flex-col gap-2">
      <Badge variant="destructive" className="w-fit">
        <Warning className="h-3 w-3 mr-1" />
        Missing: {categoryLabel}
      </Badge>
      {showReason && dataPoint.missingReason && (
        <p className="text-sm text-muted-foreground">
          {dataPoint.missingReason}
        </p>
      )}
    </div>
  )
}
