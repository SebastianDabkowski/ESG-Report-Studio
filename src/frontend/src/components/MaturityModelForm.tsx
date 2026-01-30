import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Checkbox } from '@/components/ui/checkbox'
import { WarningCircle, Plus, Trash, ArrowUp, ArrowDown } from '@phosphor-icons/react'
import type { MaturityModel, User, CriterionType } from '@/lib/types'
import { createMaturityModel, updateMaturityModel } from '@/lib/api'

const criterionSchema = z.object({
  name: z.string().min(1, 'Criterion name is required'),
  description: z.string().min(1, 'Criterion description is required'),
  criterionType: z.enum(['data-completeness', 'evidence-quality', 'process-control', 'custom']),
  targetValue: z.string().min(1, 'Target value is required'),
  unit: z.string().min(1, 'Unit is required'),
  minCompletionPercentage: z.number().min(0).max(100).optional(),
  minEvidencePercentage: z.number().min(0).max(100).optional(),
  requiredControls: z.array(z.string()).optional(),
  isMandatory: z.boolean()
})

const levelSchema = z.object({
  name: z.string().min(1, 'Level name is required'),
  description: z.string().min(1, 'Level description is required'),
  order: z.number().min(1, 'Order must be at least 1'),
  criteria: z.array(criterionSchema).min(0)
})

const maturityModelSchema = z.object({
  name: z.string().min(1, 'Model name is required'),
  description: z.string().min(1, 'Model description is required'),
  levels: z.array(levelSchema).min(1, 'At least one maturity level is required')
}).refine((data) => {
  const orders = data.levels.map(l => l.order)
  return orders.length === new Set(orders).size
}, {
  message: "Level orders must be unique",
  path: ['levels']
})

type MaturityModelFormData = z.infer<typeof maturityModelSchema>

interface MaturityModelFormProps {
  currentUser: User
  model?: MaturityModel
  onSuccess: () => void
  onCancel: () => void
}

export function MaturityModelForm({ currentUser, model, onSuccess, onCancel }: MaturityModelFormProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const isEditMode = !!model

  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors }
  } = useForm<MaturityModelFormData>({
    resolver: zodResolver(maturityModelSchema),
    defaultValues: model ? {
      name: model.name,
      description: model.description,
      levels: model.levels.map(level => ({
        name: level.name,
        description: level.description,
        order: level.order,
        criteria: level.criteria.map(criterion => ({
          name: criterion.name,
          description: criterion.description,
          criterionType: criterion.criterionType,
          targetValue: criterion.targetValue,
          unit: criterion.unit,
          minCompletionPercentage: criterion.minCompletionPercentage,
          minEvidencePercentage: criterion.minEvidencePercentage,
          requiredControls: criterion.requiredControls || [],
          isMandatory: criterion.isMandatory
        }))
      }))
    } : {
      name: '',
      description: '',
      levels: [
        {
          name: 'Initial',
          description: 'Ad-hoc reporting with minimal structure',
          order: 1,
          criteria: []
        }
      ]
    }
  })

  const { fields: levelFields, append: appendLevel, remove: removeLevel, move: moveLevel } = useFieldArray({
    control,
    name: 'levels'
  })

  const onSubmit = async (data: MaturityModelFormData) => {
    setIsSubmitting(true)
    setErrorMessage(null)

    try {
      const payload = {
        name: data.name,
        description: data.description,
        levels: data.levels.map(level => ({
          name: level.name,
          description: level.description,
          order: level.order,
          criteria: level.criteria.map(criterion => ({
            name: criterion.name,
            description: criterion.description,
            criterionType: criterion.criterionType,
            targetValue: criterion.targetValue,
            unit: criterion.unit,
            minCompletionPercentage: criterion.minCompletionPercentage,
            minEvidencePercentage: criterion.minEvidencePercentage,
            requiredControls: criterion.requiredControls || [],
            isMandatory: criterion.isMandatory
          }))
        })),
        ...(isEditMode
          ? { updatedBy: currentUser.id, updatedByName: currentUser.name }
          : { createdBy: currentUser.id, createdByName: currentUser.name })
      }

      if (isEditMode) {
        await updateMaturityModel(model.id, payload as any)
      } else {
        await createMaturityModel(payload as any)
      }

      onSuccess()
    } catch (err: any) {
      setErrorMessage(err.message || 'Failed to save maturity model')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      {errorMessage && (
        <Alert variant="destructive">
          <WarningCircle className="h-4 w-4" />
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Model Information</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label htmlFor="name">Model Name *</Label>
            <Input
              id="name"
              {...register('name')}
              placeholder="e.g., ESG Reporting Maturity Framework"
            />
            {errors.name && (
              <p className="text-sm text-destructive mt-1">{errors.name.message}</p>
            )}
          </div>

          <div>
            <Label htmlFor="description">Description *</Label>
            <Textarea
              id="description"
              {...register('description')}
              placeholder="Describe the purpose and scope of this maturity model"
              rows={3}
            />
            {errors.description && (
              <p className="text-sm text-destructive mt-1">{errors.description.message}</p>
            )}
          </div>
        </CardContent>
      </Card>

      <div>
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold">Maturity Levels</h3>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => appendLevel({
              name: '',
              description: '',
              order: levelFields.length + 1,
              criteria: []
            })}
          >
            <Plus className="mr-2 h-4 w-4" />
            Add Level
          </Button>
        </div>

        {errors.levels && typeof errors.levels.message === 'string' && (
          <Alert variant="destructive" className="mb-4">
            <AlertDescription>{errors.levels.message}</AlertDescription>
          </Alert>
        )}

        <div className="space-y-4">
          {levelFields.map((level, levelIndex) => (
            <MaturityLevelForm
              key={level.id}
              levelIndex={levelIndex}
              register={register}
              control={control}
              watch={watch}
              setValue={setValue}
              errors={errors}
              onRemove={() => removeLevel(levelIndex)}
              onMoveUp={levelIndex > 0 ? () => moveLevel(levelIndex, levelIndex - 1) : undefined}
              onMoveDown={levelIndex < levelFields.length - 1 ? () => moveLevel(levelIndex, levelIndex + 1) : undefined}
              canRemove={levelFields.length > 1}
            />
          ))}
        </div>
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving...' : isEditMode ? 'Update Model' : 'Create Model'}
        </Button>
      </div>
    </form>
  )
}

