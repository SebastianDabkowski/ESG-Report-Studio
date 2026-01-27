import { useEffect, useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useKV } from '@github/spark/hooks'
import { Plus, CalendarDots, CheckCircle, Warning } from '@phosphor-icons/react'
import type { User, ReportingPeriod, ReportSection, SectionSummary, ReportingMode, ReportScope, Organization, OrganizationalUnit } from '@/lib/types'
import { formatDate, generateId } from '@/lib/helpers'
import { createReportingPeriod, getReportingData } from '@/lib/api'

interface PeriodsViewProps {
  currentUser: User
}

const SIMPLIFIED_SECTIONS = [
  { title: 'Energy & Emissions', category: 'environmental' as const, description: 'Energy consumption, GHG emissions, carbon footprint' },
  { title: 'Waste & Recycling', category: 'environmental' as const, description: 'Waste generation, recycling rates, circular economy initiatives' },
  { title: 'Employee Health & Safety', category: 'social' as const, description: 'Workplace safety metrics, injury rates, wellness programs' },
  { title: 'Diversity & Inclusion', category: 'social' as const, description: 'Workforce diversity, equal opportunity, inclusion initiatives' },
  { title: 'Board Composition', category: 'governance' as const, description: 'Board structure, independence, diversity, expertise' },
  { title: 'Ethics & Compliance', category: 'governance' as const, description: 'Code of conduct, anti-corruption, compliance training' }
]

const EXTENDED_SECTIONS = [
  ...SIMPLIFIED_SECTIONS,
  { title: 'Water & Biodiversity', category: 'environmental' as const, description: 'Water usage, water quality, biodiversity impact' },
  { title: 'Supply Chain Environmental Impact', category: 'environmental' as const, description: 'Supplier environmental performance, sustainable sourcing' },
  { title: 'Employee Development', category: 'social' as const, description: 'Training hours, skill development, career progression' },
  { title: 'Community Engagement', category: 'social' as const, description: 'Social investment, local employment, community programs' },
  { title: 'Human Rights', category: 'social' as const, description: 'Human rights policy, supply chain labor practices' },
  { title: 'Risk Management', category: 'governance' as const, description: 'Risk framework, ESG risk integration, climate risk' },
  { title: 'Stakeholder Engagement', category: 'governance' as const, description: 'Stakeholder dialogue, materiality assessment' }
]

