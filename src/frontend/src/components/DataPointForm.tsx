import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { WarningCircle } from '@phosphor-icons/react'
import type { DataPoint } from '@/lib/types'

const dataPointSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  content: z.string().min(1, 'Content is required'),
  type: z.enum(['narrative', 'metric', 'evidence', 'assumption', 'gap']),
  classification: z.enum(['fact', 'declaration', 'plan']).optional(),
  value: z.string().optional(),
  unit: z.string().optional(),
  source: z.string().min(1, 'Source is required'),
  informationType: z.enum(['fact', 'estimate', 'declaration', 'plan'], {
    errorMap: () => ({ message: 'Information type is required' })
  }),
  assumptions: z.string().optional(),
  completenessStatus: z.enum(['missing', 'incomplete', 'complete', 'not applicable'], {
    errorMap: () => ({ message: 'Completeness status is required' })
  }).optional(),
}).refine((data) => {
  // Require assumptions when informationType is 'estimate'
  if (data.informationType === 'estimate' && (!data.assumptions || !data.assumptions.trim())) {
    return false;
  }
  return true;
}, {
  message: "Assumptions field is required when Information Type is 'estimate'",
  path: ['assumptions']
})

type DataPointFormData = z.infer<typeof dataPointSchema>

interface DataPointFormProps {
  sectionId: string
  ownerId: string
  dataPoint?: DataPoint
  onSubmit: (data: DataPointFormData) => void | Promise<void>
  onCancel: () => void
  isSubmitting?: boolean
}

export default function DataPointForm({ 
  sectionId, 
  ownerId, 
  dataPoint, 
  onSubmit, 
  onCancel,
  isSubmitting = false 
}: DataPointFormProps) {
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
      source: dataPoint.source,
      informationType: dataPoint.informationType,
      assumptions: dataPoint.assumptions || '',
      completenessStatus: dataPoint.completenessStatus,
    } : {
      type: 'narrative',
      classification: 'fact',
      source: '',
      informationType: 'fact',
      assumptions: '',
      completenessStatus: 'incomplete',
    }
  })

  const selectedType = watch('type')
  const selectedInformationType = watch('informationType')

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
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
          )}

          {/* Completeness Status */}
          <div className="space-y-2">
            <Label htmlFor="completenessStatus">Completeness Status</Label>
            <Select
              value={watch('completenessStatus') || ''}
              onValueChange={(value) => setValue('completenessStatus', value as any)}
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
