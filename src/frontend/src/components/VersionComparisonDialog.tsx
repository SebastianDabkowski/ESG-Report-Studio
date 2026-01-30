import { useEffect } from 'react'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { 
  GitCompare, 
  TrendingUp, 
  TrendingDown, 
  Plus, 
  Minus, 
  Edit3,
  AlertCircle,
  Loader2
} from 'lucide-react'
import { useCompareGenerations } from '@/hooks/useGenerationHistory'
import type { User, GenerationSectionDifference } from '@/lib/types'
import { formatDate } from '@/lib/helpers'

interface VersionComparisonDialogProps {
  generation1Id: string
  generation2Id: string
  currentUser: User
  onClose: () => void
}

export default function VersionComparisonDialog({
  generation1Id,
  generation2Id,
  currentUser,
  onClose
}: VersionComparisonDialogProps) {
  const compare = useCompareGenerations()

  useEffect(() => {
    // Trigger comparison when component mounts or IDs change
    compare.mutate({
      generation1Id,
      generation2Id,
      userId: currentUser.id
    })
    // We intentionally don't include compare.mutate in dependencies to avoid re-running on every mutation state change
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [generation1Id, generation2Id, currentUser.id])

  const comparison = compare.data

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent className="max-w-6xl max-h-[90vh] p-0">
        <DialogHeader className="p-6 pb-4">
          <DialogTitle className="flex items-center gap-2">
            <GitCompare className="h-5 w-5" />
            Version Comparison
          </DialogTitle>
          <DialogDescription>
            Compare two report generations to see what changed
          </DialogDescription>
        </DialogHeader>

        {compare.isPending && (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        )}

        {compare.isError && (
          <div className="p-6">
            <div className="flex items-center gap-2 text-destructive">
              <AlertCircle className="h-5 w-5" />
              <span>Failed to compare generations</span>
            </div>
          </div>
        )}

        {comparison && (
          <ScrollArea className="h-[calc(90vh-140px)]">
            <div className="p-6 space-y-6">
              {/* Header Info */}
              <div className="grid grid-cols-2 gap-4">
                <Card>
                  <CardHeader className="pb-3">
                    <CardTitle className="text-sm font-medium">Version 1</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-1 text-sm">
                    <div>Generated: {formatDate(comparison.generation1.generatedAt)}</div>
                    <div>By: {comparison.generation1.generatedByName}</div>
                    {comparison.generation1.status === 'final' && (
                      <Badge variant="default" className="bg-green-600 mt-1">Final</Badge>
                    )}
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="pb-3">
                    <CardTitle className="text-sm font-medium">Version 2</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-1 text-sm">
                    <div>Generated: {formatDate(comparison.generation2.generatedAt)}</div>
                    <div>By: {comparison.generation2.generatedByName}</div>
                    {comparison.generation2.status === 'final' && (
                      <Badge variant="default" className="bg-green-600 mt-1">Final</Badge>
                    )}
                  </CardContent>
                </Card>
              </div>

              {/* Summary Statistics */}
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Summary</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                    <div className="space-y-1">
                      <div className="text-sm text-muted-foreground">Total Sections</div>
                      <div className="text-2xl font-bold">{comparison.summary.totalSections}</div>
                    </div>
                    <div className="space-y-1">
                      <div className="text-sm text-green-600 flex items-center gap-1">
                        <Plus className="h-4 w-4" />
                        Added
                      </div>
                      <div className="text-2xl font-bold text-green-600">
                        {comparison.summary.sectionsAdded}
                      </div>
                    </div>
                    <div className="space-y-1">
                      <div className="text-sm text-red-600 flex items-center gap-1">
                        <Minus className="h-4 w-4" />
                        Removed
                      </div>
                      <div className="text-2xl font-bold text-red-600">
                        {comparison.summary.sectionsRemoved}
                      </div>
                    </div>
                    <div className="space-y-1">
                      <div className="text-sm text-amber-600 flex items-center gap-1">
                        <Edit3 className="h-4 w-4" />
                        Modified
                      </div>
                      <div className="text-2xl font-bold text-amber-600">
                        {comparison.summary.sectionsModified}
                      </div>
                    </div>
                  </div>

                  <Separator className="my-4" />

                  <div className="grid grid-cols-2 gap-4">
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">Data Points (V1)</span>
                      <span className="font-semibold">{comparison.summary.totalDataPoints1}</span>
                    </div>
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">Data Points (V2)</span>
                      <span className="font-semibold">{comparison.summary.totalDataPoints2}</span>
                    </div>
                  </div>

                  {comparison.summary.totalDataPoints2 !== comparison.summary.totalDataPoints1 && (
                    <div className="mt-2 flex items-center gap-2 text-sm">
                      {comparison.summary.totalDataPoints2 > comparison.summary.totalDataPoints1 ? (
                        <>
                          <TrendingUp className="h-4 w-4 text-green-600" />
                          <span className="text-green-600">
                            +{comparison.summary.totalDataPoints2 - comparison.summary.totalDataPoints1} data points
                          </span>
                        </>
                      ) : (
                        <>
                          <TrendingDown className="h-4 w-4 text-red-600" />
                          <span className="text-red-600">
                            {comparison.summary.totalDataPoints2 - comparison.summary.totalDataPoints1} data points
                          </span>
                        </>
                      )}
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Changed Data Sources */}
              {comparison.changedDataSources.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle className="text-base">Changed Data Sources</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="flex flex-wrap gap-2">
                      {comparison.changedDataSources.map((source, index) => (
                        <Badge key={index} variant="secondary">
                          {source}
                        </Badge>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Section Differences */}
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Section Details</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    {comparison.sectionDifferences
                      .filter(diff => diff.differenceType !== 'unchanged')
                      .map((diff) => (
                        <SectionDifferenceCard key={diff.sectionId} difference={diff} />
                      ))}
                    
                    {comparison.sectionDifferences.every(d => d.differenceType === 'unchanged') && (
                      <div className="text-center py-4 text-muted-foreground">
                        No differences detected between versions
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>
            </div>
          </ScrollArea>
        )}

        <div className="p-6 pt-0 flex justify-end">
          <Button onClick={onClose}>Close</Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}

interface SectionDifferenceCardProps {
  difference: GenerationSectionDifference
}

function SectionDifferenceCard({ difference }: SectionDifferenceCardProps) {
  const getIcon = () => {
    switch (difference.differenceType) {
      case 'added':
        return <Plus className="h-4 w-4 text-green-600" />
      case 'removed':
        return <Minus className="h-4 w-4 text-red-600" />
      case 'modified':
        return <Edit3 className="h-4 w-4 text-amber-600" />
      default:
        return null
    }
  }

  const getBadgeVariant = () => {
    switch (difference.differenceType) {
      case 'added':
        return 'default'
      case 'removed':
        return 'destructive'
      case 'modified':
        return 'secondary'
      default:
        return 'outline'
    }
  }

  return (
    <div className="border rounded-lg p-3 space-y-2">
      <div className="flex items-center gap-2">
        {getIcon()}
        <span className="font-medium">{difference.sectionTitle}</span>
        <Badge variant={getBadgeVariant()} className="ml-auto">
          {difference.differenceType}
        </Badge>
      </div>
      
      {difference.catalogCode && (
        <div className="text-xs text-muted-foreground">
          Code: {difference.catalogCode}
        </div>
      )}

      {difference.differenceType !== 'removed' && difference.differenceType !== 'added' && (
        <div className="text-sm text-muted-foreground">
          Data points: {difference.dataPointCount1} → {difference.dataPointCount2}
        </div>
      )}

      {difference.changes.length > 0 && (
        <ul className="text-sm space-y-1 mt-2">
          {difference.changes.map((change, index) => (
            <li key={index} className="text-muted-foreground">
              • {change}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
