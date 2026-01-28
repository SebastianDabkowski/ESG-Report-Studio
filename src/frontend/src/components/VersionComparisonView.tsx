import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ArrowsLeftRight, ArrowLeft } from '@phosphor-icons/react'
import { useState, useEffect } from 'react'
import { compareVersions, type VersionComparison } from '@/lib/api'
import { formatDateTime } from '@/lib/helpers'

interface VersionComparisonViewProps {
  entityType: string
  entityId: string
  fromVersion: string
  toVersion: string
  onClose?: () => void
}

export default function VersionComparisonView({
  entityType,
  entityId,
  fromVersion,
  toVersion,
  onClose
}: VersionComparisonViewProps) {
  const [comparison, setComparison] = useState<VersionComparison | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadComparison()
  }, [entityType, entityId, fromVersion, toVersion])

  async function loadComparison() {
    try {
      setLoading(true)
      const data = await compareVersions(entityType, entityId, fromVersion, toVersion)
      setComparison(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to compare versions')
    } finally {
      setLoading(false)
    }
  }

  function getChangeTypeColor(changeType: string): string {
    switch (changeType) {
      case 'added': return 'bg-green-100 text-green-800 border-green-300'
      case 'removed': return 'bg-red-100 text-red-800 border-red-300'
      case 'modified': return 'bg-blue-100 text-blue-800 border-blue-300'
      default: return 'bg-gray-100 text-gray-800 border-gray-300'
    }
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-2">
              <ArrowsLeftRight size={24} weight="duotone" />
              <div>
                <CardTitle>Version Comparison</CardTitle>
                <CardDescription>
                  {entityType} - Side-by-side comparison
                </CardDescription>
              </div>
            </div>
            {onClose && (
              <Button onClick={onClose} variant="outline" size="sm">
                <ArrowLeft className="mr-2" size={16} />
                Back to History
              </Button>
            )}
          </div>
        </CardHeader>

        <CardContent>
          {loading && (
            <div className="text-center py-8 text-gray-500">
              Loading comparison...
            </div>
          )}

          {error && (
            <div className="text-center py-8">
              <p className="text-red-600">{error}</p>
              <Button onClick={loadComparison} className="mt-4" variant="outline">
                Retry
              </Button>
            </div>
          )}

          {!loading && !error && comparison && (
            <>
              {/* Version Headers */}
              <div className="grid grid-cols-2 gap-4 mb-6">
                <Card className="bg-gray-50">
                  <CardHeader className="pb-3">
                    <CardTitle className="text-sm">Earlier Version</CardTitle>
                  </CardHeader>
                  <CardContent className="text-sm space-y-1">
                    <div><strong>Action:</strong> {comparison.fromVersion.action}</div>
                    <div><strong>User:</strong> {comparison.fromVersion.userName}</div>
                    <div><strong>Time:</strong> {formatDateTime(comparison.fromVersion.timestamp)}</div>
                    {comparison.fromVersion.changeNote && (
                      <div><strong>Note:</strong> {comparison.fromVersion.changeNote}</div>
                    )}
                  </CardContent>
                </Card>

                <Card className="bg-blue-50">
                  <CardHeader className="pb-3">
                    <CardTitle className="text-sm">Later Version</CardTitle>
                  </CardHeader>
                  <CardContent className="text-sm space-y-1">
                    <div><strong>Action:</strong> {comparison.toVersion.action}</div>
                    <div><strong>User:</strong> {comparison.toVersion.userName}</div>
                    <div><strong>Time:</strong> {formatDateTime(comparison.toVersion.timestamp)}</div>
                    {comparison.toVersion.changeNote && (
                      <div><strong>Note:</strong> {comparison.toVersion.changeNote}</div>
                    )}
                  </CardContent>
                </Card>
              </div>

              {/* Metadata */}
              {comparison.metadata && (
                <Card className="mb-4 bg-gray-50">
                  <CardHeader>
                    <CardTitle className="text-sm">Fragment Details</CardTitle>
                  </CardHeader>
                  <CardContent className="text-sm space-y-2">
                    {comparison.metadata.title && (
                      <div><strong>Title:</strong> {comparison.metadata.title}</div>
                    )}
                    {comparison.metadata.sectionName && (
                      <div><strong>Section:</strong> {comparison.metadata.sectionName}</div>
                    )}
                    {comparison.metadata.type && (
                      <div><strong>Type:</strong> {comparison.metadata.type}</div>
                    )}
                  </CardContent>
                </Card>
              )}

              {/* Differences Summary */}
              <div className="mb-4">
                <h3 className="text-lg font-semibold mb-2">
                  Changes ({comparison.differences.length})
                </h3>
                <div className="flex gap-2 text-sm">
                  <Badge className={getChangeTypeColor('added')}>
                    {comparison.differences.filter(d => d.changeType === 'added').length} Added
                  </Badge>
                  <Badge className={getChangeTypeColor('modified')}>
                    {comparison.differences.filter(d => d.changeType === 'modified').length} Modified
                  </Badge>
                  <Badge className={getChangeTypeColor('removed')}>
                    {comparison.differences.filter(d => d.changeType === 'removed').length} Removed
                  </Badge>
                </div>
              </div>

              {/* Differences Details */}
              {comparison.differences.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  No differences found between these versions
                </div>
              ) : (
                <div className="space-y-3">
                  {comparison.differences.map((diff, idx) => (
                    <Card key={idx} className="border-l-4" style={{
                      borderLeftColor: diff.changeType === 'added' ? '#10b981' :
                                       diff.changeType === 'removed' ? '#ef4444' :
                                       '#3b82f6'
                    }}>
                      <CardContent className="pt-4">
                        <div className="flex items-start justify-between mb-3">
                          <div className="font-medium text-gray-900">{diff.field}</div>
                          <Badge className={getChangeTypeColor(diff.changeType)}>
                            {diff.changeType}
                          </Badge>
                        </div>

                        <div className="grid grid-cols-2 gap-4">
                          {/* Earlier Version */}
                          <div className={diff.changeType === 'removed' ? 'bg-red-50 p-3 rounded' : 'bg-gray-50 p-3 rounded'}>
                            <div className="text-xs font-medium text-gray-500 mb-2">EARLIER VERSION</div>
                            <div className="text-sm">
                              {diff.fromValue ? (
                                <div className={diff.changeType === 'removed' ? 'line-through text-red-700' : ''}>
                                  {diff.fromValue}
                                </div>
                              ) : (
                                <em className="text-gray-400">(empty)</em>
                              )}
                            </div>
                          </div>

                          {/* Later Version */}
                          <div className={diff.changeType === 'added' ? 'bg-green-50 p-3 rounded' : 'bg-blue-50 p-3 rounded'}>
                            <div className="text-xs font-medium text-gray-500 mb-2">LATER VERSION</div>
                            <div className="text-sm">
                              {diff.toValue ? (
                                <div className={diff.changeType === 'added' ? 'font-medium text-green-700' : 
                                               diff.changeType === 'modified' ? 'font-medium text-blue-700' : ''}>
                                  {diff.toValue}
                                </div>
                              ) : (
                                <em className="text-gray-400">(empty)</em>
                              )}
                            </div>
                          </div>
                        </div>

                        {/* Change Visualization for long text */}
                        {diff.changeType === 'modified' && diff.fromValue && diff.toValue && 
                         diff.fromValue.length > 100 && diff.toValue.length > 100 && (
                          <div className="mt-3 pt-3 border-t border-gray-200">
                            <div className="text-xs font-medium text-gray-500 mb-2">DETAILED COMPARISON</div>
                            <div className="text-sm space-y-2">
                              <div className="bg-red-50 p-2 rounded">
                                <span className="text-xs text-red-600 font-medium">- </span>
                                <span className="text-red-700">{diff.fromValue}</span>
                              </div>
                              <div className="bg-green-50 p-2 rounded">
                                <span className="text-xs text-green-600 font-medium">+ </span>
                                <span className="text-green-700">{diff.toValue}</span>
                              </div>
                            </div>
                          </div>
                        )}
                      </CardContent>
                    </Card>
                  ))}
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
