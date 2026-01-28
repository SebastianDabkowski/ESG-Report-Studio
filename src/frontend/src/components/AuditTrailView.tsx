import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { ClockCounterClockwise, FunnelSimple } from '@phosphor-icons/react'
import type { AuditLogEntry, User } from '@/lib/types'
import { formatDateTime } from '@/lib/helpers'
import { useState, useEffect } from 'react'
import { getAuditLog, getUsers, type AuditLogFilters } from '@/lib/api'

export default function AuditTrailView() {
  const [auditLog, setAuditLog] = useState<AuditLogEntry[]>([])
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showFilters, setShowFilters] = useState(false)
  
  // Filter state
  const [filters, setFilters] = useState<AuditLogFilters>({})
  const [filterUserId, setFilterUserId] = useState<string>('')
  const [filterStartDate, setFilterStartDate] = useState<string>('')
  const [filterEndDate, setFilterEndDate] = useState<string>('')

  useEffect(() => {
    loadData()
    loadUsers()
  }, [])

  async function loadData() {
    try {
      setLoading(true)
      const data = await getAuditLog(filters)
      setAuditLog(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load audit log')
    } finally {
      setLoading(false)
    }
  }

  async function loadUsers() {
    try {
      const usersData = await getUsers()
      setUsers(usersData)
    } catch (err) {
      console.error('Failed to load users:', err)
    }
  }

  function applyFilters() {
    const newFilters: AuditLogFilters = {}
    if (filterUserId) newFilters.userId = filterUserId
    if (filterStartDate) newFilters.startDate = filterStartDate
    if (filterEndDate) newFilters.endDate = filterEndDate
    
    setFilters(newFilters)
    loadData()
  }

  function clearFilters() {
    setFilterUserId('')
    setFilterStartDate('')
    setFilterEndDate('')
    setFilters({})
    loadData()
  }

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-foreground">
            Audit Trail
          </h2>
          <p className="text-sm text-muted-foreground mt-1">
            Loading audit history...
          </p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-foreground">
            Audit Trail
          </h2>
          <p className="text-sm text-destructive mt-1">{error}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-foreground">
              Audit Trail
            </h2>
            <p className="text-sm text-muted-foreground mt-1">
              Complete history of all changes and actions
            </p>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowFilters(!showFilters)}
          >
            <FunnelSimple size={16} className="mr-2" />
            {showFilters ? 'Hide Filters' : 'Show Filters'}
          </Button>
        </div>

        {showFilters && (
          <Card className="mt-4">
            <CardContent className="pt-6">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="filter-user">Filter by User</Label>
                  <Select value={filterUserId} onValueChange={setFilterUserId}>
                    <SelectTrigger id="filter-user">
                      <SelectValue placeholder="All users" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">All users</SelectItem>
                      {users.map(user => (
                        <SelectItem key={user.id} value={user.id}>
                          {user.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="filter-start">Start Date</Label>
                  <Input
                    id="filter-start"
                    type="date"
                    value={filterStartDate}
                    onChange={(e) => setFilterStartDate(e.target.value)}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="filter-end">End Date</Label>
                  <Input
                    id="filter-end"
                    type="date"
                    value={filterEndDate}
                    onChange={(e) => setFilterEndDate(e.target.value)}
                  />
                </div>
              </div>

              <div className="flex gap-2 mt-4">
                <Button onClick={applyFilters} size="sm">
                  Apply Filters
                </Button>
                <Button onClick={clearFilters} variant="outline" size="sm">
                  Clear Filters
                </Button>
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      {auditLog && auditLog.length > 0 ? (
        <div className="space-y-3">
          {auditLog.map(entry => (
            <Card key={entry.id}>
              <CardContent className="py-4">
                <div className="flex items-start gap-4">
                  <ClockCounterClockwise size={18} className="text-muted-foreground mt-0.5 flex-shrink-0" />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="font-medium text-sm">{entry.userName}</span>
                      <span className="text-muted-foreground text-xs">•</span>
                      <Badge variant="outline" className="text-xs">{entry.action}</Badge>
                      <Badge variant="secondary" className="text-xs">{entry.entityType}</Badge>
                    </div>
                    <div className="text-xs text-muted-foreground font-mono">
                      {formatDateTime(entry.timestamp)}
                    </div>
                    {entry.changeNote && (
                      <div className="mt-2 text-sm italic text-muted-foreground">
                        Note: {entry.changeNote}
                      </div>
                    )}
                    {entry.changes && entry.changes.length > 0 && (
                      <div className="mt-2 space-y-1">
                        {entry.changes.map((change, idx) => (
                          <div key={idx} className="text-xs">
                            <span className="font-medium">{change.field}:</span>{' '}
                            <span className="text-muted-foreground line-through">{change.oldValue}</span>
                            {' → '}
                            <span className="text-foreground">{change.newValue}</span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <ClockCounterClockwise size={48} weight="duotone" className="text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No Audit Entries</h3>
            <p className="text-sm text-muted-foreground text-center max-w-md">
              All system actions will be logged here for compliance and traceability.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}