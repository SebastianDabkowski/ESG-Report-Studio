import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { WarningCircle, Check, Plus, X } from '@phosphor-icons/react'
import type { Simplification } from '@/lib/types'
import { createSimplification, updateSimplification, type CreateSimplificationPayload, type UpdateSimplificationPayload } from '@/lib/api'

const simplificationSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  description: z.string().min(1, 'Description is required'),
  affectedEntities: z.array(z.string().min(1, 'Entity name cannot be empty')).optional(),
  affectedSites: z.array(z.string().min(1, 'Site name cannot be empty')).optional(),
  affectedProcesses: z.array(z.string().min(1, 'Process name cannot be empty')).optional(),
  impactLevel: z.enum(['low', 'medium', 'high'], {
    required_error: 'Impact level is required'
  }),
  impactNotes: z.string().optional()
}).refine((data) => {
  // At least one boundary must be specified
  const hasEntities = data.affectedEntities && data.affectedEntities.length > 0
  const hasSites = data.affectedSites && data.affectedSites.length > 0
  const hasProcesses = data.affectedProcesses && data.affectedProcesses.length > 0
  return hasEntities || hasSites || hasProcesses
}, {
  message: "At least one affected boundary (entities, sites, or processes) must be specified",
  path: ['affectedEntities']
})

type SimplificationFormData = z.infer<typeof simplificationSchema>

interface SimplificationFormProps {
  sectionId: string
  simplification?: Simplification
  onSuccess: (simplification: Simplification) => void
  onCancel: () => void
}

export function SimplificationForm({ sectionId, simplification, onSuccess, onCancel }: SimplificationFormProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const isEditMode = !!simplification

  const {
    register,
    handleSubmit,
    control,
    setValue,
    watch,
    formState: { errors }
  } = useForm<SimplificationFormData>({
    resolver: zodResolver(simplificationSchema),
    defaultValues: simplification ? {
      title: simplification.title,
      description: simplification.description,
      affectedEntities: simplification.affectedEntities || [],
      affectedSites: simplification.affectedSites || [],
      affectedProcesses: simplification.affectedProcesses || [],
      impactLevel: simplification.impactLevel,
      impactNotes: simplification.impactNotes || ''
    } : {
      title: '',
      description: '',
      affectedEntities: [],
      affectedSites: [],
      affectedProcesses: [],
      impactLevel: 'medium' as const,
      impactNotes: ''
    }
  })

  const { fields: entityFields, append: appendEntity, remove: removeEntity } = useFieldArray({
    control,
    name: 'affectedEntities'
  })

  const { fields: siteFields, append: appendSite, remove: removeSite } = useFieldArray({
    control,
    name: 'affectedSites'
  })

  const { fields: processFields, append: appendProcess, remove: removeProcess } = useFieldArray({
    control,
    name: 'affectedProcesses'
  })

  const impactLevel = watch('impactLevel')

  const onSubmit = async (data: SimplificationFormData) => {
    setIsSubmitting(true)
    setErrorMessage(null)
    setSuccessMessage(null)

    try {
      const payload = {
        title: data.title,
        description: data.description,
        affectedEntities: data.affectedEntities || [],
        affectedSites: data.affectedSites || [],
        affectedProcesses: data.affectedProcesses || [],
        impactLevel: data.impactLevel,
        impactNotes: data.impactNotes && data.impactNotes.trim() ? data.impactNotes : undefined
      }

      if (isEditMode) {
        const updated = await updateSimplification(simplification.id, payload as UpdateSimplificationPayload)
        setSuccessMessage('Simplification updated successfully')
        onSuccess(updated)
      } else {
        const created = await createSimplification({
          ...payload,
          sectionId
        } as CreateSimplificationPayload)
        setSuccessMessage('Simplification created successfully')
        onSuccess(created)
      }
    } catch (error) {
      console.error('Error saving simplification:', error)
      setErrorMessage(error instanceof Error ? error.message : 'Failed to save simplification')
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
            placeholder="Brief title for this simplification"
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
            placeholder="Detailed description of the simplification or boundary limitation"
            rows={4}
            className={errors.description ? 'border-red-500' : ''}
          />
          {errors.description && (
            <p className="text-sm text-red-600 mt-1">{errors.description.message}</p>
          )}
        </div>

        <div className="border rounded-lg p-4 space-y-4">
          <h3 className="font-semibold text-sm">Affected Boundaries *</h3>
          <p className="text-sm text-slate-500">
            Specify at least one affected boundary (entities, sites, or processes)
          </p>

          {/* Affected Entities */}
          <div>
            <Label>Affected Entities</Label>
            <div className="space-y-2 mt-2">
              {entityFields.map((field, index) => (
                <div key={field.id} className="flex gap-2">
                  <Input
                    {...register(`affectedEntities.${index}` as const)}
                    placeholder="e.g., Subsidiary name, Entity code"
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => removeEntity(index)}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              ))}
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => appendEntity('')}
                className="w-full"
              >
                <Plus className="h-4 w-4 mr-2" />
                Add Entity
              </Button>
            </div>
          </div>

          {/* Affected Sites */}
          <div>
            <Label>Affected Sites</Label>
            <div className="space-y-2 mt-2">
              {siteFields.map((field, index) => (
                <div key={field.id} className="flex gap-2">
                  <Input
                    {...register(`affectedSites.${index}` as const)}
                    placeholder="e.g., Factory location, Office name"
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => removeSite(index)}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              ))}
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => appendSite('')}
                className="w-full"
              >
                <Plus className="h-4 w-4 mr-2" />
                Add Site
              </Button>
            </div>
          </div>

          {/* Affected Processes */}
          <div>
            <Label>Affected Processes</Label>
            <div className="space-y-2 mt-2">
              {processFields.map((field, index) => (
                <div key={field.id} className="flex gap-2">
                  <Input
                    {...register(`affectedProcesses.${index}` as const)}
                    placeholder="e.g., Production line, Supply chain stage"
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => removeProcess(index)}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              ))}
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => appendProcess('')}
                className="w-full"
              >
                <Plus className="h-4 w-4 mr-2" />
                Add Process
              </Button>
            </div>
          </div>

          {errors.affectedEntities && (
            <p className="text-sm text-red-600 mt-1">{errors.affectedEntities.message}</p>
          )}
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <Label htmlFor="impactLevel">Impact Level *</Label>
            <Select
              value={impactLevel}
              onValueChange={(value) => setValue('impactLevel', value as 'low' | 'medium' | 'high')}
            >
              <SelectTrigger className={errors.impactLevel ? 'border-red-500' : ''}>
                <SelectValue placeholder="Select impact level" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="low">Low</SelectItem>
                <SelectItem value="medium">Medium</SelectItem>
                <SelectItem value="high">High</SelectItem>
              </SelectContent>
            </Select>
            {errors.impactLevel && (
              <p className="text-sm text-red-600 mt-1">{errors.impactLevel.message}</p>
            )}
          </div>
        </div>

        <div>
          <Label htmlFor="impactNotes">Impact Notes</Label>
          <Textarea
            id="impactNotes"
            {...register('impactNotes')}
            placeholder="Additional context about the impact assessment (optional)"
            rows={3}
          />
          <p className="text-sm text-slate-500 mt-1">
            Provide additional context about why this impact level was chosen
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
          {isSubmitting ? 'Saving...' : isEditMode ? 'Update Simplification' : 'Create Simplification'}
        </Button>
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  )
}
