import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { LockKey, Warning, ShieldCheck, Pencil } from '@phosphor-icons/react'
import type { SystemRole } from '@/lib/types'

export default function RoleManagement() {
  const [roles, setRoles] = useState<SystemRole[]>([])
  const [loading, setLoading] = useState(true)
  const [editingRole, setEditingRole] = useState<SystemRole | null>(null)
  const [newDescription, setNewDescription] = useState('')
  const [deleteAttempt, setDeleteAttempt] = useState<SystemRole | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    fetchRoles()
  }, [])

  const fetchRoles = async () => {
    try {
      const response = await fetch('http://localhost:5000/api/roles')
      const data = await response.json()
      setRoles(data)
    } catch (error) {
      console.error('Failed to fetch roles:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleEditDescription = async () => {
    if (!editingRole) return

    try {
      const response = await fetch(`http://localhost:5000/api/roles/${editingRole.id}/description`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ description: newDescription })
      })

      if (response.ok) {
        await fetchRoles()
        setEditingRole(null)
        setNewDescription('')
      } else {
        const error = await response.json()
        setErrorMessage(error.error || 'Failed to update role')
      }
    } catch (error) {
      console.error('Failed to update role:', error)
      setErrorMessage('Failed to update role')
    }
  }

  const handleDeleteRole = async (role: SystemRole) => {
    try {
      const response = await fetch(`http://localhost:5000/api/roles/${role.id}`, {
        method: 'DELETE'
      })

      if (response.ok) {
        await fetchRoles()
        setDeleteAttempt(null)
        setErrorMessage(null)
      } else {
        const error = await response.json()
        setErrorMessage(error.error || 'Failed to delete role')
      }
    } catch (error) {
      console.error('Failed to delete role:', error)
      setErrorMessage('Failed to delete role')
    }
  }

  if (loading) {
    return <div className="p-6">Loading roles...</div>
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Role Management</h2>
          <p className="text-muted-foreground">
            Manage system roles and permissions. Predefined roles cannot be deleted.
          </p>
        </div>
      </div>

      <div className="grid gap-4">
        {roles.map((role) => (
          <Card key={role.id} className={role.isPredefined ? 'border-l-4 border-l-primary' : ''}>
            <CardHeader>
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <CardTitle className="text-lg">{role.name}</CardTitle>
                    {role.isPredefined && (
                      <Badge variant="secondary" className="gap-1">
                        <LockKey size={14} weight="bold" />
                        Predefined
                      </Badge>
                    )}
                    <Badge variant="outline" className="text-xs">
                      v{role.version}
                    </Badge>
                  </div>
                  <CardDescription className="mt-2">{role.description}</CardDescription>
                </div>
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      setEditingRole(role)
                      setNewDescription(role.description)
                      setErrorMessage(null)
                    }}
                  >
                    <Pencil size={16} weight="bold" className="mr-1" />
                    Edit
                  </Button>
                  {!role.isPredefined && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        setDeleteAttempt(role)
                        setErrorMessage(null)
                      }}
                    >
                      Delete
                    </Button>
                  )}
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <div>
                <div className="text-sm font-medium text-muted-foreground mb-2">Permissions:</div>
                <div className="flex flex-wrap gap-2">
                  {role.permissions.map((permission) => (
                    <Badge key={permission} variant="outline" className="gap-1">
                      <ShieldCheck size={14} weight="bold" />
                      {permission}
                    </Badge>
                  ))}
                </div>
              </div>
              
              {role.updatedAt && (
                <div className="mt-4 text-xs text-muted-foreground">
                  Last updated: {new Date(role.updatedAt).toLocaleString()} by {role.updatedBy}
                </div>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Edit Description Dialog */}
      <Dialog open={!!editingRole} onOpenChange={(open) => !open && setEditingRole(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit Role Description</DialogTitle>
            <DialogDescription>
              Update the description for {editingRole?.name}. This change will be versioned and audited.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={newDescription}
                onChange={(e) => setNewDescription(e.target.value)}
                rows={4}
                placeholder="Enter role description..."
              />
            </div>
            {errorMessage && (
              <div className="flex items-center gap-2 text-sm text-destructive">
                <Warning size={16} weight="bold" />
                {errorMessage}
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditingRole(null)}>
              Cancel
            </Button>
            <Button onClick={handleEditDescription} disabled={!newDescription.trim()}>
              Save Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteAttempt} onOpenChange={(open) => !open && setDeleteAttempt(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Role</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete the role "{deleteAttempt?.name}"?
            </DialogDescription>
          </DialogHeader>
          {deleteAttempt?.isPredefined && (
            <div className="flex items-start gap-3 rounded-md border border-destructive bg-destructive/10 p-4">
              <Warning size={20} weight="bold" className="mt-0.5 text-destructive" />
              <div className="space-y-1 text-sm">
                <p className="font-medium text-destructive">Cannot Delete Predefined Role</p>
                <p className="text-muted-foreground">
                  Predefined roles are essential for system access control and cannot be removed.
                </p>
              </div>
            </div>
          )}
          {errorMessage && (
            <div className="flex items-center gap-2 text-sm text-destructive">
              <Warning size={16} weight="bold" />
              {errorMessage}
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteAttempt(null)}>
              Cancel
            </Button>
            {deleteAttempt && !deleteAttempt.isPredefined && (
              <Button
                variant="destructive"
                onClick={() => deleteAttempt && handleDeleteRole(deleteAttempt)}
              >
                Delete Role
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
