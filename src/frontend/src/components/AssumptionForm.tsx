import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { WarningCircle, Check } from '@phosphor-icons/react'
import type { Assumption } from '@/lib/types'
import { createAssumption, updateAssumption, type CreateAssumptionPayload, type UpdateAssumptionPayload } from '@/lib/api'

const assumptionSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  description: z.string().min(1, 'Description is required'),
  scope: z.string().min(1, 'Scope is required'),
  validityStartDate: z.string().min(1, 'Validity start date is required'),
  validityEndDate: z.string().min(1, 'Validity end date is required'),
  methodology: z.string().min(1, 'Methodology is required'),
  limitations: z.string().optional()
}).refine((data) => {
  // Validate that end date is after start date
  if (data.validityStartDate && data.validityEndDate) {
    const startDate = new Date(data.validityStartDate)
    const endDate = new Date(data.validityEndDate)
    return endDate > startDate
  }
  return true
}, {
  message: "Validity end date must be after start date",
  path: ['validityEndDate']
})

type AssumptionFormData = z.infer<typeof assumptionSchema>

interface AssumptionFormProps {
  sectionId: string
  assumption?: Assumption
  linkedDataPointIds?: string[]
  onSuccess: (assumption: Assumption) => void
  onCancel: () => void
}

export function AssumptionForm({ sectionId, assumption, linkedDataPointIds = [], onSuccess, onCancel }: AssumptionFormProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const isEditMode = !!assumption

  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<AssumptionFormData>({
    resolver: zodResolver(assumptionSchema),
    defaultValues: assumption ? {
      title: assumption.title,
      description: assumption.description,
      scope: assumption.scope,
      validityStartDate: assumption.validityStartDate,
      validityEndDate: assumption.validityEndDate,
      methodology: assumption.methodology,
      limitations: assumption.limitations || ''
    } : {
      title: '',
      description: '',
      scope: '',
      validityStartDate: '',
      validityEndDate: '',
      methodology: '',
      limitations: ''
    }
  })

  const onSubmit = async (data: AssumptionFormData) => {
    setIsSubmitting(true)
    setErrorMessage(null)
    setSuccessMessage(null)

    try {
      if (isEditMode) {
        const payload: UpdateAssumptionPayload = {
          title: data.title,
          description: data.description,
          scope: data.scope,
          validityStartDate: data.validityStartDate,
          validityEndDate: data.validityEndDate,
          methodology: data.methodology,
          limitations: data.limitations || '',
          linkedDataPointIds: assumption.linkedDataPointIds
        }
        const updated = await updateAssumption(assumption.id, payload)
        setSuccessMessage('Assumption updated successfully')
        onSuccess(updated)
      } else {
        const payload: CreateAssumptionPayload = {
          sectionId,
          title: data.title,
          description: data.description,
          scope: data.scope,
          validityStartDate: data.validityStartDate,
          validityEndDate: data.validityEndDate,
          methodology: data.methodology,
          limitations: data.limitations || '',
          linkedDataPointIds
        }
        const created = await createAssumption(payload)
        setSuccessMessage('Assumption created successfully')
        onSuccess(created)
      }
    } catch (error) {
      console.error('Error saving assumption:', error)
      setErrorMessage(error instanceof Error ? error.message : 'Failed to save assumption')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      <div className="space-y-4">
        <div>
          <Label htmlFor="title">Title *</Label>
          <Input
            id="title"
            {...register('title')}
            placeholder="Brief title for this assumption"
            className={errors.title ? 'border-red-500' : ''}
          />
          {errors.title && (
            <p className="text-sm text-red-600 mt-1">{errors.title.message}</p>
          )}
        </div>

        <div>
          <Label htmlFor="description">Description *</Label>
          <Textarea
            id="description"
            {...register('description')}
            placeholder="Detailed description of the assumption"
            rows={4}
            className={errors.description ? 'border-red-500' : ''}
          />
          {errors.description && (
            <p className="text-sm text-red-600 mt-1">{errors.description.message}</p>
          )}
        </div>

        <div>
          <Label htmlFor="scope">Scope *</Label>
          <Input
            id="scope"
            {...register('scope')}
            placeholder="e.g., Company-wide, Specific facility, Product line"
            className={errors.scope ? 'border-red-500' : ''}
          />
          {errors.scope && (
            <p className="text-sm text-red-600 mt-1">{errors.scope.message}</p>
          )}
          <p className="text-sm text-slate-500 mt-1">
            Define the organizational or operational scope this assumption applies to
          </p>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <Label htmlFor="validityStartDate">Validity Start Date *</Label>
            <Input
              id="validityStartDate"
              type="date"
              {...register('validityStartDate')}
              className={errors.validityStartDate ? 'border-red-500' : ''}
            />
            {errors.validityStartDate && (
              <p className="text-sm text-red-600 mt-1">{errors.validityStartDate.message}</p>
            )}
          </div>

          <div>
            <Label htmlFor="validityEndDate">Validity End Date *</Label>
            <Input
              id="validityEndDate"
              type="date"
              {...register('validityEndDate')}
              className={errors.validityEndDate ? 'border-red-500' : ''}
            />
            {errors.validityEndDate && (
              <p className="text-sm text-red-600 mt-1">{errors.validityEndDate.message}</p>
            )}
          </div>
        </div>

        <div>
          <Label htmlFor="methodology">Methodology *</Label>
          <Textarea
            id="methodology"
            {...register('methodology')}
            placeholder="Explain the methodology used to derive this assumption"
            rows={3}
            className={errors.methodology ? 'border-red-500' : ''}
          />
          {errors.methodology && (
            <p className="text-sm text-red-600 mt-1">{errors.methodology.message}</p>
          )}
        </div>

        <div>
          <Label htmlFor="limitations">Limitations</Label>
          <Textarea
            id="limitations"
            {...register('limitations')}
            placeholder="Known limitations or constraints of this assumption"
            rows={3}
          />
          <p className="text-sm text-slate-500 mt-1">
            Document any known limitations, uncertainties, or constraints
          </p>
        </div>
      </div>

      {errorMessage && (
        <Alert variant="destructive">
          <WarningCircle className="h-4 w-4" />
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      {successMessage && (
        <Alert className="bg-green-50 border-green-200 text-green-800">
          <Check className="h-4 w-4" />
          <AlertDescription>{successMessage}</AlertDescription>
        </Alert>
      )}

      <div className="flex gap-3 pt-4">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving...' : isEditMode ? 'Update Assumption' : 'Create Assumption'}
        </Button>
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  )
}
