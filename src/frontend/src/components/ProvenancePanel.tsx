import { AlertCircle, Warning } from '@phosphor-icons/react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import type { DataPoint } from '@/lib/types'

interface ProvenancePanelProps {
  dataPoint: DataPoint
}

const sourceTypeLabels: Record<string, string> = {
  'data-point': 'Data Point',
  'evidence': 'Evidence File',
  'assumption': 'Assumption',
  'external-system': 'External System',
  'uploaded-file': 'Uploaded File',
  'other': 'Other'
}

export function ProvenancePanel({ dataPoint }: ProvenancePanelProps) {
  const hasSourceReferences = dataPoint.sourceReferences && dataPoint.sourceReferences.length > 0
  const needsReview = dataPoint.provenanceNeedsReview

  if (!hasSourceReferences && !needsReview) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Provenance</CardTitle>
          <CardDescription>Source data traceability</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="text-center py-4 text-sm text-muted-foreground">
            <p>No source references linked to this statement.</p>
            <p className="mt-2 text-xs">
              Link source data to improve auditability and traceability.
            </p>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-4">
      {/* Review Alert */}
      {needsReview && (
        <Alert variant="destructive">
          <Warning className="h-4 w-4" />
          <AlertDescription className="flex flex-col gap-2">
            <div className="font-semibold">Provenance Needs Review</div>
            <div className="text-sm">
              {dataPoint.provenanceReviewReason || 'Source data has changed since this statement was created.'}
            </div>
            {dataPoint.provenanceFlaggedAt && (
              <div className="text-xs opacity-90">
                Flagged on {new Date(dataPoint.provenanceFlaggedAt).toLocaleString()}
              </div>
            )}
          </AlertDescription>
        </Alert>
      )}

      {/* Source References */}
      {hasSourceReferences && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-sm">Source References</CardTitle>
                <CardDescription>
                  {dataPoint.sourceReferences!.length} source{dataPoint.sourceReferences!.length !== 1 ? 's' : ''} linked
                </CardDescription>
              </div>
              {dataPoint.provenanceLastVerified && (
                <div className="text-xs text-muted-foreground">
                  Last verified: {new Date(dataPoint.provenanceLastVerified).toLocaleDateString()}
                </div>
              )}
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {dataPoint.sourceReferences!.map((source, index) => (
              <div key={index} className="border-l-2 border-blue-200 pl-4 py-2">
                <div className="flex items-start gap-2 mb-2">
                  <Badge variant="outline" className="shrink-0">
                    {sourceTypeLabels[source.sourceType] || source.sourceType}
                  </Badge>
                  <code className="text-xs bg-muted px-2 py-1 rounded font-mono">
                    {source.sourceReference}
                  </code>
                </div>
                
                <p className="text-sm mb-2">{source.description}</p>
                
                <div className="grid gap-1 text-xs text-muted-foreground">
                  {source.originSystem && (
                    <div className="flex gap-2">
                      <span className="font-medium min-w-20">Origin:</span>
                      <span>{source.originSystem}</span>
                    </div>
                  )}
                  {source.ownerName && (
                    <div className="flex gap-2">
                      <span className="font-medium min-w-20">Owner:</span>
                      <span>{source.ownerName}</span>
                    </div>
                  )}
                  {source.lastUpdated && (
                    <div className="flex gap-2">
                      <span className="font-medium min-w-20">Last Updated:</span>
                      <span>{new Date(source.lastUpdated).toLocaleDateString()}</span>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      )}

      {/* Publication Snapshot Info */}
      {dataPoint.publicationSourceHash && (
        <Card className="bg-muted/50">
          <CardHeader>
            <CardTitle className="text-sm">Publication Snapshot</CardTitle>
          </CardHeader>
          <CardContent className="text-xs text-muted-foreground space-y-1">
            <div className="flex gap-2">
              <span className="font-medium">Status:</span>
              <span>{needsReview ? 'Outdated - needs review' : 'Current'}</span>
            </div>
            {dataPoint.provenanceLastVerified && (
              <div className="flex gap-2">
                <span className="font-medium">Snapshot Date:</span>
                <span>{new Date(dataPoint.provenanceLastVerified).toLocaleString()}</span>
              </div>
            )}
            <div className="mt-2 pt-2 border-t border-border">
              <p className="text-xs">
                <AlertCircle className="h-3 w-3 inline mr-1" />
                This snapshot tracks the state of source data at publication time to detect changes.
              </p>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
