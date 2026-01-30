import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useKV } from '@github/spark/hooks'
import {
  TrendUp,
  TrendDown,
  ChartLine,
  Target,
  WarningCircle,
  LockKey,
  Download,
  Funnel,
  CheckCircle,
  ClockCounterClockwise
} from '@phosphor-icons/react'
import type {
  User,
  ReportingPeriod,
  SectionSummary,
  OrganizationalUnit,
  ProgressTrendsResponse,
  OutstandingActionsResponse
} from '@/lib/types'
import { getProgressTrends, getOutstandingActions, getProgressDashboardExportUrl, type GetProgressTrendsParams, type GetOutstandingActionsParams, type ExportProgressDashboardParams } from '@/lib/api'

interface ProgressDashboardProps {
  currentUser: User
}

export default function ProgressDashboard({ currentUser }: ProgressDashboardProps) {
  const [periods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  const [sections] = useKV<SectionSummary[]>('section-summaries', [])
  const [organizationalUnits] = useKV<OrganizationalUnit[]>('organizational-units', [])

  const [trends, setTrends] = useState<ProgressTrendsResponse | null>(null)
  const [actions, setActions] = useState<OutstandingActionsResponse | null>(null)
  const [isLoadingTrends, setIsLoadingTrends] = useState(false)
  const [isLoadingActions, setIsLoadingActions] = useState(false)

  // Filters
  const [selectedPeriods, setSelectedPeriods] = useState<string[]>([])
  const [selectedCategory, setSelectedCategory] = useState<string>('all')
  const [selectedOrgUnit, setSelectedOrgUnit] = useState<string>('all')
  const [selectedSection, setSelectedSection] = useState<string>('all')
  const [selectedOwner, setSelectedOwner] = useState<string>('all')
  const [selectedPriority, setSelectedPriority] = useState<string>('all')

  // Get unique owners from sections
  const owners = Array.from(new Set(sections.map(s => ({ id: s.ownerId, name: s.ownerName }))))
    .filter((owner, index, self) => self.findIndex(o => o.id === owner.id) === index)

  // Fetch trends when filters change
  useEffect(() => {
    const fetchTrends = async () => {
      setIsLoadingTrends(true)
      try {
        const params: GetProgressTrendsParams = {}
        if (selectedPeriods.length > 0) params.periodIds = selectedPeriods
        if (selectedCategory !== 'all') params.category = selectedCategory
        if (selectedOrgUnit !== 'all') params.organizationalUnitId = selectedOrgUnit
        if (selectedSection !== 'all') params.sectionId = selectedSection
        if (selectedOwner !== 'all') params.ownerId = selectedOwner

        const data = await getProgressTrends(params)
        setTrends(data)
      } catch (error) {
        console.error('Failed to fetch progress trends:', error)
      } finally {
        setIsLoadingTrends(false)
      }
    }

    fetchTrends()
  }, [selectedPeriods, selectedCategory, selectedOrgUnit, selectedSection, selectedOwner])

  // Fetch actions when filters change
  useEffect(() => {
    const fetchActions = async () => {
      setIsLoadingActions(true)
      try {
        const params: GetOutstandingActionsParams = {}
        if (selectedPeriods.length > 0) params.periodIds = selectedPeriods
        if (selectedCategory !== 'all') params.category = selectedCategory
        if (selectedOrgUnit !== 'all') params.organizationalUnitId = selectedOrgUnit
        if (selectedSection !== 'all') params.sectionId = selectedSection
        if (selectedOwner !== 'all') params.ownerId = selectedOwner
        if (selectedPriority !== 'all') params.priority = selectedPriority

        const data = await getOutstandingActions(params)
        setActions(data)
      } catch (error) {
        console.error('Failed to fetch outstanding actions:', error)
      } finally {
        setIsLoadingActions(false)
      }
    }

    fetchActions()
  }, [selectedPeriods, selectedCategory, selectedOrgUnit, selectedSection, selectedOwner, selectedPriority])

  const handleExport = (format: 'csv' | 'pdf') => {
    const params: ExportProgressDashboardParams = { format }
    if (selectedPeriods.length > 0) params.periodIds = selectedPeriods
    if (selectedCategory !== 'all') params.category = selectedCategory
    if (selectedOrgUnit !== 'all') params.organizationalUnitId = selectedOrgUnit
    if (selectedSection !== 'all') params.sectionId = selectedSection
    if (selectedOwner !== 'all') params.ownerId = selectedOwner

    const url = getProgressDashboardExportUrl(params)
    window.open(url, '_blank')
  }

  const togglePeriodSelection = (periodId: string) => {
    if (selectedPeriods.includes(periodId)) {
      setSelectedPeriods(selectedPeriods.filter(id => id !== periodId))
    } else {
      setSelectedPeriods([...selectedPeriods, periodId])
    }
  }

  const selectAllPeriods = () => {
    if (selectedPeriods.length === periods.length) {
      setSelectedPeriods([])
    } else {
      setSelectedPeriods(periods.map(p => p.id))
    }
  }

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'high':
        return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200'
      case 'medium':
        return 'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200'
      case 'low':
        return 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200'
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-foreground">
            Progress Dashboard
          </h2>
          <p className="text-sm text-muted-foreground mt-1">
            Track completeness and maturity trends across reporting periods
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => handleExport('csv')}>
            <Download size={16} className="mr-2" />
            Export CSV
          </Button>
          <Button variant="outline" onClick={() => handleExport('pdf')}>
            <Download size={16} className="mr-2" />
            Export PDF
          </Button>
        </div>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <Funnel size={18} />
            Filters
          </CardTitle>
          <CardDescription>
            Filter data by period, category, organizational unit, section, and owner
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Period Selection */}
          <div>
            <label className="text-sm font-medium text-foreground mb-2 block">
              Reporting Periods
            </label>
            <div className="flex flex-wrap gap-2">
              <Button
                variant={selectedPeriods.length === periods.length ? 'default' : 'outline'}
                size="sm"
                onClick={selectAllPeriods}
              >
                {selectedPeriods.length === periods.length ? 'Deselect All' : 'Select All'}
              </Button>
              {periods.map(period => (
                <Button
                  key={period.id}
                  variant={selectedPeriods.includes(period.id) ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => togglePeriodSelection(period.id)}
                  className="gap-1"
                >
                  {period.name}
                  {period.status === 'closed' && <LockKey size={14} />}
                </Button>
              ))}
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
            <div>
              <label className="text-sm font-medium text-foreground mb-2 block">
                Category
              </label>
              <Select value={selectedCategory} onValueChange={setSelectedCategory}>
                <SelectTrigger>
                  <SelectValue placeholder="All Categories" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Categories</SelectItem>
                  <SelectItem value="environmental">Environmental</SelectItem>
                  <SelectItem value="social">Social</SelectItem>
                  <SelectItem value="governance">Governance</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {organizationalUnits && organizationalUnits.length > 0 && (
              <div>
                <label className="text-sm font-medium text-foreground mb-2 block">
                  Organizational Unit
                </label>
                <Select value={selectedOrgUnit} onValueChange={setSelectedOrgUnit}>
                  <SelectTrigger>
                    <SelectValue placeholder="All Units" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Units</SelectItem>
                    {organizationalUnits.map(unit => (
                      <SelectItem key={unit.id} value={unit.id}>{unit.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            <div>
              <label className="text-sm font-medium text-foreground mb-2 block">
                Section
              </label>
              <Select value={selectedSection} onValueChange={setSelectedSection}>
                <SelectTrigger>
                  <SelectValue placeholder="All Sections" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Sections</SelectItem>
                  {sections.map(section => (
                    <SelectItem key={section.id} value={section.id}>{section.title}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div>
              <label className="text-sm font-medium text-foreground mb-2 block">
                Owner
              </label>
              <Select value={selectedOwner} onValueChange={setSelectedOwner}>
                <SelectTrigger>
                  <SelectValue placeholder="All Owners" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Owners</SelectItem>
                  {owners.map(owner => (
                    <SelectItem key={owner.id} value={owner.id}>{owner.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div>
              <label className="text-sm font-medium text-foreground mb-2 block">
                Priority
              </label>
              <Select value={selectedPriority} onValueChange={setSelectedPriority}>
                <SelectTrigger>
                  <SelectValue placeholder="All Priorities" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Priorities</SelectItem>
                  <SelectItem value="high">High</SelectItem>
                  <SelectItem value="medium">Medium</SelectItem>
                  <SelectItem value="low">Low</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Summary Cards */}
      {trends && (
        <div className="grid gap-4 md:grid-cols-4">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <ChartLine size={16} />
                Total Periods
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex items-baseline gap-2">
                <span className="text-3xl font-semibold">{trends.summary.totalPeriods}</span>
                <span className="text-sm text-muted-foreground">
                  ({trends.summary.lockedPeriods} locked)
                </span>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <Target size={16} />
                Latest Completeness
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex items-baseline gap-2">
                <span className="text-3xl font-semibold">
                  {trends.summary.latestCompletenessPercentage?.toFixed(1) ?? 'N/A'}%
                </span>
                {trends.summary.completenessChange !== undefined && (
                  <div className="flex items-center gap-1">
                    {trends.summary.completenessChange > 0 ? (
                      <TrendUp size={16} className="text-green-600" />
                    ) : trends.summary.completenessChange < 0 ? (
                      <TrendDown size={16} className="text-red-600" />
                    ) : null}
                    <span className={`text-sm ${
                      trends.summary.completenessChange > 0 ? 'text-green-600' :
                      trends.summary.completenessChange < 0 ? 'text-red-600' :
                      'text-muted-foreground'
                    }`}>
                      {trends.summary.completenessChange > 0 ? '+' : ''}
                      {trends.summary.completenessChange.toFixed(1)}%
                    </span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <CheckCircle size={16} />
                Latest Maturity
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex items-baseline gap-2">
                <span className="text-3xl font-semibold">
                  {trends.summary.latestMaturityScore?.toFixed(1) ?? 'N/A'}
                </span>
                {trends.summary.maturityChange !== undefined && (
                  <div className="flex items-center gap-1">
                    {trends.summary.maturityChange > 0 ? (
                      <TrendUp size={16} className="text-green-600" />
                    ) : trends.summary.maturityChange < 0 ? (
                      <TrendDown size={16} className="text-red-600" />
                    ) : null}
                    <span className={`text-sm ${
                      trends.summary.maturityChange > 0 ? 'text-green-600' :
                      trends.summary.maturityChange < 0 ? 'text-red-600' :
                      'text-muted-foreground'
                    }`}>
                      {trends.summary.maturityChange > 0 ? '+' : ''}
                      {trends.summary.maturityChange.toFixed(1)}
                    </span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <WarningCircle size={16} />
                Outstanding Actions
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-semibold">
                {actions?.summary.totalActions ?? 0}
              </div>
              {actions && (
                <p className="text-xs text-muted-foreground mt-1">
                  {actions.summary.highPriority} high priority
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {/* Trends Table */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <ClockCounterClockwise size={20} weight="duotone" />
            Period Trends
          </CardTitle>
          <CardDescription>
            Completeness and maturity metrics across selected periods
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoadingTrends ? (
            <div className="text-center py-8 text-muted-foreground">Loading trends...</div>
          ) : trends && trends.periods.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b text-sm text-muted-foreground">
                    <th className="text-left py-3 px-2">Period</th>
                    <th className="text-center py-3 px-2">Status</th>
                    <th className="text-right py-3 px-2">Completeness</th>
                    <th className="text-right py-3 px-2">Data Points</th>
                    <th className="text-right py-3 px-2">Maturity Score</th>
                    <th className="text-left py-3 px-2">Maturity Level</th>
                    <th className="text-right py-3 px-2">Open Gaps</th>
                    <th className="text-right py-3 px-2">Blocked</th>
                  </tr>
                </thead>
                <tbody>
                  {trends.periods.map((period, idx) => (
                    <tr key={period.periodId} className="border-b hover:bg-accent/50">
                      <td className="py-3 px-2">
                        <div className="flex items-center gap-2">
                          <span className="font-medium">{period.periodName}</span>
                          {period.isLocked && <LockKey size={14} className="text-muted-foreground" />}
                        </div>
                        <div className="text-xs text-muted-foreground">
                          {period.startDate} - {period.endDate}
                        </div>
                      </td>
                      <td className="py-3 px-2 text-center">
                        <Badge variant={period.status === 'active' ? 'default' : 'secondary'}>
                          {period.status}
                        </Badge>
                      </td>
                      <td className="py-3 px-2 text-right">
                        <div className="font-semibold">{period.completenessPercentage.toFixed(1)}%</div>
                        {idx > 0 && (
                          <div className="text-xs">
                            {period.completenessPercentage - trends.periods[idx - 1].completenessPercentage > 0 ? (
                              <span className="text-green-600">
                                +{(period.completenessPercentage - trends.periods[idx - 1].completenessPercentage).toFixed(1)}%
                              </span>
                            ) : period.completenessPercentage - trends.periods[idx - 1].completenessPercentage < 0 ? (
                              <span className="text-red-600">
                                {(period.completenessPercentage - trends.periods[idx - 1].completenessPercentage).toFixed(1)}%
                              </span>
                            ) : null}
                          </div>
                        )}
                      </td>
                      <td className="py-3 px-2 text-right">
                        <span className="font-mono text-sm">
                          {period.completeDataPoints}/{period.totalDataPoints}
                        </span>
                      </td>
                      <td className="py-3 px-2 text-right">
                        <div className="font-semibold">
                          {period.maturityScore?.toFixed(1) ?? 'N/A'}
                        </div>
                        {idx > 0 && period.maturityScore && trends.periods[idx - 1].maturityScore && (
                          <div className="text-xs">
                            {period.maturityScore - trends.periods[idx - 1].maturityScore! > 0 ? (
                              <span className="text-green-600">
                                +{(period.maturityScore - trends.periods[idx - 1].maturityScore!).toFixed(1)}
                              </span>
                            ) : period.maturityScore - trends.periods[idx - 1].maturityScore! < 0 ? (
                              <span className="text-red-600">
                                {(period.maturityScore - trends.periods[idx - 1].maturityScore!).toFixed(1)}
                              </span>
                            ) : null}
                          </div>
                        )}
                      </td>
                      <td className="py-3 px-2">
                        {period.maturityLevel && (
                          <Badge variant="outline">{period.maturityLevel}</Badge>
                        )}
                      </td>
                      <td className="py-3 px-2 text-right">
                        <div className="font-semibold">{period.openGaps}</div>
                        {period.highRiskGaps > 0 && (
                          <div className="text-xs text-red-600">
                            {period.highRiskGaps} high-risk
                          </div>
                        )}
                      </td>
                      <td className="py-3 px-2 text-right">
                        <span className={period.blockedDataPoints > 0 ? 'text-red-600 font-semibold' : ''}>
                          {period.blockedDataPoints}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="text-center py-8 text-muted-foreground">
              No trend data available. Select periods to view trends.
            </div>
          )}
        </CardContent>
      </Card>

      {/* Outstanding Actions */}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <WarningCircle size={20} weight="duotone" />
            Outstanding Actions
          </CardTitle>
          <CardDescription>
            Actions requiring attention across selected periods
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoadingActions ? (
            <div className="text-center py-8 text-muted-foreground">Loading actions...</div>
          ) : actions && actions.actions.length > 0 ? (
            <div className="space-y-3">
              {actions.actions.map((action, idx) => (
                <div key={`${action.actionType}-${action.id}-${idx}`} className="border rounded-lg p-4">
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <h4 className="font-medium text-foreground">{action.title}</h4>
                        <Badge className={getPriorityColor(action.priority)}>
                          {action.priority}
                        </Badge>
                        <Badge variant="outline">
                          {action.actionType}
                        </Badge>
                        {action.periodIsLocked && (
                          <Badge variant="secondary">
                            <LockKey size={12} className="mr-1" />
                            Locked
                          </Badge>
                        )}
                      </div>
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-2 text-xs mt-2">
                        <div>
                          <span className="text-muted-foreground">Period:</span>
                          <p className="font-medium text-foreground">{action.periodName}</p>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Section:</span>
                          <p className="font-medium text-foreground">{action.sectionTitle}</p>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Category:</span>
                          <p className="font-medium text-foreground capitalize">{action.category}</p>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Owner:</span>
                          <p className="font-medium text-foreground">{action.ownerName || 'Not assigned'}</p>
                        </div>
                      </div>
                      {action.dueDate && (
                        <div className="text-xs text-muted-foreground mt-2">
                          Due: {action.dueDate}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="text-center py-8 text-muted-foreground">
              No outstanding actions for the selected filters.
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
