import { useEffect, useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { useKV } from '@github/spark/hooks'
import { 
  Building, 
  CalendarDots, 
  CheckCircle, 
  FileText, 
  TreeStructure,
  ListChecks,
  WarningCircle,
  Info
} from '@phosphor-icons/react'
import type { Organization, ReportingPeriod, SectionSummary, OrganizationalUnit } from '@/lib/types'
import { formatDate } from '@/lib/helpers'
import { getReportingData } from '@/lib/api'

interface ReportingContextSummaryProps {
  // Future enhancement: could be used for role-based visibility or personalization
}

export default function ReportingContextSummary(_props: ReportingContextSummaryProps) {
  const [organization, setOrganization] = useKV<Organization | null>('organization', null)
  const [periods, setPeriods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  const [sectionSummaries, setSectionSummaries] = useKV<SectionSummary[]>('section-summaries', [])
  const [organizationalUnits, setOrganizationalUnits] = useKV<OrganizationalUnit[]>('organizational-units', [])
  const [syncError, setSyncError] = useState<string | null>(null)

  useEffect(() => {
    let isActive = true

    const loadData = async () => {
      try {
        const snapshot = await getReportingData()
        if (!isActive) return

        if (snapshot.organization) {
          setOrganization(snapshot.organization)
        }
        if (snapshot.periods.length > 0) {
          setPeriods(snapshot.periods)
        }
        if (snapshot.sectionSummaries.length > 0) {
          setSectionSummaries(snapshot.sectionSummaries)
        }
        if (snapshot.organizationalUnits.length > 0) {
          setOrganizationalUnits(snapshot.organizationalUnits)
        }
        setSyncError(null)
      } catch (error) {
        if (!isActive) return
        // Show error to user but continue with local data
        console.error('Failed to load reporting data:', error)
        setSyncError('Backend sync unavailable. Using local data.')
      }
    }

    loadData()

    return () => {
      isActive = false
    }
  }, [setOrganization, setPeriods, setSectionSummaries, setOrganizationalUnits])

  const activePeriod = (periods || []).find(p => p.status === 'active')
  const activeSections = (sectionSummaries || []).filter(s => activePeriod && s.periodId === activePeriod.id)
  
  const environmentalSections = activeSections.filter(s => s.category === 'environmental')
  const socialSections = activeSections.filter(s => s.category === 'social')
  const governanceSections = activeSections.filter(s => s.category === 'governance')

  const avgCompleteness = activeSections.length > 0
    ? Math.round(activeSections.reduce((sum, s) => sum + s.completenessPercentage, 0) / activeSections.length)
    : 0

  const approvedCount = activeSections.filter(s => s.status === 'approved').length
  const inReviewCount = activeSections.filter(s => s.status === 'in-review').length
  const draftCount = activeSections.filter(s => s.status === 'draft').length

  const isConfigurationComplete = !!(
    organization &&
    organizationalUnits && organizationalUnits.length > 0 &&
    activePeriod &&
    activeSections.length > 0
  )

  const getCompletenessStatus = () => {
    if (!organization) return { color: 'text-destructive', message: 'Organization not configured' }
    if (!organizationalUnits || organizationalUnits.length === 0) return { color: 'text-destructive', message: 'Organizational structure not defined' }
    if (!activePeriod) return { color: 'text-destructive', message: 'No active reporting period' }
    if (activeSections.length === 0) return { color: 'text-destructive', message: 'No sections configured' }
    if (avgCompleteness < 30) return { color: 'text-warning', message: 'Configuration incomplete - data entry needed' }
    if (avgCompleteness < 70) return { color: 'text-warning', message: 'Partial configuration - review recommended' }
    return { color: 'text-success', message: 'Configuration complete - ready for review' }
  }

  const completenessStatus = getCompletenessStatus()

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold tracking-tight text-foreground">
          Reporting Context Summary
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          Comprehensive overview of reporting configuration and readiness
        </p>
        {syncError && (
          <p className="text-xs text-muted-foreground mt-1">
            {syncError}
          </p>
        )}
      </div>

      {/* Overall Status Card */}
      <Card className={isConfigurationComplete ? 'border-l-4 border-l-success' : 'border-l-4 border-l-warning'}>
        <CardHeader>
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-3">
              {isConfigurationComplete ? (
                <CheckCircle size={32} weight="fill" className="text-success" />
              ) : (
                <WarningCircle size={32} weight="fill" className="text-warning" />
              )}
              <div>
                <CardTitle className="text-lg">Configuration Status</CardTitle>
                <CardDescription className={completenessStatus.color}>
                  {completenessStatus.message}
                </CardDescription>
              </div>
            </div>
          </div>
        </CardHeader>
        {isConfigurationComplete && (
          <CardContent>
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Overall Readiness</span>
                <span className="text-sm font-mono font-semibold">{avgCompleteness}%</span>
              </div>
              <Progress value={avgCompleteness} className="h-3" />
            </div>
          </CardContent>
        )}
      </Card>

      {/* Configuration Checklist */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
              <ListChecks size={20} weight="bold" className="text-primary" />
            </div>
            <div>
              <CardTitle>Configuration Checklist</CardTitle>
              <CardDescription>Key configuration elements required for reporting</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex items-center gap-3">
            {organization ? (
              <CheckCircle size={20} weight="fill" className="text-success flex-shrink-0" />
            ) : (
              <WarningCircle size={20} weight="fill" className="text-muted-foreground flex-shrink-0" />
            )}
            <div className="flex-1">
              <div className="text-sm font-medium">Organization Information</div>
              <div className="text-xs text-muted-foreground">
                {organization ? `${organization.name} (${organization.country})` : 'Not configured'}
              </div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {organizationalUnits && organizationalUnits.length > 0 ? (
              <CheckCircle size={20} weight="fill" className="text-success flex-shrink-0" />
            ) : (
              <WarningCircle size={20} weight="fill" className="text-muted-foreground flex-shrink-0" />
            )}
            <div className="flex-1">
              <div className="text-sm font-medium">Organizational Structure</div>
              <div className="text-xs text-muted-foreground">
                {organizationalUnits && organizationalUnits.length > 0 
                  ? `${organizationalUnits.length} unit${organizationalUnits.length > 1 ? 's' : ''} defined` 
                  : 'Not defined'}
              </div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {activePeriod ? (
              <CheckCircle size={20} weight="fill" className="text-success flex-shrink-0" />
            ) : (
              <WarningCircle size={20} weight="fill" className="text-muted-foreground flex-shrink-0" />
            )}
            <div className="flex-1">
              <div className="text-sm font-medium">Active Reporting Period</div>
              <div className="text-xs text-muted-foreground">
                {activePeriod 
                  ? `${activePeriod.name} (${formatDate(activePeriod.startDate)} - ${formatDate(activePeriod.endDate)})` 
                  : 'No active period'}
              </div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {activeSections.length > 0 ? (
              <CheckCircle size={20} weight="fill" className="text-success flex-shrink-0" />
            ) : (
              <WarningCircle size={20} weight="fill" className="text-muted-foreground flex-shrink-0" />
            )}
            <div className="flex-1">
              <div className="text-sm font-medium">Report Structure</div>
              <div className="text-xs text-muted-foreground">
                {activeSections.length > 0 
                  ? `${activeSections.length} section${activeSections.length > 1 ? 's' : ''} configured` 
                  : 'No sections configured'}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Organization Details */}
      {organization && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                <Building size={20} weight="bold" className="text-primary" />
              </div>
              <div>
                <CardTitle>Organization Details</CardTitle>
                <CardDescription>Legal entity and coverage information</CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <div className="text-xs text-muted-foreground mb-1">Company Name</div>
                <div className="text-sm font-medium">{organization.name}</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">Legal Form</div>
                <div className="text-sm font-medium">{organization.legalForm}</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">Country</div>
                <div className="text-sm font-medium">{organization.country}</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">Identifier</div>
                <div className="text-sm font-medium font-mono">{organization.identifier}</div>
              </div>
              <div className="md:col-span-2">
                <div className="text-xs text-muted-foreground mb-1">Reporting Coverage</div>
                <div className="flex items-center gap-2">
                  <Badge variant="outline">
                    {organization.coverageType === 'full' ? 'Full Coverage' : 'Limited Coverage'}
                  </Badge>
                  {organization.coverageType === 'limited' && organization.coverageJustification && (
                    <TooltipProvider>
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <Info size={14} className="text-muted-foreground" aria-label="Coverage justification available" />
                        </TooltipTrigger>
                        <TooltipContent>
                          <p className="max-w-xs">See justification below</p>
                        </TooltipContent>
                      </Tooltip>
                    </TooltipProvider>
                  )}
                </div>
                {organization.coverageType === 'limited' && organization.coverageJustification && (
                  <div className="text-xs text-muted-foreground mt-2 italic">
                    {organization.coverageJustification}
                  </div>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Organizational Structure */}
      {organizationalUnits && organizationalUnits.length > 0 && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-accent/10">
                <TreeStructure size={20} weight="bold" className="text-accent" />
              </div>
              <div>
                <CardTitle>Organizational Structure</CardTitle>
                <CardDescription>Organizational units included in reporting</CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {organizationalUnits.slice(0, 5).map(unit => (
                <div key={unit.id} className="flex items-start gap-2 p-2 rounded-md border border-border">
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-medium">{unit.name}</div>
                    {unit.description && (
                      <div className="text-xs text-muted-foreground">{unit.description}</div>
                    )}
                  </div>
                </div>
              ))}
              {organizationalUnits.length > 5 && (
                <div className="text-xs text-muted-foreground text-center pt-2">
                  +{organizationalUnits.length - 5} more unit{organizationalUnits.length - 5 > 1 ? 's' : ''}
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Reporting Period Details */}
      {activePeriod && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-accent/10">
                <CalendarDots size={20} weight="bold" className="text-accent" />
              </div>
              <div>
                <CardTitle>Active Reporting Period</CardTitle>
                <CardDescription>Current reporting scope and timeline</CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <div className="text-xs text-muted-foreground mb-1">Period Name</div>
                <div className="text-sm font-medium">{activePeriod.name}</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">Status</div>
                <Badge className="bg-accent text-accent-foreground">{activePeriod.status}</Badge>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">Start Date</div>
                <div className="text-sm font-medium">{formatDate(activePeriod.startDate)}</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">End Date</div>
                <div className="text-sm font-medium">{formatDate(activePeriod.endDate)}</div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">Reporting Mode</div>
                <Badge variant="outline">
                  {activePeriod.reportingMode === 'simplified' ? 'Simplified (SME)' : 'Extended (CSRD/ESRS)'}
                </Badge>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">Report Scope</div>
                <Badge variant="outline">
                  {activePeriod.reportScope === 'single-company' ? 'Single Company' : 'Group'}
                </Badge>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Section Status Overview */}
      {activeSections.length > 0 && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                <FileText size={20} weight="bold" className="text-primary" />
              </div>
              <div>
                <CardTitle>Report Structure Overview</CardTitle>
                <CardDescription>ESG sections configured for this reporting period</CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid gap-4 md:grid-cols-3">
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium">Approved</span>
                  <span className="text-sm font-mono font-semibold text-success">{approvedCount}</span>
                </div>
              </div>
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium">In Review</span>
                  <span className="text-sm font-mono font-semibold text-warning">{inReviewCount}</span>
                </div>
              </div>
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium">Draft</span>
                  <span className="text-sm font-mono font-semibold text-muted-foreground">{draftCount}</span>
                </div>
              </div>
            </div>

            <div className="space-y-4 pt-4 border-t">
              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium">Environmental</span>
                  <span className="text-xs text-muted-foreground">{environmentalSections.length} sections</span>
                </div>
                <div className="space-y-1">
                  {environmentalSections.slice(0, 3).map(section => (
                    <div key={section.id} className="text-xs text-muted-foreground">
                      • {section.title}
                    </div>
                  ))}
                  {environmentalSections.length > 3 && (
                    <div className="text-xs text-muted-foreground">
                      +{environmentalSections.length - 3} more
                    </div>
                  )}
                </div>
              </div>

              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium">Social</span>
                  <span className="text-xs text-muted-foreground">{socialSections.length} sections</span>
                </div>
                <div className="space-y-1">
                  {socialSections.slice(0, 3).map(section => (
                    <div key={section.id} className="text-xs text-muted-foreground">
                      • {section.title}
                    </div>
                  ))}
                  {socialSections.length > 3 && (
                    <div className="text-xs text-muted-foreground">
                      +{socialSections.length - 3} more
                    </div>
                  )}
                </div>
              </div>

              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium">Governance</span>
                  <span className="text-xs text-muted-foreground">{governanceSections.length} sections</span>
                </div>
                <div className="space-y-1">
                  {governanceSections.slice(0, 3).map(section => (
                    <div key={section.id} className="text-xs text-muted-foreground">
                      • {section.title}
                    </div>
                  ))}
                  {governanceSections.length > 3 && (
                    <div className="text-xs text-muted-foreground">
                      +{governanceSections.length - 3} more
                    </div>
                  )}
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Missing Configuration Warning */}
      {!isConfigurationComplete && (
        <Card className="border-warning/50 bg-warning/5">
          <CardContent className="flex items-start gap-3 py-4">
            <WarningCircle size={24} weight="fill" className="text-warning flex-shrink-0 mt-0.5" />
            <div>
              <p className="text-sm font-medium text-warning-foreground">
                Configuration Incomplete
              </p>
              <div className="text-xs text-muted-foreground mt-1">
                <p>Complete the following steps to begin reporting:</p>
                <ul className="list-disc list-inside mt-1 space-y-0.5">
                  {!organization && <li>Configure organization information</li>}
                  {(!organizationalUnits || organizationalUnits.length === 0) && <li>Define organizational structure</li>}
                  {!activePeriod && <li>Create an active reporting period</li>}
                </ul>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
