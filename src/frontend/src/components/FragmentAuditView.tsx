import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { 
  MagnifyingGlass, 
  FileText, 
  LinkSimple, 
  Warning, 
  CheckCircle,
  FileSearch,
  Gavel,
  Lightbulb,
  Flag,
  X,
  File,
  ClockCounterClockwise
} from '@phosphor-icons/react'
import { useState, useEffect } from 'react'
import { getFragmentAuditView, type FragmentAuditView as FragmentAuditViewType } from '@/lib/api'
import { formatDateTime } from '@/lib/helpers'
import AuditTrailView from './AuditTrailView'

interface FragmentAuditViewProps {
  fragmentType: string
  fragmentId: string
  onClose?: () => void
}

export default function FragmentAuditView({ fragmentType, fragmentId, onClose }: FragmentAuditViewProps) {
  const [auditView, setAuditView] = useState<FragmentAuditViewType | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState('overview')

  useEffect(() => {
    loadAuditView()
  }, [fragmentType, fragmentId])

  async function loadAuditView() {
    try {
      setLoading(true)
      const data = await getFragmentAuditView(fragmentType, fragmentId)
      setAuditView(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load fragment audit view')
    } finally {
      setLoading(false)
    }
  }

  function getSeverityBadgeColor(severity: string): string {
    switch (severity) {
      case 'error': return 'bg-red-100 text-red-800 border-red-300'
      case 'warning': return 'bg-yellow-100 text-yellow-800 border-yellow-300'
      case 'info': return 'bg-blue-100 text-blue-800 border-blue-300'
      default: return 'bg-gray-100 text-gray-800 border-gray-300'
    }
  }

  function getIntegrityStatusBadgeColor(status: string): string {
    switch (status) {
      case 'valid': return 'bg-green-100 text-green-800 border-green-300'
      case 'failed': return 'bg-red-100 text-red-800 border-red-300'
      case 'not-checked': return 'bg-gray-100 text-gray-800 border-gray-300'
      default: return 'bg-gray-100 text-gray-800 border-gray-300'
    }
  }

  function getSourceTypeIcon(sourceType: string) {
    switch (sourceType) {
      case 'internal-document': return <FileText size={16} weight="duotone" />
      case 'uploaded-evidence': return <File size={16} weight="duotone" />
      case 'external-url': return <LinkSimple size={16} weight="duotone" />
      case 'assumption': return <Lightbulb size={16} weight="duotone" />
      default: return <FileText size={16} weight="duotone" />
    }
  }

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Loading Audit View...</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (error || !auditView) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-red-600">Error Loading Audit View</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-red-600">{error || 'Fragment not found'}</p>
          {onClose && (
            <Button onClick={onClose} variant="outline" className="mt-4">
              Close
            </Button>
          )}
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-2">
              <FileSearch size={24} weight="duotone" />
              <div>
                <CardTitle>Fragment Audit View</CardTitle>
                <CardDescription>
                  Traceability for {auditView.fragmentType}: {auditView.fragmentTitle}
                </CardDescription>
              </div>
            </div>
            {onClose && (
              <Button onClick={onClose} variant="ghost" size="sm">
                <X size={20} />
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {/* Fragment Information */}
            <div className="flex flex-wrap gap-2">
              <Badge variant="outline">
                Type: {auditView.fragmentType}
              </Badge>
              <Badge variant="outline">
                ID: {auditView.stableFragmentIdentifier}
              </Badge>
              {auditView.hasCompleteProvenance ? (
                <Badge className="bg-green-100 text-green-800 border-green-300">
                  <CheckCircle size={16} className="mr-1" weight="fill" />
                  Complete Provenance
                </Badge>
              ) : (
                <Badge className="bg-yellow-100 text-yellow-800 border-yellow-300">
                  <Warning size={16} className="mr-1" weight="fill" />
                  Incomplete Provenance
                </Badge>
              )}
            </div>

            {auditView.sectionInfo && (
              <div className="bg-gray-50 rounded-lg p-3">
                <p className="text-sm font-medium">Section: {auditView.sectionInfo.sectionTitle}</p>
                <p className="text-xs text-gray-600">
                  Category: {auditView.sectionInfo.sectionCategory}
                  {auditView.sectionInfo.catalogCode && ` • Code: ${auditView.sectionInfo.catalogCode}`}
                </p>
              </div>
            )}

            {/* Provenance Warnings */}
            {auditView.provenanceWarnings.length > 0 && (
              <div className="border rounded-lg p-4 bg-yellow-50 border-yellow-200">
                <h4 className="font-medium flex items-center gap-2 mb-2">
                  <Warning size={20} weight="fill" className="text-yellow-600" />
                  Provenance Warnings
                </h4>
                <div className="space-y-2">
                  {auditView.provenanceWarnings.map((warning, idx) => (
                    <div key={idx} className="bg-white rounded p-3">
                      <div className="flex items-start gap-2">
                        <Badge className={getSeverityBadgeColor(warning.severity)}>
                          {warning.severity}
                        </Badge>
                        <div className="flex-1">
                          <p className="text-sm font-medium">{warning.message}</p>
                          {warning.recommendation && (
                            <p className="text-xs text-gray-600 mt-1">
                              💡 {warning.recommendation}
                            </p>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Tabs for different types of links */}
      <Card>
        <CardContent className="pt-6">
          <Tabs value={activeTab} onValueChange={setActiveTab}>
            <TabsList className="grid w-full grid-cols-6">
              <TabsTrigger value="overview">
                Overview ({
                  auditView.linkedSources.length +
                  auditView.linkedEvidenceFiles.length +
                  auditView.linkedDecisions.length +
                  auditView.linkedAssumptions.length +
                  auditView.linkedGaps.length
                })
              </TabsTrigger>
              <TabsTrigger value="sources">
                Sources ({auditView.linkedSources.length})
              </TabsTrigger>
              <TabsTrigger value="evidence">
                Evidence ({auditView.linkedEvidenceFiles.length})
              </TabsTrigger>
              <TabsTrigger value="decisions">
                Decisions ({auditView.linkedDecisions.length})
              </TabsTrigger>
              <TabsTrigger value="assumptions">
                Assumptions ({auditView.linkedAssumptions.length})
              </TabsTrigger>
              <TabsTrigger value="gaps">
                Gaps ({auditView.linkedGaps.length})
              </TabsTrigger>
            </TabsList>

            <TabsContent value="overview" className="space-y-4 mt-4">
              <div className="grid grid-cols-2 gap-4">
                <Card>
                  <CardHeader className="pb-3">
                    <CardTitle className="text-sm">Traceability Summary</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2 text-sm">
                      <div className="flex justify-between">
                        <span>Sources:</span>
                        <span className="font-medium">{auditView.linkedSources.length}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Evidence Files:</span>
                        <span className="font-medium">{auditView.linkedEvidenceFiles.length}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Decisions:</span>
                        <span className="font-medium">{auditView.linkedDecisions.length}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Assumptions:</span>
                        <span className="font-medium">{auditView.linkedAssumptions.length}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>Gaps:</span>
                        <span className="font-medium">{auditView.linkedGaps.length}</span>
                      </div>
                    </div>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader className="pb-3">
                    <CardTitle className="text-sm">Audit Trail</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="text-sm">
                      <p className="text-gray-600">
                        {auditView.auditTrail.length > 0 ? (
                          <>
                            Last change: {formatDateTime(auditView.auditTrail[0].timestamp)}
                            <br />
                            By: {auditView.auditTrail[0].userName}
                          </>
                        ) : (
                          'No audit trail entries'
                        )}
                      </p>
                      {auditView.auditTrail.length > 1 && (
                        <p className="text-xs text-gray-500 mt-2">
                          Total changes: {auditView.auditTrail.length}
                        </p>
                      )}
                    </div>
                  </CardContent>
                </Card>
              </div>
            </TabsContent>

            <TabsContent value="sources" className="space-y-3 mt-4">
              {auditView.linkedSources.length === 0 ? (
                <p className="text-gray-500 text-sm py-4">No source references linked</p>
              ) : (
                auditView.linkedSources.map((source, idx) => (
                  <Card key={idx}>
                    <CardContent className="pt-4">
                      <div className="flex items-start gap-3">
                        <div className="text-gray-600">
                          {getSourceTypeIcon(source.sourceType)}
                        </div>
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-1">
                            <Badge variant="outline" className="text-xs">
                              {source.sourceType}
                            </Badge>
                            {source.lastUpdated && (
                              <span className="text-xs text-gray-500">
                                Updated: {formatDateTime(source.lastUpdated)}
                              </span>
                            )}
                          </div>
                          <p className="text-sm font-medium">{source.description}</p>
                          <p className="text-xs text-gray-600 mt-1">
                            Ref: {source.sourceReference}
                          </p>
                          {source.ownerName && (
                            <p className="text-xs text-gray-500">Owner: {source.ownerName}</p>
                          )}
                          {source.originSystem && (
                            <p className="text-xs text-gray-500">System: {source.originSystem}</p>
                          )}
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))
              )}
            </TabsContent>

            <TabsContent value="evidence" className="space-y-3 mt-4">
              {auditView.linkedEvidenceFiles.length === 0 ? (
                <p className="text-gray-500 text-sm py-4">No evidence files linked</p>
              ) : (
                auditView.linkedEvidenceFiles.map((evidence) => (
                  <Card key={evidence.evidenceId}>
                    <CardContent className="pt-4">
                      <div className="flex items-start gap-3">
                        <File size={20} weight="duotone" className="text-gray-600" />
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-1">
                            <p className="text-sm font-medium">{evidence.fileName}</p>
                            <Badge className={getIntegrityStatusBadgeColor(evidence.integrityStatus)}>
                              {evidence.integrityStatus}
                            </Badge>
                          </div>
                          {evidence.description && (
                            <p className="text-xs text-gray-600">{evidence.description}</p>
                          )}
                          <p className="text-xs text-gray-500 mt-1">
                            Uploaded by {evidence.uploadedBy} on {formatDateTime(evidence.uploadedAt)}
                          </p>
                          {evidence.checksum && (
                            <p className="text-xs text-gray-400 font-mono mt-1">
                              Checksum: {evidence.checksum.substring(0, 16)}...
                            </p>
                          )}
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))
              )}
            </TabsContent>

            <TabsContent value="decisions" className="space-y-3 mt-4">
              {auditView.linkedDecisions.length === 0 ? (
                <p className="text-gray-500 text-sm py-4">No decisions linked</p>
              ) : (
                auditView.linkedDecisions.map((decision) => (
                  <Card key={decision.decisionId}>
                    <CardContent className="pt-4">
                      <div className="flex items-start gap-3">
                        <Gavel size={20} weight="duotone" className="text-gray-600" />
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-1">
                            <p className="text-sm font-medium">{decision.title}</p>
                            <Badge variant="outline">v{decision.version}</Badge>
                            <Badge className={
                              decision.status === 'active' ? 'bg-green-100 text-green-800 border-green-300' :
                              decision.status === 'deprecated' ? 'bg-gray-100 text-gray-800 border-gray-300' :
                              'bg-yellow-100 text-yellow-800 border-yellow-300'
                            }>
                              {decision.status}
                            </Badge>
                          </div>
                          <p className="text-xs text-gray-600">{decision.decisionText}</p>
                          <p className="text-xs text-gray-500 mt-1">
                            By {decision.decisionBy} on {formatDateTime(decision.decisionDate)}
                          </p>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))
              )}
            </TabsContent>

            <TabsContent value="assumptions" className="space-y-3 mt-4">
              {auditView.linkedAssumptions.length === 0 ? (
                <p className="text-gray-500 text-sm py-4">No assumptions linked</p>
              ) : (
                auditView.linkedAssumptions.map((assumption) => (
                  <Card key={assumption.assumptionId}>
                    <CardContent className="pt-4">
                      <div className="flex items-start gap-3">
                        <Lightbulb size={20} weight="duotone" className="text-gray-600" />
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-1">
                            <p className="text-sm font-medium">{assumption.title}</p>
                            <Badge variant="outline">v{assumption.version}</Badge>
                            <Badge className={
                              assumption.status === 'active' ? 'bg-green-100 text-green-800 border-green-300' :
                              assumption.status === 'deprecated' ? 'bg-gray-100 text-gray-800 border-gray-300' :
                              'bg-red-100 text-red-800 border-red-300'
                            }>
                              {assumption.status}
                            </Badge>
                          </div>
                          <p className="text-xs text-gray-600">{assumption.description}</p>
                          {assumption.methodology && (
                            <p className="text-xs text-gray-500 mt-1">
                              Methodology: {assumption.methodology}
                            </p>
                          )}
                          <p className="text-xs text-gray-500 mt-1">
                            By {assumption.createdBy} on {formatDateTime(assumption.createdAt)}
                          </p>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))
              )}
            </TabsContent>

            <TabsContent value="gaps" className="space-y-3 mt-4">
              {auditView.linkedGaps.length === 0 ? (
                <p className="text-gray-500 text-sm py-4">No gaps identified</p>
              ) : (
                auditView.linkedGaps.map((gap) => (
                  <Card key={gap.gapId}>
                    <CardContent className="pt-4">
                      <div className="flex items-start gap-3">
                        <Flag size={20} weight="duotone" className="text-gray-600" />
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-1">
                            <p className="text-sm font-medium">{gap.title}</p>
                            <Badge className={
                              gap.impact === 'high' ? 'bg-red-100 text-red-800 border-red-300' :
                              gap.impact === 'medium' ? 'bg-yellow-100 text-yellow-800 border-yellow-300' :
                              'bg-blue-100 text-blue-800 border-blue-300'
                            }>
                              {gap.impact} impact
                            </Badge>
                            {gap.resolved && (
                              <Badge className="bg-green-100 text-green-800 border-green-300">
                                Resolved
                              </Badge>
                            )}
                          </div>
                          <p className="text-xs text-gray-600">{gap.description}</p>
                          {gap.improvementPlan && (
                            <p className="text-xs text-gray-500 mt-1">
                              Plan: {gap.improvementPlan}
                            </p>
                          )}
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))
              )}
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>

      {/* Audit Trail Section */}
      {auditView.auditTrail.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <ClockCounterClockwise size={20} weight="duotone" />
              Recent Changes
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {auditView.auditTrail.slice(0, 5).map((entry) => (
                <div key={entry.id} className="border-l-2 border-gray-200 pl-3 py-1">
                  <div className="flex items-center gap-2 text-sm">
                    <Badge variant="outline" className="text-xs">
                      {entry.action}
                    </Badge>
                    <span className="text-gray-600">{entry.userName}</span>
                    <span className="text-xs text-gray-400">
                      {formatDateTime(entry.timestamp)}
                    </span>
                  </div>
                  {entry.changeNote && (
                    <p className="text-xs text-gray-600 mt-1">{entry.changeNote}</p>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
