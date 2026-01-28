import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Checkbox } from '@/components/ui/checkbox'
import { WarningCircle } from '@phosphor-icons/react'
import type { DataPoint, User, NarrativeSourceReference } from '@/lib/types'
import { SourceReferencesManager } from './SourceReferencesManager'

const dataPointSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  content: z.string().min(1, 'Content is required'),
  type: z.enum(['narrative', 'metric', 'evidence', 'assumption', 'gap']),
  classification: z.enum(['fact', 'declaration', 'plan']).optional(),
  value: z.string().optional(),
  unit: z.string().optional(),
  ownerId: z.string(),
  source: z.string().min(1, 'Source is required'),
  informationType: z.enum(['fact', 'estimate', 'declaration', 'plan'], {
    errorMap: () => ({ message: 'Information type is required' })
  }),
  assumptions: z.string().optional(),
  estimateType: z.enum(['point', 'range', 'proxy-based', 'extrapolated']).optional(),
  estimateMethod: z.string().optional(),
  confidenceLevel: z.enum(['low', 'medium', 'high']).optional(),
  estimateInputs: z.string().optional(),
  completenessStatus: z.enum(['missing', 'incomplete', 'complete', 'not applicable'], {
    errorMap: () => ({ message: 'Completeness status is required' })
  }).optional(),
  changeNote: z.string().optional(),
  isBlocked: z.boolean().default(false),
  blockerReason: z.string().optional(),
  blockerDueDate: z.string().optional(),
}).refine((data) => {
  // Require assumptions when informationType is 'estimate'
  if (data.informationType === 'estimate' && (!data.assumptions || !data.assumptions.trim())) {
    return false;
  }
  return true;
}, {
  message: "Assumptions field is required when Information Type is 'estimate'",
  path: ['assumptions']
}).refine((data) => {
  // Require estimate type when informationType is 'estimate'
  if (data.informationType === 'estimate' && !data.estimateType) {
    return false;
  }
  return true;
}, {
  message: "Estimate type is required for estimates",
  path: ['estimateType']
}).refine((data) => {
  // Require estimate method when informationType is 'estimate'
  if (data.informationType === 'estimate' && (!data.estimateMethod || !data.estimateMethod.trim())) {
    return false;
  }
  return true;
}, {
  message: "Estimate method is required for estimates",
  path: ['estimateMethod']
}).refine((data) => {
  // Require confidence level when informationType is 'estimate'
  if (data.informationType === 'estimate' && !data.confidenceLevel) {
    return false;
  }
  return true;
}, {
  message: "Confidence level is required for estimates",
  path: ['confidenceLevel']
}).refine((data) => {
  // Require owner when completeness status is 'complete'
  if (data.completenessStatus === 'complete' && (!data.ownerId || !data.ownerId.trim())) {
    return false;
  }
  return true;
}, {
  message: "An owner must be assigned before setting completeness status to 'complete'",
  path: ['ownerId']
}).refine((data) => {
  // Require blocker reason when isBlocked is true
  if (data.isBlocked && (!data.blockerReason || !data.blockerReason.trim())) {
    return false;
  }
  return true;
}, {
  message: "Blocker reason is required when marking as blocked",
  path: ['blockerReason']
})

type DataPointFormData = z.infer<typeof dataPointSchema>

interface DataPointFormProps {
  sectionId: string
  ownerId: string
  availableUsers: User[]
  dataPoint?: DataPoint
  onSubmit: (data: DataPointFormData & { contributorIds: string[], sourceReferences: NarrativeSourceReference[] }) => void | Promise<void>
  onCancel: () => void
  isSubmitting?: boolean
}

