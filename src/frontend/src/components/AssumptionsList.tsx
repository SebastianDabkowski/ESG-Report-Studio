import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { 
  Notebook, 
  Plus, 
  Pencil, 
  Trash, 
  Warning, 
  Check, 
  CalendarBlank,
  Target,
  Info
} from '@phosphor-icons/react'
import type { Assumption } from '@/lib/types'
import { getAssumptions, deleteAssumption, deprecateAssumption } from '@/lib/api'
import { AssumptionForm } from './AssumptionForm'
import { DeprecateAssumptionDialog } from './DeprecateAssumptionDialog'

interface AssumptionsListProps {
  sectionId: string
}

export function AssumptionsList({ sectionId }: AssumptionsListProps) {
  const [assumptions, setAssumptions] = useState<Assumption[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [editingAssumption, setEditingAssumption] = useState<Assumption | undefined>()
  const [deprecatingAssumption, setDeprecatingAssumption] = useState<Assumption | undefined>()

  const loadAssumptions = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await getAssumptions(sectionId)
      setAssumptions(data)
    } catch (err) {
      console.error('Error loading assumptions:', err)
      setError(err instanceof Error ? err.message : 'Failed to load assumptions')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadAssumptions()
  }, [sectionId])

  const handleCreateNew = () => {
    setEditingAssumption(undefined)
    setShowForm(true)
  }

  const handleEdit = (assumption: Assumption) => {
    if (assumption.status !== 'active') {
      setError('Cannot edit a deprecated or invalid assumption')
      return
    }
    setEditingAssumption(assumption)
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this assumption?')) {
      return
    }

    try {
      await deleteAssumption(id)
      await loadAssumptions()
    } catch (err) {
      console.error('Error deleting assumption:', err)
      setError(err instanceof Error ? err.message : 'Failed to delete assumption')
    }
  }

  const handleDeprecate = (assumption: Assumption) => {
    setDeprecatingAssumption(assumption)
  }

  const handleFormSuccess = async () => {
    setShowForm(false)
    setEditingAssumption(undefined)
    await loadAssumptions()
  }

  const handleDeprecateSuccess = async () => {
    setDeprecatingAssumption(undefined)
    await loadAssumptions()
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'active':
        return <Badge className="bg-green-100 text-green-800 border-green-200">Active</Badge>
      case 'deprecated':
        return <Badge className="bg-yellow-100 text-yellow-800 border-yellow-200">Deprecated</Badge>
      case 'invalid':
        return <Badge className="bg-red-100 text-red-800 border-red-200">Invalid</Badge>
      default:
        return <Badge variant="outline">{status}</Badge>
    }
  }

  const formatDate = (dateString: string) => {
    try {
      return new Date(dateString).toLocaleDateString()
    } catch {
      return dateString
    }
  }

  if (showForm) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>
            {editingAssumption ? 'Edit Assumption' : 'New Assumption'}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <AssumptionForm
            sectionId={sectionId}
            assumption={editingAssumption}
            onSuccess={handleFormSuccess}
            onCancel={() => {
              setShowForm(false)
              setEditingAssumption(undefined)
            }}
          />
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Notebook className="h-5 w-5 text-slate-600" weight="duotone" />
          <h3 className="text-lg font-semibold">Assumptions</h3>
        </div>
        <Button onClick={handleCreateNew} size="sm">
          <Plus className="h-4 w-4 mr-2" />
          Add Assumption
        </Button>
      </div>

      {error && (
        <Alert variant="destructive">
          <Warning className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {loading ? (
        <Card>
          <CardContent className="py-8 text-center text-slate-500">
            Loading assumptions...
          </CardContent>
        </Card>
      ) : assumptions.length === 0 ? (
        <Card>
          <CardContent className="py-8 text-center">
            <Notebook className="h-12 w-12 mx-auto mb-3 text-slate-300" weight="duotone" />
            <p className="text-slate-500 mb-4">No assumptions recorded yet</p>
            <Button onClick={handleCreateNew} variant="outline">
              <Plus className="h-4 w-4 mr-2" />
              Add First Assumption
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {assumptions.map((assumption) => (
            <Card key={assumption.id} className={assumption.status !== 'active' ? 'opacity-75' : ''}>
              <CardContent className="pt-6">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex-1 space-y-3">
                    <div className="flex items-start gap-3">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          <h4 className="font-semibold text-slate-900">{assumption.title}</h4>
                          {getStatusBadge(assumption.status)}
                          {assumption.version > 1 && (
                            <Badge variant="outline" className="text-xs">
                              v{assumption.version}
                            </Badge>
                          )}
                        </div>
                        <p className="text-sm text-slate-600">{assumption.description}</p>
                      </div>
                    </div>

                    <div className="grid grid-cols-2 gap-4 text-sm">
                      <div className="flex items-center gap-2 text-slate-600">
                        <Target className="h-4 w-4" weight="duotone" />
                        <span className="font-medium">Scope:</span>
                        <span>{assumption.scope}</span>
                      </div>
                      <div className="flex items-center gap-2 text-slate-600">
                        <CalendarBlank className="h-4 w-4" weight="duotone" />
                        <span className="font-medium">Valid:</span>
                        <span>
                          {formatDate(assumption.validityStartDate)} - {formatDate(assumption.validityEndDate)}
                        </span>
                      </div>
                    </div>

                    {assumption.methodology && (
                      <div className="text-sm">
                        <p className="font-medium text-slate-700 mb-1">Methodology:</p>
                        <p className="text-slate-600">{assumption.methodology}</p>
                      </div>
                    )}

                    {assumption.limitations && (
                      <div className="text-sm">
                        <p className="font-medium text-slate-700 mb-1">Limitations:</p>
                        <p className="text-slate-600">{assumption.limitations}</p>
                      </div>
                    )}

                    {assumption.linkedDataPointIds.length > 0 && (
                      <div className="flex items-center gap-2 text-sm text-slate-600">
                        <Info className="h-4 w-4" weight="duotone" />
                        <span>Linked to {assumption.linkedDataPointIds.length} data point(s)</span>
                      </div>
                    )}

                    {assumption.status === 'deprecated' && assumption.replacementAssumptionId && (
                      <Alert className="bg-yellow-50 border-yellow-200">
                        <Warning className="h-4 w-4 text-yellow-600" />
                        <AlertDescription className="text-yellow-800">
                          Replaced by assumption: {assumption.replacementAssumptionId}
                        </AlertDescription>
                      </Alert>
                    )}

                    {assumption.status === 'invalid' && assumption.deprecationJustification && (
                      <Alert className="bg-red-50 border-red-200">
                        <Warning className="h-4 w-4 text-red-600" />
                        <AlertDescription className="text-red-800">
                          Marked as invalid: {assumption.deprecationJustification}
                        </AlertDescription>
                      </Alert>
                    )}
                  </div>

                  <div className="flex gap-2">
                    {assumption.status === 'active' && (
                      <>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleEdit(assumption)}
                          title="Edit assumption"
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDeprecate(assumption)}
                          title="Deprecate assumption"
                        >
                          <Warning className="h-4 w-4 text-yellow-600" />
                        </Button>
                      </>
                    )}
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleDelete(assumption.id)}
                      title="Delete assumption"
                      className="text-red-600 hover:text-red-700"
                    >
                      <Trash className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {deprecatingAssumption && (
        <DeprecateAssumptionDialog
          assumption={deprecatingAssumption}
          availableAssumptions={assumptions.filter(a => a.status === 'active' && a.id !== deprecatingAssumption.id)}
          onSuccess={handleDeprecateSuccess}
          onCancel={() => setDeprecatingAssumption(undefined)}
        />
      )}
    </div>
  )
}
