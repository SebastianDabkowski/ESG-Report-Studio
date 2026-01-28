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
  Calendar,
  User as UserIcon
} from '@phosphor-icons/react'
import type { RemediationAction, User } from '@/lib/types'
import { 
  getRemediationActions, 
  deleteRemediationAction,
  completeRemediationAction
} from '@/lib/api'
import { RemediationActionForm } from './RemediationActionForm'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'

interface RemediationActionsListProps {
  remediationPlanId: string
  users?: User[]
}

export function RemediationActionsList({ remediationPlanId, users = [] }: RemediationActionsListProps) {
  const [actions, setActions] = useState<RemediationAction[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [editingAction, setEditingAction] = useState<RemediationAction | undefined>()

  const loadActions = async () => {
    setIsLoading(true)
    setError(null)
    try {
      const data = await getRemediationActions(remediationPlanId)
      setActions(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load actions')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    loadActions()
  }, [remediationPlanId])

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this action?')) return

    try {
      await deleteRemediationAction(id)
      await loadActions()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete action')
    }
  }

  const handleComplete = async (action: RemediationAction) => {
    try {
      await completeRemediationAction(action.id, {
        completedBy: 'current-user', // TODO: Get from auth context
        completionNotes: '',
        evidenceIds: []
      })
      await loadActions()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to complete action')
    }
  }

  const handleEdit = (action: RemediationAction) => {
    setEditingAction(action)
    setShowForm(true)
  }

  const handleFormSuccess = async () => {
    setShowForm(false)
    setEditingAction(undefined)
    await loadActions()
  }

  const handleFormCancel = () => {
    setShowForm(false)
    setEditingAction(undefined)
  }

  const getStatusBadge = (status: RemediationAction['status']) => {
    const statusConfig = {
      pending: { variant: 'secondary' as const, label: 'Pending' },
      'in-progress': { variant: 'default' as const, label: 'In Progress' },
      completed: { variant: 'default' as const, label: 'Completed', className: 'bg-green-100 text-green-800' },
      cancelled: { variant: 'destructive' as const, label: 'Cancelled' }
    }
    const config = statusConfig[status]
    return <Badge variant={config.variant} className={config.className}>{config.label}</Badge>
  }

  const isOverdue = (dueDate: string, status: RemediationAction['status']) => {
    if (status === 'completed' || status === 'cancelled') return false
    const due = new Date(dueDate)
    const now = new Date()
    // Compare dates at start of day to avoid timezone issues
    due.setHours(0, 0, 0, 0)
    now.setHours(0, 0, 0, 0)
    return due < now
  }

  const isUpcoming = (dueDate: string, status: RemediationAction['status']) => {
    if (status === 'completed' || status === 'cancelled') return false
    const due = new Date(dueDate)
    const now = new Date()
    // Compare dates at start of day to avoid timezone issues
    due.setHours(0, 0, 0, 0)
    now.setHours(0, 0, 0, 0)
    const daysUntilDue = Math.ceil((due.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))
    return daysUntilDue >= 0 && daysUntilDue <= 14 // Upcoming if within 14 days
  }

  if (isLoading) {
    return <div className="text-sm text-gray-500">Loading actions...</div>
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
        <h3 className="text-lg font-semibold">Actions</h3>
        <Button size="sm" onClick={() => setShowForm(true)}>
          <Plus className="h-4 w-4 mr-1" />
          Add Action
        </Button>
      </div>

      {actions.length === 0 ? (
        <Card className="p-6 text-center text-gray-500">
          <p>No actions yet. Add an action to get started.</p>
        </Card>
      ) : (
        <div className="space-y-3">
          {actions.map(action => (
            <Card key={action.id} className="p-4">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <h4 className="font-medium">{action.title}</h4>
                    {getStatusBadge(action.status)}
                    {isOverdue(action.dueDate, action.status) && (
                      <Badge variant="destructive">Overdue</Badge>
                    )}
                    {!isOverdue(action.dueDate, action.status) && isUpcoming(action.dueDate, action.status) && (
                      <Badge variant="default" className="bg-yellow-100 text-yellow-800">Due Soon</Badge>
                    )}
                  </div>
                  <p className="text-sm text-gray-600 mb-3">{action.description}</p>
                  <div className="flex items-center gap-4 text-xs text-gray-500">
                    <div className="flex items-center gap-1">
                      <UserIcon className="h-3 w-3" />
                      <span>{action.ownerName}</span>
                    </div>
                    <div className="flex items-center gap-1">
                      <Calendar className="h-3 w-3" />
                      <span>Due: {new Date(action.dueDate).toLocaleDateString()}</span>
                    </div>
                    {action.completedAt && (
                      <div className="flex items-center gap-1">
                        <Check className="h-3 w-3" />
                        <span>Completed: {new Date(action.completedAt).toLocaleDateString()}</span>
                      </div>
                    )}
                  </div>
                  {action.completionNotes && (
                    <p className="text-xs text-gray-500 mt-2 italic">
                      Note: {action.completionNotes}
                    </p>
                  )}
                </div>
                <div className="flex items-center gap-1 ml-4">
                  {action.status !== 'completed' && action.status !== 'cancelled' && (
                    <>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => handleComplete(action)}
                        title="Mark as complete"
                      >
                        <Check className="h-4 w-4" />
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => handleEdit(action)}
                        title="Edit action"
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </>
                  )}
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => handleDelete(action.id)}
                    title="Delete action"
                  >
                    <Trash className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      <Dialog open={showForm} onOpenChange={setShowForm}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              {editingAction ? 'Edit Action' : 'Add Action'}
            </DialogTitle>
          </DialogHeader>
          <RemediationActionForm
            remediationPlanId={remediationPlanId}
            action={editingAction}
            users={users}
            onSuccess={handleFormSuccess}
            onCancel={handleFormCancel}
          />
        </DialogContent>
      </Dialog>
    </div>
  )
}
