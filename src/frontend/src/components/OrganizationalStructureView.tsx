import { useEffect, useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/components/ui/alert-dialog'
import { useKV } from '@github/spark/hooks'
import { Plus, Trash, PencilSimple, FloppyDisk, Tree } from '@phosphor-icons/react'
import type { User, OrganizationalUnit } from '@/lib/types'
import { getReportingData, createOrganizationalUnit, updateOrganizationalUnit, deleteOrganizationalUnit } from '@/lib/api'

interface OrganizationalStructureViewProps {
  currentUser: User
}

export default function OrganizationalStructureView({ currentUser }: OrganizationalStructureViewProps) {
  const [organizationalUnits, setOrganizationalUnits] = useKV<OrganizationalUnit[]>('organizational-units', [])
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [isEditOpen, setIsEditOpen] = useState(false)
  const [isDeleteOpen, setIsDeleteOpen] = useState(false)
  const [syncError, setSyncError] = useState<string | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)
  
  // Form state
  const [editingUnit, setEditingUnit] = useState<OrganizationalUnit | null>(null)
  const [deletingUnit, setDeletingUnit] = useState<OrganizationalUnit | null>(null)
  const [name, setName] = useState('')
  const [parentId, setParentId] = useState<string>('')
  const [description, setDescription] = useState('')

  useEffect(() => {
    let isActive = true

    const loadFromApi = async () => {
      try {
        const snapshot = await getReportingData()
        if (!isActive) return

        if (snapshot.organizationalUnits.length > 0) {
          setOrganizationalUnits(snapshot.organizationalUnits)
        }

        setSyncError(null)
      } catch (error) {
        if (!isActive) return
        setSyncError('Backend sync unavailable. Using local data.')
      }
    }

    loadFromApi()

    return () => {
      isActive = false
    }
  }, [setOrganizationalUnits])

  const resetForm = () => {
    setName('')
    setParentId('')
    setDescription('')
    setValidationError(null)
    setEditingUnit(null)
  }

  const validateForm = (): string | null => {
    if (!name.trim()) {
      return 'Name is required'
    }
    if (name.length > 255) {
      return 'Name must be 255 characters or less'
    }
    if (description.length > 1000) {
      return 'Description must be 1000 characters or less'
    }
    return null
  }

  const handleCreate = async () => {
    setValidationError(null)
    setSyncError(null)

    const error = validateForm()
    if (error) {
      setValidationError(error)
      return
    }

    try {
      const created = await createOrganizationalUnit({
        name,
        parentId: parentId || undefined,
        description,
        createdBy: currentUser.id
      })

      setOrganizationalUnits((current) => [...(current || []), created])
      setIsCreateOpen(false)
      resetForm()
      setSyncError(null)
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to create organizational unit.'
      setValidationError(errorMessage)
    }
  }

  const handleEdit = (unit: OrganizationalUnit) => {
    setEditingUnit(unit)
    setName(unit.name)
    setParentId(unit.parentId || '')
    setDescription(unit.description)
    setIsEditOpen(true)
  }

  const handleUpdate = async () => {
    if (!editingUnit) return

    setValidationError(null)
    setSyncError(null)

    const error = validateForm()
    if (error) {
      setValidationError(error)
      return
    }

    try {
      const updated = await updateOrganizationalUnit(editingUnit.id, {
        name,
        parentId: parentId || undefined,
        description
      })

      setOrganizationalUnits((current) =>
        (current || []).map((u) => (u.id === updated.id ? updated : u))
      )
      setIsEditOpen(false)
      resetForm()
      setSyncError(null)
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to update organizational unit.'
      setValidationError(errorMessage)
    }
  }

  const handleDeleteClick = (unit: OrganizationalUnit) => {
    setDeletingUnit(unit)
    setIsDeleteOpen(true)
  }

  const handleDelete = async () => {
    if (!deletingUnit) return

    try {
      await deleteOrganizationalUnit(deletingUnit.id)
      setOrganizationalUnits((current) => (current || []).filter((u) => u.id !== deletingUnit.id))
      setIsDeleteOpen(false)
      setDeletingUnit(null)
      setSyncError(null)
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to delete organizational unit.'
      setSyncError(errorMessage)
      setIsDeleteOpen(false)
    }
  }

  const getParentName = (parentId?: string): string => {
    if (!parentId) return 'None (Top Level)'
    const parent = organizationalUnits?.find((u) => u.id === parentId)
    return parent?.name || 'Unknown'
  }

  const getChildCount = (unitId: string): number => {
    return organizationalUnits?.filter((u) => u.parentId === unitId).length || 0
  }

  const topLevelUnits = organizationalUnits?.filter((u) => !u.parentId) || []
  const availableParents = editingUnit
    ? organizationalUnits?.filter((u) => u.id !== editingUnit.id) || []
    : organizationalUnits || []

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-foreground">
            Organizational Structure
          </h2>
          <p className="text-sm text-muted-foreground mt-1">
            Define your organizational units for responsibility assignment
          </p>
          {syncError && (
            <p className="text-xs text-destructive mt-1">
              {syncError}
            </p>
          )}
        </div>

        {currentUser.role !== 'auditor' && (
          <Dialog
            open={isCreateOpen}
            onOpenChange={(open) => {
              setIsCreateOpen(open)
              if (!open) resetForm()
            }}
          >
            <DialogTrigger asChild>
              <Button className="gap-2">
                <Plus size={16} weight="bold" />
                Add Unit
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Create Organizational Unit</DialogTitle>
                <DialogDescription>
                  Add a new organizational unit to your structure. You can nest units by selecting a parent.
                </DialogDescription>
              </DialogHeader>

              <div className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="name">
                    Name <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="name"
                    placeholder="e.g., Engineering Department"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="parent">Parent Unit</Label>
                  <Select value={parentId || 'none'} onValueChange={(value) => setParentId(value === 'none' ? '' : value)}>
                    <SelectTrigger>
                      <SelectValue placeholder="None (Top Level)" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">None (Top Level)</SelectItem>
                      {availableParents.map((unit) => (
                        <SelectItem key={unit.id} value={unit.id}>
                          {unit.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="description">Description</Label>
                  <Textarea
                    id="description"
                    placeholder="Describe the purpose and scope of this unit"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    rows={3}
                  />
                </div>

                {validationError && (
                  <p className="text-sm text-destructive">{validationError}</p>
                )}
              </div>

              <DialogFooter>
                <Button variant="outline" onClick={() => setIsCreateOpen(false)}>
                  Cancel
                </Button>
                <Button onClick={handleCreate} className="gap-2">
                  <FloppyDisk size={16} weight="bold" />
                  Create
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        )}
      </div>

      {organizationalUnits && organizationalUnits.length === 0 ? (
        <Card className="border-amber-500/50 bg-amber-500/5">
          <CardContent className="flex flex-col items-center gap-4 py-12">
            <Tree size={48} weight="duotone" className="text-amber-600 dark:text-amber-500" />
            <div className="text-center">
              <p className="text-sm font-medium text-amber-900 dark:text-amber-100 mb-1">
                No organizational units defined
              </p>
              <p className="text-xs text-amber-700 dark:text-amber-400">
                Define at least one organizational unit before creating reporting periods.
                Units help you assign responsibilities and track accountability.
              </p>
            </div>
            {currentUser.role !== 'auditor' && (
              <Button onClick={() => setIsCreateOpen(true)} className="gap-2">
                <Plus size={16} weight="bold" />
                Add First Unit
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>All Organizational Units</CardTitle>
            <CardDescription>
              {organizationalUnits?.length || 0} unit{organizationalUnits && organizationalUnits.length !== 1 ? 's' : ''} defined
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {organizationalUnits?.map((unit) => (
                <div
                  key={unit.id}
                  className="flex items-center justify-between p-4 rounded-lg border border-border bg-card hover:bg-accent/50 transition-colors"
                >
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <h3 className="text-sm font-medium text-foreground">{unit.name}</h3>
                      {getChildCount(unit.id) > 0 && (
                        <span className="text-xs text-muted-foreground">
                          ({getChildCount(unit.id)} child unit{getChildCount(unit.id) !== 1 ? 's' : ''})
                        </span>
                      )}
                    </div>
                    {unit.description && (
                      <p className="text-xs text-muted-foreground mt-1">{unit.description}</p>
                    )}
                    <p className="text-xs text-muted-foreground mt-1">
                      Parent: {getParentName(unit.parentId)}
                    </p>
                  </div>

                  {currentUser.role !== 'auditor' && (
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleEdit(unit)}
                        className="gap-2"
                      >
                        <PencilSimple size={14} weight="bold" />
                        Edit
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleDeleteClick(unit)}
                        className="gap-2 text-destructive hover:bg-destructive hover:text-destructive-foreground"
                      >
                        <Trash size={14} weight="bold" />
                        Delete
                      </Button>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Edit Dialog */}
      <Dialog
        open={isEditOpen}
        onOpenChange={(open) => {
          setIsEditOpen(open)
          if (!open) resetForm()
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit Organizational Unit</DialogTitle>
            <DialogDescription>
              Update the details of this organizational unit.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="edit-name">
                Name <span className="text-destructive">*</span>
              </Label>
              <Input
                id="edit-name"
                placeholder="e.g., Engineering Department"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-parent">Parent Unit</Label>
              <Select value={parentId || 'none'} onValueChange={(value) => setParentId(value === 'none' ? '' : value)}>
                <SelectTrigger>
                  <SelectValue placeholder="None (Top Level)" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">None (Top Level)</SelectItem>
                  {availableParents.map((unit) => (
                    <SelectItem key={unit.id} value={unit.id}>
                      {unit.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-description">Description</Label>
              <Textarea
                id="edit-description"
                placeholder="Describe the purpose and scope of this unit"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
              />
            </div>

            {validationError && (
              <p className="text-sm text-destructive">{validationError}</p>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsEditOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleUpdate} className="gap-2">
              <FloppyDisk size={16} weight="bold" />
              Save Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={isDeleteOpen} onOpenChange={setIsDeleteOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Organizational Unit</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete "{deletingUnit?.name}"? This action cannot be undone.
              {deletingUnit && getChildCount(deletingUnit.id) > 0 && (
                <span className="block mt-2 text-destructive font-medium">
                  Warning: This unit has {getChildCount(deletingUnit.id)} child unit{getChildCount(deletingUnit.id) !== 1 ? 's' : ''}. 
                  You must delete or reassign them first.
                </span>
              )}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
