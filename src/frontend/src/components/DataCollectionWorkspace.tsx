import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Progress } from '@/components/ui/progress'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Separator } from '@/components/ui/separator'
import { useKV } from '@github/spark/hooks'
import DataPointForm from '@/components/DataPointForm'
import DataPointEvidenceManager from '@/components/DataPointEvidenceManager'
import EvidenceUploadForm from '@/components/EvidenceUploadForm'
import ImportDataDialog from '@/components/ImportDataDialog'
import { 
  Leaf, 
  Users, 
  Briefcase, 
  Target, 
  PaperclipHorizontal, 
  WarningCircle, 
  Info,
  Clock,
  User,
  FileText,
  Plus,
  UploadSimple,
  FileArrowDown
} from '@phosphor-icons/react'
import type { User as UserType, ReportingPeriod, SectionSummary, DataPoint, Gap, Evidence } from '@/lib/types'
import { getStatusColor, getStatusBorderColor, getClassificationColor, getCompletenessStatusColor } from '@/lib/helpers'
import { getUsers, getDataPoints, createDataPoint, updateDataPoint } from '@/lib/api'
import { useEffect } from 'react'

interface DataCollectionWorkspaceProps {
  currentUser: UserType
}

export default function DataCollectionWorkspace({ currentUser }: DataCollectionWorkspaceProps) {
  const [periods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  const [sections] = useKV<SectionSummary[]>('section-summaries', [])
  const [dataPoints, setDataPoints] = useKV<DataPoint[]>('data-points', [])
  const [gaps] = useKV<Gap[]>('gaps', [])
  const [evidence, setEvidence] = useKV<Evidence[]>('evidence', [])
  const [users, setUsers] = useKV<UserType[]>('users', [])
  
  const [selectedDataItem, setSelectedDataItem] = useState<DataPoint | null>(null)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isEvidenceUploadOpen, setIsEvidenceUploadOpen] = useState(false)
  const [isImportDialogOpen, setIsImportDialogOpen] = useState(false)
  const [editingDataPoint, setEditingDataPoint] = useState<DataPoint | null>(null)
  const [selectedSectionId, setSelectedSectionId] = useState<string | null>(null)
  const [activeCategory, setActiveCategory] = useState<'environmental' | 'social' | 'governance'>('environmental')
  const [showMyItemsOnly, setShowMyItemsOnly] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Fetch users on mount
  useEffect(() => {
    const fetchUsers = async () => {
      try {
        const fetchedUsers = await getUsers()
        setUsers(fetchedUsers)
      } catch (error) {
        console.error('Failed to fetch users:', error)
      }
    }
    
    fetchUsers()
  }, [setUsers])

  const activePeriod = periods?.find(p => p.status === 'active')
  const activeSections = sections?.filter(s => activePeriod && s.periodId === activePeriod.id) || []
  
  // Filter data points by assigned user if "My Items" is enabled
  const getFilteredDataPoints = (sectionId: string) => {
    const sectionDataPoints = dataPoints?.filter(dp => dp.sectionId === sectionId) || []
    if (showMyItemsOnly) {
      return sectionDataPoints.filter(dp => 
        dp.ownerId === currentUser.id || (dp.contributorIds?.includes(currentUser.id) ?? false)
      )
    }
    return sectionDataPoints
  }
  
  // Group sections by category
  const environmentalSections = activeSections.filter(s => s.category === 'environmental')
  const socialSections = activeSections.filter(s => s.category === 'social')
  const governanceSections = activeSections.filter(s => s.category === 'governance')

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case 'environmental':
        return <Leaf size={20} weight="duotone" className="text-green-600" />
      case 'social':
        return <Users size={20} weight="duotone" className="text-blue-600" />
      case 'governance':
        return <Briefcase size={20} weight="duotone" className="text-purple-600" />
      default:
        return null
    }
  }



  const handleOpenDataItem = (dataPoint: DataPoint) => {
    setSelectedDataItem(dataPoint)
    setIsDetailOpen(true)
  }

  const handleCreateDataPoint = (sectionId: string) => {
    setSelectedSectionId(sectionId)
    setEditingDataPoint(null)
    setIsFormOpen(true)
  }

  const handleEditDataPoint = (dataPoint: DataPoint) => {
    setEditingDataPoint(dataPoint)
    setSelectedSectionId(dataPoint.sectionId)
    setIsFormOpen(true)
    setIsDetailOpen(false)
  }

  const handleFormSubmit = async (formData: Omit<DataPoint, 'id' | 'sectionId' | 'createdAt' | 'updatedAt' | 'evidenceIds'>) => {
    setSubmitError(null)
    setIsSubmitting(true)
    
    try {
      if (editingDataPoint) {
        // Update existing data point
        const payload = {
          type: formData.type,
          classification: formData.classification,
          title: formData.title,
          content: formData.content,
          value: formData.value,
          unit: formData.unit,
          ownerId: formData.ownerId,
          contributorIds: formData.contributorIds || [],
          source: formData.source,
          informationType: formData.informationType,
          assumptions: formData.assumptions,
          completenessStatus: formData.completenessStatus
        }
        
        const updatedDataPoint = await updateDataPoint(editingDataPoint.id, payload)
        
        const updatedDataPoints = dataPoints?.map(dp => 
          dp.id === editingDataPoint.id ? updatedDataPoint : dp
        ) || []
        setDataPoints(updatedDataPoints)
      } else {
        // Create new data point
        const payload = {
          sectionId: selectedSectionId!,
          type: formData.type,
          classification: formData.classification,
          title: formData.title,
          content: formData.content,
          value: formData.value,
          unit: formData.unit,
          ownerId: formData.ownerId,
          contributorIds: formData.contributorIds || [],
          source: formData.source,
          informationType: formData.informationType,
          assumptions: formData.assumptions,
          completenessStatus: formData.completenessStatus
        }
        
        const newDataPoint = await createDataPoint(payload)
        setDataPoints([...(dataPoints || []), newDataPoint])
      }
      
      setIsFormOpen(false)
      setEditingDataPoint(null)
      setSelectedSectionId(null)
      setSubmitError(null)
    } catch (error) {
      // Validation error from backend
      setSubmitError(error instanceof Error ? error.message : 'Failed to save data point')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleFormCancel = () => {
    setIsFormOpen(false)
    setEditingDataPoint(null)
    setSelectedSectionId(null)
    setSubmitError(null)
  }

  const handleUploadEvidence = (sectionId: string) => {
    setSelectedSectionId(sectionId)
    setIsEvidenceUploadOpen(true)
  }

  const handleEvidenceSubmit = async (formData: { title: string; description?: string; sourceUrl?: string; file?: File }) => {
    if (!selectedSectionId) return

    const newEvidence: Evidence = {
      id: crypto.randomUUID(),
      sectionId: selectedSectionId,
      title: formData.title,
      description: formData.description,
      fileName: formData.file?.name,
      fileUrl: formData.file ? `/api/evidence/files/${crypto.randomUUID()}/${formData.file.name}` : undefined,
      sourceUrl: formData.sourceUrl,
      uploadedBy: currentUser.id,
      uploadedAt: new Date().toISOString(),
      linkedDataPoints: []
    }

    setEvidence([...(evidence || []), newEvidence])
    setIsEvidenceUploadOpen(false)
    setSelectedSectionId(null)
  }

  const handleLinkEvidence = async (evidenceId: string) => {
    if (!selectedDataItem) return

    // Update evidence to add data point link
    const updatedEvidence = evidence?.map(e =>
      e.id === evidenceId
        ? { ...e, linkedDataPoints: [...e.linkedDataPoints, selectedDataItem.id] }
        : e
    ) || []
    setEvidence(updatedEvidence)

    // Update data point to add evidence link
    const updatedDataPoints = dataPoints?.map(dp =>
      dp.id === selectedDataItem.id
        ? { ...dp, evidenceIds: [...dp.evidenceIds, evidenceId] }
        : dp
    ) || []
    setDataPoints(updatedDataPoints)

    // Update selected data item to reflect the change
    setSelectedDataItem({
      ...selectedDataItem,
      evidenceIds: [...selectedDataItem.evidenceIds, evidenceId]
    })
  }

  const handleUnlinkEvidence = async (evidenceId: string) => {
    if (!selectedDataItem) return

    // Update evidence to remove data point link
    const updatedEvidence = evidence?.map(e =>
      e.id === evidenceId
        ? { ...e, linkedDataPoints: e.linkedDataPoints.filter(id => id !== selectedDataItem.id) }
        : e
    ) || []
    setEvidence(updatedEvidence)

    // Update data point to remove evidence link
    const updatedDataPoints = dataPoints?.map(dp =>
      dp.id === selectedDataItem.id
        ? { ...dp, evidenceIds: dp.evidenceIds.filter(id => id !== evidenceId) }
        : dp
    ) || []
    setDataPoints(updatedDataPoints)

    // Update selected data item to reflect the change
    setSelectedDataItem({
      ...selectedDataItem,
      evidenceIds: selectedDataItem.evidenceIds.filter(id => id !== evidenceId)
    })
  }

  const getDataItemEvidence = (dataPointId: string): Evidence[] => {
    const dataPoint = dataPoints?.find(dp => dp.id === dataPointId)
    if (!dataPoint || !dataPoint.evidenceIds.length) return []
    
    return evidence?.filter(e => dataPoint.evidenceIds.includes(e.id)) || []
  }

  const handleImportComplete = async () => {
    // Refresh data points from the API after import
    try {
      const refreshedDataPoints = await getDataPoints()
      setDataPoints(refreshedDataPoints)
    } catch (error) {
      console.error('Failed to refresh data points after import:', error)
      // Note: In production, you might want to show a toast notification here
      // alerting the user that the data refresh failed and they should manually refresh
    }
  }

  const renderCategoryContent = (categorySections: SectionSummary[]) => {
    if (categorySections.length === 0) {
      return (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Info size={48} weight="duotone" className="text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No Sections Available</h3>
            <p className="text-sm text-muted-foreground text-center max-w-md">
              No sections are configured for this category in the current reporting period.
            </p>
          </CardContent>
        </Card>
      )
    }

    return (
      <div className="space-y-6">
        {categorySections.map(section => {
          const sectionDataPoints = getFilteredDataPoints(section.id)
          const sectionGaps = gaps?.filter(g => g.sectionId === section.id && !g.resolved) || []

          return (
            <Card key={section.id} className={`border-l-4 ${getStatusBorderColor(section.status)}`}>
              <CardHeader>
                <div className="flex items-start justify-between gap-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      {getCategoryIcon(section.category)}
                      <CardTitle className="text-lg">{section.title}</CardTitle>
                      <Badge className={getStatusColor(section.status)} variant="secondary">
                        {section.status}
                      </Badge>
                    </div>
                    <CardDescription>{section.description}</CardDescription>
                  </div>
                  
                  <div className="text-right flex-shrink-0">
                    <div className="text-sm font-semibold font-mono mb-1">
                      {section.completenessPercentage}%
                    </div>
                    <Progress value={section.completenessPercentage} className="h-2 w-24" />
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center gap-4 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1">
                    <Target size={14} />
                    {section.dataPointCount} data points
                  </span>
                  <span className="flex items-center gap-1">
                    <PaperclipHorizontal size={14} />
                    {section.evidenceCount} evidence
                  </span>
                  <span className="flex items-center gap-1">
                    <WarningCircle size={14} />
                    {section.gapCount} gaps
                  </span>
                  <span className="flex items-center gap-1">
                    <User size={14} />
                    {section.ownerName}
                  </span>
                </div>

                <Separator />

                {sectionDataPoints.length > 0 ? (
                  <div className="space-y-2">
                    <div className="flex items-center justify-between mb-2">
                      <h4 className="text-sm font-semibold text-muted-foreground">Data Items</h4>
                      <div className="flex gap-2">
                        <Button 
                          variant="outline" 
                          size="sm" 
                          onClick={() => handleUploadEvidence(section.id)}
                          className="flex items-center gap-1"
                        >
                          <UploadSimple size={16} />
                          Upload Evidence
                        </Button>
                        <Button 
                          variant="outline" 
                          size="sm" 
                          onClick={() => handleCreateDataPoint(section.id)}
                          className="flex items-center gap-1"
                        >
                          <Plus size={16} />
                          Add Data Point
                        </Button>
                      </div>
                    </div>
                    {sectionDataPoints.map(dp => (
                      <div 
                        key={dp.id} 
                        className="border border-border rounded-lg p-3 hover:bg-accent cursor-pointer transition-colors"
                        role="button"
                        tabIndex={0}
                        onClick={() => handleOpenDataItem(dp)}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter' || e.key === ' ') {
                            e.preventDefault()
                            handleOpenDataItem(dp)
                          }
                        }}
                      >
                        <div className="flex items-start justify-between gap-2 mb-1">
                          <h5 className="font-medium text-sm">{dp.title}</h5>
                          <div className="flex gap-1 flex-shrink-0">
                            <Badge variant="outline" className="text-xs capitalize">{dp.type}</Badge>
                            {dp.classification && (
                              <Badge className={`${getClassificationColor(dp.classification)} text-xs`}>
                                {dp.classification}
                              </Badge>
                            )}
                          </div>
                        </div>
                        <p className="text-xs text-muted-foreground line-clamp-2">{dp.content}</p>
                        {dp.value && (
                          <p className="text-xs font-mono mt-1">
                            Value: {dp.value} {dp.unit && `(${dp.unit})`}
                          </p>
                        )}
                      </div>
                    ))}
                  </div>
                ) : (
                  <div>
                    <Alert>
                      <Info size={16} />
                      <AlertDescription>
                        No data items have been added to this section yet.
                      </AlertDescription>
                    </Alert>
                    <div className="mt-2 flex gap-2">
                      <Button 
                        variant="outline" 
                        size="sm" 
                        onClick={() => handleUploadEvidence(section.id)}
                        className="flex items-center gap-1"
                      >
                        <UploadSimple size={16} />
                        Upload Evidence
                      </Button>
                      <Button 
                        variant="outline" 
                        size="sm" 
                        onClick={() => handleCreateDataPoint(section.id)}
                        className="flex items-center gap-1"
                      >
                        <Plus size={16} />
                        Add First Data Point
                      </Button>
                    </div>
                  </div>
                )}

                {sectionGaps.length > 0 && (
                  <div className="space-y-2 pt-2">
                    <h4 className="text-sm font-semibold text-muted-foreground mb-2 flex items-center gap-1">
                      <WarningCircle size={16} />
                      Data Gaps
                    </h4>
                    {sectionGaps.map(gap => (
                      <div key={gap.id} className="border border-amber-200 bg-amber-50 rounded-lg p-2">
                        <div className="flex items-start justify-between gap-2">
                          <p className="text-xs font-medium">{gap.title}</p>
                          <Badge 
                            variant={gap.impact === 'high' ? 'destructive' : gap.impact === 'medium' ? 'default' : 'secondary'}
                            className="text-xs"
                          >
                            {gap.impact}
                          </Badge>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          )
        })}
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold tracking-tight text-foreground">
          Data Collection Workspace
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          View and manage ESG data by Environmental, Social, and Governance categories
        </p>
      </div>

      {activePeriod ? (
        <>
          <Card className="border-blue-200 bg-blue-50">
            <CardHeader className="pb-3">
              <div className="flex items-center gap-2">
                <Clock size={18} className="text-blue-600" />
                <CardTitle className="text-base">Active Reporting Period</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold">{activePeriod.name}</p>
                  <p className="text-sm text-muted-foreground">
                    {activePeriod.startDate} to {activePeriod.endDate}
                  </p>
                </div>
                <div className="text-right">
                  <Badge variant="outline" className="capitalize">
                    {activePeriod.reportingMode}
                  </Badge>
                </div>
              </div>
            </CardContent>
          </Card>

          <div className="flex items-center gap-2">
            <Button
              variant={showMyItemsOnly ? "default" : "outline"}
              size="sm"
              onClick={() => setShowMyItemsOnly(!showMyItemsOnly)}
              className="flex items-center gap-2"
            >
              <User size={16} />
              {showMyItemsOnly ? 'Showing My Items' : 'Show My Items Only'}
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsImportDialogOpen(true)}
              className="flex items-center gap-2"
            >
              <FileArrowDown size={16} />
              Import from CSV
            </Button>
            {showMyItemsOnly && (
              <p className="text-sm text-muted-foreground">
                Filtering data items where you are owner or contributor
              </p>
            )}
          </div>

          <Tabs value={activeCategory} onValueChange={(v) => setActiveCategory(v as 'environmental' | 'social' | 'governance')} className="space-y-4">
            <TabsList className="grid w-full grid-cols-3">
              <TabsTrigger value="environmental" className="flex items-center gap-2">
                <Leaf size={16} weight="duotone" />
                Environmental ({environmentalSections.length})
              </TabsTrigger>
              <TabsTrigger value="social" className="flex items-center gap-2">
                <Users size={16} weight="duotone" />
                Social ({socialSections.length})
              </TabsTrigger>
              <TabsTrigger value="governance" className="flex items-center gap-2">
                <Briefcase size={16} weight="duotone" />
                Governance ({governanceSections.length})
              </TabsTrigger>
            </TabsList>

            <TabsContent value="environmental" className="space-y-4">
              {renderCategoryContent(environmentalSections)}
            </TabsContent>

            <TabsContent value="social" className="space-y-4">
              {renderCategoryContent(socialSections)}
            </TabsContent>

            <TabsContent value="governance" className="space-y-4">
              {renderCategoryContent(governanceSections)}
            </TabsContent>
          </Tabs>
        </>
      ) : (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <FileText size={48} weight="duotone" className="text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No Active Reporting Period</h3>
            <p className="text-sm text-muted-foreground text-center max-w-md">
              Create a reporting period to start collecting ESG data.
            </p>
          </CardContent>
        </Card>
      )}

      {/* Data Item Detail Dialog */}
      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
          {selectedDataItem ? (
            <>
              <DialogHeader>
                <DialogTitle className="flex items-center gap-2">
                  {selectedDataItem.title}
                  {selectedDataItem.classification && (
                    <Badge className={getClassificationColor(selectedDataItem.classification)}>
                      {selectedDataItem.classification}
                    </Badge>
                  )}
                </DialogTitle>
                <DialogDescription>
                  <Badge variant="outline" className="capitalize">
                    {selectedDataItem.type}
                  </Badge>
                </DialogDescription>
              </DialogHeader>

              <div className="space-y-4 py-4">
                <div>
                  <h4 className="text-sm font-semibold mb-2">Content</h4>
                  <p className="text-sm text-muted-foreground">{selectedDataItem.content}</p>
                </div>

                {selectedDataItem.value && (
                  <div>
                    <h4 className="text-sm font-semibold mb-2">Value</h4>
                    <p className="text-lg font-mono">
                      {selectedDataItem.value} {selectedDataItem.unit && <span className="text-sm text-muted-foreground">({selectedDataItem.unit})</span>}
                    </p>
                  </div>
                )}

                <Separator />

                <div>
                  <h4 className="text-sm font-semibold mb-2 flex items-center gap-1">
                    <Info size={16} />
                    Metadata
                  </h4>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Owner:</span>
                      <span className="font-medium">
                        {users?.find(u => u.id === selectedDataItem.ownerId)?.name || 'Unknown'}
                      </span>
                    </div>
                    {selectedDataItem.contributorIds && selectedDataItem.contributorIds.length > 0 && (
                      <div className="flex flex-col gap-1">
                        <span className="text-muted-foreground">Contributors:</span>
                        <div className="flex flex-wrap gap-1">
                          {selectedDataItem.contributorIds.map(contributorId => {
                            const contributor = users?.find(u => u.id === contributorId)
                            return contributor ? (
                              <Badge key={contributorId} variant="outline" className="text-xs">
                                {contributor.name}
                              </Badge>
                            ) : null
                          })}
                        </div>
                      </div>
                    )}
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Type:</span>
                      <span className="font-medium capitalize">{selectedDataItem.type}</span>
                    </div>
                    {selectedDataItem.classification && (
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">Classification:</span>
                        <span className="font-medium capitalize">{selectedDataItem.classification}</span>
                      </div>
                    )}
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Source:</span>
                      <span className="font-medium">{selectedDataItem.source}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Information Type:</span>
                      <span className="font-medium capitalize">{selectedDataItem.informationType}</span>
                    </div>
                    {selectedDataItem.assumptions && (
                      <div className="flex flex-col gap-1">
                        <span className="text-muted-foreground">Assumptions:</span>
                        <span className="font-medium text-sm bg-amber-50 p-2 rounded border border-amber-200">
                          {selectedDataItem.assumptions}
                        </span>
                      </div>
                    )}
                    <div className="flex justify-between items-center">
                      <span className="text-muted-foreground">Completeness:</span>
                      <Badge className={`${getCompletenessStatusColor(selectedDataItem.completenessStatus)} capitalize border`}>
                        {selectedDataItem.completenessStatus}
                      </Badge>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Created:</span>
                      <span className="font-medium">
                        {new Date(selectedDataItem.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Last Updated:</span>
                      <span className="font-medium">
                        {new Date(selectedDataItem.updatedAt).toLocaleDateString()}
                      </span>
                    </div>
                  </div>
                </div>

                <Separator />

                <DataPointEvidenceManager
                  dataPoint={selectedDataItem}
                  allEvidence={evidence || []}
                  onLinkEvidence={handleLinkEvidence}
                  onUnlinkEvidence={handleUnlinkEvidence}
                />

                <Separator />

                <div>
                  <h4 className="text-sm font-semibold mb-2 flex items-center gap-1">
                    <Clock size={16} />
                    History
                  </h4>
                  <div className="space-y-2 text-sm">
                    <div className="border-l-2 border-muted pl-3 py-2">
                      <p className="font-medium">Created</p>
                      <p className="text-xs text-muted-foreground">
                        {new Date(selectedDataItem.createdAt).toLocaleString()}
                      </p>
                    </div>
                    {selectedDataItem.updatedAt !== selectedDataItem.createdAt && (
                      <div className="border-l-2 border-muted pl-3 py-2">
                        <p className="font-medium">Last Modified</p>
                        <p className="text-xs text-muted-foreground">
                          {new Date(selectedDataItem.updatedAt).toLocaleString()}
                        </p>
                      </div>
                    )}
                  </div>
                </div>
              </div>

              <DialogFooter>
                <Button variant="outline" onClick={() => setIsDetailOpen(false)}>
                  Close
                </Button>
                <Button onClick={() => handleEditDataPoint(selectedDataItem)}>
                  Edit Data Point
                </Button>
              </DialogFooter>
            </>
          ) : null}
        </DialogContent>
      </Dialog>

      {/* Data Point Form Dialog */}
      <Dialog open={isFormOpen} onOpenChange={setIsFormOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>
              {editingDataPoint ? 'Edit Data Point' : 'Create Data Point'}
            </DialogTitle>
            <DialogDescription>
              {editingDataPoint 
                ? 'Update the data point with required metadata for auditability.' 
                : 'Add a new data point with required metadata for auditability.'}
            </DialogDescription>
          </DialogHeader>
          {submitError && (
            <Alert variant="destructive">
              <WarningCircle size={16} className="h-4 w-4" />
              <AlertDescription>{submitError}</AlertDescription>
            </Alert>
          )}
          {selectedSectionId && (
            <DataPointForm
              sectionId={selectedSectionId}
              ownerId={currentUser.id}
              availableUsers={users || []}
              dataPoint={editingDataPoint || undefined}
              onSubmit={handleFormSubmit}
              onCancel={handleFormCancel}
              isSubmitting={isSubmitting}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Evidence Upload Dialog */}
      <Dialog open={isEvidenceUploadOpen} onOpenChange={setIsEvidenceUploadOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Upload Evidence</DialogTitle>
            <DialogDescription>
              Upload a file or provide a reference URL to support your ESG disclosures.
            </DialogDescription>
          </DialogHeader>
          {selectedSectionId && (
            <EvidenceUploadForm
              sectionId={selectedSectionId}
              uploadedBy={currentUser.id}
              onSubmit={handleEvidenceSubmit}
              onCancel={() => {
                setIsEvidenceUploadOpen(false)
                setSelectedSectionId(null)
              }}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Import Data Dialog */}
      <ImportDataDialog
        open={isImportDialogOpen}
        onOpenChange={setIsImportDialogOpen}
        onImportComplete={handleImportComplete}
      />
    </div>
  )
}
