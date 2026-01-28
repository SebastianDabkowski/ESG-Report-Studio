import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Checkbox } from '@/components/ui/checkbox'
import { PaperclipHorizontal, Link as LinkIcon, X, Info } from '@phosphor-icons/react'
import type { Evidence, DataPoint } from '@/lib/types'
import { formatDateTime } from '@/lib/helpers'

interface DataPointEvidenceManagerProps {
  dataPoint: DataPoint
  allEvidence: Evidence[]
  onLinkEvidence: (evidenceId: string) => void | Promise<void>
  onUnlinkEvidence: (evidenceId: string) => void | Promise<void>
}

export default function DataPointEvidenceManager({
  dataPoint,
  allEvidence,
  onLinkEvidence,
  onUnlinkEvidence
}: DataPointEvidenceManagerProps) {
  const [isLinkDialogOpen, setIsLinkDialogOpen] = useState(false)
  const [selectedEvidenceIds, setSelectedEvidenceIds] = useState<Set<string>>(new Set())

  // Get evidence already linked to this data point
  const linkedEvidence = allEvidence.filter(e => dataPoint.evidenceIds.includes(e.id))
  
  // Get evidence available to link (same section, not already linked)
  const availableEvidence = allEvidence.filter(e => 
    e.sectionId === dataPoint.sectionId && !dataPoint.evidenceIds.includes(e.id)
  )

  const handleToggleEvidence = (evidenceId: string) => {
    setSelectedEvidenceIds(prev => {
      const newSet = new Set(prev)
      if (newSet.has(evidenceId)) {
        newSet.delete(evidenceId)
      } else {
        newSet.add(evidenceId)
      }
      return newSet
    })
  }

  const handleLinkSelected = async () => {
    for (const evidenceId of selectedEvidenceIds) {
      await onLinkEvidence(evidenceId)
    }
    setSelectedEvidenceIds(new Set())
    setIsLinkDialogOpen(false)
  }

  const handleUnlink = async (evidenceId: string) => {
    await onUnlinkEvidence(evidenceId)
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold">Evidence</h3>
        <Button 
          size="sm" 
          variant="outline" 
          onClick={() => setIsLinkDialogOpen(true)}
          disabled={availableEvidence.length === 0}
        >
          <LinkIcon size={14} className="mr-2" />
          Link Evidence
        </Button>
      </div>

      {linkedEvidence.length > 0 ? (
        <div className="space-y-2">
          {linkedEvidence.map(evidence => (
            <Card key={evidence.id} className="relative">
              <CardHeader className="pb-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex-1">
                    <CardTitle className="text-sm flex items-center gap-2">
                      <PaperclipHorizontal size={16} />
                      {evidence.title}
                    </CardTitle>
                    {evidence.description && (
                      <CardDescription className="text-xs mt-1">
                        {evidence.description}
                      </CardDescription>
                    )}
                  </div>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => handleUnlink(evidence.id)}
                    className="h-6 w-6 p-0"
                  >
                    <X size={14} />
                  </Button>
                </div>
              </CardHeader>
              <CardContent className="pt-0 pb-2">
                <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
                  {evidence.fileName && (
                    <Badge variant="outline" className="text-xs">
                      {evidence.fileName}
                    </Badge>
                  )}
                  {evidence.sourceUrl && (
                    <a 
                      href={evidence.sourceUrl} 
                      target="_blank" 
                      rel="noopener noreferrer"
                      className="text-blue-600 hover:underline"
                    >
                      View Source
                    </a>
                  )}
                  <span>Uploaded: {formatDateTime(evidence.uploadedAt)}</span>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Alert>
          <Info size={16} />
          <AlertDescription className="text-sm">
            No evidence linked to this data point. Click "Link Evidence" to add supporting documentation.
          </AlertDescription>
        </Alert>
      )}

      {/* Link Evidence Dialog */}
      <Dialog open={isLinkDialogOpen} onOpenChange={setIsLinkDialogOpen}>
        <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Link Evidence to Data Point</DialogTitle>
            <DialogDescription>
              Select evidence items from this section to link to "{dataPoint.title}"
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-3 py-4">
            {availableEvidence.length > 0 ? (
              availableEvidence.map(evidence => (
                <div 
                  key={evidence.id}
                  className="flex items-start gap-3 p-3 border rounded-lg hover:bg-muted/50 cursor-pointer"
                  onClick={() => handleToggleEvidence(evidence.id)}
                >
                  <Checkbox
                    checked={selectedEvidenceIds.has(evidence.id)}
                    onCheckedChange={() => handleToggleEvidence(evidence.id)}
                  />
                  <div className="flex-1">
                    <div className="font-medium text-sm flex items-center gap-2">
                      <PaperclipHorizontal size={16} />
                      {evidence.title}
                    </div>
                    {evidence.description && (
                      <p className="text-xs text-muted-foreground mt-1">
                        {evidence.description}
                      </p>
                    )}
                    <div className="flex flex-wrap gap-2 mt-2 text-xs text-muted-foreground">
                      {evidence.fileName && (
                        <Badge variant="outline" className="text-xs">
                          {evidence.fileName}
                        </Badge>
                      )}
                      <span>Uploaded: {formatDateTime(evidence.uploadedAt)}</span>
                    </div>
                  </div>
                </div>
              ))
            ) : (
              <Alert>
                <Info size={16} />
                <AlertDescription>
                  No evidence available in this section. Upload evidence first before linking.
                </AlertDescription>
              </Alert>
            )}
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button variant="outline" onClick={() => setIsLinkDialogOpen(false)}>
              Cancel
            </Button>
            <Button 
              onClick={handleLinkSelected}
              disabled={selectedEvidenceIds.size === 0}
            >
              Link {selectedEvidenceIds.size > 0 ? `(${selectedEvidenceIds.size})` : ''}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}
