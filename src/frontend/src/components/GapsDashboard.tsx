import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useKV } from '@github/spark/hooks'
import { 
  WarningCircle, 
  CheckCircle, 
  Target,
  Funnel,
  ArrowUp,
  ArrowDown
} from '@phosphor-icons/react'
import type { 
  User, 
  ReportingPeriod, 
  SectionSummary,
  GapDashboardResponse
} from '@/lib/types'
import { getGapsDashboard } from '@/lib/api'

interface GapsDashboardProps {
  currentUser: User
}

export default function GapsDashboard({ currentUser }: GapsDashboardProps) {
  const [periods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  const [sections] = useKV<SectionSummary[]>('section-summaries', [])
  
  const [dashboard, setDashboard] = useState<GapDashboardResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  
  // Filters
  const [selectedPeriod, setSelectedPeriod] = useState<string>('all')
  const [selectedStatus, setSelectedStatus] = useState<string>('all')
  const [selectedSection, setSelectedSection] = useState<string>('all')
  const [selectedDuePeriod, setSelectedDuePeriod] = useState<string>('all')
  
  // Sorting
  const [sortBy, setSortBy] = useState<'risk' | 'dueDate' | 'section'>('risk')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc')
  
  const activePeriod = (periods || []).find(p => p.status === 'active')
  const activePeriodId = activePeriod?.id
  const activeSections = (sections || []).filter(s => activePeriod && s.periodId === activePeriod.id)
  
  // Fetch dashboard data when filters or sorting change
  useEffect(() => {
    const fetchDashboard = async () => {
      setIsLoading(true)
      setError(null)
      
      try {
        const periodId = selectedPeriod === 'all' ? activePeriodId : selectedPeriod
        const data = await getGapsDashboard({
          periodId,
          status: selectedStatus === 'all' ? undefined : selectedStatus as 'open' | 'resolved',
          sectionId: selectedSection === 'all' ? undefined : selectedSection,
          duePeriod: selectedDuePeriod === 'all' ? undefined : selectedDuePeriod,
          sortBy,
          sortOrder
        })
        setDashboard(data)
      } catch (err) {
        console.error('Failed to fetch gaps dashboard:', err)
        setError('Failed to load dashboard. Please try again.')
      } finally {
        setIsLoading(false)
      }
    }
    
    fetchDashboard()
  }, [selectedPeriod, selectedStatus, selectedSection, selectedDuePeriod, sortBy, sortOrder, activePeriodId])
  
  const toggleSort = (field: 'risk' | 'dueDate' | 'section') => {
    if (sortBy === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
    } else {
      setSortBy(field)
      setSortOrder(field === 'risk' ? 'desc' : 'asc')
    }
  }
  
  const getImpactColor = (impact: string) => {
    switch (impact.toLowerCase()) {
      case 'high':
        return 'destructive'
      case 'medium':
        return 'warning'
      case 'low':
        return 'secondary'
      default:
        return 'default'
    }
  }
  
  const getStatusBadge = (status: string) => {
    if (status === 'resolved') {
      return <Badge variant="default" className="bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">Resolved</Badge>
    }
    return <Badge variant="destructive">Open</Badge>
  }
  
  if (error) {
    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-foreground">
            Gaps Dashboard
          </h2>
          <p className="text-sm text-muted-foreground mt-1">
            Track and manage data gaps with filtering and sorting
          </p>
        </div>
        
        <Card className="border-destructive">
          <CardContent className="pt-6">
            <p className="text-destructive">{error}</p>
          </CardContent>
        </Card>
      </div>
    )
  }
  
  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-foreground">
            Gaps Dashboard
          </h2>
          <p className="text-sm text-muted-foreground mt-1">
            Track and manage data gaps with filtering and sorting
          </p>
        </div>
      </div>
      
      {/* Summary Metrics */}
      {dashboard && (
        <div className="grid gap-4 md:grid-cols-4">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <WarningCircle size={16} />
                Total Gaps
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-foreground">
                {dashboard.summary.totalGaps}
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                {dashboard.summary.openGaps} open, {dashboard.summary.resolvedGaps} resolved
              </p>
            </CardContent>
          </Card>
          
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <Target size={16} />
                By Risk Level
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-1">
                <div className="flex justify-between text-sm">
                  <span className="text-destructive">High:</span>
                  <span className="font-semibold">{dashboard.summary.highRiskGaps}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-amber-600">Medium:</span>
                  <span className="font-semibold">{dashboard.summary.mediumRiskGaps}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">Low:</span>
                  <span className="font-semibold">{dashboard.summary.lowRiskGaps}</span>
                </div>
              </div>
            </CardContent>
          </Card>
          
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <CheckCircle size={16} />
                Remediation Status
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-foreground">
                {dashboard.summary.withRemediationPlan}
                <span className="text-sm font-normal text-muted-foreground ml-1">
                  / {dashboard.summary.totalGaps}
                </span>
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                with remediation plan
              </p>
            </CardContent>
          </Card>
          
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <Funnel size={16} />
                Filtered Results
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold text-foreground">
                {dashboard.totalCount}
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                gap(s) match filters
              </p>
            </CardContent>
          </Card>
        </div>
      )}
      
      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Filters & Sorting</CardTitle>
          <CardDescription>
            Filter gaps by status, section, and due period. Sort by risk level or due date.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <div>
              <label className="text-sm font-medium text-foreground mb-2 block">
                Reporting Period
              </label>
              <Select value={selectedPeriod} onValueChange={setSelectedPeriod}>
                <SelectTrigger>
                  <SelectValue placeholder="Select period" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Active Period</SelectItem>
                  {(periods || []).map(period => (
                    <SelectItem key={period.id} value={period.id}>
                      {period.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            
            <div>
              <label className="text-sm font-medium text-foreground mb-2 block">
                Status
              </label>
              <Select value={selectedStatus} onValueChange={setSelectedStatus}>
                <SelectTrigger>
                  <SelectValue placeholder="Select status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Gaps</SelectItem>
                  <SelectItem value="open">Open</SelectItem>
                  <SelectItem value="resolved">Resolved</SelectItem>
                </SelectContent>
              </Select>
            </div>
            
            <div>
              <label className="text-sm font-medium text-foreground mb-2 block">
                Section
              </label>
              <Select value={selectedSection} onValueChange={setSelectedSection}>
                <SelectTrigger>
                  <SelectValue placeholder="Select section" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Sections</SelectItem>
                  {activeSections.map(section => (
                    <SelectItem key={section.id} value={section.id}>
                      {section.title}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            
            <div>
              <label className="text-sm font-medium text-foreground mb-2 block">
                Due Period
              </label>
              <Select value={selectedDuePeriod} onValueChange={setSelectedDuePeriod}>
                <SelectTrigger>
                  <SelectValue placeholder="Select due period" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Periods</SelectItem>
                  <SelectItem value="Q1">Q1</SelectItem>
                  <SelectItem value="Q2">Q2</SelectItem>
                  <SelectItem value="Q3">Q3</SelectItem>
                  <SelectItem value="Q4">Q4</SelectItem>
                  <SelectItem value="2025">2025</SelectItem>
                  <SelectItem value="2026">2026</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>
      
      {/* Gaps List */}
      {isLoading && (
        <Card>
          <CardContent className="pt-6">
            <p className="text-muted-foreground">Loading gaps...</p>
          </CardContent>
        </Card>
      )}
      
      {!isLoading && dashboard && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>Gaps ({dashboard.totalCount})</CardTitle>
              <div className="flex gap-2">
                <button
                  onClick={() => toggleSort('risk')}
                  className={`text-sm px-3 py-1.5 rounded-md border ${
                    sortBy === 'risk' 
                      ? 'bg-accent text-accent-foreground border-accent' 
                      : 'border-border hover:bg-accent/50'
                  }`}
                >
                  Risk {sortBy === 'risk' && (sortOrder === 'desc' ? <ArrowDown className="inline ml-1" size={14} /> : <ArrowUp className="inline ml-1" size={14} />)}
                </button>
                <button
                  onClick={() => toggleSort('dueDate')}
                  className={`text-sm px-3 py-1.5 rounded-md border ${
                    sortBy === 'dueDate' 
                      ? 'bg-accent text-accent-foreground border-accent' 
                      : 'border-border hover:bg-accent/50'
                  }`}
                >
                  Due Date {sortBy === 'dueDate' && (sortOrder === 'desc' ? <ArrowDown className="inline ml-1" size={14} /> : <ArrowUp className="inline ml-1" size={14} />)}
                </button>
                <button
                  onClick={() => toggleSort('section')}
                  className={`text-sm px-3 py-1.5 rounded-md border ${
                    sortBy === 'section' 
                      ? 'bg-accent text-accent-foreground border-accent' 
                      : 'border-border hover:bg-accent/50'
                  }`}
                >
                  Section {sortBy === 'section' && (sortOrder === 'desc' ? <ArrowDown className="inline ml-1" size={14} /> : <ArrowUp className="inline ml-1" size={14} />)}
                </button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {dashboard.gaps.length === 0 ? (
              <div className="text-center py-8">
                <p className="text-muted-foreground">No gaps match the selected filters.</p>
              </div>
            ) : (
              <div className="space-y-3">
                {dashboard.gaps.map((item, idx) => (
                  <div key={item.gap.id || idx} className="border border-border rounded-lg p-4">
                    <div className="flex items-start justify-between mb-2">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          <h4 className="font-semibold text-foreground">{item.gap.title}</h4>
                          <Badge variant={getImpactColor(item.gap.impact)}>
                            {item.gap.impact} risk
                          </Badge>
                          {getStatusBadge(item.status)}
                        </div>
                        <p className="text-sm text-muted-foreground">{item.gap.description}</p>
                      </div>
                    </div>
                    
                    <div className="mt-3 grid grid-cols-2 md:grid-cols-4 gap-2 text-xs">
                      <div>
                        <span className="text-muted-foreground">Section:</span>
                        <p className="font-medium text-foreground">{item.sectionTitle}</p>
                      </div>
                      <div>
                        <span className="text-muted-foreground">Category:</span>
                        <p className="font-medium text-foreground capitalize">{item.category}</p>
                      </div>
                      <div>
                        <span className="text-muted-foreground">Owner:</span>
                        <p className="font-medium text-foreground">{item.ownerName || 'Not assigned'}</p>
                      </div>
                      <div>
                        <span className="text-muted-foreground">Due Period:</span>
                        <p className="font-medium text-foreground">{item.duePeriod || 'Not set'}</p>
                      </div>
                    </div>
                    
                    {item.remediationPlanId && (
                      <div className="mt-2 pt-2 border-t border-border">
                        <p className="text-xs text-muted-foreground">
                          Remediation Plan: 
                          <span className="font-medium text-foreground ml-1 capitalize">
                            {item.remediationPlanStatus || 'planned'}
                          </span>
                        </p>
                      </div>
                    )}
                    
                    {item.gap.improvementPlan && (
                      <div className="mt-2 pt-2 border-t border-border">
                        <p className="text-xs text-muted-foreground">
                          Improvement Plan:
                        </p>
                        <p className="text-sm text-foreground mt-1">{item.gap.improvementPlan}</p>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  )
}
