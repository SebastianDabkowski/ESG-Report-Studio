import { useState, useEffect, useCallback } from 'react'
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
  Buildings,
  Factory,
  Gear
} from '@phosphor-icons/react'
import type { Simplification } from '@/lib/types'
import { getSimplifications, deleteSimplification } from '@/lib/api'
import { SimplificationForm } from './SimplificationForm'

interface SimplificationsListProps {
  sectionId: string
}

export function SimplificationsList({ sectionId }: SimplificationsListProps) {
  const [simplifications, setSimplifications] = useState<Simplification[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [editingSimplification, setEditingSimplification] = useState<Simplification | undefined>()

  const loadSimplifications = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await getSimplifications(sectionId)
      setSimplifications(data)
    } catch (err) {
      console.error('Error loading simplifications:', err)
      setError(err instanceof Error ? err.message : 'Failed to load simplifications')
    } finally {
      setLoading(false)
    }
  }, [sectionId])

  useEffect(() => {
    loadSimplifications()
  }, [loadSimplifications])

  const handleCreateNew = () => {
    setEditingSimplification(undefined)
    setShowForm(true)
  }

  const handleEdit = (simplification: Simplification) => {
    setEditingSimplification(simplification)
    setShowForm(true)
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to remove this simplification? This action will be recorded in the audit log.')) {
      return
    }

    try {
      await deleteSimplification(id)
      await loadSimplifications()
    } catch (err) {
      console.error('Error deleting simplification:', err)
      setError(err instanceof Error ? err.message : 'Failed to delete simplification')
    }
  }

  const handleFormSuccess = async () => {
    setShowForm(false)
    setEditingSimplification(undefined)
    await loadSimplifications()
  }

  const getImpactBadge = (level: string) => {
    switch (level) {
      case 'low':
        return <Badge className="bg-blue-100 text-blue-800 border-blue-200">Low Impact</Badge>
      case 'medium':
        return <Badge className="bg-yellow-100 text-yellow-800 border-yellow-200">Medium Impact</Badge>
      case 'high':
        return <Badge className="bg-red-100 text-red-800 border-red-200">High Impact</Badge>
      default:
        return <Badge>Unknown</Badge>
    }
  }

  if (showForm) {
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-semibold">
            {editingSimplification ? 'Edit Simplification' : 'Create Simplification'}
          </h3>
          <Button variant="outline" onClick={() => setShowForm(false)}>
            Back to List
          </Button>
        </div>
        <SimplificationForm
          sectionId={sectionId}
          simplification={editingSimplification}
          onSuccess={handleFormSuccess}
          onCancel={() => setShowForm(false)}
        />
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Notebook className="h-5 w-5 text-slate-600" />
          <h3 className="text-lg font-semibold">Simplifications & Boundary Limitations</h3>
        </div>
        <Button onClick={handleCreateNew}>
          <Plus className="h-4 w-4 mr-2" />
          Add Simplification
        </Button>
      </div>

      {error && (
        <Alert variant="destructive">
          <Warning className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {loading ? (
        <div className="text-center py-8 text-slate-500">Loading simplifications...</div>
      ) : simplifications.length === 0 ? (
        <Card>
          <CardContent className="py-8">
            <div className="text-center text-slate-500">
              <Notebook className="h-12 w-12 mx-auto mb-2 opacity-50" />
              <p>No simplifications recorded yet.</p>
              <p className="text-sm mt-1">
                Document any scope limitations or boundary choices that affect this section.
              </p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {simplifications.map((simplification) => (
            <Card key={simplification.id} className="hover:shadow-md transition-shadow">
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <CardTitle className="text-base">{simplification.title}</CardTitle>
                      {getImpactBadge(simplification.impactLevel)}
                    </div>
                    <p className="text-sm text-slate-600">{simplification.description}</p>
                  </div>
                  <div className="flex gap-2 ml-4">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleEdit(simplification)}
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleDelete(simplification.id)}
                    >
                      <Trash className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {simplification.affectedEntities.length > 0 && (
                    <div>
                      <div className="flex items-center gap-2 text-sm font-medium mb-1">
                        <Buildings className="h-4 w-4 text-slate-500" />
                        Affected Entities
                      </div>
                      <div className="flex flex-wrap gap-2">
                        {simplification.affectedEntities.map((entity, idx) => (
                          <Badge key={idx} variant="outline" className="bg-slate-50">
                            {entity}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  )}

                  {simplification.affectedSites.length > 0 && (
                    <div>
                      <div className="flex items-center gap-2 text-sm font-medium mb-1">
                        <Factory className="h-4 w-4 text-slate-500" />
                        Affected Sites
                      </div>
                      <div className="flex flex-wrap gap-2">
                        {simplification.affectedSites.map((site, idx) => (
                          <Badge key={idx} variant="outline" className="bg-slate-50">
                            {site}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  )}

                  {simplification.affectedProcesses.length > 0 && (
                    <div>
                      <div className="flex items-center gap-2 text-sm font-medium mb-1">
                        <Gear className="h-4 w-4 text-slate-500" />
                        Affected Processes
                      </div>
                      <div className="flex flex-wrap gap-2">
                        {simplification.affectedProcesses.map((process, idx) => (
                          <Badge key={idx} variant="outline" className="bg-slate-50">
                            {process}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  )}

                  {simplification.impactNotes && (
                    <div className="mt-2 p-3 bg-slate-50 rounded-md">
                      <p className="text-sm font-medium mb-1">Impact Notes</p>
                      <p className="text-sm text-slate-600">{simplification.impactNotes}</p>
                    </div>
                  )}

                  <div className="text-xs text-slate-500 pt-2 border-t">
                    Created by {simplification.createdBy} on{' '}
                    {new Date(simplification.createdAt).toLocaleDateString()}
                    {simplification.updatedAt && (
                      <> • Updated {new Date(simplification.updatedAt).toLocaleDateString()}</>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
