import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { LockKey, ClockCounterClockwise, ShieldCheck, ShieldWarning, MagnifyingGlass } from '@phosphor-icons/react'
import type { PermissionMatrix, AuditLogEntry } from '@/lib/types'
import { Input } from '@/components/ui/input'

export default function PermissionMatrixView() {
  const [matrix, setMatrix] = useState<PermissionMatrix | null>(null)
  const [history, setHistory] = useState<AuditLogEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState('')

  useEffect(() => {
    fetchMatrix()
    fetchHistory()
  }, [])

  const fetchMatrix = async () => {
    try {
      const response = await fetch('http://localhost:5000/api/permissions/matrix')
      const data = await response.json()
      setMatrix(data)
    } catch (error) {
      console.error('Failed to fetch permission matrix:', error)
    } finally {
      setLoading(false)
    }
  }

  const fetchHistory = async () => {
    try {
      const response = await fetch('http://localhost:5000/api/permissions/history?limit=50')
      const data = await response.json()
      setHistory(data)
    } catch (error) {
      console.error('Failed to fetch permission history:', error)
    }
  }

  if (loading) {
    return <div className="p-6">Loading permission matrix...</div>
  }

  if (!matrix) {
    return <div className="p-6">Failed to load permission matrix.</div>
  }

  // Filter entries based on search term
  const filteredEntries = matrix.entries.filter(entry =>
    entry.roleName.toLowerCase().includes(searchTerm.toLowerCase())
  )

  // Helper to get action badge color
  const getActionBadgeColor = (action: string) => {
    switch (action) {
      case 'view':
        return 'bg-blue-100 text-blue-800 border-blue-300'
      case 'edit':
        return 'bg-yellow-100 text-yellow-800 border-yellow-300'
      case 'approve':
      case 'export':
        return 'bg-green-100 text-green-800 border-green-300'
      case 'reject':
        return 'bg-red-100 text-red-800 border-red-300'
      case 'manage':
        return 'bg-purple-100 text-purple-800 border-purple-300'
      case 'comment':
      case 'submit':
        return 'bg-gray-100 text-gray-800 border-gray-300'
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300'
    }
  }

  // Helper to format resource type for display
  const formatResourceType = (resourceType: string) => {
    return resourceType
      .split('-')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ')
  }

  // Helper to format action description
  const getActionDescription = (action: string) => {
    const descriptions: Record<string, string> = {
      view: 'View and read',
      edit: 'Create and modify',
      comment: 'Add comments',
      submit: 'Submit for approval',
      approve: 'Approve changes',
      reject: 'Reject submissions',
      export: 'Export data',
      manage: 'Full management'
    }
    return descriptions[action] || action
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Permission Matrix</h2>
          <p className="text-muted-foreground">
            View role permissions across key system resources
          </p>
        </div>
        <Badge variant="outline" className="gap-2">
          <ClockCounterClockwise size={16} weight="bold" />
          Updated {new Date(matrix.generatedAt).toLocaleString()}
        </Badge>
      </div>

      <Tabs defaultValue="matrix" className="space-y-6">
        <TabsList>
          <TabsTrigger value="matrix">Permission Matrix</TabsTrigger>
          <TabsTrigger value="history">Change History</TabsTrigger>
        </TabsList>

        <TabsContent value="matrix" className="space-y-6">
          {/* Search Bar */}
          <div className="flex items-center gap-2">
            <div className="relative flex-1 max-w-sm">
              <MagnifyingGlass 
                size={18} 
                className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" 
                weight="bold"
              />
              <Input
                type="text"
                placeholder="Search roles..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>
            {searchTerm && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSearchTerm('')}
              >
                Clear
              </Button>
            )}
          </div>

          {/* Matrix Overview Card */}
          <Card>
            <CardHeader>
              <CardTitle>Matrix Overview</CardTitle>
              <CardDescription>
                {matrix.entries.length} roles × {matrix.resourceTypes.length} resource types × {matrix.allActions.length} actions
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <div>
                  <span className="text-sm font-medium text-muted-foreground">Resource Types:</span>
                  <div className="flex flex-wrap gap-2 mt-1">
                    {matrix.resourceTypes.map((resourceType) => (
                      <Badge key={resourceType} variant="outline">
                        {formatResourceType(resourceType)}
                      </Badge>
                    ))}
                  </div>
                </div>
                <div>
                  <span className="text-sm font-medium text-muted-foreground">Available Actions:</span>
                  <div className="flex flex-wrap gap-2 mt-1">
                    {matrix.allActions.map((action) => (
                      <Badge 
                        key={action} 
                        variant="outline"
                        className={getActionBadgeColor(action)}
                      >
                        {getActionDescription(action)}
                      </Badge>
                    ))}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Role Permission Cards */}
          <div className="grid gap-4">
            {filteredEntries.length === 0 ? (
              <Card>
                <CardContent className="py-8 text-center text-muted-foreground">
                  No roles found matching "{searchTerm}"
                </CardContent>
              </Card>
            ) : (
              filteredEntries.map((entry) => (
                <Card key={entry.roleId} className={entry.isPredefined ? 'border-l-4 border-l-primary' : ''}>
                  <CardHeader>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <CardTitle className="text-lg">{entry.roleName}</CardTitle>
                        {entry.isPredefined && (
                          <Badge variant="secondary" className="gap-1">
                            <LockKey size={14} weight="bold" />
                            Predefined
                          </Badge>
                        )}
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-4">
                      {matrix.resourceTypes.map((resourceType) => {
                        const actions = entry.resourceActions[resourceType] || []
                        
                        if (actions.length === 0) {
                          return null
                        }

                        return (
                          <div key={resourceType} className="space-y-2">
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-medium">
                                {formatResourceType(resourceType)}
                              </span>
                              <div className="h-px flex-1 bg-border" />
                            </div>
                            <div className="flex flex-wrap gap-2">
                              {actions.map((action) => (
                                <Badge
                                  key={action}
                                  variant="outline"
                                  className={`gap-1 ${getActionBadgeColor(action)}`}
                                >
                                  <ShieldCheck size={14} weight="bold" />
                                  {getActionDescription(action)}
                                </Badge>
                              ))}
                            </div>
                          </div>
                        )
                      })}
                      
                      {/* Show if role has no permissions */}
                      {Object.values(entry.resourceActions).every(actions => actions.length === 0) && (
                        <div className="flex items-center gap-2 text-sm text-muted-foreground py-4 justify-center">
                          <ShieldWarning size={16} weight="bold" />
                          No permissions assigned
                        </div>
                      )}
                    </div>
                  </CardContent>
                </Card>
              ))
            )}
          </div>
        </TabsContent>

        <TabsContent value="history" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Permission Change History</CardTitle>
              <CardDescription>
                Audit trail of permission-related changes including role modifications and user assignments
              </CardDescription>
            </CardHeader>
            <CardContent>
              {history.length === 0 ? (
                <div className="py-8 text-center text-muted-foreground">
                  No permission changes recorded yet
                </div>
              ) : (
                <div className="space-y-4">
                  {history.map((entry, index) => (
                    <div
                      key={index}
                      className="flex items-start gap-4 rounded-lg border p-4 hover:bg-muted/50 transition-colors"
                    >
                      <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10">
                        <ClockCounterClockwise size={20} weight="bold" className="text-primary" />
                      </div>
                      <div className="flex-1 space-y-1">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                            <span className="font-medium">{entry.userName}</span>
                            <Badge variant="outline" className="text-xs">
                              {entry.action}
                            </Badge>
                          </div>
                          <span className="text-xs text-muted-foreground">
                            {new Date(entry.timestamp).toLocaleString()}
                          </span>
                        </div>
                        <p className="text-sm text-muted-foreground">{entry.description}</p>
                        {entry.changes.length > 0 && (
                          <div className="mt-2 space-y-1">
                            {entry.changes.map((change, changeIndex) => (
                              <div key={changeIndex} className="text-xs font-mono bg-muted rounded px-2 py-1">
                                <span className="text-muted-foreground">{change.field}:</span>{' '}
                                {change.oldValue && (
                                  <>
                                    <span className="text-red-600 line-through">{change.oldValue}</span>
                                    {' → '}
                                  </>
                                )}
                                <span className="text-green-600">{change.newValue}</span>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}
