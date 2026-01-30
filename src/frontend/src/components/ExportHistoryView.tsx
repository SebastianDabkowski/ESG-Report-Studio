import { formatDistanceToNow } from 'date-fns'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { 
  FileDown, 
  FileText, 
  Download, 
  Clock, 
  User, 
  Hash,
  FileType
} from 'lucide-react'
import { useExportHistory } from '@/hooks/useGenerationHistory'
import type { ExportHistoryEntry } from '@/lib/types'
import { formatDate } from '@/lib/helpers'

interface ExportHistoryViewProps {
  periodId: string
}

export default function ExportHistoryView({ periodId }: ExportHistoryViewProps) {
  const { data: history, isLoading, error } = useExportHistory(periodId)

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes'
    const k = 1024
    const sizes = ['Bytes', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return (bytes / Math.pow(k, i)).toFixed(2) + ' ' + sizes[i]
  }

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileDown className="h-5 w-5" />
            Export History
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-muted-foreground">
            Loading export history...
          </div>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileDown className="h-5 w-5" />
            Export History
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-destructive">
            Failed to load export history
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!history || history.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileDown className="h-5 w-5" />
            Export History
          </CardTitle>
          <CardDescription>
            Track all exports for this period
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-muted-foreground">
            No exports yet. Export a report to see history.
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <FileDown className="h-5 w-5" />
          Export History
        </CardTitle>
        <CardDescription>
          {history.length} export{history.length !== 1 ? 's' : ''} recorded
        </CardDescription>
      </CardHeader>
      <CardContent>
        <ScrollArea className="h-[600px] pr-4">
          <div className="space-y-4">
            {history.map((entry) => (
              <ExportHistoryCard key={entry.id} entry={entry} formatFileSize={formatFileSize} />
            ))}
          </div>
        </ScrollArea>
      </CardContent>
    </Card>
  )
}

interface ExportHistoryCardProps {
  entry: ExportHistoryEntry
  formatFileSize: (bytes: number) => string
}

function ExportHistoryCard({ entry, formatFileSize }: ExportHistoryCardProps) {
  const getFormatIcon = () => {
    if (entry.format === 'pdf') {
      return <FileText className="h-4 w-4 text-red-600" />
    }
    return <FileType className="h-4 w-4 text-blue-600" />
  }

  return (
    <div className="border rounded-lg p-4 space-y-3 hover:border-primary/50 transition-colors">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-1">
            {getFormatIcon()}
            <span className="font-medium">{entry.fileName}</span>
            <Badge variant="outline" className="uppercase">
              {entry.format}
            </Badge>
            {entry.variantName && (
              <Badge variant="secondary">{entry.variantName}</Badge>
            )}
          </div>
          <div className="text-sm text-muted-foreground">
            {formatFileSize(entry.fileSize)}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3 text-sm">
        <div className="flex items-center gap-2 text-muted-foreground">
          <Clock className="h-4 w-4" />
          <span>{formatDistanceToNow(new Date(entry.exportedAt), { addSuffix: true })}</span>
        </div>
        <div className="flex items-center gap-2 text-muted-foreground">
          <User className="h-4 w-4" />
          <span>{entry.exportedByName}</span>
        </div>
        <div className="flex items-center gap-2 text-muted-foreground">
          <Download className="h-4 w-4" />
          <span>{entry.downloadCount} download{entry.downloadCount !== 1 ? 's' : ''}</span>
        </div>
        <div className="flex items-center gap-2 text-muted-foreground">
          <Hash className="h-4 w-4" />
          <span className="font-mono text-xs truncate" title={entry.fileChecksum}>
            {entry.fileChecksum.slice(0, 16)}...
          </span>
        </div>
      </div>

      <div className="flex flex-wrap gap-2 pt-2 border-t text-xs text-muted-foreground">
        {entry.includedTitlePage && <Badge variant="outline" className="text-xs">Title Page</Badge>}
        {entry.includedTableOfContents && <Badge variant="outline" className="text-xs">Table of Contents</Badge>}
        {entry.includedAttachments && <Badge variant="outline" className="text-xs">Attachments</Badge>}
      </div>

      {entry.lastDownloadedAt && (
        <div className="text-xs text-muted-foreground">
          Last downloaded: {formatDate(entry.lastDownloadedAt)}
        </div>
      )}
    </div>
  )
}
