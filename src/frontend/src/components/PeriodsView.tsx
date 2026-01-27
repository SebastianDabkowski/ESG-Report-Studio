import { useEffect, useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useKV } from '@github/spark/hooks'
import { Plus, CalendarDots, CheckCircle } from '@phosphor-icons/react'
import type { User, ReportingPeriod, ReportSection, SectionSummary, ReportVariant } from '@/lib/types'
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
  const [periods, setPeriods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  const [sections, setSections] = useKV<ReportSection[]>('report-sections', [])
  const [sectionSummaries, setSectionSummaries] = useKV<SectionSummary[]>('section-summaries', [])
  
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [name, setName] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [variant, setVariant] = useState<ReportVariant>('simplified')
  const [syncError, setSyncError] = useState<string | null>(null)

  useEffect(() => {
    let isActive = true

    const loadFromApi = async () => {
      try {
        const snapshot = await getReportingData()
        if (!isActive) return

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
  }, [setPeriods, setSections, setSectionSummaries])

  const createLocalPeriod = () => {
    const newPeriod: ReportingPeriod = {
      id: generateId(),
      name,
      startDate,
      endDate,
      variant,
      status: 'active',
      createdAt: new Date().toISOString(),
      ownerId: currentUser.id
    }

    setPeriods((current) => {
      const updated = current?.map(p => ({ ...p, status: 'closed' as const })) || []
      return [...updated, newPeriod]
    })

    const sectionTemplates = variant === 'simplified' ? SIMPLIFIED_SECTIONS : EXTENDED_SECTIONS
    
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
    setVariant('simplified')
  }

  const handleCreate = async () => {
    try {
      const snapshot = await createReportingPeriod({
        name,
        startDate,
        endDate,
        variant,
        ownerId: currentUser.id,
        ownerName: currentUser.name
      })

      setPeriods(snapshot.periods)
      setSections(snapshot.sections)
      setSectionSummaries(snapshot.sectionSummaries)
      setSyncError(null)
      setIsCreateOpen(false)
      setName('')
      setStartDate('')
      setEndDate('')
      setVariant('simplified')
    } catch (error) {
      setSyncError('Backend sync unavailable. Created locally only.')
      createLocalPeriod()
    }
  }

  return (
    <div className="space-y-6">
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
          <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
            <DialogTrigger asChild>
              <Button className="gap-2">
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
                  <Label htmlFor="variant">Report Variant</Label>
                  <Select value={variant} onValueChange={(v) => setVariant(v as ReportVariant)}>
                    <SelectTrigger id="variant">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="simplified">
                        <div>
                          <div className="font-medium">Simplified</div>
                          <div className="text-xs text-muted-foreground">6 core ESG sections</div>
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
                    
                    <div className="text-right">
                      <div className="text-xs text-muted-foreground mb-1">Variant</div>
                      <Badge variant="outline">
                        {period.variant === 'simplified' ? 'Simplified' : 'Extended'}
                      </Badge>
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