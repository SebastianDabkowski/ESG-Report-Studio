import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Plus, PencilSimple, Trash, Warning, ClockCounterClockwise } from '@phosphor-icons/react'
import type { Decision } from '@/lib/types'
import { getDecisions, deleteDecision } from '@/lib/api'
import { formatDateTime } from '@/lib/helpers'
import DecisionForm from './DecisionForm'
import DecisionVersionHistory from './DecisionVersionHistory'
import DeprecateDecisionDialog from './DeprecateDecisionDialog'

interface DecisionsListProps {
  sectionId?: string
}

export default function DecisionsList({ sectionId }: DecisionsListProps) {
  const [decisions, setDecisions] = useState<Decision[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [editingDecision, setEditingDecision] = useState<Decision | undefined>(undefined)
  const [viewingHistory, setViewingHistory] = useState<Decision | undefined>(undefined)
  const [deprecatingDecision, setDeprecatingDecision] = useState<Decision | undefined>(undefined)

  useEffect(() => {
    loadDecisions()
  }, [sectionId])

  async function loadDecisions() {
    try {
      setLoading(true)
      const data = await getDecisions(sectionId)
      setDecisions(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load decisions')
    } finally {
      setLoading(false)
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Are you sure you want to delete this decision? This action cannot be undone.')) {
      return
    }

    try {
      await deleteDecision(id)
      await loadDecisions()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete decision')
    }
  }

  function handleEdit(decision: Decision) {
    setEditingDecision(decision)
    setShowForm(true)
  }

  function handleFormSuccess() {
    setShowForm(false)
    setEditingDecision(undefined)
    loadDecisions()
  }

  function handleFormCancel() {
    setShowForm(false)
    setEditingDecision(undefined)
  }

  function handleDeprecateSuccess() {
    setDeprecatingDecision(undefined)
    loadDecisions()
  }

  if (showForm) {
    return (
      <DecisionForm
        sectionId={sectionId}
        decision={editingDecision}
        onSuccess={handleFormSuccess}
        onCancel={handleFormCancel}
      />
    )
  }

  if (viewingHistory) {
    return (
      <DecisionVersionHistory
        decision={viewingHistory}
        onClose={() => setViewingHistory(undefined)}
      />
    )
  }

  return (
    <>
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Decision Log</CardTitle>
              <CardDescription>
                Record and track decisions about assumptions, simplifications, and boundaries
              </CardDescription>
            </div>
            <Button onClick={() => setShowForm(true)} size="sm">
              <Plus className="w-4 h-4 mr-1" />
              Add Decision
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {loading && <p className="text-muted-foreground">Loading decisions...</p>}
          
          {error && (
            <div className="bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded mb-4">
              {error}
            </div>
          )}

          {!loading && decisions.length === 0 && (
            <div className="text-center py-8 text-muted-foreground">
              <p className="mb-2">No decisions recorded yet.</p>
              <p className="text-sm">
                Click "Add Decision" to record your first decision.
              </p>
            </div>
          )}

          {!loading && decisions.length > 0 && (
            <div className="space-y-4">
              {decisions.map((decision) => (
                <div
                  key={decision.id}
                  className="border rounded-lg p-4 space-y-3"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <h4 className="font-medium">{decision.title}</h4>
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
                        <Badge variant="outline">v{decision.version}</Badge>
                        {decision.referencedByFragmentIds.length > 0 && (
                          <Badge variant="outline">
                            {decision.referencedByFragmentIds.length} reference(s)
                          </Badge>
                        )}
                      </div>
                      <p className="text-sm text-muted-foreground">
                        Created by {decision.createdBy} on {formatDateTime(decision.createdAt)}
                        {decision.updatedBy && ` • Updated by ${decision.updatedBy} on ${formatDateTime(decision.updatedAt!)}`}
                      </p>
                    </div>
                    <div className="flex gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setViewingHistory(decision)}
                        title="View version history"
                      >
                        <ClockCounterClockwise className="w-4 h-4" />
                      </Button>
                      {decision.status === 'active' && (
                        <>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleEdit(decision)}
                            title="Edit decision"
                          >
                            <PencilSimple className="w-4 h-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => setDeprecatingDecision(decision)}
                            title="Deprecate decision"
                          >
                            <Warning className="w-4 h-4" />
                          </Button>
                        </>
                      )}
                      {decision.referencedByFragmentIds.length === 0 && (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(decision.id)}
                          title="Delete decision"
                        >
                          <Trash className="w-4 h-4" />
                        </Button>
                      )}
                    </div>
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
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {deprecatingDecision && (
        <DeprecateDecisionDialog
          decision={deprecatingDecision}
          onSuccess={handleDeprecateSuccess}
          onCancel={() => setDeprecatingDecision(undefined)}
        />
      )}
    </>
  )
}
