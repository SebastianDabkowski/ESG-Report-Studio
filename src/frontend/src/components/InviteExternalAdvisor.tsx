import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import { UserPlus, Clock, ShieldCheck, Warning, CheckCircle, XCircle } from '@phosphor-icons/react'
import type { User, SystemRole, ReportSection, InviteExternalAdvisorRequest, InviteExternalAdvisorResponse } from '@/lib/types'

interface InviteExternalAdvisorProps {
  periodId?: string
}

export default function InviteExternalAdvisor({ periodId }: InviteExternalAdvisorProps) {
  const [users, setUsers] = useState<User[]>([])
  const [roles, setRoles] = useState<SystemRole[]>([])
  const [sections, setSections] = useState<ReportSection[]>([])
  const [selectedUser, setSelectedUser] = useState<string>('')
  const [selectedRole, setSelectedRole] = useState<string>('')
  const [selectedSections, setSelectedSections] = useState<string[]>([])
  const [expiryDate, setExpiryDate] = useState<string>('')
  const [reason, setReason] = useState<string>('')
  const [loading, setLoading] = useState(true)
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  useEffect(() => {
    fetchData()
  }, [periodId])

  const fetchData = async () => {
    try {
      const [usersRes, rolesRes, sectionsRes] = await Promise.all([
        fetch('http://localhost:5000/api/users'),
        fetch('http://localhost:5000/api/roles'),
        periodId 
          ? fetch(`http://localhost:5000/api/sections?periodId=${periodId}`)
          : fetch('http://localhost:5000/api/sections')
      ])

      const usersData = await usersRes.json()
      const rolesData = await rolesRes.json()
      const sectionsData = await sectionsRes.json()

      setUsers(usersData)
      setRoles(rolesData.filter((r: SystemRole) => 
        r.name.toLowerCase().includes('advisor')
      ))
      setSections(sectionsData)
    } catch (error) {
      console.error('Failed to fetch data:', error)
      setErrorMessage('Failed to load data')
    } finally {
      setLoading(false)
    }
  }

  const handleOpenInviteDialog = () => {
    setSelectedUser('')
    setSelectedRole('')
    setSelectedSections([])
    setExpiryDate('')
    setReason('')
    setErrorMessage(null)
    setSuccessMessage(null)
    setInviteDialogOpen(true)
  }

  const handleToggleSection = (sectionId: string) => {
    setSelectedSections(prev => 
      prev.includes(sectionId)
        ? prev.filter(id => id !== sectionId)
        : [...prev, sectionId]
    )
  }

  const handleInvite = async () => {
    setErrorMessage(null)
    setSuccessMessage(null)

    if (!selectedUser) {
      setErrorMessage('Please select a user')
      return
    }

    if (!selectedRole) {
      setErrorMessage('Please select an advisor role')
      return
    }

    if (selectedSections.length === 0) {
      setErrorMessage('Please select at least one section')
      return
    }

    // For demo purposes, using a mock manager ID
    const currentUserId = 'user-2' // Admin user

    const request: InviteExternalAdvisorRequest = {
      userId: selectedUser,
      roleId: selectedRole,
      sectionIds: selectedSections,
      accessExpiresAt: expiryDate ? new Date(expiryDate).toISOString() : undefined,
      reason: reason || undefined,
      invitedBy: currentUserId
    }

    try {
      const response = await fetch('http://localhost:5000/api/users/invite-external-advisor', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request)
      })

      const result: InviteExternalAdvisorResponse = await response.json()

      if (response.ok && result.success) {
        setSuccessMessage(`Successfully invited ${result.user?.name} as external advisor with access to ${result.sectionGrants.length} section(s)`)
        setInviteDialogOpen(false)
        await fetchData()
      } else {
        setErrorMessage(result.errorMessage || 'Failed to invite advisor')
      }
    } catch (error) {
      console.error('Failed to invite advisor:', error)
      setErrorMessage('Failed to invite advisor')
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString()
  }

  const isExpired = (expiryDate?: string) => {
    if (!expiryDate) return false
    return new Date(expiryDate) < new Date()
  }

  const advisorUsers = users.filter(u => 
    u.roleIds && u.roleIds.some(roleId => 
      roles.some(r => r.id === roleId)
    )
  )

  if (loading) {
    return <div className="p-6">Loading...</div>
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">External Advisor Management</h2>
          <p className="text-muted-foreground">
            Invite external advisors with limited, time-bounded access to specific sections
          </p>
        </div>
        <Button onClick={handleOpenInviteDialog}>
          <UserPlus size={20} weight="bold" className="mr-2" />
          Invite Advisor
        </Button>
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

      {/* Active Advisors List */}
      <Card>
        <CardHeader>
          <CardTitle>Active External Advisors</CardTitle>
          <CardDescription>Users with external advisor roles and their access status</CardDescription>
        </CardHeader>
        <CardContent>
          {advisorUsers.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No external advisors invited yet
            </div>
          ) : (
            <div className="space-y-2">
              {advisorUsers.map((user) => (
                <div
                  key={user.id}
                  className="flex items-center justify-between rounded-md border p-3"
                >
                  <div className="flex items-center gap-3">
                    <ShieldCheck size={24} weight="duotone" className="text-blue-600" />
                    <div>
                      <div className="font-medium">{user.name}</div>
                      <div className="text-xs text-muted-foreground">{user.email}</div>
                      {user.roleIds && (
                        <div className="mt-1 flex flex-wrap gap-1">
                          {user.roleIds.map(roleId => {
                            const role = roles.find(r => r.id === roleId)
                            return role ? (
                              <Badge key={roleId} variant="secondary" className="text-xs">
                                {role.name}
                              </Badge>
                            ) : null
                          })}
                        </div>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    {user.accessExpiresAt && (
                      <div className={`flex items-center gap-1 text-sm ${isExpired(user.accessExpiresAt) ? 'text-destructive' : 'text-muted-foreground'}`}>
                        <Clock size={16} weight="bold" />
                        {isExpired(user.accessExpiresAt) ? (
                          <span>Expired {formatDate(user.accessExpiresAt)}</span>
                        ) : (
                          <span>Expires {formatDate(user.accessExpiresAt)}</span>
                        )}
                      </div>
                    )}
                    {isExpired(user.accessExpiresAt) && (
                      <Badge variant="destructive">Access Expired</Badge>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Invite Dialog */}
      <Dialog open={inviteDialogOpen} onOpenChange={setInviteDialogOpen}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>Invite External Advisor</DialogTitle>
            <DialogDescription>
              Select a user, assign an advisor role, grant access to specific sections, and optionally set an expiry date
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            {/* User Selection */}
            <div className="space-y-2">
              <Label>Select User</Label>
              <select
                value={selectedUser}
                onChange={(e) => setSelectedUser(e.target.value)}
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              >
                <option value="">Choose a user...</option>
                {users.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.name} ({user.email})
                  </option>
                ))}
              </select>
            </div>

            {/* Role Selection */}
            <div className="space-y-2">
              <Label>Select Advisor Role</Label>
              <select
                value={selectedRole}
                onChange={(e) => setSelectedRole(e.target.value)}
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              >
                <option value="">Choose an advisor role...</option>
                {roles.map((role) => (
                  <option key={role.id} value={role.id}>
                    {role.name} - {role.description}
                  </option>
                ))}
              </select>
            </div>

            {/* Section Access */}
            <div className="space-y-2">
              <Label>Grant Access to Sections</Label>
              <div className="max-h-64 space-y-2 overflow-y-auto rounded-md border p-4">
                {sections.length === 0 ? (
                  <div className="text-center text-sm text-muted-foreground">
                    No sections available
                  </div>
                ) : (
                  sections.map((section) => (
                    <div key={section.id} className="flex items-start gap-3 rounded-md p-2 hover:bg-accent">
                      <Checkbox
                        id={section.id}
                        checked={selectedSections.includes(section.id)}
                        onCheckedChange={() => handleToggleSection(section.id)}
                        className="mt-1"
                      />
                      <label htmlFor={section.id} className="flex-1 cursor-pointer">
                        <div className="font-medium">{section.title}</div>
                        <div className="text-sm text-muted-foreground">{section.description}</div>
                      </label>
                    </div>
                  ))
                )}
              </div>
              <div className="text-xs text-muted-foreground">
                Selected: {selectedSections.length} section(s)
              </div>
            </div>

            {/* Expiry Date */}
            <div className="space-y-2">
              <Label>Access Expiry Date (Optional)</Label>
              <Input
                type="date"
                value={expiryDate}
                onChange={(e) => setExpiryDate(e.target.value)}
                min={new Date().toISOString().split('T')[0]}
              />
              <div className="text-xs text-muted-foreground">
                Leave empty for indefinite access
              </div>
            </div>

            {/* Reason */}
            <div className="space-y-2">
              <Label>Reason for Access (Optional)</Label>
              <Textarea
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                placeholder="e.g., External audit review, consultant engagement, etc."
                rows={3}
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
            <Button variant="outline" onClick={() => setInviteDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleInvite}>
              Invite Advisor
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
