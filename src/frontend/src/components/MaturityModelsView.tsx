import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Plus, List, ChartLineUp, CheckCircle, Info, Trash } from '@phosphor-icons/react'
import type { MaturityModel, User } from '@/lib/types'
import { getMaturityModels, deleteMaturityModel } from '@/lib/api'
import { MaturityModelForm } from '@/components/MaturityModelForm'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"

interface MaturityModelsViewProps {
  currentUser: User
}

export default function MaturityModelsView({ currentUser }: MaturityModelsViewProps) {
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [editingModel, setEditingModel] = useState<MaturityModel | null>(null)
  const [deletingModelId, setDeletingModelId] = useState<string | null>(null)
  const [includeInactive, setIncludeInactive] = useState(false)
  const queryClient = useQueryClient()

  const { data: models = [], isLoading, error } = useQuery({
    queryKey: ['maturity-models', includeInactive],
    queryFn: () => getMaturityModels(includeInactive)
  })

  const deleteMutation = useMutation({
    mutationFn: deleteMaturityModel,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['maturity-models'] })
      setDeletingModelId(null)
    }
  })

  const handleCreateSuccess = () => {
    setShowCreateForm(false)
    queryClient.invalidateQueries({ queryKey: ['maturity-models'] })
  }

  const handleUpdateSuccess = () => {
    setEditingModel(null)
    queryClient.invalidateQueries({ queryKey: ['maturity-models'] })
  }

  const handleDelete = (id: string) => {
    deleteMutation.mutate(id)
  }

  const activeModel = models.find(m => m.isActive)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <p className="text-muted-foreground">Loading maturity models...</p>
      </div>
    )
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertDescription>
          Failed to load maturity models. Please try again.
        </AlertDescription>
      </Alert>
    )
  }

  if (showCreateForm) {
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-bold">Create Maturity Model</h2>
            <p className="text-muted-foreground">
              Define maturity levels and criteria to measure reporting progress
            </p>
          </div>
          <Button variant="outline" onClick={() => setShowCreateForm(false)}>
            Cancel
          </Button>
        </div>
        <MaturityModelForm
          currentUser={currentUser}
          onSuccess={handleCreateSuccess}
          onCancel={() => setShowCreateForm(false)}
        />
      </div>
    )
  }

  if (editingModel) {
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-bold">Update Maturity Model</h2>
            <p className="text-muted-foreground">
              Updates will create a new version (v{editingModel.version + 1})
            </p>
          </div>
          <Button variant="outline" onClick={() => setEditingModel(null)}>
            Cancel
          </Button>
        </div>
        <MaturityModelForm
          currentUser={currentUser}
          model={editingModel}
          onSuccess={handleUpdateSuccess}
          onCancel={() => setEditingModel(null)}
        />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">Maturity Models</h2>
          <p className="text-muted-foreground">
            Define and manage reporting maturity frameworks
          </p>
        </div>
        {currentUser.role === 'admin' && (
          <Button onClick={() => setShowCreateForm(true)}>
            <Plus className="mr-2 h-4 w-4" />
            Create Maturity Model
          </Button>
        )}
      </div>

      {models.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <ChartLineUp className="h-12 w-12 text-muted-foreground mb-4" />
            <p className="text-lg font-medium mb-2">No maturity models defined</p>
            <p className="text-sm text-muted-foreground mb-4">
              Create a maturity model to track reporting progress over time
            </p>
            {currentUser.role === 'admin' && (
              <Button onClick={() => setShowCreateForm(true)}>
                <Plus className="mr-2 h-4 w-4" />
                Create First Model
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <>
          <div className="flex items-center gap-2">
            <Button
              variant={includeInactive ? 'outline' : 'default'}
              size="sm"
              onClick={() => setIncludeInactive(false)}
            >
              Active
            </Button>
            <Button
              variant={includeInactive ? 'default' : 'outline'}
              size="sm"
              onClick={() => setIncludeInactive(true)}
            >
              All Versions
            </Button>
          </div>

          <div className="grid gap-4">
            {models.map((model) => (
              <Card key={`${model.id}-${model.version}`} className={model.isActive ? 'border-primary' : ''}>
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <CardTitle className="text-xl">{model.name}</CardTitle>
                        <Badge variant={model.isActive ? 'default' : 'secondary'}>
                          v{model.version}
                        </Badge>
                        {model.isActive && (
                          <Badge variant="outline" className="bg-green-500/10 text-green-700 border-green-200">
                            <CheckCircle className="mr-1 h-3 w-3" />
                            Active
                          </Badge>
                        )}
                      </div>
                      <CardDescription>{model.description}</CardDescription>
                    </div>
                    {currentUser.role === 'admin' && model.isActive && (
                      <div className="flex gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setEditingModel(model)}
                        >
                          Update
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setDeletingModelId(model.id)}
                        >
                          <Trash className="h-4 w-4" />
                        </Button>
                      </div>
                    )}
                  </div>
                  <div className="flex gap-4 text-xs text-muted-foreground mt-2">
                    <span>Created by {model.createdByName}</span>
                    <span>•</span>
                    <span>{new Date(model.createdAt).toLocaleDateString()}</span>
                    {model.updatedAt && (
                      <>
                        <span>•</span>
                        <span>Updated {new Date(model.updatedAt).toLocaleDateString()}</span>
                      </>
                    )}
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div>
                      <h4 className="text-sm font-medium mb-2">Maturity Levels ({model.levels.length})</h4>
                      <div className="space-y-2">
                        {model.levels.sort((a, b) => a.order - b.order).map((level) => (
                          <div key={level.id} className="border rounded-lg p-3">
                            <div className="flex items-center gap-2 mb-1">
                              <Badge variant="outline">Level {level.order}</Badge>
                              <span className="font-medium">{level.name}</span>
                            </div>
                            <p className="text-sm text-muted-foreground mb-2">{level.description}</p>
                            <div className="text-xs text-muted-foreground">
                              {level.criteria.length} {level.criteria.length === 1 ? 'criterion' : 'criteria'}
                              {level.criteria.length > 0 && (
                                <span className="ml-2">
                                  ({level.criteria.filter(c => c.isMandatory).length} mandatory)
                                </span>
                              )}
                            </div>
                            {level.criteria.length > 0 && (
                              <div className="mt-2 space-y-1">
                                {level.criteria.map((criterion) => (
                                  <div key={criterion.id} className="text-xs border-l-2 pl-2 py-1">
                                    <div className="flex items-center gap-1">
                                      <span className="font-medium">{criterion.name}</span>
                                      {criterion.isMandatory && (
                                        <Badge variant="outline" className="text-xs">Required</Badge>
                                      )}
                                    </div>
                                    <div className="text-muted-foreground">
                                      Type: {criterion.criterionType} • Target: {criterion.targetValue}{criterion.unit}
                                    </div>
                                  </div>
                                ))}
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </>
      )}

      <AlertDialog open={deletingModelId !== null} onOpenChange={(open) => !open && setDeletingModelId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Maturity Model</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete this maturity model? This will remove all versions.
              This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => deletingModelId && handleDelete(deletingModelId)}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
