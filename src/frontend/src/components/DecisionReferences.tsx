import { useState, useEffect, useCallback } from 'react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ScrollText } from '@phosphor-icons/react'
import type { Decision } from '@/lib/types'
import { getDecisionsByFragment } from '@/lib/api'
import { formatDateTime } from '@/lib/helpers'

interface DecisionReferencesProps {
  fragmentId: string
}

export default function DecisionReferences({ fragmentId }: DecisionReferencesProps) {
  const [decisions, setDecisions] = useState<Decision[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [expanded, setExpanded] = useState<string[]>([])

  const loadDecisions = useCallback(async () => {
    try {
      setLoading(true)
      const data = await getDecisionsByFragment(fragmentId)
      setDecisions(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load decisions')
    } finally {
      setLoading(false)
    }
  }, [fragmentId])

  useEffect(() => {
    loadDecisions()
  }, [loadDecisions])

  function toggleExpanded(decisionId: string) {
    setExpanded(prev => 
      prev.includes(decisionId) 
        ? prev.filter(id => id !== decisionId)
        : [...prev, decisionId]
    )
  }

  if (loading) {
    return (
      <div className="text-sm text-muted-foreground">
        Loading decision references...
      </div>
    )
  }

  if (error) {
    return (
      <div className="text-sm text-red-600">
        {error}
      </div>
    )
  }

  if (decisions.length === 0) {
    return (
      <div className="text-sm text-muted-foreground">
        No decisions reference this data point.
      </div>
    )
  }

  return (
    <div className="space-y-2">
      {decisions.map((decision) => {
        const isExpanded = expanded.includes(decision.id)
        return (
          <div
            key={decision.id}
            className="border rounded-md p-3 space-y-2 bg-blue-50/50"
          >
            <div className="flex items-start justify-between gap-2">
              <div className="flex-1">
                <div className="flex items-center gap-2 mb-1">
                  <h5 className="font-medium text-sm">{decision.title}</h5>
                  <Badge
                    variant={
                      decision.status === 'active'
                        ? 'default'
                        : decision.status === 'deprecated'
                        ? 'secondary'
                        : 'outline'
                    }
                    className="text-xs"
                  >
                    {decision.status}
                  </Badge>
                  <Badge variant="outline" className="text-xs">
                    v{decision.version}
                  </Badge>
                </div>
                <p className="text-xs text-muted-foreground">
                  Created by {decision.createdBy} on {formatDateTime(decision.createdAt)}
                </p>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => toggleExpanded(decision.id)}
              >
                {isExpanded ? 'Collapse' : 'Expand'}
              </Button>
            </div>

            {isExpanded && (
              <div className="space-y-2 text-xs pt-2 border-t">
                <div>
                  <p className="font-medium mb-1">Context:</p>
                  <p className="text-muted-foreground whitespace-pre-wrap">{decision.context}</p>
                </div>
                <div>
                  <p className="font-medium mb-1">Decision:</p>
                  <p className="text-muted-foreground whitespace-pre-wrap">{decision.decisionText}</p>
                </div>
                <div>
                  <p className="font-medium mb-1">Alternatives:</p>
                  <p className="text-muted-foreground whitespace-pre-wrap">{decision.alternatives}</p>
                </div>
                <div>
                  <p className="font-medium mb-1">Consequences:</p>
                  <p className="text-muted-foreground whitespace-pre-wrap">{decision.consequences}</p>
                </div>
              </div>
            )}
          </div>
        )
      })}
    </div>
  )
}
