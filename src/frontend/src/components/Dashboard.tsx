import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import { Badge } from '@/components/ui/badge'
import { useKV } from '@github/spark/hooks'
import { CheckCircle, WarningCircle, FileText, PaperclipHorizontal, Lightbulb, Target } from '@phosphor-icons/react'
import type { User, ReportingPeriod, SectionSummary, Gap } from '@/lib/types'
import { getStatusColor, getStatusBorderColor, formatDate } from '@/lib/helpers'

interface DashboardProps {
  currentUser: User
}

export default function Dashboard({ currentUser }: DashboardProps) {
  const [periods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  const [sections] = useKV<SectionSummary[]>('section-summaries', [])
  const [gaps] = useKV<Gap[]>('gaps', [])

  const activePeriod = (periods || []).find(p => p.status === 'active')
  const activeSections = (sections || []).filter(s => activePeriod && s.periodId === activePeriod.id)
  
  const approvedCount = activeSections.filter(s => s.status === 'approved').length
  const inReviewCount = activeSections.filter(s => s.status === 'in-review').length
  const draftCount = activeSections.filter(s => s.status === 'draft').length
  
  const totalDataPoints = activeSections.reduce((sum, s) => sum + s.dataPointCount, 0)
  const totalEvidence = activeSections.reduce((sum, s) => sum + s.evidenceCount, 0)
  const unresolvedGaps = (gaps || []).filter(g => !g.resolved).length
  
  const avgCompleteness = activeSections.length > 0
    ? Math.round(activeSections.reduce((sum, s) => sum + s.completenessPercentage, 0) / activeSections.length)
    : 0

  const criticalGaps = (gaps || []).filter(g => !g.resolved && g.impact === 'high')

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold tracking-tight text-foreground">
          Dashboard Overview
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          Monitor report readiness, track progress, and identify gaps
        </p>
      </div>

      {activePeriod ? (
        <>
          <Card className="border-l-4 border-l-accent">
            <CardHeader>
              <CardTitle className="text-lg">Active Reporting Period</CardTitle>
              <CardDescription>
                {activePeriod.name} • {formatDate(activePeriod.startDate)} - {formatDate(activePeriod.endDate)}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium">Overall Completeness</span>
                  <span className="text-sm font-mono font-semibold">{avgCompleteness}%</span>
                </div>
                <Progress value={avgCompleteness} className="h-3" />
              </div>
            </CardContent>
          </Card>

          <div className="grid gap-4 md:grid-cols-4">
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Total Sections
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-center gap-2">
                  <FileText size={24} weight="duotone" className="text-primary" />
                  <span className="text-3xl font-semibold">{activeSections.length}</span>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Data Points
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-center gap-2">
                  <Target size={24} weight="duotone" className="text-accent" />
                  <span className="text-3xl font-semibold">{totalDataPoints}</span>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Evidence Items
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-center gap-2">
                  <PaperclipHorizontal size={24} weight="duotone" className="text-success" />
                  <span className="text-3xl font-semibold">{totalEvidence}</span>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  Unresolved Gaps
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-center gap-2">
                  <WarningCircle size={24} weight="duotone" className="text-alert" />
                  <span className="text-3xl font-semibold">{unresolvedGaps}</span>
                </div>
              </CardContent>
            </Card>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Section Status</CardTitle>
                <CardDescription>Current status distribution across all sections</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <CheckCircle size={20} weight="fill" className="text-success" />
                    <span className="text-sm">Approved</span>
                  </div>
                  <span className="text-sm font-semibold font-mono">{approvedCount}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Lightbulb size={20} weight="fill" className="text-warning" />
                    <span className="text-sm">In Review</span>
                  </div>
                  <span className="text-sm font-semibold font-mono">{inReviewCount}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <FileText size={20} weight="fill" className="text-muted-foreground" />
                    <span className="text-sm">Draft</span>
                  </div>
                  <span className="text-sm font-semibold font-mono">{draftCount}</span>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Critical Gaps</CardTitle>
                <CardDescription>High-impact gaps requiring immediate attention</CardDescription>
              </CardHeader>
              <CardContent>
                {criticalGaps.length > 0 ? (
                  <div className="space-y-3">
                    {criticalGaps.slice(0, 3).map(gap => (
                      <div key={gap.id} className="flex items-start gap-2">
                        <WarningCircle size={16} weight="fill" className="text-alert mt-0.5 flex-shrink-0" />
                        <div className="flex-1 min-w-0">
                          <div className="text-sm font-medium truncate">{gap.title}</div>
                          <div className="text-xs text-muted-foreground">{gap.description}</div>
                        </div>
                      </div>
                    ))}
                    {criticalGaps.length > 3 && (
                      <div className="text-xs text-muted-foreground pt-2">
                        +{criticalGaps.length - 3} more critical gaps
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="text-sm text-muted-foreground">
                    No critical gaps identified
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Section Details</CardTitle>
              <CardDescription>Detailed view of all sections and their progress</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {activeSections.map(section => (
                  <div
                    key={section.id}
                    className={`border-l-4 ${getStatusBorderColor(section.status)} rounded-r-lg border border-border bg-card p-4`}
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <h4 className="font-medium text-sm">{section.title}</h4>
                          <Badge className={getStatusColor(section.status)} variant="secondary">
                            {section.status}
                          </Badge>
                        </div>
                        <p className="text-xs text-muted-foreground mb-3">{section.description}</p>
                        
                        <div className="flex items-center gap-4 text-xs text-muted-foreground">
                          <span>{section.dataPointCount} data points</span>
                          <span>{section.evidenceCount} evidence</span>
                          <span>{section.gapCount} gaps</span>
                          <span className="font-medium">Owner: {section.ownerName}</span>
                        </div>
                      </div>
                      
                      <div className="text-right flex-shrink-0">
                        <div className="text-sm font-semibold font-mono mb-1">
                          {section.completenessPercentage}%
                        </div>
                        <Progress value={section.completenessPercentage} className="h-2 w-24" />
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </>
      ) : (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <FileText size={48} weight="duotone" className="text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No Active Reporting Period</h3>
            <p className="text-sm text-muted-foreground text-center max-w-md">
              Create a new reporting period to begin collecting ESG data and building your report.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}