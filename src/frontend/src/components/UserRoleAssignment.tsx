import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { ShieldCheck, UserCircle, Warning, CheckCircle, XCircle } from '@phosphor-icons/react'
import type { User, SystemRole, EffectivePermissionsResponse } from '@/lib/types'

interface UserRoleAssignmentProps {
  userId?: string
}

export default function UserRoleAssignment({ userId: propUserId }: UserRoleAssignmentProps) {
  const [users, setUsers] = useState<User[]>([])
  const [roles, setRoles] = useState<SystemRole[]>([])
  const [selectedUser, setSelectedUser] = useState<User | null>(null)
  const [effectivePermissions, setEffectivePermissions] = useState<EffectivePermissionsResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [assignDialogOpen, setAssignDialogOpen] = useState(false)
  const [selectedRoles, setSelectedRoles] = useState<string[]>([])
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  useEffect(() => {
    fetchUsers()
    fetchRoles()
  }, [])

  useEffect(() => {
    if (propUserId && users.length > 0) {
      const user = users.find(u => u.id === propUserId)
      if (user) {
        handleSelectUser(user)
      }
    }
  }, [propUserId, users])

  const fetchUsers = async () => {
    try {
      const response = await fetch('http://localhost:5000/api/users')
      const data = await response.json()
      setUsers(data)
    } catch (error) {
      console.error('Failed to fetch users:', error)
      setErrorMessage('Failed to fetch users')
    } finally {
      setLoading(false)
    }
  }

  const fetchRoles = async () => {
    try {
      const response = await fetch('http://localhost:5000/api/roles')
      const data = await response.json()
      setRoles(data)
    } catch (error) {
      console.error('Failed to fetch roles:', error)
      setErrorMessage('Failed to fetch roles')
    }
  }

  const fetchEffectivePermissions = async (userId: string) => {
    try {
      const response = await fetch(`http://localhost:5000/api/users/${userId}/effective-permissions`)
      const data = await response.json()
      setEffectivePermissions(data)
    } catch (error) {
      console.error('Failed to fetch effective permissions:', error)
      setErrorMessage('Failed to fetch effective permissions')
    }
  }

  const handleSelectUser = async (user: User) => {
    setSelectedUser(user)
    setErrorMessage(null)
    setSuccessMessage(null)
    await fetchEffectivePermissions(user.id)
  }

  const handleOpenAssignDialog = () => {
    if (selectedUser) {
      setSelectedRoles(selectedUser.roleIds || [])
      setAssignDialogOpen(true)
      setErrorMessage(null)
    }
  }

  const handleToggleRole = (roleId: string) => {
    setSelectedRoles(prev => 
      prev.includes(roleId) 
        ? prev.filter(id => id !== roleId)
        : [...prev, roleId]
    )
  }

  const handleAssignRoles = async () => {
    if (!selectedUser) return

    try {
      const response = await fetch(`http://localhost:5000/api/users/${selectedUser.id}/roles`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ roleIds: selectedRoles })
      })

      if (response.ok) {
        await fetchUsers()
        await fetchEffectivePermissions(selectedUser.id)
        setAssignDialogOpen(false)
        setSuccessMessage('Roles assigned successfully')
        setErrorMessage(null)
        
        // Update selected user
        const updatedUser = await response.json()
        setSelectedUser(updatedUser)
      } else {
        const error = await response.json()
        setErrorMessage(error.error || 'Failed to assign roles')
      }
    } catch (error) {
      console.error('Failed to assign roles:', error)
      setErrorMessage('Failed to assign roles')
    }
  }

  const handleRemoveRole = async (roleId: string) => {
    if (!selectedUser) return

    try {
      const response = await fetch(`http://localhost:5000/api/users/${selectedUser.id}/roles/${roleId}`, {
        method: 'DELETE'
      })

      if (response.ok) {
        await fetchUsers()
        await fetchEffectivePermissions(selectedUser.id)
        setSuccessMessage('Role removed successfully')
        setErrorMessage(null)
        
        // Update selected user
        const updatedUser = await response.json()
        setSelectedUser(updatedUser)
      } else {
        const error = await response.json()
        setErrorMessage(error.error || 'Failed to remove role')
      }
    } catch (error) {
      console.error('Failed to remove role:', error)
      setErrorMessage('Failed to remove role')
    }
  }

  const getRoleName = (roleId: string) => {
    const role = roles.find(r => r.id === roleId)
    return role?.name || roleId
  }

  if (loading) {
    return <div className="p-6">Loading users...</div>
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">User Role Assignment</h2>
          <p className="text-muted-foreground">
            Assign roles to users and view their effective permissions
          </p>
        </div>
      </div>

      {errorMessage && (
        <div className="flex items-center gap-2 rounded-md border border-destructive bg-destructive/10 p-4 text-sm">
          <Warning size={20} weight="bold" className="text-destructive" />
          <span className="text-destructive">{errorMessage}</span>
        </div>
      )}

      {successMessage && (
        <div className="flex items-center gap-2 rounded-md border border-green-600 bg-green-50 p-4 text-sm">
          <CheckCircle size={20} weight="bold" className="text-green-600" />
          <span className="text-green-700">{successMessage}</span>
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Users List */}
        <Card>
          <CardHeader>
            <CardTitle>Users</CardTitle>
            <CardDescription>Select a user to manage their roles</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {users.map((user) => (
                <button
                  key={user.id}
                  onClick={() => handleSelectUser(user)}
                  className={`w-full rounded-md border p-3 text-left transition-colors hover:bg-accent ${
                    selectedUser?.id === user.id ? 'border-primary bg-accent' : ''
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <UserCircle size={24} weight="duotone" />
                      <div>
                        <div className="font-medium">{user.name}</div>
                        <div className="text-xs text-muted-foreground">{user.email}</div>
                      </div>
                    </div>
                    {user.roleIds && user.roleIds.length > 0 && (
                      <Badge variant="secondary">{user.roleIds.length} role{user.roleIds.length !== 1 ? 's' : ''}</Badge>
                    )}
                  </div>
                </button>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* User Details and Permissions */}
        {selectedUser ? (
          <Card>
            <CardHeader>
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle>{selectedUser.name}</CardTitle>
                  <CardDescription>{selectedUser.email}</CardDescription>
                </div>
                <Button onClick={handleOpenAssignDialog}>
                  Manage Roles
                </Button>
              </div>
            </CardHeader>
            <CardContent className="space-y-6">
              {/* Assigned Roles */}
              <div>
                <div className="mb-2 text-sm font-medium">Assigned Roles</div>
                {selectedUser.roleIds && selectedUser.roleIds.length > 0 ? (
                  <div className="flex flex-wrap gap-2">
                    {selectedUser.roleIds.map((roleId) => (
                      <Badge key={roleId} variant="outline" className="gap-1">
                        {getRoleName(roleId)}
                        <button
                          onClick={() => handleRemoveRole(roleId)}
                          className="ml-1 rounded-full hover:bg-destructive/10"
                        >
                          <XCircle size={14} weight="bold" />
                        </button>
                      </Badge>
                    ))}
                  </div>
                ) : (
                  <div className="text-sm text-muted-foreground">No roles assigned</div>
                )}
              </div>

              {/* Effective Permissions */}
              {effectivePermissions && (
                <div>
                  <div className="mb-2 text-sm font-medium">
                    Effective Permissions ({effectivePermissions.effectivePermissions.length})
                  </div>
                  {effectivePermissions.effectivePermissions.length > 0 ? (
                    <div className="space-y-4">
                      <div className="flex flex-wrap gap-2">
                        {effectivePermissions.effectivePermissions.map((permission) => (
                          <Badge key={permission} variant="default" className="gap-1">
                            <ShieldCheck size={14} weight="bold" />
                            {permission}
                          </Badge>
                        ))}
                      </div>

                      {/* Role Details */}
                      <div className="space-y-2 rounded-md border p-3">
                        <div className="text-xs font-medium text-muted-foreground">
                          Permission Sources
                        </div>
                        {effectivePermissions.roleDetails.map((detail) => (
                          <div key={detail.roleId} className="text-xs">
                            <div className="font-medium">{detail.roleName}</div>
                            <div className="text-muted-foreground">
                              {detail.permissions.join(', ')}
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  ) : (
                    <div className="text-sm text-muted-foreground">No permissions</div>
                  )}
                </div>
              )}
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardContent className="flex h-full items-center justify-center p-12">
              <div className="text-center text-muted-foreground">
                <UserCircle size={48} weight="duotone" className="mx-auto mb-2" />
                <p>Select a user to view and manage their roles</p>
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Assign Roles Dialog */}
      <Dialog open={assignDialogOpen} onOpenChange={setAssignDialogOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Manage Roles for {selectedUser?.name}</DialogTitle>
            <DialogDescription>
              Select the roles to assign to this user. Changes are tracked with audit trail.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label>Available Roles</Label>
              <div className="max-h-96 space-y-2 overflow-y-auto rounded-md border p-4">
                {roles.map((role) => (
                  <div key={role.id} className="flex items-start gap-3 rounded-md p-2 hover:bg-accent">
                    <Checkbox
                      id={role.id}
                      checked={selectedRoles.includes(role.id)}
                      onCheckedChange={() => handleToggleRole(role.id)}
                      className="mt-1"
                    />
                    <label htmlFor={role.id} className="flex-1 cursor-pointer">
                      <div className="flex items-center gap-2">
                        <div className="font-medium">{role.name}</div>
                        {role.isPredefined && (
                          <Badge variant="secondary" className="text-xs">Predefined</Badge>
                        )}
                      </div>
                      <div className="text-sm text-muted-foreground">{role.description}</div>
                      <div className="mt-1 flex flex-wrap gap-1">
                        {role.permissions.map((permission) => (
                          <Badge key={permission} variant="outline" className="text-xs">
                            {permission}
                          </Badge>
                        ))}
                      </div>
                    </label>
                  </div>
                ))}
              </div>
            </div>
            {errorMessage && (
              <div className="flex items-center gap-2 text-sm text-destructive">
                <Warning size={16} weight="bold" />
                {errorMessage}
              </div>
            )}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setAssignDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleAssignRoles}>
              Save Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
