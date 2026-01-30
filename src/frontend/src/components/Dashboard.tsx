import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useKV } from '@github/spark/hooks'
import { CheckCircle, WarningCircle, FileText, PaperclipHorizontal, Lightbulb, Target, ChartBar, Circle, Eye, FilePdf } from '@phosphor-icons/react'
import type { User, ReportingPeriod, SectionSummary, Gap, CompletenessStats, OrganizationalUnit } from '@/lib/types'
import { getStatusColor, getStatusBorderColor, getProgressStatusColor, getProgressStatusLabel, formatDate } from '@/lib/helpers'
import { getCompletenessStats, exportReportPdf } from '@/lib/api'
import ReportPreviewDialog from './ReportPreviewDialog'

interface DashboardProps {
  currentUser: User
}

export default function Dashboard({ currentUser }: DashboardProps) {
  const [periods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  const [sections] = useKV<SectionSummary[]>('section-summaries', [])
  const [gaps] = useKV<Gap[]>('gaps', [])
  const [organizationalUnits] = useKV<OrganizationalUnit[]>('organizational-units', [])

  const [completenessStats, setCompletenessStats] = useState<CompletenessStats | null>(null)
  const [selectedCategory, setSelectedCategory] = useState<string>('all')
  const [selectedOrgUnit, setSelectedOrgUnit] = useState<string>('all')
  const [isLoadingStats, setIsLoadingStats] = useState(false)
  const [showPreview, setShowPreview] = useState(false)
  const [isExportingPdf, setIsExportingPdf] = useState(false)

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

  // Export PDF handler
  const handleExportPdf = async () => {
    if (!activePeriod) return

    setIsExportingPdf(true)
    try {
      await exportReportPdf(activePeriod.id, {
        generatedBy: currentUser.id,
        includeTitlePage: true,
        includeTableOfContents: true,
        includePageNumbers: true
      })
    } catch (error) {
      console.error('Failed to export PDF:', error)
      alert('Failed to export PDF. Please try again.')
    } finally {
      setIsExportingPdf(false)
    }
  }

  // Fetch completeness stats when filters change
  useEffect(() => {
    const fetchStats = async () => {
      if (!activePeriod) {
        setCompletenessStats(null)
        return
      }

      setIsLoadingStats(true)
      try {
        const params: { periodId: string; category?: string; organizationalUnitId?: string } = { 
          periodId: activePeriod.id 
        }
        if (selectedCategory !== 'all') params.category = selectedCategory
        if (selectedOrgUnit !== 'all') params.organizationalUnitId = selectedOrgUnit
        
        const stats = await getCompletenessStats(params)
        setCompletenessStats(stats)
      } catch (error) {
        console.error('Failed to fetch completeness stats:', error)
      } finally {
        setIsLoadingStats(false)
      }
    }

    fetchStats()
  }, [activePeriod, selectedCategory, selectedOrgUnit])

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
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle className="text-lg">Active Reporting Period</CardTitle>
                  <CardDescription>
                    {activePeriod.name} • {formatDate(activePeriod.startDate)} - {formatDate(activePeriod.endDate)}
                  </CardDescription>
                </div>
                <div className="flex gap-2">
                  <Button 
                    onClick={() => setShowPreview(true)}
                    variant="outline"
                    size="sm"
                    className="gap-2"
                  >
                    <Eye weight="regular" className="h-4 w-4" />
                    Preview Report
                  </Button>
                  <Button 
                    onClick={handleExportPdf}
                    disabled={isExportingPdf}
                    variant="default"
                    size="sm"
                    className="gap-2"
                  >
                    <FilePdf weight="regular" className="h-4 w-4" />
                    {isExportingPdf ? 'Exporting...' : 'Export PDF'}
                  </Button>
                </div>
              </div>
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

          {/* Completeness Dashboard */}
          <Card>
            <CardHeader>
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle className="text-lg flex items-center gap-2">
                    <ChartBar size={20} weight="duotone" className="text-primary" />
                    Completeness Overview
                  </CardTitle>
                  <CardDescription>Track data completeness by category and organizational unit</CardDescription>
                </div>
                <div className="flex gap-2">
                  <Select value={selectedCategory} onValueChange={setSelectedCategory}>
                    <SelectTrigger className="w-40">
                      <SelectValue placeholder="All Categories" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All Categories</SelectItem>
                      <SelectItem value="environmental">Environmental</SelectItem>
                      <SelectItem value="social">Social</SelectItem>
                      <SelectItem value="governance">Governance</SelectItem>
                    </SelectContent>
                  </Select>
                  
                  {organizationalUnits && organizationalUnits.length > 0 && (
                    <Select value={selectedOrgUnit} onValueChange={setSelectedOrgUnit}>
                      <SelectTrigger className="w-40">
                        <SelectValue placeholder="All Units" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All Units</SelectItem>
                        {organizationalUnits.map(unit => (
                          <SelectItem key={unit.id} value={unit.id}>{unit.name}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                </div>
              </div>
            </CardHeader>
            <CardContent>
              {isLoadingStats ? (
                <div className="text-center py-8 text-muted-foreground">Loading completeness data...</div>
              ) : completenessStats ? (
                <div className="space-y-6">
                  {/* Overall Stats */}
                  <div className="grid gap-4 md:grid-cols-4">
                    <div className="bg-red-50 dark:bg-red-950/20 rounded-lg p-4 border border-red-200 dark:border-red-800">
                      <div className="text-xs font-medium text-red-700 dark:text-red-400 mb-1">Missing</div>
                      <div className="text-2xl font-bold text-red-900 dark:text-red-100">{completenessStats.overall.missingCount}</div>
                      <div className="text-xs text-red-600 dark:text-red-500 mt-1">
                        {completenessStats.overall.totalCount > 0 
                          ? `${Math.round((completenessStats.overall.missingCount / completenessStats.overall.totalCount) * 100)}%`
                          : '0%'}
                      </div>
                    </div>
                    <div className="bg-amber-50 dark:bg-amber-950/20 rounded-lg p-4 border border-amber-200 dark:border-amber-800">
                      <div className="text-xs font-medium text-amber-700 dark:text-amber-400 mb-1">Incomplete</div>
                      <div className="text-2xl font-bold text-amber-900 dark:text-amber-100">{completenessStats.overall.incompleteCount}</div>
                      <div className="text-xs text-amber-600 dark:text-amber-500 mt-1">
                        {completenessStats.overall.totalCount > 0 
                          ? `${Math.round((completenessStats.overall.incompleteCount / completenessStats.overall.totalCount) * 100)}%`
                          : '0%'}
                      </div>
                    </div>
                    <div className="bg-green-50 dark:bg-green-950/20 rounded-lg p-4 border border-green-200 dark:border-green-800">
                      <div className="text-xs font-medium text-green-700 dark:text-green-400 mb-1">Complete</div>
                      <div className="text-2xl font-bold text-green-900 dark:text-green-100">{completenessStats.overall.completeCount}</div>
                      <div className="text-xs text-green-600 dark:text-green-500 mt-1">
                        {completenessStats.overall.completePercentage.toFixed(1)}%
                      </div>
                    </div>
                    <div className="bg-gray-50 dark:bg-gray-950/20 rounded-lg p-4 border border-gray-200 dark:border-gray-800">
                      <div className="text-xs font-medium text-gray-700 dark:text-gray-400 mb-1">Not Applicable</div>
                      <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{completenessStats.overall.notApplicableCount}</div>
                      <div className="text-xs text-gray-600 dark:text-gray-500 mt-1">
                        {completenessStats.overall.totalCount > 0 
                          ? `${Math.round((completenessStats.overall.notApplicableCount / completenessStats.overall.totalCount) * 100)}%`
                          : '0%'}
                      </div>
                    </div>
                  </div>

                  {/* By Category */}
                  {selectedOrgUnit === 'all' && completenessStats.byCategory.length > 0 && (
                    <div>
                      <h4 className="font-medium text-sm mb-3">By E/S/G Category</h4>
                      <div className="space-y-3">
                        {completenessStats.byCategory.map(cat => (
                          <div key={cat.id} className="border rounded-lg p-4">
                            <div className="flex items-center justify-between mb-2">
                              <div className="flex items-center gap-2">
                                <Circle 
                                  size={12} 
                                  weight="fill" 
                                  className={
                                    cat.id === 'environmental' ? 'text-green-500' :
                                    cat.id === 'social' ? 'text-blue-500' :
                                    'text-purple-500'
                                  }
                                />
                                <span className="font-medium text-sm">{cat.name}</span>
                              </div>
                              <div className="text-sm font-semibold">{cat.completePercentage.toFixed(1)}% Complete</div>
                            </div>
                            <div className="grid grid-cols-4 gap-2 text-xs">
                              <div className="text-center">
                                <div className="text-red-600 dark:text-red-400 font-semibold">{cat.missingCount}</div>
                                <div className="text-muted-foreground">Missing</div>
                              </div>
                              <div className="text-center">
                                <div className="text-amber-600 dark:text-amber-400 font-semibold">{cat.incompleteCount}</div>
                                <div className="text-muted-foreground">Incomplete</div>
                              </div>
                              <div className="text-center">
                                <div className="text-green-600 dark:text-green-400 font-semibold">{cat.completeCount}</div>
                                <div className="text-muted-foreground">Complete</div>
                              </div>
                              <div className="text-center">
                                <div className="text-gray-600 dark:text-gray-400 font-semibold">{cat.notApplicableCount}</div>
                                <div className="text-muted-foreground">N/A</div>
                              </div>
                            </div>
                            {cat.totalCount > 0 && (
                              <Progress value={cat.completePercentage} className="h-2 mt-3" />
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  {/* By Organizational Unit */}
                  {selectedCategory === 'all' && completenessStats.byOrganizationalUnit.length > 0 && (
                    <div>
                      <h4 className="font-medium text-sm mb-3">By Organizational Unit</h4>
                      <div className="space-y-3">
                        {completenessStats.byOrganizationalUnit.map(unit => (
                          <div key={unit.id} className="border rounded-lg p-4">
                            <div className="flex items-center justify-between mb-2">
                              <span className="font-medium text-sm">{unit.name}</span>
                              <div className="text-sm font-semibold">{unit.completePercentage.toFixed(1)}% Complete</div>
                            </div>
                            <div className="grid grid-cols-4 gap-2 text-xs">
                              <div className="text-center">
                                <div className="text-red-600 dark:text-red-400 font-semibold">{unit.missingCount}</div>
                                <div className="text-muted-foreground">Missing</div>
                              </div>
                              <div className="text-center">
                                <div className="text-amber-600 dark:text-amber-400 font-semibold">{unit.incompleteCount}</div>
                                <div className="text-muted-foreground">Incomplete</div>
                              </div>
                              <div className="text-center">
                                <div className="text-green-600 dark:text-green-400 font-semibold">{unit.completeCount}</div>
                                <div className="text-muted-foreground">Complete</div>
                              </div>
                              <div className="text-center">
                                <div className="text-gray-600 dark:text-gray-400 font-semibold">{unit.notApplicableCount}</div>
                                <div className="text-muted-foreground">N/A</div>
                              </div>
                            </div>
                            {unit.totalCount > 0 && (
                              <Progress value={unit.completePercentage} className="h-2 mt-3" />
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              ) : (
                <div className="text-center py-8 text-muted-foreground">No completeness data available</div>
              )}
            </CardContent>
          </Card>

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
                          <Badge className={getProgressStatusColor(section.progressStatus)} variant="secondary">
                            {getProgressStatusLabel(section.progressStatus)}
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

      {/* Report Preview Dialog */}
      {showPreview && activePeriod && (
        <ReportPreviewDialog
          period={activePeriod}
          currentUser={currentUser}
          onClose={() => setShowPreview(false)}
        />
      )}
    </div>
  )
}