import { useState } from 'react'
import { formatDistanceToNow } from 'date-fns'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { 
  History, 
  FileText, 
  CheckCircle2, 
  Clock, 
  User, 
  Hash, 
  FileStack,
  GitCompare,
  Eye
} from 'lucide-react'
import { useGenerationHistory, useMarkGenerationFinal } from '@/hooks/useGenerationHistory'
import type { GenerationHistoryEntry, User as UserType } from '@/lib/types'
import { formatDate } from '@/lib/helpers'
import VersionComparisonDialog from './VersionComparisonDialog'

interface GenerationHistoryViewProps {
  periodId: string
  currentUser: UserType
  onViewGeneration?: (generationId: string) => void
}

export default function GenerationHistoryView({ 
  periodId, 
  currentUser,
  onViewGeneration 
}: GenerationHistoryViewProps) {
  const { data: history, isLoading, error } = useGenerationHistory(periodId)
  const markFinal = useMarkGenerationFinal()
  const [selectedForComparison, setSelectedForComparison] = useState<string[]>([])
  const [showComparison, setShowComparison] = useState(false)

  const handleMarkFinal = async (generationId: string) => {
    if (!confirm('Mark this generation as final? This action cannot be undone.')) {
      return
    }

    try {
      await markFinal.mutateAsync({
        generationId,
        payload: {
          generationId,
          userId: currentUser.id,
          userName: currentUser.name,
          note: 'Marked as final version'
        }
      })
    } catch (err) {
      console.error('Failed to mark generation as final:', err)
      alert('Failed to mark generation as final')
    }
  }

  const handleToggleComparison = (generationId: string) => {
    setSelectedForComparison(prev => {
      if (prev.includes(generationId)) {
        return prev.filter(id => id !== generationId)
      }
      if (prev.length >= 2) {
        // Replace the oldest selection
        return [prev[1], generationId]
      }
      return [...prev, generationId]
    })
  }

  const handleCompare = () => {
    if (selectedForComparison.length === 2) {
      setShowComparison(true)
    }
  }

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <History className="h-5 w-5" />
            Generation History
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-muted-foreground">
            Loading history...
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
            <History className="h-5 w-5" />
            Generation History
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-destructive">
            Failed to load generation history
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
            <History className="h-5 w-5" />
            Generation History
          </CardTitle>
          <CardDescription>
            Track all report generations for this period
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-muted-foreground">
            No generations yet. Generate a report to see history.
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <>
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <History className="h-5 w-5" />
                Generation History
              </CardTitle>
              <CardDescription>
                {history.length} generation{history.length !== 1 ? 's' : ''} recorded
              </CardDescription>
            </div>
            {selectedForComparison.length === 2 && (
              <Button onClick={handleCompare} size="sm">
                <GitCompare className="h-4 w-4 mr-2" />
                Compare Selected
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          <ScrollArea className="h-[600px] pr-4">
            <div className="space-y-4">
              {history.map((entry) => (
                <GenerationHistoryCard
                  key={entry.id}
                  entry={entry}
                  currentUser={currentUser}
                  isSelected={selectedForComparison.includes(entry.id)}
                  onToggleCompare={() => handleToggleComparison(entry.id)}
                  onMarkFinal={() => handleMarkFinal(entry.id)}
                  onView={() => onViewGeneration?.(entry.id)}
                />
              ))}
            </div>
          </ScrollArea>
        </CardContent>
      </Card>

      {showComparison && selectedForComparison.length === 2 && (
        <VersionComparisonDialog
          generation1Id={selectedForComparison[0]}
          generation2Id={selectedForComparison[1]}
          currentUser={currentUser}
          onClose={() => setShowComparison(false)}
        />
      )}
    </>
  )
}

interface GenerationHistoryCardProps {
  entry: GenerationHistoryEntry
  currentUser: UserType
  isSelected: boolean
  onToggleCompare: () => void
  onMarkFinal: () => void
  onView?: () => void
}

function GenerationHistoryCard({
  entry,
  currentUser,
  isSelected,
  onToggleCompare,
  onMarkFinal,
  onView
}: GenerationHistoryCardProps) {
  return (
    <div 
      className={`border rounded-lg p-4 space-y-3 transition-colors ${
        isSelected ? 'border-primary bg-primary/5' : 'border-border hover:border-primary/50'
      }`}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-1">
            <FileText className="h-4 w-4 text-muted-foreground" />
            <span className="font-medium">
              Generation #{entry.id.slice(0, 8)}
            </span>
            {entry.status === 'final' && (
              <Badge variant="default" className="bg-green-600">
                <CheckCircle2 className="h-3 w-3 mr-1" />
                Final
              </Badge>
            )}
            {entry.status === 'draft' && (
              <Badge variant="outline">Draft</Badge>
            )}
            {entry.variantName && (
              <Badge variant="secondary">{entry.variantName}</Badge>
            )}
          </div>
          {entry.generationNote && (
            <p className="text-sm text-muted-foreground mb-2">
              {entry.generationNote}
            </p>
          )}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3 text-sm">
        <div className="flex items-center gap-2 text-muted-foreground">
          <Clock className="h-4 w-4" />
          <span>{formatDistanceToNow(new Date(entry.generatedAt), { addSuffix: true })}</span>
        </div>
        <div className="flex items-center gap-2 text-muted-foreground">
          <User className="h-4 w-4" />
          <span>{entry.generatedByName}</span>
        </div>
        <div className="flex items-center gap-2 text-muted-foreground">
          <FileStack className="h-4 w-4" />
          <span>
            {entry.sectionCount} sections, {entry.dataPointCount} data points
          </span>
        </div>
        <div className="flex items-center gap-2 text-muted-foreground">
          <Hash className="h-4 w-4" />
          <span className="font-mono text-xs truncate" title={entry.checksum}>
            {entry.checksum.slice(0, 16)}...
          </span>
        </div>
      </div>

      {entry.markedFinalAt && (
        <div className="text-xs text-muted-foreground border-t pt-2">
          Marked final by {entry.markedFinalByName} on {formatDate(entry.markedFinalAt)}
        </div>
      )}

      <div className="flex gap-2 pt-2 border-t">
        {onView && (
          <Button variant="outline" size="sm" onClick={onView}>
            <Eye className="h-4 w-4 mr-2" />
            View
          </Button>
        )}
        <Button
          variant={isSelected ? "default" : "outline"}
          size="sm"
          onClick={onToggleCompare}
        >
          <GitCompare className="h-4 w-4 mr-2" />
          {isSelected ? 'Selected' : 'Compare'}
        </Button>
        {entry.status === 'draft' && (
          <Button variant="outline" size="sm" onClick={onMarkFinal}>
            <CheckCircle2 className="h-4 w-4 mr-2" />
            Mark Final
          </Button>
        )}
      </div>
    </div>
  )
}
