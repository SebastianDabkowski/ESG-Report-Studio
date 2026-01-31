import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Switch } from '@/components/ui/switch'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Archive, Pencil, CheckCircle, WarningCircle, Calendar, FileText } from '@phosphor-icons/react'
import type { StandardsCatalogItem, CreateStandardRequest, UpdateStandardRequest } from '@/lib/types'
import { 
  getStandardsCatalog, 
  createStandard, 
  updateStandard, 
  deprecateStandard 
} from '@/lib/api'

export default function StandardsCatalog() {
  const [includeDeprecated, setIncludeDeprecated] = useState(false)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [selectedStandard, setSelectedStandard] = useState<StandardsCatalogItem | null>(null)
  
  // Form state
  const [identifier, setIdentifier] = useState('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [version, setVersion] = useState('')
  const [effectiveStartDate, setEffectiveStartDate] = useState('')
  const [effectiveEndDate, setEffectiveEndDate] = useState('')
  const [formError, setFormError] = useState<string | null>(null)

  const queryClient = useQueryClient()

  // Fetch standards
  const { data: standards = [], isLoading } = useQuery({
    queryKey: ['standards-catalog', includeDeprecated],
    queryFn: () => getStandardsCatalog(includeDeprecated)
  })

  // Create standard mutation
  const createMutation = useMutation({
    mutationFn: (payload: CreateStandardRequest) => createStandard(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['standards-catalog'] })
      resetForm()
      setIsCreateOpen(false)
      setFormError(null)
    },
    onError: (error: Error) => {
      setFormError(error.message || 'Failed to create standard')
    }
  })

  // Update standard mutation
  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateStandardRequest }) => 
      updateStandard(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['standards-catalog'] })
      resetForm()
      setIsEditOpen(false)
      setFormError(null)
    },
    onError: (error: Error) => {
      setFormError(error.message || 'Failed to update standard')
    }
  })

  // Deprecate standard mutation
  const deprecateMutation = useMutation({
    mutationFn: (id: string) => deprecateStandard(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['standards-catalog'] })
    },
    onError: (error: Error) => {
      alert(`Failed to deprecate standard: ${error.message}`)
    }
  })

  const resetForm = () => {
    setIdentifier('')
    setTitle('')
    setDescription('')
    setVersion('')
    setEffectiveStartDate('')
    setEffectiveEndDate('')
    setFormError(null)
  }

  const handleCreate = () => {
    if (!identifier || !title || !version) {
      setFormError('Identifier, title, and version are required')
      return
    }

    createMutation.mutate({
      identifier,
      title,
      description,
      version,
      effectiveStartDate: effectiveStartDate || undefined,
      effectiveEndDate: effectiveEndDate || undefined
    })
  }

  const handleEdit = (standard: StandardsCatalogItem) => {
    setSelectedStandard(standard)
    setTitle(standard.title)
    setDescription(standard.description)
    setVersion(standard.version)
    setEffectiveStartDate(standard.effectiveStartDate || '')
    setEffectiveEndDate(standard.effectiveEndDate || '')
    setIsEditOpen(true)
  }

  const handleUpdate = () => {
    if (!selectedStandard) return
    
    if (!title || !version) {
      setFormError('Title and version are required')
      return
    }

    updateMutation.mutate({
      id: selectedStandard.id,
      payload: {
        title,
        description,
        version,
        effectiveStartDate: effectiveStartDate || undefined,
        effectiveEndDate: effectiveEndDate || undefined
      }
    })
  }

  const handleDeprecate = (id: string) => {
    if (confirm('Are you sure you want to deprecate this standard? It will not be selectable for new reports by default.')) {
      deprecateMutation.mutate(id)
    }
  }

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return 'N/A'
    try {
      return new Date(dateStr).toLocaleDateString()
    } catch {
      return dateStr
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">Standards Catalogue</h2>
          <p className="text-muted-foreground">
            Manage reporting standards (e.g., CSRD/ESRS, SME model) for ESG reporting
          </p>
        </div>
        <Button onClick={() => setIsCreateOpen(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Add Standard
        </Button>
      </div>

      <div className="flex items-center space-x-2">
        <Switch
          id="include-deprecated"
          checked={includeDeprecated}
          onCheckedChange={setIncludeDeprecated}
        />
        <Label htmlFor="include-deprecated" className="cursor-pointer">
          Show deprecated standards
        </Label>
      </div>

      {isLoading ? (
        <Card>
          <CardContent className="pt-6">
            <p className="text-center text-muted-foreground">Loading standards...</p>
          </CardContent>
        </Card>
      ) : standards.length === 0 ? (
        <Card>
          <CardContent className="pt-6">
            <div className="text-center">
              <FileText className="mx-auto h-12 w-12 text-muted-foreground" />
              <p className="mt-2 text-muted-foreground">
                {includeDeprecated 
                  ? 'No standards found in the catalogue'
                  : 'No active standards found. Use the toggle above to show deprecated standards.'}
              </p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {standards.map((standard) => (
            <Card key={standard.id} className={standard.isDeprecated ? 'opacity-60' : ''}>
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <CardTitle className="text-lg">{standard.title}</CardTitle>
                    <CardDescription className="mt-1">
                      {standard.identifier} v{standard.version}
                    </CardDescription>
                  </div>
                  {standard.isDeprecated && (
                    <Badge variant="secondary" className="ml-2">
                      <Archive className="mr-1 h-3 w-3" />
                      Deprecated
                    </Badge>
                  )}
                </div>
              </CardHeader>
              <CardContent className="space-y-3">
                <p className="text-sm text-muted-foreground">{standard.description}</p>
                
                <div className="space-y-2 text-sm">
                  <div className="flex items-center text-muted-foreground">
                    <Calendar className="mr-2 h-4 w-4" />
                    <span>
                      Effective: {formatDate(standard.effectiveStartDate)} - {formatDate(standard.effectiveEndDate)}
                    </span>
                  </div>
                </div>

                <div className="flex gap-2 pt-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleEdit(standard)}
                    disabled={standard.isDeprecated}
                  >
                    <Pencil className="mr-1 h-3 w-3" />
                    Edit
                  </Button>
                  {!standard.isDeprecated && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleDeprecate(standard.id)}
                    >
                      <Archive className="mr-1 h-3 w-3" />
                      Deprecate
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Create Standard Dialog */}
      <Dialog open={isCreateOpen} onOpenChange={(open) => {
        setIsCreateOpen(open)
        if (!open) resetForm()
      }}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Add New Standard</DialogTitle>
            <DialogDescription>
              Create a new reporting standard in the catalogue
            </DialogDescription>
          </DialogHeader>
          
          <div className="space-y-4">
            {formError && (
              <Alert variant="destructive">
                <WarningCircle className="h-4 w-4" />
                <AlertDescription>{formError}</AlertDescription>
              </Alert>
            )}

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="identifier">Identifier *</Label>
                <Input
                  id="identifier"
                  placeholder="e.g., CSRD-2024, SME-v1"
                  value={identifier}
                  onChange={(e) => setIdentifier(e.target.value)}
                />
                <p className="text-xs text-muted-foreground">
                  Unique stable identifier for the standard
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="version">Version *</Label>
                <Input
                  id="version"
                  placeholder="e.g., 1.0, 2024.1"
                  value={version}
                  onChange={(e) => setVersion(e.target.value)}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="title">Title *</Label>
              <Input
                id="title"
                placeholder="e.g., Corporate Sustainability Reporting Directive"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                placeholder="Detailed description of the standard..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="effectiveStartDate">Effective Start Date</Label>
                <Input
                  id="effectiveStartDate"
                  type="date"
                  value={effectiveStartDate}
                  onChange={(e) => setEffectiveStartDate(e.target.value)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="effectiveEndDate">Effective End Date</Label>
                <Input
                  id="effectiveEndDate"
                  type="date"
                  value={effectiveEndDate}
                  onChange={(e) => setEffectiveEndDate(e.target.value)}
                />
              </div>
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsCreateOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreate} disabled={createMutation.isPending}>
              {createMutation.isPending ? 'Creating...' : 'Create Standard'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Standard Dialog */}
      <Dialog open={isEditOpen} onOpenChange={(open) => {
        setIsEditOpen(open)
        if (!open) {
          resetForm()
          setSelectedStandard(null)
        }
      }}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Edit Standard</DialogTitle>
            <DialogDescription>
              Update the details of this reporting standard
            </DialogDescription>
          </DialogHeader>
          
          <div className="space-y-4">
            {formError && (
              <Alert variant="destructive">
                <WarningCircle className="h-4 w-4" />
                <AlertDescription>{formError}</AlertDescription>
              </Alert>
            )}

            {selectedStandard && (
              <Alert>
                <CheckCircle className="h-4 w-4" />
                <AlertDescription>
                  Editing: {selectedStandard.identifier} (Identifier cannot be changed)
                </AlertDescription>
              </Alert>
            )}

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-version">Version *</Label>
                <Input
                  id="edit-version"
                  placeholder="e.g., 1.0, 2024.1"
                  value={version}
                  onChange={(e) => setVersion(e.target.value)}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-title">Title *</Label>
              <Input
                id="edit-title"
                placeholder="e.g., Corporate Sustainability Reporting Directive"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-description">Description</Label>
              <Textarea
                id="edit-description"
                placeholder="Detailed description of the standard..."
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-effectiveStartDate">Effective Start Date</Label>
                <Input
                  id="edit-effectiveStartDate"
                  type="date"
                  value={effectiveStartDate}
                  onChange={(e) => setEffectiveStartDate(e.target.value)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="edit-effectiveEndDate">Effective End Date</Label>
                <Input
                  id="edit-effectiveEndDate"
                  type="date"
                  value={effectiveEndDate}
                  onChange={(e) => setEffectiveEndDate(e.target.value)}
                />
              </div>
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsEditOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleUpdate} disabled={updateMutation.isPending}>
              {updateMutation.isPending ? 'Updating...' : 'Update Standard'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
