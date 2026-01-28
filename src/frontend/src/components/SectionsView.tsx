import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Progress } from '@/components/ui/progress'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Checkbox } from '@/components/ui/checkbox'
import { useKV } from '@github/spark/hooks'
import { Plus, CheckCircle, WarningCircle, Target, Article, Lightbulb, FileText, PaperclipHorizontal, UserCircle, Users, CheckSquare, Square } from '@phosphor-icons/react'
import type { User, ReportingPeriod, SectionSummary, DataPoint, Gap, Classification, ContentType } from '@/lib/types'
import { getStatusColor, getStatusBorderColor, getClassificationColor, getCompletenessStatusColor, canApproveSection, canEditSection, generateId, calculateCompleteness } from '@/lib/helpers'
import { updateSectionOwner, bulkUpdateSectionOwner, getUsers, type BulkUpdateFailure } from '@/lib/api'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

interface SectionsViewProps {
  currentUser: User
}

export default function SectionsView({ currentUser }: SectionsViewProps) {
  const [periods] = useKV<ReportingPeriod[]>('reporting-periods', [])
  const [sections, setSections] = useKV<SectionSummary[]>('section-summaries', [])
  const [dataPoints, setDataPoints] = useKV<DataPoint[]>('data-points', [])
  const [gaps, setGaps] = useKV<Gap[]>('gaps', [])
  
  const [selectedSection, setSelectedSection] = useState<SectionSummary | null>(null)
  const [selectedSectionIds, setSelectedSectionIds] = useState<Set<string>>(new Set())
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [isAddDataOpen, setIsAddDataOpen] = useState(false)
  const [isAddGapOpen, setIsAddGapOpen] = useState(false)
  const [isChangeOwnerOpen, setIsChangeOwnerOpen] = useState(false)
  const [isBulkChangeOwnerOpen, setIsBulkChangeOwnerOpen] = useState(false)
  
  const [dataTitle, setDataTitle] = useState('')
  const [dataContent, setDataContent] = useState('')
  const [dataType, setDataType] = useState<ContentType>('narrative')
  const [dataClassification, setDataClassification] = useState<Classification>('fact')
  
  const [gapTitle, setGapTitle] = useState('')
  const [gapDescription, setGapDescription] = useState('')
  const [gapImpact, setGapImpact] = useState<'low' | 'medium' | 'high'>('medium')
  
  const [newOwnerId, setNewOwnerId] = useState('')
  const [ownerChangeNote, setOwnerChangeNote] = useState('')
  const [ownerChangeError, setOwnerChangeError] = useState<string | null>(null)
  const [bulkUpdateResult, setBulkUpdateResult] = useState<{ updated: number; skipped: BulkUpdateFailure[] } | null>(null)

  const queryClient = useQueryClient()
  
  // Fetch users for owner selection
  const { data: users = [], isLoading: usersLoading } = useQuery({
    queryKey: ['users'],
    queryFn: getUsers
  })
  
  // Mutation for updating section owner
  const updateOwnerMutation = useMutation({
    mutationFn: ({ sectionId, ownerId, changeNote }: { sectionId: string; ownerId: string; changeNote?: string }) =>
      updateSectionOwner(sectionId, {
        ownerId,
        updatedBy: currentUser.id,
        changeNote
      }),
    onSuccess: (updatedSection) => {
      // Update local state
      setSections((current) => {
        const updated = current || []
        return updated.map(s => {
          if (s.id === updatedSection.id) {
            const newOwner = users.find(u => u.id === updatedSection.ownerId)
            return { 
              ...s, 
              ownerId: updatedSection.ownerId,
              ownerName: newOwner?.name || updatedSection.ownerId
            }
          }
          return s
        })
      })
      
      // Invalidate queries to refresh audit log
      queryClient.invalidateQueries({ queryKey: ['audit-log'] })
      
      // Close dialog and reset state
      setIsChangeOwnerOpen(false)
      setNewOwnerId('')
      setOwnerChangeNote('')
      setOwnerChangeError(null)
    },
    onError: (error: Error) => {
      // Display error to user
      setOwnerChangeError(error.message || 'Failed to update section owner. Please try again.')
    }
  })

  // Mutation for bulk updating section owners
  const bulkUpdateOwnerMutation = useMutation({
    mutationFn: ({ sectionIds, ownerId, changeNote }: { sectionIds: string[]; ownerId: string; changeNote?: string }) =>
      bulkUpdateSectionOwner({
        sectionIds,
        ownerId,
        updatedBy: currentUser.id,
        changeNote
      }),
    onSuccess: (result) => {
      // Update local state for all successfully updated sections
      setSections((current) => {
        const updated = current || []
        return updated.map(s => {
          const updatedSection = result.updatedSections.find(us => us.id === s.id)
          if (updatedSection) {
            const newOwner = users.find(u => u.id === updatedSection.ownerId)
            return { 
              ...s, 
              ownerId: updatedSection.ownerId,
              ownerName: newOwner?.name || updatedSection.ownerId
            }
          }
          return s
        })
      })
      
      // Store result for display
      setBulkUpdateResult({
        updated: result.updatedSections.length,
        skipped: result.skippedSections
      })
      
      // Clear selection
      setSelectedSectionIds(new Set())
      
      // Invalidate queries to refresh audit log
      queryClient.invalidateQueries({ queryKey: ['audit-log'] })
    },
    onError: (error: Error) => {
      // Display error to user
      setOwnerChangeError(error.message || 'Failed to update section owners. Please try again.')
    }
  })

  const activePeriod = periods?.find(p => p.status === 'active')
  const activeSections = sections?.filter(s => activePeriod && s.periodId === activePeriod.id) || []
  
  const sectionDataPoints = selectedSection 
    ? dataPoints?.filter(d => d.sectionId === selectedSection.id) || []
    : []
  
  const sectionGaps = selectedSection
    ? gaps?.filter(g => g.sectionId === selectedSection.id) || []
    : []

  const handleOpenDetail = (section: SectionSummary) => {
    setSelectedSection(section)
    setIsDetailOpen(true)
  }

  const handleAddData = () => {
    if (!selectedSection) return

    const newDataPoint: DataPoint = {
      id: generateId(),
      sectionId: selectedSection.id,
      type: dataType,
      classification: dataClassification,
      title: dataTitle,
      content: dataContent,
      ownerId: currentUser.id,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      evidenceIds: []
    }

    setDataPoints((current) => [...(current || []), newDataPoint])

    setSections((current) => {
      const updated = current || []
      return updated.map(s => {
        if (s.id === selectedSection.id) {
          const newCount = s.dataPointCount + 1
          const { level, percentage } = calculateCompleteness(newCount, s.evidenceCount, s.gapCount)
          return { ...s, dataPointCount: newCount, completeness: level, completenessPercentage: percentage }
        }
        return s
      })
    })

    setIsAddDataOpen(false)
    setDataTitle('')
    setDataContent('')
    setDataType('narrative')
    setDataClassification('fact')
  }

  const handleAddGap = () => {
    if (!selectedSection) return

    const newGap: Gap = {
      id: generateId(),
      sectionId: selectedSection.id,
      title: gapTitle,
      description: gapDescription,
      impact: gapImpact,
      createdBy: currentUser.id,
      createdAt: new Date().toISOString(),
      resolved: false
    }

    setGaps((current) => [...(current || []), newGap])

    setSections((current) => {
      const updated = current || []
      return updated.map(s => {
        if (s.id === selectedSection.id) {
          const newGapCount = s.gapCount + 1
          const { level, percentage } = calculateCompleteness(s.dataPointCount, s.evidenceCount, newGapCount)
          return { ...s, gapCount: newGapCount, completeness: level, completenessPercentage: percentage }
        }
        return s
      })
    })

    setIsAddGapOpen(false)
    setGapTitle('')
    setGapDescription('')
    setGapImpact('medium')
  }

  const handleApprove = (sectionId: string) => {
    setSections((current) => {
      const updated = current || []
      return updated.map(s => 
        s.id === sectionId 
          ? { ...s, status: 'approved' as const, approvedAt: new Date().toISOString(), approvedBy: currentUser.id }
          : s
      )
    })
  }

  const handleSubmitForReview = (sectionId: string) => {
    setSections((current) => {
      const updated = current || []
      return updated.map(s => 
        s.id === sectionId 
          ? { ...s, status: 'in-review' as const }
          : s
      )
    })
  }

  const handleChangeOwner = () => {
    if (!selectedSection || !newOwnerId) return
    
    // Prevent no-op changes
    if (newOwnerId === selectedSection.ownerId) {
      setOwnerChangeError('The selected user is already the owner of this section.')
      return
    }
    
    // Clear any previous errors
    setOwnerChangeError(null)
    
    updateOwnerMutation.mutate({
      sectionId: selectedSection.id,
      ownerId: newOwnerId,
      changeNote: ownerChangeNote || undefined
    })
  }

  const handleBulkChangeOwner = () => {
    if (selectedSectionIds.size === 0 || !newOwnerId) return
    
    // Clear any previous errors and results
    setOwnerChangeError(null)
    setBulkUpdateResult(null)
    
    // Check if any selected section would actually change
    const selectedSections = activeSections.filter(s => selectedSectionIds.has(s.id))
    const sectionsToUpdate = selectedSections.filter(s => s.ownerId !== newOwnerId)
    
    if (sectionsToUpdate.length === 0) {
      setOwnerChangeError('All selected sections already have this owner.')
      return
    }
    
    bulkUpdateOwnerMutation.mutate({
      sectionIds: Array.from(selectedSectionIds),
      ownerId: newOwnerId,
      changeNote: ownerChangeNote || undefined
    })
  }

  const handleToggleSelection = (sectionId: string) => {
    setSelectedSectionIds(prev => {
      const newSet = new Set(prev)
      if (newSet.has(sectionId)) {
        newSet.delete(sectionId)
      } else {
        newSet.add(sectionId)
      }
      return newSet
    })
  }

  const handleSelectAll = () => {
    if (selectedSectionIds.size === activeSections.length) {
      setSelectedSectionIds(new Set())
    } else {
      setSelectedSectionIds(new Set(activeSections.map(s => s.id)))
    }
  }

  const canChangeOwner = (userRole: string) => {
    return userRole === 'admin' || userRole === 'report-owner'
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-foreground">
            Report Sections
          </h2>
          <p className="text-sm text-muted-foreground mt-1">
            Manage section content, data points, and evidence
          </p>
        </div>
        
        {activeSections.length > 0 && canChangeOwner(currentUser.role) && (
          <div className="flex items-center gap-2">
            <Button 
              variant="outline" 
              size="sm"
              onClick={handleSelectAll}
              className="gap-2"
            >
              {selectedSectionIds.size === activeSections.length ? (
                <CheckSquare size={16} weight="fill" />
              ) : (
                <Square size={16} />
              )}
              {selectedSectionIds.size === activeSections.length ? 'Deselect All' : 'Select All'}
            </Button>
            {selectedSectionIds.size > 0 && (
              <Button 
                size="sm"
                onClick={() => {
                  setNewOwnerId('')
                  setOwnerChangeNote('')
                  setOwnerChangeError(null)
                  setBulkUpdateResult(null)
                  setIsBulkChangeOwnerOpen(true)
                }}
                className="gap-2"
              >
                <Users size={16} weight="bold" />
                Assign Owner ({selectedSectionIds.size})
              </Button>
            )}
          </div>
        )}
      </div>

      {activePeriod ? (
        <div className="grid gap-4">
          {activeSections.map(section => (
            <Card 
              key={section.id} 
              className={`border-l-4 ${getStatusBorderColor(section.status)} hover:shadow-md transition-shadow ${
                selectedSectionIds.has(section.id) ? 'ring-2 ring-primary ring-offset-2' : ''
              }`}
            >
              <CardHeader>
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-3 flex-1">
                    {canChangeOwner(currentUser.role) && (
                      <Checkbox
                        checked={selectedSectionIds.has(section.id)}
                        onCheckedChange={() => handleToggleSelection(section.id)}
                        onClick={(e) => e.stopPropagation()}
                        className="mt-1"
                        aria-label={`Select section ${section.title}`}
                      />
                    )}
                    <div 
                      className="flex-1 cursor-pointer"
                      onClick={() => handleOpenDetail(section)}
                    >
                      <div className="flex items-center gap-2 mb-1">
                        <CardTitle className="text-lg">{section.title}</CardTitle>
                        <Badge className={getStatusColor(section.status)} variant="secondary">
                          {section.status}
                        </Badge>
                        <Badge variant="outline" className="capitalize">
                          {section.category}
                        </Badge>
                      </div>
                      <CardDescription>{section.description}</CardDescription>
                    </div>
                  </div>
                  
                  <div className="text-right flex-shrink-0">
                    <div className="text-sm font-semibold font-mono mb-1">
                      {section.completenessPercentage}%
                    </div>
                    <Progress value={section.completenessPercentage} className="h-2 w-24" />
                  </div>
                </div>
              </CardHeader>
              <CardContent>
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
                  <span className="font-medium">Owner: {section.ownerName}</span>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <FileText size={48} weight="duotone" className="text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">No Active Period</h3>
            <p className="text-sm text-muted-foreground text-center max-w-md">
              Create a reporting period to view and manage sections.
            </p>
          </CardContent>
        </Card>
      )}

      <Dialog open={isDetailOpen} onOpenChange={setIsDetailOpen}>
        <DialogContent className="max-w-3xl max-h-[80vh] overflow-y-auto">
          {selectedSection && (
            <>
              <DialogHeader>
                <DialogTitle className="flex items-center gap-2">
                  {selectedSection.title}
                  <Badge className={getStatusColor(selectedSection.status)}>
                    {selectedSection.status}
                  </Badge>
                </DialogTitle>
                <DialogDescription>{selectedSection.description}</DialogDescription>
              </DialogHeader>

              <div className="space-y-6 py-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium">Completeness</span>
                    <span className="text-sm font-mono font-semibold">{selectedSection.completenessPercentage}%</span>
                  </div>
                  <Progress value={selectedSection.completenessPercentage} className="h-3 w-48" />
                </div>

                <div className="flex items-center justify-between p-3 border border-border rounded-lg bg-muted/30">
                  <div className="flex items-center gap-2">
                    <UserCircle size={20} className="text-muted-foreground" />
                    <div>
                      <span className="text-sm font-medium">Section Owner</span>
                      <p className="text-sm text-muted-foreground">{selectedSection.ownerName}</p>
                    </div>
                  </div>
                  {canChangeOwner(currentUser.role) && (
                    <Button 
                      size="sm" 
                      variant="outline" 
                      onClick={(e) => {
                        e.stopPropagation()
                        setNewOwnerId(selectedSection.ownerId)
                        setOwnerChangeError(null)
                        setIsChangeOwnerOpen(true)
                      }}
                      disabled={usersLoading}
                    >
                      Change Owner
                    </Button>
                  )}
                </div>

                {selectedSection.completenessPercentage < 70 && (
                  <Alert>
                    <WarningCircle size={16} />
                    <AlertDescription>
                      This section needs more data and evidence before it can be approved.
                    </AlertDescription>
                  </Alert>
                )}

                <div>
                  <div className="flex items-center justify-between mb-3">
                    <h4 className="font-semibold flex items-center gap-2">
                      <Target size={18} />
                      Data Points ({sectionDataPoints.length})
                    </h4>
                    {canEditSection(currentUser.role) && selectedSection.status !== 'approved' && (
                      <Button size="sm" onClick={() => setIsAddDataOpen(true)} className="gap-2">
                        <Plus size={14} weight="bold" />
                        Add Data
                      </Button>
                    )}
                  </div>
                  
                  {sectionDataPoints.length > 0 ? (
                    <div className="space-y-2">
                      {sectionDataPoints.map(dp => (
                        <div key={dp.id} className="border border-border rounded-lg p-3">
                          <div className="flex items-start justify-between gap-2 mb-2">
                            <h5 className="font-medium text-sm">{dp.title}</h5>
                            <div className="flex gap-1 flex-shrink-0">
                              <Badge variant="outline" className="text-xs capitalize">{dp.type}</Badge>
                              {dp.classification && (
                                <Badge className={`${getClassificationColor(dp.classification)} text-xs`}>
                                  {dp.classification}
                                </Badge>
                              )}
                              {dp.completenessStatus && (
                                <Badge className={`${getCompletenessStatusColor(dp.completenessStatus)} text-xs capitalize border`}>
                                  {dp.completenessStatus}
                                </Badge>
                              )}
                            </div>
                          </div>
                          <p className="text-xs text-muted-foreground">{dp.content}</p>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground">No data points added yet.</p>
                  )}
                </div>

                <div>
                  <div className="flex items-center justify-between mb-3">
                    <h4 className="font-semibold flex items-center gap-2">
                      <WarningCircle size={18} />
                      Gaps ({sectionGaps.filter(g => !g.resolved).length})
                    </h4>
                    {canEditSection(currentUser.role) && selectedSection.status !== 'approved' && (
                      <Button size="sm" variant="outline" onClick={() => setIsAddGapOpen(true)} className="gap-2">
                        <Plus size={14} weight="bold" />
                        Document Gap
                      </Button>
                    )}
                  </div>
                  
                  {sectionGaps.filter(g => !g.resolved).length > 0 ? (
                    <div className="space-y-2">
                      {sectionGaps.filter(g => !g.resolved).map(gap => (
                        <div key={gap.id} className="border border-border rounded-lg p-3">
                          <div className="flex items-start justify-between gap-2 mb-1">
                            <h5 className="font-medium text-sm">{gap.title}</h5>
                            <Badge 
                              variant={gap.impact === 'high' ? 'destructive' : gap.impact === 'medium' ? 'default' : 'secondary'}
                              className="text-xs"
                            >
                              {gap.impact} impact
                            </Badge>
                          </div>
                          <p className="text-xs text-muted-foreground">{gap.description}</p>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground">No unresolved gaps.</p>
                  )}
                </div>
              </div>

              <DialogFooter>
                {canEditSection(currentUser.role) && selectedSection.status === 'draft' && (
                  <Button onClick={() => handleSubmitForReview(selectedSection.id)}>
                    Submit for Review
                  </Button>
                )}
                {canApproveSection(currentUser.role) && selectedSection.status === 'in-review' && (
                  <Button onClick={() => handleApprove(selectedSection.id)}>
                    <CheckCircle size={16} weight="fill" className="mr-2" />
                    Approve Section
                  </Button>
                )}
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={isAddDataOpen} onOpenChange={setIsAddDataOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Data Point</DialogTitle>
            <DialogDescription>Add narrative, metric, or other content to this section.</DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="data-type">Type</Label>
              <Select value={dataType} onValueChange={(v) => setDataType(v as ContentType)}>
                <SelectTrigger id="data-type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="narrative">Narrative</SelectItem>
                  <SelectItem value="metric">Metric</SelectItem>
                  <SelectItem value="evidence">Evidence Reference</SelectItem>
                  <SelectItem value="assumption">Assumption</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="data-classification">Classification</Label>
              <Select value={dataClassification} onValueChange={(v) => setDataClassification(v as Classification)}>
                <SelectTrigger id="data-classification">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="fact">Fact (verified data)</SelectItem>
                  <SelectItem value="declaration">Declaration (policy or commitment)</SelectItem>
                  <SelectItem value="plan">Plan (future intention)</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="data-title">Title</Label>
              <Input
                id="data-title"
                placeholder="e.g., Total GHG Emissions"
                value={dataTitle}
                onChange={(e) => setDataTitle(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="data-content">Content</Label>
              <Textarea
                id="data-content"
                placeholder="Enter the data point content..."
                value={dataContent}
                onChange={(e) => setDataContent(e.target.value)}
                rows={4}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddDataOpen(false)}>Cancel</Button>
            <Button onClick={handleAddData} disabled={!dataTitle || !dataContent}>
              Add Data Point
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isAddGapOpen} onOpenChange={setIsAddGapOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Document Gap</DialogTitle>
            <DialogDescription>
              Explicitly record missing data or information limitations.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="gap-title">Gap Title</Label>
              <Input
                id="gap-title"
                placeholder="e.g., Missing Scope 3 emissions data"
                value={gapTitle}
                onChange={(e) => setGapTitle(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="gap-description">Description</Label>
              <Textarea
                id="gap-description"
                placeholder="Describe what's missing and why..."
                value={gapDescription}
                onChange={(e) => setGapDescription(e.target.value)}
                rows={3}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="gap-impact">Impact Level</Label>
              <Select value={gapImpact} onValueChange={(v) => setGapImpact(v as 'low' | 'medium' | 'high')}>
                <SelectTrigger id="gap-impact">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="low">Low - Minor omission</SelectItem>
                  <SelectItem value="medium">Medium - Notable gap</SelectItem>
                  <SelectItem value="high">High - Critical missing data</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddGapOpen(false)}>Cancel</Button>
            <Button onClick={handleAddGap} disabled={!gapTitle || !gapDescription}>
              Document Gap
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isChangeOwnerOpen} onOpenChange={setIsChangeOwnerOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Change Section Owner</DialogTitle>
            <DialogDescription>
              Assign a new owner to be accountable for this section. This change will be recorded in the audit log.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {selectedSection && (
              <div className="p-3 border border-border rounded-lg bg-muted/30">
                <p className="text-sm text-muted-foreground mb-1">Current Owner</p>
                <p className="text-sm font-medium">{selectedSection.ownerName}</p>
              </div>
            )}

            {ownerChangeError && (
              <Alert variant="destructive">
                <WarningCircle size={16} />
                <AlertDescription>{ownerChangeError}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="new-owner">New Owner</Label>
              <Select 
                value={newOwnerId} 
                onValueChange={(value) => {
                  setNewOwnerId(value)
                  setOwnerChangeError(null) // Clear error when selection changes
                }}
                disabled={usersLoading}
              >
                <SelectTrigger id="new-owner">
                  <SelectValue placeholder={usersLoading ? "Loading users..." : "Select a user"} />
                </SelectTrigger>
                <SelectContent>
                  {users.map(user => (
                    <SelectItem key={user.id} value={user.id}>
                      {user.name} ({user.role})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="change-note">Change Note (Optional)</Label>
              <Textarea
                id="change-note"
                placeholder="Reason for changing the owner..."
                value={ownerChangeNote}
                onChange={(e) => setOwnerChangeNote(e.target.value)}
                rows={3}
              />
            </div>
          </div>

          <DialogFooter>
            <Button 
              variant="outline" 
              onClick={() => {
                setIsChangeOwnerOpen(false)
                setNewOwnerId('')
                setOwnerChangeNote('')
                setOwnerChangeError(null)
              }}
            >
              Cancel
            </Button>
            <Button 
              onClick={handleChangeOwner} 
              disabled={!newOwnerId || updateOwnerMutation.isPending}
            >
              {updateOwnerMutation.isPending ? 'Updating...' : 'Update Owner'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isBulkChangeOwnerOpen} onOpenChange={(open) => {
        setIsBulkChangeOwnerOpen(open)
        if (!open) {
          setNewOwnerId('')
          setOwnerChangeNote('')
          setOwnerChangeError(null)
          setBulkUpdateResult(null)
        }
      }}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Bulk Assign Owner</DialogTitle>
            <DialogDescription>
              Assign an owner to {selectedSectionIds.size} selected section{selectedSectionIds.size !== 1 ? 's' : ''}. 
              Changes will only be applied to sections you have permission to modify.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {bulkUpdateResult ? (
              <Alert>
                <CheckCircle size={16} />
                <AlertDescription>
                  <div className="space-y-1">
                    <p className="font-medium">Update Complete</p>
                    <p>{bulkUpdateResult.updated} section{bulkUpdateResult.updated !== 1 ? 's' : ''} updated successfully.</p>
                    {bulkUpdateResult.skipped.length > 0 && (
                      <div className="mt-3">
                        <p className="font-medium text-destructive">{bulkUpdateResult.skipped.length} section{bulkUpdateResult.skipped.length !== 1 ? 's' : ''} skipped:</p>
                        <ul className="mt-2 space-y-1 text-sm">
                          {bulkUpdateResult.skipped.map((failure, idx) => (
                            <li key={idx} className="text-muted-foreground">
                              • {activeSections.find(s => s.id === failure.sectionId)?.title || failure.sectionId}: {failure.reason}
                            </li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                </AlertDescription>
              </Alert>
            ) : (
              <>
                {ownerChangeError && (
                  <Alert variant="destructive">
                    <WarningCircle size={16} />
                    <AlertDescription>{ownerChangeError}</AlertDescription>
                  </Alert>
                )}

                <div className="space-y-2">
                  <Label htmlFor="bulk-new-owner">New Owner</Label>
                  <Select 
                    value={newOwnerId} 
                    onValueChange={(value) => {
                      setNewOwnerId(value)
                      setOwnerChangeError(null)
                    }}
                    disabled={usersLoading}
                  >
                    <SelectTrigger id="bulk-new-owner">
                      <SelectValue placeholder={usersLoading ? "Loading users..." : "Select a user"} />
                    </SelectTrigger>
                    <SelectContent>
                      {users.map(user => (
                        <SelectItem key={user.id} value={user.id}>
                          {user.name} ({user.role})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="bulk-change-note">Change Note (Optional)</Label>
                  <Textarea
                    id="bulk-change-note"
                    placeholder="Reason for changing the owners..."
                    value={ownerChangeNote}
                    onChange={(e) => setOwnerChangeNote(e.target.value)}
                    rows={3}
                  />
                </div>

                <div className="p-3 border border-border rounded-lg bg-muted/30">
                  <p className="text-sm text-muted-foreground mb-2">Selected Sections:</p>
                  <ul className="space-y-1 max-h-40 overflow-y-auto">
                    {Array.from(selectedSectionIds).map(id => {
                      const section = activeSections.find(s => s.id === id)
                      return section ? (
                        <li key={id} className="text-sm flex items-center justify-between">
                          <span>{section.title}</span>
                          <span className="text-xs text-muted-foreground">Owner: {section.ownerName}</span>
                        </li>
                      ) : null
                    })}
                  </ul>
                </div>
              </>
            )}
          </div>

          <DialogFooter>
            {bulkUpdateResult ? (
              <Button 
                onClick={() => {
                  setIsBulkChangeOwnerOpen(false)
                  setNewOwnerId('')
                  setOwnerChangeNote('')
                  setOwnerChangeError(null)
                  setBulkUpdateResult(null)
                }}
              >
                Close
              </Button>
            ) : (
              <>
                <Button 
                  variant="outline" 
                  onClick={() => {
                    setIsBulkChangeOwnerOpen(false)
                    setNewOwnerId('')
                    setOwnerChangeNote('')
                    setOwnerChangeError(null)
                  }}
                >
                  Cancel
                </Button>
                <Button 
                  onClick={handleBulkChangeOwner} 
                  disabled={!newOwnerId || bulkUpdateOwnerMutation.isPending}
                >
                  {bulkUpdateOwnerMutation.isPending ? 'Updating...' : `Assign to ${selectedSectionIds.size} Section${selectedSectionIds.size !== 1 ? 's' : ''}`}
                </Button>
              </>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}