export default function PeriodsView({ currentUser }: PeriodsViewProps) {
  const [organization, setOrganization] = useKV<Organization | null>('organization', null)
  const [organizationalUnits, setOrganizationalUnits] = useKV<OrganizationalUnit[]>('organizational-units', [])
  const [periods, setPeriods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  const [sections, setSections] = useKV<ReportSection[]>('report-sections', [])
  const [sectionSummaries, setSectionSummaries] = useKV<SectionSummary[]>('section-summaries', [])
  
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [name, setName] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [reportingMode, setReportingMode] = useState<ReportingMode>('simplified')
  const [reportScope, setReportScope] = useState<ReportScope>('single-company')
  const [syncError, setSyncError] = useState<string | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)

  useEffect(() => {
    let isActive = true

    const loadFromApi = async () => {
      try {
        const snapshot = await getReportingData()
        if (!isActive) return

        if (snapshot.organization) {
          setOrganization(snapshot.organization)
        }
        if (snapshot.organizationalUnits.length > 0) {
          setOrganizationalUnits(snapshot.organizationalUnits)
        }
        if (snapshot.periods.length > 0) {
          setPeriods(snapshot.periods)
        }
        if (snapshot.sections.length > 0) {
          setSections(snapshot.sections)
        }
        if (snapshot.sectionSummaries.length > 0) {
          setSectionSummaries(snapshot.sectionSummaries)
        }

        setSyncError(null)
      } catch (error) {
        if (!isActive) return
        setSyncError('Backend sync unavailable. Using local data.')
      }
    }

    loadFromApi()

    return () => {
      isActive = false
    }
  }, [setPeriods, setSections, setSectionSummaries, setOrganization, setOrganizationalUnits])

  const createLocalPeriod = () => {
    const newPeriod: ReportingPeriod = {
      id: generateId(),
      name,
      startDate,
      endDate,
      reportingMode,
      reportScope,
      status: 'active',
      createdAt: new Date().toISOString(),
      ownerId: currentUser.id
    }

    setPeriods((current) => {
      const updated = current?.map(p => ({ ...p, status: 'closed' as const })) || []
      return [...updated, newPeriod]
    })

    const sectionTemplates = reportingMode === 'simplified' ? SIMPLIFIED_SECTIONS : EXTENDED_SECTIONS
    
    const newSections: ReportSection[] = sectionTemplates.map((template, index) => ({
      id: generateId(),
      periodId: newPeriod.id,
      title: template.title,
      category: template.category,
      description: template.description,
      ownerId: currentUser.id,
      status: 'draft',
      completeness: 'empty',
      order: index
    }))

    setSections((current) => [...(current || []), ...newSections])

    const newSummaries: SectionSummary[] = newSections.map(section => ({
      ...section,
      dataPointCount: 0,
      evidenceCount: 0,
      gapCount: 0,
      assumptionCount: 0,
      completenessPercentage: 0,
      ownerName: currentUser.name
    }))

    setSectionSummaries((current) => [...(current || []), ...newSummaries])

    setIsCreateOpen(false)
    setName('')
    setStartDate('')
    setEndDate('')
    setReportingMode('simplified')
    setReportScope('single-company')
  }

  const validateDates = (): string | null => {
    if (!startDate || !endDate) {
      return 'Start date and end date are required.'
    }

    const start = new Date(startDate)
    const end = new Date(endDate)

    if (isNaN(start.getTime()) || isNaN(end.getTime())) {
      return 'Invalid date format. Please provide valid dates.'
    }

    if (start >= end) {
      return 'Start date must be before end date.'
    }

    return null
  }

  const handleCreate = async () => {
    // Clear previous errors
    setValidationError(null)
    setSyncError(null)

    if (!organization) {
      setValidationError('Organization must be configured before creating periods.')
      return
    }

    if (!organizationalUnits || organizationalUnits.length === 0) {
      setValidationError('Organizational structure must be defined before creating periods. Please add at least one organizational unit in the Structure tab.')
      return
    }

    // Client-side validation
    const dateError = validateDates()
    if (dateError) {
      setValidationError(dateError)
      return
    }

    try {
      const snapshot = await createReportingPeriod({
        name,
        startDate,
        endDate,
        reportingMode,
        reportScope,
        ownerId: currentUser.id,
        ownerName: currentUser.name,
        organizationId: organization.id
      })

      setPeriods(snapshot.periods)
      setSections(snapshot.sections)
      setSectionSummaries(snapshot.sectionSummaries)
      setValidationError(null)
      setSyncError(null)
      setIsCreateOpen(false)
      setName('')
      setStartDate('')
      setEndDate('')
      setReportingMode('simplified')
      setReportScope('single-company')
    } catch (error) {
      // Display server validation error
      const errorMessage = error instanceof Error ? error.message : 'Failed to create reporting period.'
      setValidationError(errorMessage)
    }
  }

  return (
    <div className="space-y-6">
      {!organization && (
        <Card className="border-amber-500/50 bg-amber-500/5">
          <CardContent className="flex items-center gap-3 py-4">
            <Warning size={24} weight="fill" className="text-amber-600 dark:text-amber-500" />
            <div>
              <p className="text-sm font-medium text-amber-900 dark:text-amber-100">
                Organization configuration required
              </p>
              <p className="text-xs text-amber-700 dark:text-amber-400">
                You must configure your organization information before creating reporting periods. Go to the Organization tab to get started.
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {organization && (!organizationalUnits || organizationalUnits.length === 0) && (
        <Card className="border-amber-500/50 bg-amber-500/5">
          <CardContent className="flex items-center gap-3 py-4">
            <Warning size={24} weight="fill" className="text-amber-600 dark:text-amber-500" />
            <div>
              <p className="text-sm font-medium text-amber-900 dark:text-amber-100">
                Organizational structure required
              </p>
              <p className="text-xs text-amber-700 dark:text-amber-400">
                You must define at least one organizational unit before creating reporting periods. Go to the Structure tab to get started.
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-foreground">
            Reporting Periods
          </h2>
          <p className="text-sm text-muted-foreground mt-1">
            Manage annual and custom reporting cycles
          </p>
          {syncError && (
            <p className="text-xs text-muted-foreground mt-1">
              {syncError}
            </p>
          )}
        </div>
        
        {currentUser.role !== 'auditor' && (
          <Dialog open={isCreateOpen} onOpenChange={(open) => {
            setIsCreateOpen(open)
            if (!open) {
              setValidationError(null)
              setSyncError(null)
            }
          }}>
            <DialogTrigger asChild>
              <Button className="gap-2" disabled={!organization}>
                <Plus size={16} weight="bold" />
                New Period
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Create Reporting Period</DialogTitle>
                <DialogDescription>
                  Define a new reporting period and generate the ESG report structure.
                </DialogDescription>
              </DialogHeader>
              
              <div className="space-y-4 py-4">
                <div className="space-y-2">
                  <Label htmlFor="period-name">Period Name</Label>
                  <Input
                    id="period-name"
                    placeholder="e.g., FY 2024, Q1 2024"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                  />
                </div>
                
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="start-date">Start Date</Label>
                    <Input
                      id="start-date"
                      type="date"
                      value={startDate}
                      onChange={(e) => setStartDate(e.target.value)}
                    />
                  </div>
                  
                  <div className="space-y-2">
                    <Label htmlFor="end-date">End Date</Label>
                    <Input
                      id="end-date"
                      type="date"
                      value={endDate}
                      onChange={(e) => setEndDate(e.target.value)}
                    />
                  </div>
                </div>
                
                <div className="space-y-2">
                  <Label htmlFor="reporting-mode">Reporting Mode</Label>
                  <Select value={reportingMode} onValueChange={(v) => setReportingMode(v as ReportingMode)}>
                    <SelectTrigger id="reporting-mode">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="simplified">
                        <div>
                          <div className="font-medium">Simplified</div>
                          <div className="text-xs text-muted-foreground">6 core ESG sections for SMEs</div>
                        </div>
                      </SelectItem>
                      <SelectItem value="extended">
                        <div>
                          <div className="font-medium">Extended</div>
                          <div className="text-xs text-muted-foreground">13 comprehensive sections (GRI/SASB aligned)</div>
                        </div>
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="report-scope">Reporting Scope</Label>
                  <Select value={reportScope} onValueChange={(v) => setReportScope(v as ReportScope)}>
                    <SelectTrigger id="report-scope">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="single-company">
                        <div>
                          <div className="font-medium">Single Company</div>
                          <div className="text-xs text-muted-foreground">Report covers a single legal entity</div>
                        </div>
                      </SelectItem>
                      <SelectItem value="group">
                        <div>
                          <div className="font-medium">Group</div>
                          <div className="text-xs text-muted-foreground">Report covers multiple entities in a group</div>
                        </div>
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                {validationError && (
                  <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                    {validationError}
                  </div>
                )}
              </div>
              
              <DialogFooter>
                <Button variant="outline" onClick={() => setIsCreateOpen(false)}>
                  Cancel
                </Button>
                <Button 
                  onClick={handleCreate}
                  disabled={!name || !startDate || !endDate}
                >
                  Create Period
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        )}
      </div>

      <div className="grid gap-4">
        {periods && periods.length > 0 ? (
          periods.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()).map(period => {
            const periodSections = sections?.filter(s => s.periodId === period.id) || []
            const approvedCount = periodSections.filter(s => s.status === 'approved').length
            
            return (
              <Card key={period.id} className={period.status === 'active' ? 'border-l-4 border-l-accent' : ''}>
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <CardTitle className="text-lg">{period.name}</CardTitle>
                        {period.status === 'active' && (
                          <Badge className="bg-accent text-accent-foreground">Active</Badge>
                        )}
                      </div>
                      <CardDescription className="flex items-center gap-2">
                        <CalendarDots size={14} />
                        {formatDate(period.startDate)} - {formatDate(period.endDate)}
                      </CardDescription>
                    </div>
                    
                    <div className="text-right space-y-3">
                      <div>
                        <div className="text-xs text-muted-foreground mb-1">Mode</div>
                        <Badge variant="outline">
                          {period.reportingMode === 'simplified' ? 'Simplified' : 'Extended'}
                        </Badge>
                      </div>
                      <div>
                        <div className="text-xs text-muted-foreground mb-1">Scope</div>
                        <Badge variant="outline">
                          {period.reportScope === 'single-company' ? 'Single Company' : 'Group'}
                        </Badge>
                      </div>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="flex items-center gap-6 text-sm">
                    <div className="flex items-center gap-2">
                      <CheckCircle size={16} className="text-success" weight="fill" />
                      <span className="font-mono font-semibold">{approvedCount}</span>
                      <span className="text-muted-foreground">/ {periodSections.length} sections approved</span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            )
          })
        ) : (
          <Card>
            <CardContent className="flex flex-col items-center justify-center py-12">
              <CalendarDots size={48} weight="duotone" className="text-muted-foreground mb-4" />
              <h3 className="text-lg font-semibold mb-2">No Reporting Periods</h3>
              <p className="text-sm text-muted-foreground text-center max-w-md">
                Create your first reporting period to begin collecting ESG data.
              </p>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  )
}