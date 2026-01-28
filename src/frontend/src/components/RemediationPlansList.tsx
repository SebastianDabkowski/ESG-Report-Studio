import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { 
  Plus, 
  Pencil, 
  Trash, 
  Check,
  WarningCircle,
  ArrowRight,
  Calendar,
  User as UserIcon,
  Target
} from '@phosphor-icons/react'
import type { RemediationPlan, User } from '@/lib/types'
import { 
  getRemediationPlans, 
  deleteRemediationPlan,
  completeRemediationPlan
} from '@/lib/api'
import { RemediationPlanForm } from './RemediationPlanForm'
import { RemediationActionsList } from './RemediationActionsList'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'

interface RemediationPlansListProps {
  sectionId: string
  gapId?: string
  assumptionId?: string
  dataPointId?: string
  users?: User[]
}

export function RemediationPlansList({ 
  sectionId, 
  gapId,
  assumptionId,
  dataPointId,
  users = [] 
}: RemediationPlansListProps) {
  const [plans, setPlans] = useState<RemediationPlan[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [editingPlan, setEditingPlan] = useState<RemediationPlan | undefined>()
  const [expandedPlanId, setExpandedPlanId] = useState<string | null>(null)

  const loadPlans = async () => {
    setIsLoading(true)
    setError(null)
    try {
      const data = await getRemediationPlans(sectionId)
      setPlans(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load remediation plans')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    loadPlans()
  }, [sectionId])

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this remediation plan and all its actions?')) return

    try {
      await deleteRemediationPlan(id)
      await loadPlans()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete plan')
    }
  }

  const handleComplete = async (plan: RemediationPlan) => {
    try {
      await completeRemediationPlan(plan.id, 'current-user') // TODO: Get from auth context
      await loadPlans()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to complete plan')
    }
  }

  const handleEdit = (plan: RemediationPlan) => {
    setEditingPlan(plan)
    setShowForm(true)
  }

  const handleFormSuccess = async () => {
    setShowForm(false)
    setEditingPlan(undefined)
    await loadPlans()
  }

  const handleFormCancel = () => {
    setShowForm(false)
    setEditingPlan(undefined)
  }

  const toggleExpanded = (planId: string) => {
    setExpandedPlanId(expandedPlanId === planId ? null : planId)
  }

  const getStatusBadge = (status: RemediationPlan['status']) => {
    const statusConfig = {
      planned: { variant: 'secondary' as const, label: 'Planned' },
      'in-progress': { variant: 'default' as const, label: 'In Progress' },
      completed: { variant: 'success' as const, label: 'Completed' },
      cancelled: { variant: 'destructive' as const, label: 'Cancelled' }
    }
    const config = statusConfig[status]
    return <Badge variant={config.variant}>{config.label}</Badge>
  }

  const getPriorityBadge = (priority: RemediationPlan['priority']) => {
    const priorityConfig = {
      low: { variant: 'secondary' as const, label: 'Low' },
      medium: { variant: 'default' as const, label: 'Medium' },
      high: { variant: 'destructive' as const, label: 'High' }
    }
    const config = priorityConfig[priority]
    return <Badge variant={config.variant}>{config.label}</Badge>
  }

  if (isLoading) {
    return <div className="text-sm text-gray-500">Loading remediation plans...</div>
  }

  return (
    <div className="space-y-4">
      {error && (
        <Alert variant="destructive">
          <WarningCircle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <div className="flex justify-between items-center">
        <h3 className="text-lg font-semibold">Remediation Plans</h3>
        <Button size="sm" onClick={() => setShowForm(true)}>
          <Plus className="h-4 w-4 mr-1" />
          Add Plan
        </Button>
      </div>

      {plans.length === 0 ? (
        <Card className="p-6 text-center text-gray-500">
          <p>No remediation plans yet. Create a plan to track missing data resolution.</p>
        </Card>
      ) : (
        <div className="space-y-3">
          {plans.map(plan => (
            <Card key={plan.id} className="p-4">
              <div className="flex items-start justify-between mb-3">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <h4 className="font-medium">{plan.title}</h4>
                    {getStatusBadge(plan.status)}
                    {getPriorityBadge(plan.priority)}
                  </div>
                  <p className="text-sm text-gray-600 mb-3">{plan.description}</p>
                  <div className="flex items-center gap-4 text-xs text-gray-500">
                    <div className="flex items-center gap-1">
                      <UserIcon className="h-3 w-3" />
                      <span>{plan.ownerName}</span>
                    </div>
                    <div className="flex items-center gap-1">
                      <Target className="h-3 w-3" />
                      <span>Target: {plan.targetPeriod}</span>
                    </div>
                    {plan.completedAt && (
                      <div className="flex items-center gap-1">
                        <Check className="h-3 w-3" />
                        <span>Completed: {new Date(plan.completedAt).toLocaleDateString()}</span>
                      </div>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-1 ml-4">
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => toggleExpanded(plan.id)}
                    title={expandedPlanId === plan.id ? "Hide actions" : "Show actions"}
                  >
                    <ArrowRight 
                      className={`h-4 w-4 transition-transform ${expandedPlanId === plan.id ? 'rotate-90' : ''}`} 
                    />
                  </Button>
                  {plan.status !== 'completed' && plan.status !== 'cancelled' && (
                    <>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => handleComplete(plan)}
                        title="Mark as complete"
                      >
                        <Check className="h-4 w-4" />
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => handleEdit(plan)}
                        title="Edit plan"
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </>
                  )}
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => handleDelete(plan.id)}
                    title="Delete plan"
                  >
                    <Trash className="h-4 w-4" />
                  </Button>
                </div>
              </div>

              {expandedPlanId === plan.id && (
                <div className="mt-4 pt-4 border-t">
                  <RemediationActionsList 
                    remediationPlanId={plan.id} 
                    users={users}
                  />
                </div>
              )}
            </Card>
          ))}
        </div>
      )}

      <Dialog open={showForm} onOpenChange={setShowForm}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              {editingPlan ? 'Edit Remediation Plan' : 'Create Remediation Plan'}
            </DialogTitle>
          </DialogHeader>
          <RemediationPlanForm
            sectionId={sectionId}
            plan={editingPlan}
            gapId={gapId}
            assumptionId={assumptionId}
            dataPointId={dataPointId}
            users={users}
            onSuccess={handleFormSuccess}
            onCancel={handleFormCancel}
          />
        </DialogContent>
      </Dialog>
    </div>
  )
}
