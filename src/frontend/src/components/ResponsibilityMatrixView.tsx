import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Textarea } from '@/components/ui/textarea'
import { useKV } from '@github/spark/hooks'
import { UsersThree, UserCircle, Warning, CheckCircle, Article } from '@phosphor-icons/react'
import type { User, ReportingPeriod, OwnerAssignment, ResponsibilityMatrix } from '@/lib/types'
import { getProgressStatusColor, getProgressStatusLabel } from '@/lib/helpers'
import { getResponsibilityMatrix, bulkUpdateSectionOwner, getUsers, type BulkUpdateFailure } from '@/lib/api'

interface ResponsibilityMatrixViewProps {
  currentUser: User
}

export default function ResponsibilityMatrixView({ currentUser }: ResponsibilityMatrixViewProps) {
  const [periods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  
  const [selectedPeriod, setSelectedPeriod] = useState<string>('all')
  const [ownerFilter, setOwnerFilter] = useState<string>('all')
  const [selectedAssignment, setSelectedAssignment] = useState<OwnerAssignment | null>(null)
  const [isAssignDialogOpen, setIsAssignDialogOpen] = useState(false)
  const [newOwnerId, setNewOwnerId] = useState('')
  const [changeNote, setChangeNote] = useState('')
  const [assignError, setAssignError] = useState<string | null>(null)
  const [bulkUpdateResult, setBulkUpdateResult] = useState<{ updated: number; skipped: BulkUpdateFailure[] } | null>(null)

  const [matrix, setMatrix] = useState<ResponsibilityMatrix | null>(null)
  const [users, setUsers] = useState<User[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [isUpdating, setIsUpdating] = useState(false)

  // Fetch users on mount
  useEffect(() => {
    const fetchUsers = async () => {
      try {
        const userData = await getUsers()
        setUsers(userData)
      } catch (error) {
        console.error('Failed to fetch users:', error)
      }
    }
    fetchUsers()
  }, [])

  // Fetch responsibility matrix when filters change
  useEffect(() => {
    const fetchMatrix = async () => {
      setIsLoading(true)
      try {
        const matrixData = await getResponsibilityMatrix({
          periodId: selectedPeriod && selectedPeriod !== 'all' ? selectedPeriod : undefined,
          ownerFilter: ownerFilter && ownerFilter !== 'all' ? ownerFilter : undefined
        })
        setMatrix(matrixData)
      } catch (error) {
        console.error('Failed to fetch responsibility matrix:', error)
        setMatrix(null)
      } finally {
        setIsLoading(false)
      }
    }
    fetchMatrix()
  }, [selectedPeriod, ownerFilter])

  // Mutation for bulk updating section owners
  const handleBulkUpdateOwner = async (sectionIds: string[], ownerId: string, changeNote?: string) => {
    setIsUpdating(true)
    setAssignError(null)
    
    try {
      const result = await bulkUpdateSectionOwner({
        sectionIds,
        ownerId,
        updatedBy: currentUser.id,
        changeNote
      })
      
      const updated = result.updatedSections.length
      const skipped = result.skippedSections

      setBulkUpdateResult({ updated, skipped })
      
      // Refresh the matrix
      const matrixData = await getResponsibilityMatrix({
        periodId: selectedPeriod && selectedPeriod !== 'all' ? selectedPeriod : undefined,
        ownerFilter: ownerFilter && ownerFilter !== 'all' ? ownerFilter : undefined
      })
      setMatrix(matrixData)
      
      // Close dialog and reset form
      setIsAssignDialogOpen(false)
      setNewOwnerId('')
      setChangeNote('')
    } catch (error: any) {
      setAssignError(error.message || 'Failed to assign owner')
    } finally {
      setIsUpdating(false)
    }
  }

  const handleAssignOwner = (assignment: OwnerAssignment) => {
    setSelectedAssignment(assignment)
    setIsAssignDialogOpen(true)
    setAssignError(null)
    setBulkUpdateResult(null)
  }

  const handleSubmitAssignment = () => {
    if (!selectedAssignment || !newOwnerId) {
      setAssignError('Please select an owner')
      return
    }

    const sectionIds = selectedAssignment.sections.map(s => s.id)
    handleBulkUpdateOwner(sectionIds, newOwnerId, changeNote)
  }

  const handleCloseAssignDialog = () => {
    setIsAssignDialogOpen(false)
    setSelectedAssignment(null)
    setNewOwnerId('')
    setChangeNote('')
    setAssignError(null)
    setBulkUpdateResult(null)
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">Responsibility Matrix</h2>
          <p className="text-muted-foreground mt-1">
            View and manage section ownership assignments
          </p>
        </div>
        <UsersThree size={32} className="text-muted-foreground" />
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Filters</CardTitle>
          <CardDescription>Filter the responsibility matrix by period and owner</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label>Reporting Period</Label>
              <Select value={selectedPeriod} onValueChange={setSelectedPeriod}>
                <SelectTrigger>
                  <SelectValue placeholder="All periods" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All periods</SelectItem>
                  {periods?.map((period) => (
                    <SelectItem key={period.id} value={period.id}>
                      {period.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>Owner Filter</Label>
              <Select value={ownerFilter} onValueChange={setOwnerFilter}>
                <SelectTrigger>
                  <SelectValue placeholder="All owners" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All owners</SelectItem>
                  <SelectItem value="unassigned">Unassigned only</SelectItem>
                  {users.map((user) => (
                    <SelectItem key={user.id} value={user.id}>
                      {user.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Summary Statistics */}
      {matrix && (
        <div className="grid gap-4 md:grid-cols-3">
          <Card>
            <CardHeader className="pb-3">
              <CardDescription>Total Sections</CardDescription>
              <CardTitle className="text-4xl">{matrix.totalSections}</CardTitle>
            </CardHeader>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardDescription>Assigned Owners</CardDescription>
              <CardTitle className="text-4xl">
                {matrix.assignments.filter(a => a.ownerId).length}
              </CardTitle>
            </CardHeader>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardDescription className="flex items-center gap-2">
                <Warning size={16} weight="fill" className="text-amber-500" />
                Unassigned Sections
              </CardDescription>
              <CardTitle className="text-4xl">{matrix.unassignedSections}</CardTitle>
            </CardHeader>
          </Card>
        </div>
      )}

      {/* Results */}
      {bulkUpdateResult && bulkUpdateResult.updated > 0 && (
        <Alert className="bg-green-50 border-green-200">
          <CheckCircle className="h-4 w-4 text-green-600" />
          <AlertDescription className="text-green-800">
            Successfully updated {bulkUpdateResult.updated} section(s).
            {bulkUpdateResult.skipped.length > 0 && (
              <span className="ml-1">
                {bulkUpdateResult.skipped.length} section(s) were skipped.
              </span>
            )}
          </AlertDescription>
        </Alert>
      )}

      {/* Owner Assignments */}
      {isLoading ? (
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-center py-8">
              <div className="text-muted-foreground">Loading responsibility matrix...</div>
            </div>
          </CardContent>
        </Card>
      ) : matrix && matrix.assignments.length > 0 ? (
        <div className="space-y-4">
          {matrix.assignments.map((assignment) => (
            <Card key={assignment.ownerId || 'unassigned'}>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    {assignment.ownerId ? (
                      <UserCircle size={32} weight="fill" className="text-primary" />
                    ) : (
                      <Warning size={32} weight="fill" className="text-amber-500" />
                    )}
                    <div>
                      <CardTitle className="text-xl">{assignment.ownerName}</CardTitle>
                      {assignment.ownerEmail && (
                        <CardDescription>{assignment.ownerEmail}</CardDescription>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <div className="text-2xl font-bold">{assignment.sections.length}</div>
                      <div className="text-sm text-muted-foreground">Section(s)</div>
                    </div>
                    <div className="text-right">
                      <div className="text-2xl font-bold">{assignment.totalDataPoints}</div>
                      <div className="text-sm text-muted-foreground">Data Point(s)</div>
                    </div>
                    {!assignment.ownerId && (
                      <Button
                        onClick={() => handleAssignOwner(assignment)}
                        variant="default"
                        size="sm"
                      >
                        Assign Owner
                      </Button>
                    )}
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  {assignment.sections.map((section) => (
                    <div
                      key={section.id}
                      className="flex items-center justify-between p-3 border rounded-lg hover:bg-accent/50 transition-colors"
                    >
                      <div className="flex items-center gap-3 flex-1">
                        <Article size={20} className="text-muted-foreground" />
                        <div className="flex-1">
                          <div className="font-medium">{section.title}</div>
                          <div className="text-sm text-muted-foreground line-clamp-1">
                            {section.description}
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center gap-3">
                        <Badge variant="outline" className="capitalize">
                          {section.category}
                        </Badge>
                        <Badge className={getProgressStatusColor(section.progressStatus)}>
                          {getProgressStatusLabel(section.progressStatus)}
                        </Badge>
                        <div className="text-sm text-muted-foreground">
                          {section.dataPointCount} data point(s)
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="pt-6">
            <div className="flex flex-col items-center justify-center py-8 text-center">
              <UsersThree size={48} className="text-muted-foreground mb-4" />
              <div className="text-lg font-medium">No sections found</div>
              <div className="text-sm text-muted-foreground mt-1">
                {ownerFilter !== 'all' || selectedPeriod !== 'all'
                  ? 'Try adjusting your filters to see more results'
                  : 'Create a reporting period to get started'}
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Assign Owner Dialog */}
      <Dialog open={isAssignDialogOpen} onOpenChange={handleCloseAssignDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Assign Owner</DialogTitle>
            <DialogDescription>
              Assign an owner to {selectedAssignment?.sections.length || 0} section(s)
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {assignError && (
              <Alert variant="destructive">
                <AlertDescription>{assignError}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="owner">New Owner *</Label>
              <Select value={newOwnerId} onValueChange={setNewOwnerId} disabled={isUpdating}>
                <SelectTrigger id="owner">
                  <SelectValue placeholder="Select owner" />
                </SelectTrigger>
                <SelectContent>
                  {users.map((user) => (
                    <SelectItem key={user.id} value={user.id}>
                      {user.name} ({user.email})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="note">Change Note</Label>
              <Textarea
                id="note"
                placeholder="Optional note explaining the reason for this change"
                value={changeNote}
                onChange={(e) => setChangeNote(e.target.value)}
                rows={3}
                disabled={isUpdating}
              />
            </div>

            {selectedAssignment && (
              <div className="space-y-2">
                <Label>Sections to be updated:</Label>
                <div className="max-h-40 overflow-y-auto border rounded-md p-2 space-y-1">
                  {selectedAssignment.sections.map((section) => (
                    <div key={section.id} className="text-sm py-1">
                      • {section.title}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={handleCloseAssignDialog}>
              Cancel
            </Button>
            <Button
              onClick={handleSubmitAssignment}
              disabled={!newOwnerId || isUpdating}
            >
              {isUpdating ? 'Assigning...' : 'Assign Owner'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
