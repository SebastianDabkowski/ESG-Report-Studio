import { useState, useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Progress } from '@/components/ui/progress'
import { useKV } from '@github/spark/hooks'
import { ChartBar, CheckCircle, WarningCircle, Clock, User, Target } from '@phosphor-icons/react'
import type { User as UserType, ReportingPeriod, ReadinessReport } from '@/lib/types'
import { getReadinessReport } from '@/lib/api'
import { getProgressStatusColor, getProgressStatusLabel } from '@/lib/helpers'

interface ReadinessReportViewProps {
  currentUser: UserType
}

export default function ReadinessReportView({ currentUser }: ReadinessReportViewProps) {
  const [periods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  const [users] = useKV<UserType[]>('users', [])
  
  const [report, setReport] = useState<ReadinessReport | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  
  // Filters
  const [selectedPeriod, setSelectedPeriod] = useState<string>('all')
  const [selectedOwner, setSelectedOwner] = useState<string>('all')
  const [selectedCategory, setSelectedCategory] = useState<string>('all')
  
  const activePeriod = (periods || []).find(p => p.status === 'active')
  
  // Fetch report data when filters change
  useEffect(() => {
    const fetchReport = async () => {
      setIsLoading(true)
      setError(null)
      
      try {
        const params: any = {}
        
        // Use selected period, or default to active period
        const periodId = selectedPeriod === 'all' ? activePeriod?.id : selectedPeriod
        if (periodId) params.periodId = periodId
        
        if (selectedOwner !== 'all') params.ownerId = selectedOwner
        if (selectedCategory !== 'all') params.category = selectedCategory
        
        const data = await getReadinessReport(params)
        setReport(data)
      } catch (err) {
        console.error('Failed to fetch readiness report:', err)
        setError('Failed to load readiness report. Please try again.')
      } finally {
        setIsLoading(false)
      }
    }
    
    fetchReport()
  }, [selectedPeriod, selectedOwner, selectedCategory, activePeriod])
  
  if (error) {
    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-foreground">
            Coverage and Readiness Report
          </h2>
          <p className="text-sm text-muted-foreground mt-1">
            Monitor ownership completeness and data completion
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
      <div>
        <h2 className="text-2xl font-semibold tracking-tight text-foreground">
          Coverage and Readiness Report
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          Monitor ownership completeness and data completion at a glance
        </p>
      </div>
      
      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Filters</CardTitle>
          <CardDescription>
            Filter the report by period, owner, or ESG area
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">
                Reporting Period
              </label>
              <Select value={selectedPeriod} onValueChange={setSelectedPeriod}>
                <SelectTrigger>
                  <SelectValue placeholder="Select period" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Periods</SelectItem>
                  {(periods || []).map((period) => (
                    <SelectItem key={period.id} value={period.id}>
                      {period.name} {period.status === 'active' ? '(Active)' : ''}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">
                Owner
              </label>
              <Select value={selectedOwner} onValueChange={setSelectedOwner}>
                <SelectTrigger>
                  <SelectValue placeholder="Select owner" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Owners</SelectItem>
                  {(users || []).map((user) => (
                    <SelectItem key={user.id} value={user.id}>
                      {user.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            
            <div className="space-y-2">
              <label className="text-sm font-medium text-foreground">
                ESG Category
              </label>
              <Select value={selectedCategory} onValueChange={setSelectedCategory}>
                <SelectTrigger>
                  <SelectValue placeholder="Select category" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Categories</SelectItem>
                  <SelectItem value="environmental">Environmental</SelectItem>
                  <SelectItem value="social">Social</SelectItem>
                  <SelectItem value="governance">Governance</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>
      
      {isLoading ? (
        <Card>
          <CardContent className="pt-6">
            <p className="text-muted-foreground text-center">Loading readiness report...</p>
          </CardContent>
        </Card>
      ) : report ? (
        <>
          {/* Key Metrics */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <Card>
              <CardHeader className="pb-3">
                <CardDescription className="flex items-center gap-2">
                  <User size={16} className="text-muted-foreground" />
                  Ownership Coverage
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="flex items-baseline justify-between">
                    <span className="text-3xl font-bold text-foreground">
                      {report.metrics.ownershipPercentage}%
                    </span>
                    <span className="text-sm text-muted-foreground">
                      {report.metrics.itemsWithOwners}/{report.metrics.totalItems} items
                    </span>
                  </div>
                  <Progress value={report.metrics.ownershipPercentage} className="h-2" />
                </div>
              </CardContent>
            </Card>
            
            <Card>
              <CardHeader className="pb-3">
                <CardDescription className="flex items-center gap-2">
                  <CheckCircle size={16} className="text-muted-foreground" />
                  Completion Rate
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="flex items-baseline justify-between">
                    <span className="text-3xl font-bold text-foreground">
                      {report.metrics.completionPercentage}%
                    </span>
                    <span className="text-sm text-muted-foreground">
                      {report.metrics.completedItems}/{report.metrics.totalItems} items
                    </span>
                  </div>
                  <Progress value={report.metrics.completionPercentage} className="h-2" />
                </div>
              </CardContent>
            </Card>
            
            <Card>
              <CardHeader className="pb-3">
                <CardDescription className="flex items-center gap-2">
                  <WarningCircle size={16} className="text-muted-foreground" />
                  Blocked Items
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="flex items-baseline justify-between">
                    <span className="text-3xl font-bold text-destructive">
                      {report.metrics.blockedCount}
                    </span>
                    <span className="text-sm text-muted-foreground">
                      items
                    </span>
                  </div>
                  {report.metrics.blockedCount > 0 && (
                    <p className="text-xs text-muted-foreground">
                      Require immediate attention
                    </p>
                  )}
                </div>
              </CardContent>
            </Card>
            
            <Card>
              <CardHeader className="pb-3">
                <CardDescription className="flex items-center gap-2">
                  <Clock size={16} className="text-muted-foreground" />
                  Overdue Items
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="flex items-baseline justify-between">
                    <span className="text-3xl font-bold text-amber-600">
                      {report.metrics.overdueCount}
                    </span>
                    <span className="text-sm text-muted-foreground">
                      items
                    </span>
                  </div>
                  {report.metrics.overdueCount > 0 && (
                    <p className="text-xs text-muted-foreground">
                      Past deadline
                    </p>
                  )}
                </div>
              </CardContent>
            </Card>
          </div>
          
          {/* Items List */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <ChartBar size={20} />
                Item Details
              </CardTitle>
              <CardDescription>
                {report.items.length} item{report.items.length !== 1 ? 's' : ''} matching current filters
              </CardDescription>
            </CardHeader>
            <CardContent>
              {report.items.length === 0 ? (
                <p className="text-muted-foreground text-center py-8">
                  No items found matching the selected filters.
                </p>
              ) : (
                <div className="space-y-3">
                  {report.items.map((item) => (
                    <div
                      key={item.id}
                      className="flex items-center justify-between p-4 border border-border rounded-lg hover:bg-accent/50 transition-colors"
                    >
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <Badge variant="outline" className="text-xs">
                            {item.type}
                          </Badge>
                          <Badge 
                            variant="outline" 
                            className={`text-xs ${
                              item.category === 'environmental' ? 'border-green-500 text-green-700' :
                              item.category === 'social' ? 'border-blue-500 text-blue-700' :
                              'border-purple-500 text-purple-700'
                            }`}
                          >
                            {item.category.charAt(0).toUpperCase() + item.category.slice(1)}
                          </Badge>
                          <Badge 
                            variant="outline"
                            className={`text-xs ${getProgressStatusColor(item.progressStatus)}`}
                          >
                            {getProgressStatusLabel(item.progressStatus)}
                          </Badge>
                          {item.isBlocked && (
                            <Badge variant="destructive" className="text-xs">
                              Blocked
                            </Badge>
                          )}
                          {item.isOverdue && (
                            <Badge variant="outline" className="text-xs border-amber-500 text-amber-700">
                              Overdue
                            </Badge>
                          )}
                        </div>
                        <h4 className="font-medium text-foreground truncate">
                          {item.title}
                        </h4>
                        <p className="text-sm text-muted-foreground mt-1">
                          Owner: {item.ownerName || <span className="text-destructive">Unassigned</span>}
                          {item.deadline && (
                            <span className="ml-3">
                              Deadline: {new Date(item.deadline).toLocaleDateString()}
                            </span>
                          )}
                        </p>
                      </div>
                      
                      <div className="flex items-center gap-3 ml-4">
                        <div className="text-right min-w-[80px]">
                          <div className="text-sm font-medium text-foreground">
                            {item.completenessPercentage}%
                          </div>
                          <div className="text-xs text-muted-foreground">
                            Complete
                          </div>
                        </div>
                        <Progress 
                          value={item.completenessPercentage} 
                          className="w-24 h-2"
                        />
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </>
      ) : null}
    </div>
  )
}
