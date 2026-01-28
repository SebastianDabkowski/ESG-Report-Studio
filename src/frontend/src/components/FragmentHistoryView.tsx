import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ClockCounterClockwise, ArrowsLeftRight, File, ChatCircle } from '@phosphor-icons/react'
import { useState, useEffect, useCallback } from 'react'
import { getEntityTimeline, type EntityTimeline } from '@/lib/api'
import { formatDateTime } from '@/lib/helpers'
import VersionComparisonView from './VersionComparisonView'

interface FragmentHistoryViewProps {
  entityType: string
  entityId: string
  entityTitle?: string
  onClose?: () => void
}

export default function FragmentHistoryView({ 
  entityType, 
  entityId, 
  entityTitle,
  onClose 
}: FragmentHistoryViewProps) {
  const [timeline, setTimeline] = useState<EntityTimeline | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedVersions, setSelectedVersions] = useState<string[]>([])
  const [showComparison, setShowComparison] = useState(false)

  const loadTimeline = useCallback(async () => {
    try {
      setLoading(true)
      const data = await getEntityTimeline(entityType, entityId)
      setTimeline(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load change history')
    } finally {
      setLoading(false)
    }
  }, [entityType, entityId])

  useEffect(() => {
    loadTimeline()
  }, [loadTimeline])

  function handleVersionSelect(versionId: string) {
    setSelectedVersions(prev => {
      if (prev.includes(versionId)) {
        return prev.filter(id => id !== versionId)
      }
      // Only allow 2 versions to be selected
      if (prev.length >= 2) {
        return [prev[1], versionId]
      }
      return [...prev, versionId]
    })
  }

  function handleCompare() {
    if (selectedVersions.length === 2) {
      setShowComparison(true)
    }
  }

  function getActionBadgeColor(action: string): string {
    switch (action.toLowerCase()) {
      case 'create': return 'bg-green-100 text-green-800 border-green-300'
      case 'update': return 'bg-blue-100 text-blue-800 border-blue-300'
      case 'delete': return 'bg-red-100 text-red-800 border-red-300'
      case 'approve': return 'bg-purple-100 text-purple-800 border-purple-300'
      default: return 'bg-gray-100 text-gray-800 border-gray-300'
    }
  }

  if (showComparison && selectedVersions.length === 2) {
    return (
      <VersionComparisonView
        entityType={entityType}
        entityId={entityId}
        fromVersion={selectedVersions[0]}
        toVersion={selectedVersions[1]}
        onClose={() => {
          setShowComparison(false)
          setSelectedVersions([])
        }}
      />
    )
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-2">
              <ClockCounterClockwise size={24} weight="duotone" />
              <div>
                <CardTitle>Change History</CardTitle>
                <CardDescription>
                  {entityTitle ? `${entityTitle} - ` : ''}{entityType}
                </CardDescription>
              </div>
            </div>
            <div className="flex gap-2">
              {selectedVersions.length === 2 && (
                <Button
                  onClick={handleCompare}
                  variant="outline"
                  size="sm"
                >
                  <ArrowsLeftRight className="mr-2" size={16} />
                  Compare Selected
                </Button>
              )}
              {onClose && (
                <Button onClick={onClose} variant="outline" size="sm">
                  Close
                </Button>
              )}
            </div>
          </div>
        </CardHeader>

        <CardContent>
          {loading && (
            <div className="text-center py-8 text-gray-500">
              Loading change history...
            </div>
          )}

          {error && (
            <div className="text-center py-8">
              <p className="text-red-600">{error}</p>
              <Button onClick={loadTimeline} className="mt-4" variant="outline">
                Retry
              </Button>
            </div>
          )}

          {!loading && !error && timeline && (
            <>
              {/* Metadata Section */}
              {timeline.metadata && (
                <Card className="mb-4 bg-gray-50">
                  <CardHeader>
                    <CardTitle className="text-sm">Fragment Details</CardTitle>
                  </CardHeader>
                  <CardContent className="text-sm space-y-2">
                    {timeline.metadata.title && (
                      <div><strong>Title:</strong> {timeline.metadata.title}</div>
                    )}
                    {timeline.metadata.sectionName && (
                      <div><strong>Section:</strong> {timeline.metadata.sectionName}</div>
                    )}
                    {timeline.metadata.type && (
                      <div><strong>Type:</strong> {timeline.metadata.type}</div>
                    )}
                    {timeline.metadata.status && (
                      <div><strong>Status:</strong> {timeline.metadata.status}</div>
                    )}
                    {timeline.metadata.evidenceCount !== undefined && (
                      <div><strong>Evidence Items:</strong> {timeline.metadata.evidenceCount}</div>
                    )}
                    {timeline.metadata.notesCount !== undefined && (
                      <div><strong>Notes:</strong> {timeline.metadata.notesCount}</div>
                    )}
                  </CardContent>
                </Card>
              )}

              {/* Summary */}
              <div className="mb-4 text-sm text-gray-600">
                Total changes: {timeline.totalChanges}
                {selectedVersions.length > 0 && (
                  <span className="ml-4">
                    Selected: {selectedVersions.length} version{selectedVersions.length > 1 ? 's' : ''}
                  </span>
                )}
              </div>

              {/* Timeline */}
              <div className="space-y-3">
                {timeline.timeline.length === 0 ? (
                  <div className="text-center py-8 text-gray-500">
                    No change history available
                  </div>
                ) : (
                  timeline.timeline.map((entry) => (
                    <Card 
                      key={entry.id}
                      className={`transition-colors ${
                        selectedVersions.includes(entry.id) 
                          ? 'border-blue-500 bg-blue-50' 
                          : 'hover:bg-gray-50'
                      }`}
                    >
                      <CardContent className="pt-4">
                        <div className="flex items-start justify-between mb-3">
                          <div className="flex items-center gap-2">
                            <input
                              type="checkbox"
                              checked={selectedVersions.includes(entry.id)}
                              onChange={() => handleVersionSelect(entry.id)}
                              className="rounded border-gray-300"
                            />
                            <Badge className={getActionBadgeColor(entry.action)}>
                              {entry.action}
                            </Badge>
                            <span className="text-sm font-medium">{entry.userName}</span>
                          </div>
                          <span className="text-xs text-gray-500">
                            {formatDateTime(entry.timestamp)}
                          </span>
                        </div>

                        {entry.changeNote && (
                          <div className="mb-3 text-sm text-gray-700 flex items-start gap-2">
                            <ChatCircle size={16} className="mt-0.5 flex-shrink-0" />
                            <span>{entry.changeNote}</span>
                          </div>
                        )}

                        {entry.changes && entry.changes.length > 0 && (
                          <div className="space-y-2">
                            {entry.changes.map((change, idx) => (
                              <div key={idx} className="text-sm border-l-2 border-gray-300 pl-3">
                                <div className="font-medium text-gray-700">{change.field}</div>
                                <div className="grid grid-cols-2 gap-2 mt-1">
                                  <div>
                                    <span className="text-xs text-gray-500">Before:</span>
                                    <div className="text-gray-600 truncate">
                                      {change.before || <em className="text-gray-400">(empty)</em>}
                                    </div>
                                  </div>
                                  <div>
                                    <span className="text-xs text-gray-500">After:</span>
                                    <div className="text-gray-900 truncate font-medium">
                                      {change.after || <em className="text-gray-400">(empty)</em>}
                                    </div>
                                  </div>
                                </div>
                              </div>
                            ))}
                          </div>
                        )}
                      </CardContent>
                    </Card>
                  ))
                )}
              </div>

              {/* Evidence and Notes */}
              {timeline.metadata?.evidence && timeline.metadata.evidence.length > 0 && (
                <Card className="mt-4">
                  <CardHeader>
                    <CardTitle className="text-sm flex items-center gap-2">
                      <File size={16} />
                      Related Evidence
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {timeline.metadata.evidence.map((ev: any) => (
                        <div key={ev.id} className="text-sm flex items-center justify-between">
                          <span>{ev.fileName}</span>
                          <span className="text-xs text-gray-500">
                            {formatDateTime(ev.uploadedAt)}
                          </span>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}

              {timeline.metadata?.notes && timeline.metadata.notes.length > 0 && (
                <Card className="mt-4">
                  <CardHeader>
                    <CardTitle className="text-sm flex items-center gap-2">
                      <ChatCircle size={16} />
                      Related Notes
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {timeline.metadata.notes.map((note: any) => (
                        <div key={note.id} className="text-sm border-l-2 border-gray-300 pl-3">
                          <div className="text-gray-700">{note.content}</div>
                          <div className="text-xs text-gray-500 mt-1">
                            {note.createdBy} - {formatDateTime(note.createdAt)}
                          </div>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
