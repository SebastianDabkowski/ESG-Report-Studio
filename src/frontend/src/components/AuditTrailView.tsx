import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { useKV } from '@github/spark/hooks'
import { ClockCounterClockwise } from '@phosphor-icons/react'
import type { AuditLogEntry } from '@/lib/types'
import { formatDateTime } from '@/lib/helpers'

export default function AuditTrailView() {
  const [auditLog] = useKV<AuditLogEntry[]>('audit-log', [])

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold tracking-tight text-foreground">
          Audit Trail
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          Complete history of all changes and actions
        </p>
      </div>

      {auditLog && auditLog.length > 0 ? (
        <div className="space-y-3">
          {auditLog.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()).map(entry => (
            <Card key={entry.id}>
              <CardContent className="py-4">
                <div className="flex items-start gap-4">
                  <ClockCounterClockwise size={18} className="text-muted-foreground mt-0.5 flex-shrink-0" />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="font-medium text-sm">{entry.userName}</span>
                      <span className="text-muted-foreground text-xs">•</span>
                      <Badge variant="outline" className="text-xs">{entry.action}</Badge>
                      <Badge variant="secondary" className="text-xs">{entry.entityType}</Badge>
                    </div>
                    <div className="text-xs text-muted-foreground font-mono">
                      {formatDateTime(entry.timestamp)}
                    </div>
                    {entry.changes && entry.changes.length > 0 && (
                      <div className="mt-2 space-y-1">
                        {entry.changes.map((change, idx) => (
                          <div key={idx} className="text-xs">
                            <span className="font-medium">{change.field}:</span>{' '}
                            <span className="text-muted-foreground line-through">{change.oldValue}</span>
                            {' → '}
                            <span className="text-foreground">{change.newValue}</span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <ClockCounterClockwise size={48} weight="duotone" className="text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No Audit Entries</h3>
            <p className="text-sm text-muted-foreground text-center max-w-md">
              All system actions will be logged here for compliance and traceability.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}