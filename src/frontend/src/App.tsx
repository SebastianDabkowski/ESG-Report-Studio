import { useState } from 'react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Building } from '@phosphor-icons/react'
import Dashboard from '@/components/Dashboard'
import ProgressDashboard from '@/components/ProgressDashboard'
import CompletenessComparisonView from '@/components/CompletenessComparisonView'
import ReportingContextSummary from '@/components/ReportingContextSummary'
import OrganizationView from '@/components/OrganizationView'
import OrganizationalStructureView from '@/components/OrganizationalStructureView'
import PeriodsView from '@/components/PeriodsView'
import SectionsView from '@/components/SectionsView'
import DataCollectionWorkspace from '@/components/DataCollectionWorkspace'
import EvidenceView from '@/components/EvidenceView'
import AuditTrailView from '@/components/AuditTrailView'
import ResponsibilityMatrixView from '@/components/ResponsibilityMatrixView'
import ReadinessReportView from '@/components/ReadinessReportView'
import GapsAndImprovementsView from '@/components/GapsAndImprovementsView'
import GapsDashboard from '@/components/GapsDashboard'
import HistoryDemoView from '@/components/HistoryDemoView'
import MaturityModelsView from '@/components/MaturityModelsView'
import { MaturityAssessmentView } from '@/components/MaturityAssessmentView'
import { useKV } from '@github/spark/hooks'
import type { User } from '@/lib/types'

function App() {
  const [currentUser] = useKV<User>('current-user', {
    id: 'user-1',
    name: 'Sarah Chen',
    email: 'sarah.chen@company.com',
    role: 'report-owner',
    avatarUrl: undefined
  })

  const [activeTab, setActiveTab] = useState('dashboard')

  if (!currentUser) return null

  return (
    <div className="min-h-screen bg-background">
      <header className="border-b border-border bg-card">
        <div className="flex items-center justify-between px-6 py-4">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary">
              <Building size={24} weight="bold" className="text-primary-foreground" />
            </div>
            <div>
              <h1 className="text-xl font-semibold tracking-tight text-foreground">
                ESG Report Studio
              </h1>
              <p className="text-xs text-muted-foreground">
                Enterprise Reporting Platform
              </p>
            </div>
          </div>
          
          <div className="flex items-center gap-4">
            <div className="text-right">
              <div className="text-sm font-medium text-foreground">{currentUser.name}</div>
              <div className="text-xs text-muted-foreground">
                {currentUser.role.split('-').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ')}
              </div>
            </div>
          </div>
        </div>
      </header>

      <main className="p-6">
        <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
          <TabsList className="bg-muted">
            <TabsTrigger value="dashboard">Dashboard</TabsTrigger>
            <TabsTrigger value="progress">Progress Trends</TabsTrigger>
            <TabsTrigger value="comparison">Period Comparison</TabsTrigger>
            <TabsTrigger value="summary">Summary</TabsTrigger>
            <TabsTrigger value="organization">Organization</TabsTrigger>
            <TabsTrigger value="structure">Structure</TabsTrigger>
            <TabsTrigger value="periods">Periods</TabsTrigger>
            <TabsTrigger value="data-collection">Data Collection</TabsTrigger>
            <TabsTrigger value="sections">Sections</TabsTrigger>
            <TabsTrigger value="accountability">Accountability</TabsTrigger>
            <TabsTrigger value="readiness">Readiness</TabsTrigger>
            <TabsTrigger value="gaps-dashboard">Gaps Dashboard</TabsTrigger>
            <TabsTrigger value="gaps-improvements">Gaps & Improvements</TabsTrigger>
            <TabsTrigger value="evidence">Evidence</TabsTrigger>
            <TabsTrigger value="audit">Audit Trail</TabsTrigger>
            <TabsTrigger value="history">Change History</TabsTrigger>
            <TabsTrigger value="maturity">Maturity Models</TabsTrigger>
            <TabsTrigger value="assessment">Maturity Assessment</TabsTrigger>
          </TabsList>

          <TabsContent value="dashboard" className="space-y-6">
            <Dashboard currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="progress" className="space-y-6">
            <ProgressDashboard currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="comparison" className="space-y-6">
            <CompletenessComparisonView />
          </TabsContent>

          <TabsContent value="summary" className="space-y-6">
            <ReportingContextSummary />
          </TabsContent>

          <TabsContent value="organization" className="space-y-6">
            <OrganizationView currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="structure" className="space-y-6">
            <OrganizationalStructureView currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="periods" className="space-y-6">
            <PeriodsView currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="data-collection" className="space-y-6">
            <DataCollectionWorkspace currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="sections" className="space-y-6">
            <SectionsView currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="accountability" className="space-y-6">
            <ResponsibilityMatrixView currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="readiness" className="space-y-6">
            <ReadinessReportView currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="gaps-dashboard" className="space-y-6">
            <GapsDashboard currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="gaps-improvements" className="space-y-6">
            <GapsAndImprovementsView currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="evidence" className="space-y-6">
            <EvidenceView currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="audit" className="space-y-6">
            <AuditTrailView />
          </TabsContent>

          <TabsContent value="history" className="space-y-6">
            <HistoryDemoView />
          </TabsContent>

          <TabsContent value="maturity" className="space-y-6">
            <MaturityModelsView currentUser={currentUser} />
          </TabsContent>

          <TabsContent value="assessment" className="space-y-6">
            <MaturityAssessmentView currentUser={currentUser} />
          </TabsContent>
        </Tabs>
      </main>
    </div>
  )
}

export default App