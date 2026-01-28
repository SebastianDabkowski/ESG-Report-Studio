import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ArrowLeft } from '@phosphor-icons/react'
import type { Decision, DecisionVersion } from '@/lib/types'
import { getDecisionVersionHistory } from '@/lib/api'
import { formatDateTime } from '@/lib/helpers'

interface DecisionVersionHistoryProps {
  decision: Decision
  onClose: () => void
}

export default function DecisionVersionHistory({ decision, onClose }: DecisionVersionHistoryProps) {
  const [versions, setVersions] = useState<DecisionVersion[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadVersions()
  }, [decision.id])

  async function loadVersions() {
    try {
      setLoading(true)
      const data = await getDecisionVersionHistory(decision.id)
      setVersions(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load version history')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="sm" onClick={onClose}>
            <ArrowLeft className="w-4 h-4" />
          </Button>
          <div>
            <CardTitle>Version History: {decision.title}</CardTitle>
            <CardDescription>
              All versions of this decision (read-only)
            </CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {loading && <p className="text-muted-foreground">Loading version history...</p>}
        
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded mb-4">
            {error}
          </div>
        )}

        {!loading && (
          <div className="space-y-6">
            {/* Current version */}
            <div className="border-2 border-primary rounded-lg p-4 space-y-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Badge variant="default">Current (v{decision.version})</Badge>
                  <Badge
                    variant={
                      decision.status === 'active'
                        ? 'default'
                        : decision.status === 'deprecated'
                        ? 'secondary'
                        : 'outline'
                    }
                  >
                    {decision.status}
                  </Badge>
                </div>
                <p className="text-sm text-muted-foreground">
                  {decision.updatedBy 
                    ? `Updated by ${decision.updatedBy} on ${formatDateTime(decision.updatedAt!)}`
                    : `Created by ${decision.createdBy} on ${formatDateTime(decision.createdAt)}`
                  }
                </p>
              </div>

              <div>
                <h4 className="font-medium mb-1">{decision.title}</h4>
              </div>

              <div className="space-y-2 text-sm">
                <div>
                  <p className="font-medium mb-1">Context:</p>
                  <p className="text-muted-foreground whitespace-pre-wrap">{decision.context}</p>
                </div>
                <div>
                  <p className="font-medium mb-1">Decision:</p>
                  <p className="text-muted-foreground whitespace-pre-wrap">{decision.decisionText}</p>
                </div>
                <div>
                  <p className="font-medium mb-1">Alternatives Considered:</p>
                  <p className="text-muted-foreground whitespace-pre-wrap">{decision.alternatives}</p>
                </div>
                <div>
                  <p className="font-medium mb-1">Consequences:</p>
                  <p className="text-muted-foreground whitespace-pre-wrap">{decision.consequences}</p>
                </div>
                {decision.changeNote && (
                  <div>
                    <p className="font-medium mb-1">Change Note:</p>
                    <p className="text-muted-foreground whitespace-pre-wrap">{decision.changeNote}</p>
                  </div>
                )}
              </div>
            </div>

            {/* Previous versions */}
            {versions.length > 0 && (
              <>
                <div>
                  <h3 className="font-medium text-sm text-muted-foreground mb-3">Previous Versions (Read-Only)</h3>
                </div>
                {versions.map((version) => (
                  <div
                    key={version.id}
                    className="border rounded-lg p-4 space-y-3 bg-muted/30"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Badge variant="outline">v{version.version}</Badge>
                        <Badge variant="secondary">{version.status}</Badge>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        {version.createdBy} on {formatDateTime(version.createdAt)}
                      </p>
                    </div>

                    <div>
                      <h4 className="font-medium mb-1">{version.title}</h4>
                    </div>

                    <div className="space-y-2 text-sm">
                      <div>
                        <p className="font-medium mb-1">Context:</p>
                        <p className="text-muted-foreground whitespace-pre-wrap">{version.context}</p>
                      </div>
                      <div>
                        <p className="font-medium mb-1">Decision:</p>
                        <p className="text-muted-foreground whitespace-pre-wrap">{version.decisionText}</p>
                      </div>
                      <div>
                        <p className="font-medium mb-1">Alternatives Considered:</p>
                        <p className="text-muted-foreground whitespace-pre-wrap">{version.alternatives}</p>
                      </div>
                      <div>
                        <p className="font-medium mb-1">Consequences:</p>
                        <p className="text-muted-foreground whitespace-pre-wrap">{version.consequences}</p>
                      </div>
                      {version.changeNote && (
                        <div>
                          <p className="font-medium mb-1">Change Note:</p>
                          <p className="text-muted-foreground whitespace-pre-wrap">{version.changeNote}</p>
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </>
            )}

            {versions.length === 0 && (
              <p className="text-sm text-muted-foreground text-center py-4">
                No previous versions. This is the original decision.
              </p>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
