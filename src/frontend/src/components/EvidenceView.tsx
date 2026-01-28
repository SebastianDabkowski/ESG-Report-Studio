import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { useKV } from '@github/spark/hooks'
import { PaperclipHorizontal, FileText, ShieldCheck, ShieldWarning, Question } from '@phosphor-icons/react'
import type { User, Evidence, ReportSection } from '@/lib/types'
import { formatDateTime } from '@/lib/helpers'

interface EvidenceViewProps {
  currentUser: User
}

export default function EvidenceView({ currentUser }: EvidenceViewProps) {
  const [evidence] = useKV<Evidence[]>('evidence', [])
  const [sections] = useKV<ReportSection[]>('report-sections', [])

  const getSectionTitle = (sectionId: string) => {
    const section = sections?.find(s => s.id === sectionId)
    return section?.title || 'Unknown Section'
  }

  const getIntegrityBadge = (status: string) => {
    switch (status) {
      case 'valid':
        return (
          <Badge variant="outline" className="text-green-600 border-green-600">
            <ShieldCheck size={14} className="mr-1" />
            Valid
          </Badge>
        )
      case 'failed':
        return (
          <Badge variant="outline" className="text-red-600 border-red-600">
            <ShieldWarning size={14} className="mr-1" />
            Failed
          </Badge>
        )
      default:
        return (
          <Badge variant="outline" className="text-gray-600">
            <Question size={14} className="mr-1" />
            Not Checked
          </Badge>
        )
    }
  }

  const formatFileSize = (bytes?: number) => {
    if (!bytes) return 'N/A'
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold tracking-tight text-foreground">
          Evidence Registry
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          All supporting documentation and references across the report
        </p>
      </div>

      {evidence && evidence.length > 0 ? (
        <div className="grid gap-4">
          {evidence.map(item => (
            <Card key={item.id}>
              <CardHeader>
                <div className="flex items-start justify-between gap-4">
                  <div className="flex-1">
                    <CardTitle className="text-base flex items-center gap-2">
                      <PaperclipHorizontal size={18} />
                      {item.title}
                    </CardTitle>
                    <CardDescription className="mt-1">
                      {item.description || 'No description provided'}
                    </CardDescription>
                  </div>
                  <div className="flex flex-col gap-2 flex-shrink-0">
                    <Badge variant="outline">
                      {getSectionTitle(item.sectionId)}
                    </Badge>
                    {getIntegrityBadge(item.integrityStatus)}
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="flex items-center gap-4 text-xs text-muted-foreground">
                    {item.fileName && <span>File: {item.fileName}</span>}
                    {item.sourceUrl && <span>URL: {item.sourceUrl}</span>}
                    <span>Uploaded: {formatDateTime(item.uploadedAt)}</span>
                    <span>{item.linkedDataPoints.length} linked data points</span>
                  </div>
                  {item.fileSize && (
                    <div className="flex items-center gap-4 text-xs text-muted-foreground">
                      <span>Size: {formatFileSize(item.fileSize)}</span>
                      {item.contentType && <span>Type: {item.contentType}</span>}
                      {item.checksum && (
                        <span className="font-mono" title={item.checksum}>
                          Checksum: {item.checksum.substring(0, 16)}...
                        </span>
                      )}
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <PaperclipHorizontal size={48} weight="duotone" className="text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No Evidence Items</h3>
            <p className="text-sm text-muted-foreground text-center max-w-md">
              Evidence files and references will appear here as they are added to sections.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}