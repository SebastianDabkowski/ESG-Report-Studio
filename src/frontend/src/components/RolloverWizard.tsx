import { useState } from 'react'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Checkbox } from '@/components/ui/checkbox'
import { Badge } from '@/components/ui/badge'
import { ArrowRight, CheckCircle, Clock, FileText, Database, Paperclip } from '@phosphor-icons/react'
import type { ReportingPeriod, User, RolloverOptions, ReportingMode, ReportScope, RolloverReconciliation, InactiveOwnerWarning } from '@/lib/types'
import { rolloverPeriod } from '@/lib/api'
import RolloverReconciliationReport from './RolloverReconciliationReport'

interface RolloverWizardProps {
  isOpen: boolean
  onClose: () => void
  periods: ReportingPeriod[]
  currentUser: User
  onSuccess: () => void
}

type WizardStep = 'select-source' | 'configure-target' | 'select-options' | 'review' | 'complete'

export default function RolloverWizard({ isOpen, onClose, periods, currentUser, onSuccess }: RolloverWizardProps) {
  const [step, setStep] = useState<WizardStep>('select-source')
  const [sourcePeriodId, setSourcePeriodId] = useState<string>('')
  const [targetPeriodName, setTargetPeriodName] = useState<string>('')
  const [targetPeriodStartDate, setTargetPeriodStartDate] = useState<string>('')
  const [targetPeriodEndDate, setTargetPeriodEndDate] = useState<string>('')
  const [targetReportingMode, setTargetReportingMode] = useState<ReportingMode>('simplified')
  const [targetReportScope, setTargetReportScope] = useState<ReportScope>('single-company')
  const [copyStructure, setCopyStructure] = useState<boolean>(true)
  const [copyDisclosures, setCopyDisclosures] = useState<boolean>(true)
  const [copyDataValues, setCopyDataValues] = useState<boolean>(false)
  const [copyAttachments, setCopyAttachments] = useState<boolean>(false)
  const [dueDateAdjustmentDays, setDueDateAdjustmentDays] = useState<number>(0)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [reconciliation, setReconciliation] = useState<RolloverReconciliation | null>(null)
  const [inactiveOwnerWarnings, setInactiveOwnerWarnings] = useState<InactiveOwnerWarning[] | null>(null)

  const sourcePeriod = periods.find(p => p.id === sourcePeriodId)
  
  // Filter out draft periods (governance requirement)
  const eligiblePeriods = periods.filter(p => p.status !== 'draft')

  const handleClose = () => {
    setStep('select-source')
    setSourcePeriodId('')
    setTargetPeriodName('')
    setTargetPeriodStartDate('')
    setTargetPeriodEndDate('')
    setTargetReportingMode('simplified')
    setTargetReportScope('single-company')
    setCopyStructure(true)
    setCopyDisclosures(true)
    setCopyDataValues(false)
    setCopyAttachments(false)
    setDueDateAdjustmentDays(0)
    setError(null)
    setReconciliation(null)
    setInactiveOwnerWarnings(null)
    onClose()
  }

  const handleNext = () => {
    setError(null)
    
    if (step === 'select-source') {
      if (!sourcePeriodId) {
        setError('Please select a source period')
        return
      }
      
      // Pre-fill target period details from source
      if (sourcePeriod) {
        setTargetReportingMode(sourcePeriod.reportingMode)
        setTargetReportScope(sourcePeriod.reportScope)
      }
      
      setStep('configure-target')
    } else if (step === 'configure-target') {
      if (!targetPeriodName || !targetPeriodStartDate || !targetPeriodEndDate) {
        setError('Please fill in all target period details')
        return
      }
      
      // Validate dates
      const start = new Date(targetPeriodStartDate)
      const end = new Date(targetPeriodEndDate)
      if (start >= end) {
        setError('Start date must be before end date')
        return
      }
      
      setStep('select-options')
    } else if (step === 'select-options') {
      setStep('review')
    }
  }

  const handleBack = () => {
    setError(null)
    
    if (step === 'configure-target') {
      setStep('select-source')
    } else if (step === 'select-options') {
      setStep('configure-target')
    } else if (step === 'review') {
      setStep('select-options')
    }
  }

  const handleSubmit = async () => {
    setError(null)
    setIsSubmitting(true)

    try {
      const options: RolloverOptions = {
        copyStructure,
        copyDisclosures,
        copyDataValues,
        copyAttachments,
        dueDateAdjustmentDays: dueDateAdjustmentDays > 0 ? dueDateAdjustmentDays : undefined
      }

      const result = await rolloverPeriod({
        sourcePeriodId,
        targetPeriodName,
        targetPeriodStartDate,
        targetPeriodEndDate,
        targetReportingMode,
        targetReportScope,
        options,
        performedBy: currentUser.id
      })

      // Store reconciliation and warnings if available
      if (result.reconciliation) {
        setReconciliation(result.reconciliation)
      }
      
      if (result.inactiveOwnerWarnings && result.inactiveOwnerWarnings.length > 0) {
        setInactiveOwnerWarnings(result.inactiveOwnerWarnings)
      }
      
      setStep('complete')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to perform rollover')
    } finally {
      setIsSubmitting(false)
    }
  }

  const renderStepIndicator = () => {
    const steps = [
      { key: 'select-source', label: 'Source' },
      { key: 'configure-target', label: 'Target' },
      { key: 'select-options', label: 'Options' },
      { key: 'review', label: 'Review' }
    ]

    const currentStepIndex = steps.findIndex(s => s.key === step)

    return (
      <div className="flex items-center justify-between mb-6">
        {steps.map((s, index) => (
          <div key={s.key} className="flex items-center">
            <div className="flex items-center">
              <div className={`flex items-center justify-center w-8 h-8 rounded-full ${
                index < currentStepIndex ? 'bg-green-500 text-white' :
                index === currentStepIndex ? 'bg-blue-500 text-white' :
                'bg-gray-200 text-gray-500'
              }`}>
                {index < currentStepIndex ? (
                  <CheckCircle weight="fill" className="w-5 h-5" />
                ) : (
                  <span className="text-sm font-medium">{index + 1}</span>
                )}
              </div>
              <span className={`ml-2 text-sm font-medium ${
                index <= currentStepIndex ? 'text-gray-900' : 'text-gray-400'
              }`}>
                {s.label}
              </span>
            </div>
            {index < steps.length - 1 && (
              <ArrowRight className="w-4 h-4 mx-4 text-gray-400" />
            )}
          </div>
        ))}
      </div>
    )
  }

  const renderStepContent = () => {
    if (step === 'select-source') {
      return (
        <div className="space-y-4">
          <div>
            <Label htmlFor="source-period">Select Source Period</Label>
            <p className="text-sm text-muted-foreground mb-2">
              Choose the period to copy from. Only stable (non-draft) periods are available.
            </p>
            <Select value={sourcePeriodId} onValueChange={setSourcePeriodId}>
              <SelectTrigger id="source-period">
                <SelectValue placeholder="Select a period..." />
              </SelectTrigger>
              <SelectContent>
                {eligiblePeriods.map((period) => (
                  <SelectItem key={period.id} value={period.id}>
                    {period.name} ({period.startDate} to {period.endDate})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          
          {sourcePeriod && (
            <div className="rounded-md border p-3 bg-muted">
              <h4 className="font-medium mb-2">Source Period Details</h4>
              <div className="grid grid-cols-2 gap-2 text-sm">
                <div>
                  <span className="text-muted-foreground">Name:</span>
                  <span className="ml-2 font-medium">{sourcePeriod.name}</span>
                </div>
                <div>
                  <span className="text-muted-foreground">Reporting Mode:</span>
                  <span className="ml-2">
                    <Badge variant="outline">{sourcePeriod.reportingMode}</Badge>
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground">Start Date:</span>
                  <span className="ml-2">{sourcePeriod.startDate}</span>
                </div>
                <div>
                  <span className="text-muted-foreground">End Date:</span>
                  <span className="ml-2">{sourcePeriod.endDate}</span>
                </div>
                <div>
                  <span className="text-muted-foreground">Scope:</span>
                  <span className="ml-2">
                    <Badge variant="outline">{sourcePeriod.reportScope}</Badge>
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground">Status:</span>
                  <span className="ml-2">
                    <Badge>{sourcePeriod.status}</Badge>
                  </span>
                </div>
              </div>
            </div>
          )}
        </div>
      )
    }

    if (step === 'configure-target') {
      return (
        <div className="space-y-4">
          <div>
            <Label htmlFor="target-name">Target Period Name</Label>
            <Input
              id="target-name"
              value={targetPeriodName}
              onChange={(e) => setTargetPeriodName(e.target.value)}
              placeholder="e.g., FY2025, Q1 2025"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="target-start">Start Date</Label>
              <Input
                id="target-start"
                type="date"
                value={targetPeriodStartDate}
                onChange={(e) => setTargetPeriodStartDate(e.target.value)}
              />
            </div>
            <div>
              <Label htmlFor="target-end">End Date</Label>
              <Input
                id="target-end"
                type="date"
                value={targetPeriodEndDate}
                onChange={(e) => setTargetPeriodEndDate(e.target.value)}
              />
            </div>
          </div>

          <div>
            <Label htmlFor="target-mode">Reporting Mode</Label>
            <Select value={targetReportingMode} onValueChange={(value) => setTargetReportingMode(value as ReportingMode)}>
              <SelectTrigger id="target-mode">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="simplified">Simplified</SelectItem>
                <SelectItem value="extended">Extended (CSRD/ESRS)</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div>
            <Label htmlFor="target-scope">Report Scope</Label>
            <Select value={targetReportScope} onValueChange={(value) => setTargetReportScope(value as ReportScope)}>
              <SelectTrigger id="target-scope">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="single-company">Single Company</SelectItem>
                <SelectItem value="group">Group</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      )
    }

    if (step === 'select-options') {
      return (
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground mb-4">
            Select what content to copy from the source period to the target period.
          </p>

          <div className="space-y-3">
            <div className="flex items-start space-x-3 rounded-md border p-3">
              <Checkbox
                id="copy-structure"
                checked={copyStructure}
                onCheckedChange={(checked) => setCopyStructure(checked === true)}
                disabled
              />
              <div className="flex-1">
                <label htmlFor="copy-structure" className="flex items-center text-sm font-medium cursor-pointer">
                  <FileText className="w-4 h-4 mr-2" />
                  Copy Structure (Required)
                </label>
                <p className="text-xs text-muted-foreground mt-1">
                  Sections, titles, descriptions, and ownership assignments
                </p>
              </div>
            </div>

            <div className="flex items-start space-x-3 rounded-md border p-3">
              <Checkbox
                id="copy-disclosures"
                checked={copyDisclosures}
                onCheckedChange={(checked) => setCopyDisclosures(checked === true)}
                disabled={!copyStructure}
              />
              <div className="flex-1">
                <label htmlFor="copy-disclosures" className="flex items-center text-sm font-medium cursor-pointer">
                  <Clock className="w-4 h-4 mr-2" />
                  Copy Disclosures
                </label>
                <p className="text-xs text-muted-foreground mt-1">
                  Open gaps, active assumptions, and active remediation plans
                </p>
              </div>
            </div>

            <div className="flex items-start space-x-3 rounded-md border p-3">
              <Checkbox
                id="copy-data"
                checked={copyDataValues}
                onCheckedChange={(checked) => {
                  setCopyDataValues(checked === true)
                  if (!checked) setCopyAttachments(false)
                }}
                disabled={!copyStructure}
              />
              <div className="flex-1">
                <label htmlFor="copy-data" className="flex items-center text-sm font-medium cursor-pointer">
                  <Database className="w-4 h-4 mr-2" />
                  Copy Data Values
                </label>
                <p className="text-xs text-muted-foreground mt-1">
                  Data points, narratives, and metrics
                </p>
              </div>
            </div>

            <div className="flex items-start space-x-3 rounded-md border p-3">
              <Checkbox
                id="copy-attachments"
                checked={copyAttachments}
                onCheckedChange={(checked) => setCopyAttachments(checked === true)}
                disabled={!copyDataValues}
              />
              <div className="flex-1">
                <label htmlFor="copy-attachments" className="flex items-center text-sm font-medium cursor-pointer">
                  <Paperclip className="w-4 h-4 mr-2" />
                  Copy Attachments
                </label>
                <p className="text-xs text-muted-foreground mt-1">
                  Evidence files and supporting documents
                </p>
              </div>
            </div>
          </div>

          {copyDisclosures && (
            <div className="rounded-md border p-4 bg-blue-50">
              <Label htmlFor="due-date-adjustment" className="text-sm font-medium">
                Due Date Adjustment (Days)
              </Label>
              <p className="text-xs text-muted-foreground mt-1 mb-2">
                Automatically adjust task due dates when carrying forward remediation actions. Enter the number of days to add to existing due dates (e.g., 365 for one year).
              </p>
              <Input
                id="due-date-adjustment"
                type="number"
                min="0"
                value={dueDateAdjustmentDays}
                onChange={(e) => setDueDateAdjustmentDays(parseInt(e.target.value) || 0)}
                placeholder="0"
              />
            </div>
          )}

          {!copyStructure && (
            <p className="text-xs text-amber-600 bg-amber-50 border border-amber-200 rounded p-2">
              Structure copy is required. Other options depend on it.
            </p>
          )}
        </div>
      )
    }

    if (step === 'review') {
      return (
        <div className="space-y-4">
          <div className="rounded-md border p-4 bg-muted">
            <h4 className="font-medium mb-3">Rollover Summary</h4>
            
            <div className="space-y-3">
              <div>
                <span className="text-sm text-muted-foreground">From:</span>
                <p className="font-medium">{sourcePeriod?.name}</p>
              </div>
              
              <div>
                <span className="text-sm text-muted-foreground">To:</span>
                <p className="font-medium">{targetPeriodName}</p>
                <p className="text-sm text-muted-foreground">
                  {targetPeriodStartDate} to {targetPeriodEndDate}
                </p>
              </div>
              
              <div>
                <span className="text-sm text-muted-foreground">What will be copied:</span>
                <ul className="mt-2 space-y-1">
                  <li className="flex items-center text-sm">
                    <CheckCircle className="w-4 h-4 mr-2 text-green-500" weight="fill" />
                    Section structure and ownership
                  </li>
                  {copyDisclosures && (
                    <li className="flex items-center text-sm">
                      <CheckCircle className="w-4 h-4 mr-2 text-green-500" weight="fill" />
                      Open gaps, active assumptions, and remediation plans
                    </li>
                  )}
                  {copyDataValues && (
                    <li className="flex items-center text-sm">
                      <CheckCircle className="w-4 h-4 mr-2 text-green-500" weight="fill" />
                      Data points and values
                    </li>
                  )}
                  {copyAttachments && (
                    <li className="flex items-center text-sm">
                      <CheckCircle className="w-4 h-4 mr-2 text-green-500" weight="fill" />
                      Evidence files and attachments
                    </li>
                  )}
                </ul>
              </div>
            </div>
          </div>

          <div className="rounded-md border border-amber-200 bg-amber-50 p-3">
            <p className="text-sm text-amber-800">
              <strong>Note:</strong> This action will create a new reporting period with the selected content.
              The source period will remain unchanged.
            </p>
          </div>
        </div>
      )
    }

    if (step === 'complete') {
      return (
        <div className="space-y-4">
          <div className="rounded-md border border-green-200 bg-green-50 p-4 mb-4">
            <div className="flex items-center gap-2">
              <CheckCircle className="text-green-600" weight="fill" size={24} />
              <div>
                <h4 className="font-medium text-green-900">Rollover Completed Successfully</h4>
                <p className="text-sm text-green-700">
                  The new period "{targetPeriodName}" has been created.
                </p>
              </div>
            </div>
          </div>

          {inactiveOwnerWarnings && inactiveOwnerWarnings.length > 0 && (
            <div className="rounded-md border border-amber-200 bg-amber-50 p-4 mb-4">
              <div className="flex items-start gap-2">
                <div className="flex-shrink-0">
                  <svg className="w-5 h-5 text-amber-600" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                  </svg>
                </div>
                <div className="flex-1">
                  <h4 className="font-medium text-amber-900 mb-2">Inactive Owner Warnings</h4>
                  <p className="text-sm text-amber-700 mb-3">
                    The following items have been assigned to inactive users. Please reassign these items before proceeding:
                  </p>
                  <div className="space-y-2 max-h-48 overflow-y-auto">
                    {inactiveOwnerWarnings.map((warning, index) => (
                      <div key={index} className="text-sm bg-white rounded p-2 border border-amber-200">
                        <div className="font-medium text-amber-900">{warning.entityType}: {warning.entityTitle}</div>
                        <div className="text-amber-700">Owner: {warning.userName} (inactive)</div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          )}

          {reconciliation && <RolloverReconciliationReport reconciliation={reconciliation} />}
        </div>
      )
    }

    return null
  }

  return (
    <Dialog open={isOpen} onOpenChange={handleClose}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Rollover Reporting Period</DialogTitle>
          <DialogDescription>
            Create a new reporting period by copying selected content from an existing period
          </DialogDescription>
        </DialogHeader>

        {renderStepIndicator()}

        <div className="py-4">
          {renderStepContent()}
          
          {error && (
            <div className="mt-4 rounded-md border border-red-200 bg-red-50 p-3">
              <p className="text-sm text-red-800">{error}</p>
            </div>
          )}
        </div>

        <DialogFooter>
          {step !== 'select-source' && step !== 'complete' && (
            <Button variant="outline" onClick={handleBack} disabled={isSubmitting}>
              Back
            </Button>
          )}
          
          {step === 'complete' ? (
            <Button onClick={() => { handleClose(); onSuccess(); }}>
              Close
            </Button>
          ) : step !== 'review' ? (
            <Button onClick={handleNext}>
              Next
            </Button>
          ) : (
            <Button onClick={handleSubmit} disabled={isSubmitting}>
              {isSubmitting ? 'Creating...' : 'Create Period'}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