interface MaturityLevelFormProps {
  levelIndex: number
  register: any
  control: any
  watch: any
  setValue: any
  errors: any
  onRemove: () => void
  onMoveUp?: () => void
  onMoveDown?: () => void
  canRemove: boolean
}

function MaturityLevelForm({
  levelIndex,
  register,
  control,
  watch,
  setValue,
  errors,
  onRemove,
  onMoveUp,
  onMoveDown,
  canRemove
}: MaturityLevelFormProps) {
  const { fields: criteriaFields, append: appendCriterion, remove: removeCriterion } = useFieldArray({
    control,
    name: `levels.${levelIndex}.criteria`
  })

  const levelErrors = errors.levels?.[levelIndex]

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="text-base">
            Level {watch(`levels.${levelIndex}.order`)} - {watch(`levels.${levelIndex}.name`) || 'Unnamed Level'}
          </CardTitle>
          <div className="flex gap-2">
            {onMoveUp && (
              <Button type="button" variant="ghost" size="sm" onClick={onMoveUp}>
                <ArrowUp className="h-4 w-4" />
              </Button>
            )}
            {onMoveDown && (
              <Button type="button" variant="ghost" size="sm" onClick={onMoveDown}>
                <ArrowDown className="h-4 w-4" />
              </Button>
            )}
            {canRemove && (
              <Button type="button" variant="ghost" size="sm" onClick={onRemove}>
                <Trash className="h-4 w-4" />
              </Button>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <Label>Level Name *</Label>
            <Input
              {...register(`levels.${levelIndex}.name`)}
              placeholder="e.g., Initial, Repeatable, Managed"
            />
            {levelErrors?.name && (
              <p className="text-sm text-destructive mt-1">{levelErrors.name.message}</p>
            )}
          </div>

          <div>
            <Label>Order *</Label>
            <Input
              type="number"
              {...register(`levels.${levelIndex}.order`, { valueAsNumber: true })}
              min={1}
            />
            {levelErrors?.order && (
              <p className="text-sm text-destructive mt-1">{levelErrors.order.message}</p>
            )}
          </div>
        </div>

        <div>
          <Label>Description *</Label>
          <Textarea
            {...register(`levels.${levelIndex}.description`)}
            placeholder="Describe what this maturity level represents"
            rows={2}
          />
          {levelErrors?.description && (
            <p className="text-sm text-destructive mt-1">{levelErrors.description.message}</p>
          )}
        </div>

        <div>
          <div className="flex items-center justify-between mb-2">
            <Label>Criteria</Label>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => appendCriterion({
                name: '',
                description: '',
                criterionType: 'data-completeness',
                targetValue: '',
                unit: '%',
                minCompletionPercentage: undefined,
                minEvidencePercentage: undefined,
                requiredControls: [],
                isMandatory: true
              })}
            >
              <Plus className="mr-2 h-3 w-3" />
              Add Criterion
            </Button>
          </div>

          <div className="space-y-3">
            {criteriaFields.map((criterion, criterionIndex) => (
              <MaturityCriterionForm
                key={criterion.id}
                levelIndex={levelIndex}
                criterionIndex={criterionIndex}
                register={register}
                watch={watch}
                setValue={setValue}
                errors={levelErrors?.criteria?.[criterionIndex]}
                onRemove={() => removeCriterion(criterionIndex)}
              />
            ))}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

interface MaturityCriterionFormProps {
  levelIndex: number
  criterionIndex: number
  register: any
  watch: any
  setValue: any
  errors: any
  onRemove: () => void
}

function MaturityCriterionForm({
  levelIndex,
  criterionIndex,
  register,
  watch,
  setValue,
  errors,
  onRemove
}: MaturityCriterionFormProps) {
  const criterionType = watch(`levels.${levelIndex}.criteria.${criterionIndex}.criterionType`) as CriterionType
  const isMandatory = watch(`levels.${levelIndex}.criteria.${criterionIndex}.isMandatory`)

  return (
    <div className="border rounded-lg p-3 space-y-3">
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 grid grid-cols-2 gap-3">
          <div className="col-span-2">
            <Label className="text-xs">Criterion Name *</Label>
            <Input
              {...register(`levels.${levelIndex}.criteria.${criterionIndex}.name`)}
              placeholder="e.g., Data completeness"
              className="h-8 text-sm"
            />
            {errors?.name && (
              <p className="text-xs text-destructive mt-1">{errors.name.message}</p>
            )}
          </div>

          <div>
            <Label className="text-xs">Type *</Label>
            <Select
              value={criterionType}
              onValueChange={(value) => setValue(`levels.${levelIndex}.criteria.${criterionIndex}.criterionType`, value)}
            >
              <SelectTrigger className="h-8 text-sm">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="data-completeness">Data Completeness</SelectItem>
                <SelectItem value="evidence-quality">Evidence Quality</SelectItem>
                <SelectItem value="process-control">Process Control</SelectItem>
                <SelectItem value="custom">Custom</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div>
            <Label className="text-xs">Target Value *</Label>
            <Input
              {...register(`levels.${levelIndex}.criteria.${criterionIndex}.targetValue`)}
              placeholder="e.g., 80"
              className="h-8 text-sm"
            />
          </div>

          <div>
            <Label className="text-xs">Unit *</Label>
            <Input
              {...register(`levels.${levelIndex}.criteria.${criterionIndex}.unit`)}
              placeholder="e.g., %"
              className="h-8 text-sm"
            />
          </div>

          {(criterionType === 'data-completeness' || criterionType === 'evidence-quality') && (
            <>
              {criterionType === 'data-completeness' && (
                <div>
                  <Label className="text-xs">Min Completion %</Label>
                  <Input
                    type="number"
                    {...register(`levels.${levelIndex}.criteria.${criterionIndex}.minCompletionPercentage`, { valueAsNumber: true })}
                    placeholder="e.g., 80"
                    className="h-8 text-sm"
                    min={0}
                    max={100}
                  />
                </div>
              )}
              {criterionType === 'evidence-quality' && (
                <div>
                  <Label className="text-xs">Min Evidence %</Label>
                  <Input
                    type="number"
                    {...register(`levels.${levelIndex}.criteria.${criterionIndex}.minEvidencePercentage`, { valueAsNumber: true })}
                    placeholder="e.g., 60"
                    className="h-8 text-sm"
                    min={0}
                    max={100}
                  />
                </div>
              )}
            </>
          )}

          <div className="col-span-2">
            <Label className="text-xs">Description *</Label>
            <Textarea
              {...register(`levels.${levelIndex}.criteria.${criterionIndex}.description`)}
              placeholder="Describe this criterion"
              rows={2}
              className="text-sm"
            />
            {errors?.description && (
              <p className="text-xs text-destructive mt-1">{errors.description.message}</p>
            )}
          </div>

          <div className="col-span-2 flex items-center gap-2">
            <Checkbox
              checked={isMandatory}
              onCheckedChange={(checked) => 
                setValue(`levels.${levelIndex}.criteria.${criterionIndex}.isMandatory`, checked)
              }
            />
            <Label className="text-xs font-normal">Mandatory criterion</Label>
          </div>
        </div>

        <Button type="button" variant="ghost" size="sm" onClick={onRemove}>
          <Trash className="h-4 w-4" />
        </Button>
      </div>
    </div>
  )
}