export default function DataPointForm({ 
  sectionId, 
  ownerId, 
  availableUsers,
  dataPoint, 
  onSubmit, 
  onCancel,
  isSubmitting = false 
}: DataPointFormProps) {
  const [contributorIds, setContributorIds] = useState<string[]>(dataPoint?.contributorIds || [])
  const [sourceReferences, setSourceReferences] = useState<NarrativeSourceReference[]>(
    dataPoint?.sourceReferences || []
  )
  
  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
    watch
  } = useForm<DataPointFormData>({
    resolver: zodResolver(dataPointSchema),
    defaultValues: dataPoint ? {
      title: dataPoint.title,
      content: dataPoint.content,
      type: dataPoint.type,
      classification: dataPoint.classification,
      value: dataPoint.value?.toString() || '',
      unit: dataPoint.unit || '',
      ownerId: dataPoint.ownerId,
      source: dataPoint.source,
      informationType: dataPoint.informationType,
      assumptions: dataPoint.assumptions || '',
      estimateType: dataPoint.estimateType,
      estimateMethod: dataPoint.estimateMethod || '',
      confidenceLevel: dataPoint.confidenceLevel,
      estimateInputs: dataPoint.estimateInputs || '',
      completenessStatus: dataPoint.completenessStatus,
      changeNote: '',
      isBlocked: dataPoint.isBlocked || false,
      blockerReason: dataPoint.blockerReason || '',
      blockerDueDate: dataPoint.blockerDueDate || '',
    } : {
      type: 'narrative',
      classification: 'fact',
      ownerId: ownerId,
      source: '',
      informationType: 'fact',
      assumptions: '',
      estimateType: undefined,
      estimateMethod: '',
      confidenceLevel: undefined,
      estimateInputs: '',
      completenessStatus: undefined,
      changeNote: '',
      isBlocked: false,
      blockerReason: '',
      blockerDueDate: '',
    }
  })

  const selectedType = watch('type')
  const selectedInformationType = watch('informationType')
  const selectedOwnerId = watch('ownerId')
  const isBlocked = watch('isBlocked')
  
  // Remove owner from contributors when owner changes
  useEffect(() => {
    if (selectedOwnerId && contributorIds.includes(selectedOwnerId)) {
      setContributorIds(prev => prev.filter(id => id !== selectedOwnerId))
    }
  }, [selectedOwnerId, contributorIds])
  
  // Clear estimate fields when informationType changes away from 'estimate'
  useEffect(() => {
    if (selectedInformationType !== 'estimate') {
      setValue('estimateType', undefined)
      setValue('estimateMethod', '')
      setValue('confidenceLevel', undefined)
    }
  }, [selectedInformationType, setValue])
  
  const handleToggleContributor = (userId: string) => {
    setContributorIds(prev =>
      prev.includes(userId)
        ? prev.filter(id => id !== userId)
        : [...prev, userId]
    )
  }
  
  const wrappedOnSubmit = (data: DataPointFormData) => {
    onSubmit({ ...data, contributorIds, sourceReferences })
  }

  return (
    <form onSubmit={handleSubmit(wrappedOnSubmit)} className="space-y-4">
      {/* Title */}
      <div className="space-y-2">
        <Label htmlFor="title">Title *</Label>
        <Input
          id="title"
          {...register('title')}
          placeholder="e.g., Total GHG Emissions"
          className={errors.title ? 'border-red-500' : ''}
        />
        {errors.title && (
          <p className="text-sm text-red-600">{errors.title.message}</p>
        )}
      </div>

      {/* Content */}
      <div className="space-y-2">
        <Label htmlFor="content">Content *</Label>
        <Textarea
          id="content"
          {...register('content')}
          placeholder="Describe the data point..."
          rows={4}
          className={errors.content ? 'border-red-500' : ''}
        />
        {errors.content && (
          <p className="text-sm text-red-600">{errors.content.message}</p>
        )}
      </div>

      {/* Type and Classification */}
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label htmlFor="type">Type *</Label>
          <Select
            value={watch('type')}
            onValueChange={(value) => setValue('type', value as 'narrative' | 'metric' | 'evidence' | 'assumption' | 'gap')}
          >
            <SelectTrigger id="type">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="narrative">Narrative</SelectItem>
              <SelectItem value="metric">Metric</SelectItem>
              <SelectItem value="evidence">Evidence</SelectItem>
              <SelectItem value="assumption">Assumption</SelectItem>
              <SelectItem value="gap">Gap</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          <Label htmlFor="classification">Classification</Label>
          <Select
            value={watch('classification') || ''}
            onValueChange={(value) => setValue('classification', value as 'fact' | 'declaration' | 'plan')}
          >
            <SelectTrigger id="classification">
              <SelectValue placeholder="Select classification" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="fact">Fact</SelectItem>
              <SelectItem value="declaration">Declaration</SelectItem>
              <SelectItem value="plan">Plan</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Value and Unit (only for metric type) */}
      {selectedType === 'metric' && (
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="value">Value</Label>
            <Input
              id="value"
              {...register('value')}
              placeholder="e.g., 1000"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="unit">Unit</Label>
            <Input
              id="unit"
              {...register('unit')}
              placeholder="e.g., tonnes CO2e"
            />
          </div>
        </div>
      )}

      {/* Required Metadata Section */}
      <div className="border-t pt-4 mt-4">
        <h3 className="text-sm font-semibold mb-3">Required Metadata</h3>
        <div className="space-y-4">
          {/* Source */}
          <div className="space-y-2">
            <Label htmlFor="source">Source *</Label>
            <Input
              id="source"
              {...register('source')}
              placeholder="e.g., Energy Management System, Annual Report 2024"
              className={errors.source ? 'border-red-500' : ''}
            />
            {errors.source && (
              <p className="text-sm text-red-600">{errors.source.message}</p>
            )}
          </div>

          {/* Information Type */}
          <div className="space-y-2">
            <Label htmlFor="informationType">Information Type *</Label>
            <Select
              value={watch('informationType')}
              onValueChange={(value) => setValue('informationType', value)}
            >
              <SelectTrigger id="informationType" className={errors.informationType ? 'border-red-500' : ''}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="fact">Fact</SelectItem>
                <SelectItem value="estimate">Estimate</SelectItem>
                <SelectItem value="declaration">Declaration</SelectItem>
                <SelectItem value="plan">Plan</SelectItem>
              </SelectContent>
            </Select>
            {errors.informationType && (
              <p className="text-sm text-red-600">{errors.informationType.message}</p>
            )}
          </div>

          {/* Assumptions (required for estimate) */}
          {selectedInformationType === 'estimate' && (
            <>
              <div className="space-y-2">
                <Label htmlFor="assumptions">Assumptions *</Label>
                <Textarea
                  id="assumptions"
                  {...register('assumptions')}
                  placeholder="Describe the assumptions used for this estimate..."
                  rows={3}
                  className={errors.assumptions ? 'border-red-500' : ''}
                />
                {errors.assumptions && (
                  <p className="text-sm text-red-600">{errors.assumptions.message}</p>
                )}
                <p className="text-xs text-muted-foreground">
                  Required for estimates to ensure transparency and auditability
                </p>
              </div>

              {/* Estimate Type */}
              <div className="space-y-2">
                <Label htmlFor="estimateType">Estimate Type *</Label>
                <Select
                  value={watch('estimateType') || ''}
                  onValueChange={(value) => setValue('estimateType', value as 'point' | 'range' | 'proxy-based' | 'extrapolated')}
                >
                  <SelectTrigger id="estimateType" className={errors.estimateType ? 'border-red-500' : ''}>
                    <SelectValue placeholder="Select estimate type" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="point">Point Estimate</SelectItem>
                    <SelectItem value="range">Range Estimate</SelectItem>
                    <SelectItem value="proxy-based">Proxy-Based</SelectItem>
                    <SelectItem value="extrapolated">Extrapolated</SelectItem>
                  </SelectContent>
                </Select>
                {errors.estimateType && (
                  <p className="text-sm text-red-600">{errors.estimateType.message}</p>
                )}
                <p className="text-xs text-muted-foreground">
                  Select the type of estimation method used
                </p>
              </div>

              {/* Estimate Method */}
              <div className="space-y-2">
                <Label htmlFor="estimateMethod">Estimation Method *</Label>
                <Textarea
                  id="estimateMethod"
                  {...register('estimateMethod')}
                  placeholder="Describe the methodology used to derive this estimate..."
                  rows={3}
                  className={errors.estimateMethod ? 'border-red-500' : ''}
                />
                {errors.estimateMethod && (
                  <p className="text-sm text-red-600">{errors.estimateMethod.message}</p>
                )}
                <p className="text-xs text-muted-foreground">
                  Explain how the estimate was calculated or derived
                </p>
              </div>

              {/* Confidence Level */}
              <div className="space-y-2">
                <Label htmlFor="confidenceLevel">Confidence Level *</Label>
                <Select
                  value={watch('confidenceLevel') || ''}
                  onValueChange={(value) => setValue('confidenceLevel', value as 'low' | 'medium' | 'high')}
                >
                  <SelectTrigger id="confidenceLevel" className={errors.confidenceLevel ? 'border-red-500' : ''}>
                    <SelectValue placeholder="Select confidence level" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="low">Low</SelectItem>
                    <SelectItem value="medium">Medium</SelectItem>
                    <SelectItem value="high">High</SelectItem>
                  </SelectContent>
                </Select>
                {errors.confidenceLevel && (
                  <p className="text-sm text-red-600">{errors.confidenceLevel.message}</p>
                )}
                <p className="text-xs text-muted-foreground">
                  Indicate your confidence in the accuracy of this estimate
                </p>
              </div>

              {/* Estimate Inputs */}
              <div className="space-y-2">
                <Label htmlFor="estimateInputs">Input Data and Sources</Label>
                <Textarea
                  id="estimateInputs"
                  {...register('estimateInputs')}
                  placeholder="Document the input data used in this estimate (e.g., 'Energy consumption: 1000 kWh from meter reading, Emission factor: 0.5 kg CO2/kWh from national grid data')..."
                  rows={3}
                />
                <p className="text-xs text-muted-foreground">
                  List the inputs and their sources for provenance tracking
                </p>
              </div>
            </>
          )}

          {/* Completeness Status */}
          <div className="space-y-2">
            <Label htmlFor="completenessStatus">Completeness Status</Label>
            <Select
              value={watch('completenessStatus') || ''}
              onValueChange={(value) => {
                // Validate the value is a valid CompletenessStatus before setting
                if (value === 'incomplete' || value === 'complete' || value === 'not applicable') {
                  setValue('completenessStatus', value)
                }
              }}
            >
              <SelectTrigger id="completenessStatus" className={errors.completenessStatus ? 'border-red-500' : ''}>
                <SelectValue placeholder="Auto-calculate" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="incomplete">Incomplete</SelectItem>
                <SelectItem value="complete">Complete</SelectItem>
                <SelectItem value="not applicable">Not Applicable</SelectItem>
              </SelectContent>
            </Select>
            <p className="text-xs text-muted-foreground">Leave empty to auto-calculate based on required fields and evidence</p>
            {errors.completenessStatus && (
              <p className="text-sm text-red-600">{errors.completenessStatus.message}</p>
            )}
          </div>
        </div>
      </div>

      {/* Ownership & Responsibilities */}
      <div className="space-y-4 pt-4 border-t">
        <h3 className="font-medium text-sm">Ownership & Responsibilities</h3>
        
        {/* Owner Selection */}
        <div className="space-y-2">
          <Label htmlFor="ownerId">Owner *</Label>
          <Select
            value={watch('ownerId')}
            onValueChange={(value) => setValue('ownerId', value)}
          >
            <SelectTrigger id="ownerId" className={errors.ownerId ? 'border-red-500' : ''}>
              <SelectValue placeholder="Select owner" />
            </SelectTrigger>
            <SelectContent>
              {availableUsers.map(user => (
                <SelectItem key={user.id} value={user.id}>
                  {user.name} ({user.role})
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.ownerId && (
            <p className="text-sm text-red-600">{errors.ownerId.message}</p>
          )}
        </div>

        {/* Contributors Selection */}
        <div className="space-y-2">
          <Label>Contributors</Label>
          <div className="space-y-2 border rounded-md p-3">
            {availableUsers.filter(u => u.id !== watch('ownerId')).map(user => (
              <div key={user.id} className="flex items-center space-x-2">
                <Checkbox
                  id={`contributor-${user.id}`}
                  checked={contributorIds.includes(user.id)}
                  onCheckedChange={() => handleToggleContributor(user.id)}
                />
                <label
                  htmlFor={`contributor-${user.id}`}
                  className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer"
                >
                  {user.name} ({user.role})
                </label>
              </div>
            ))}
            {availableUsers.filter(u => u.id !== watch('ownerId')).length === 0 && (
              <p className="text-sm text-muted-foreground">No other users available</p>
            )}
          </div>
          <p className="text-xs text-muted-foreground">Select users who will contribute to this data item</p>
        </div>
      </div>

      {/* Source References / Provenance */}
      <div className="space-y-4 pt-4 border-t">
        <SourceReferencesManager
          sourceReferences={sourceReferences}
          onChange={setSourceReferences}
          disabled={isSubmitting}
        />
      </div>

      {/* Blocker Status */}
      <div className="space-y-4 pt-4 border-t">
        <h3 className="font-medium text-sm">Blocker Status</h3>
        
        {/* Is Blocked Checkbox */}
        <div className="flex items-center space-x-2">
          <Checkbox
            id="isBlocked"
            checked={isBlocked}
            onCheckedChange={(checked) => setValue('isBlocked', checked === true)}
          />
          <label
            htmlFor="isBlocked"
            className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer"
          >
            Mark as Blocked
          </label>
        </div>
        <p className="text-xs text-muted-foreground">
          Use this to indicate that work on this data point cannot proceed due to external dependencies or issues
        </p>

        {/* Blocker Reason (conditional) */}
        {isBlocked && (
          <>
            <div className="space-y-2">
              <Label htmlFor="blockerReason">Blocker Reason *</Label>
              <Textarea
                id="blockerReason"
                {...register('blockerReason')}
                placeholder="Describe what is blocking progress on this data point..."
                rows={3}
                className={errors.blockerReason ? 'border-red-500' : ''}
              />
              {errors.blockerReason && (
                <p className="text-sm text-red-600">{errors.blockerReason.message}</p>
              )}
              <p className="text-xs text-muted-foreground">
                Explain what needs to happen to unblock this data point
              </p>
            </div>

            {/* Blocker Due Date (optional) */}
            <div className="space-y-2">
              <Label htmlFor="blockerDueDate">Expected Resolution Date (Optional)</Label>
              <Input
                id="blockerDueDate"
                type="date"
                {...register('blockerDueDate')}
              />
              <p className="text-xs text-muted-foreground">
                When do you expect this blocker to be resolved?
              </p>
            </div>
          </>
        )}
      </div>

      {/* Change Note (optional, for updates only) */}
      {dataPoint && (
        <div className="space-y-4 pt-4 border-t">
          <h3 className="font-medium text-sm">Change Documentation</h3>
          <div className="space-y-2">
            <Label htmlFor="changeNote">Change Note (Optional)</Label>
            <Textarea
              id="changeNote"
              {...register('changeNote')}
              placeholder="Describe the reason for these changes..."
              rows={2}
            />
            <p className="text-xs text-muted-foreground">
              Add a note to document why you're making these changes (visible in audit trail)
            </p>
          </div>
        </div>
      )}

      {/* Validation Error Alert */}
      {Object.keys(errors).length > 0 && (
        <Alert variant="destructive">
          <WarningCircle size={16} />
          <AlertDescription>
            Please fix the errors above before submitting.
          </AlertDescription>
        </Alert>
      )}

      {/* Form Actions */}
      <div className="flex justify-end gap-2 pt-4 border-t">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving...' : dataPoint ? 'Update Data Point' : 'Create Data Point'}
        </Button>
      </div>
    </form>
  )
}
