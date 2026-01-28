import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { WarningCircle, Check } from '@phosphor-icons/react'
import type { RemediationPlan, User } from '@/lib/types'
import { createRemediationPlan, updateRemediationPlan, type CreateRemediationPlanPayload, type UpdateRemediationPlanPayload } from '@/lib/api'

const remediationPlanSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  description: z.string().min(1, 'Description is required'),
  targetPeriod: z.string().min(1, 'Target period is required'),
  ownerId: z.string().min(1, 'Owner is required'),
  ownerName: z.string().min(1, 'Owner name is required'),
  priority: z.enum(['low', 'medium', 'high']),
  status: z.enum(['planned', 'in-progress', 'completed', 'cancelled']).optional()
})

type RemediationPlanFormData = z.infer<typeof remediationPlanSchema>

interface RemediationPlanFormProps {
  sectionId: string
  plan?: RemediationPlan
  gapId?: string
  assumptionId?: string
  dataPointId?: string
  users?: User[]
  onSuccess: (plan: RemediationPlan) => void
  onCancel: () => void
}

export function RemediationPlanForm({ 
  sectionId, 
  plan, 
  gapId, 
  assumptionId, 
  dataPointId,
  users = [],
  onSuccess, 
  onCancel 
}: RemediationPlanFormProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [selectedPriority, setSelectedPriority] = useState<'low' | 'medium' | 'high'>(plan?.priority || 'medium')
  const [selectedStatus, setSelectedStatus] = useState<'planned' | 'in-progress' | 'completed' | 'cancelled'>(plan?.status || 'planned')
  const [selectedOwnerId, setSelectedOwnerId] = useState<string>(plan?.ownerId || '')

  const isEditMode = !!plan

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue
  } = useForm<RemediationPlanFormData>({
    resolver: zodResolver(remediationPlanSchema),
    defaultValues: plan ? {
      title: plan.title,
      description: plan.description,
      targetPeriod: plan.targetPeriod,
      ownerId: plan.ownerId,
      ownerName: plan.ownerName,
      priority: plan.priority,
      status: plan.status
    } : {
      title: '',
      description: '',
      targetPeriod: '',
      ownerId: '',
      ownerName: '',
      priority: 'medium',
      status: 'planned'
    }
  })

  const handleOwnerChange = (userId: string) => {
    setSelectedOwnerId(userId)
    setValue('ownerId', userId)
    const user = users.find(u => u.id === userId)
    if (user) {
      setValue('ownerName', user.name)
    }
  }

  const onSubmit = async (data: RemediationPlanFormData) => {
    setIsSubmitting(true)
    setErrorMessage(null)

    try {
      if (isEditMode) {
        const payload: UpdateRemediationPlanPayload = {
          title: data.title,
          description: data.description,
          targetPeriod: data.targetPeriod,
          ownerId: data.ownerId,
          ownerName: data.ownerName,
          priority: selectedPriority,
          status: selectedStatus
        }
        const updated = await updateRemediationPlan(plan.id, payload)
        onSuccess(updated)
      } else {
        const payload: CreateRemediationPlanPayload = {
          sectionId,
          title: data.title,
          description: data.description,
          targetPeriod: data.targetPeriod,
          ownerId: data.ownerId,
          ownerName: data.ownerName,
          priority: selectedPriority,
          gapId,
          assumptionId,
          dataPointId
        }
        const created = await createRemediationPlan(payload)
        onSuccess(created)
      }
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : 'An error occurred')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {errorMessage && (
        <Alert variant="destructive">
          <WarningCircle className="h-4 w-4" />
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <div className="space-y-2">
        <Label htmlFor="title">Title *</Label>
        <Input
          id="title"
          {...register('title')}
          placeholder="e.g., Obtain actual energy consumption data"
        />
        {errors.title && <p className="text-sm text-red-600">{errors.title.message}</p>}
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Description *</Label>
        <Textarea
          id="description"
          {...register('description')}
          placeholder="Describe what needs to be addressed and why..."
          rows={4}
        />
        {errors.description && <p className="text-sm text-red-600">{errors.description.message}</p>}
      </div>

      <div className="space-y-2">
        <Label htmlFor="targetPeriod">Target Period *</Label>
        <Input
          id="targetPeriod"
          {...register('targetPeriod')}
          placeholder="e.g., Q1 2026, FY 2026"
        />
        {errors.targetPeriod && <p className="text-sm text-red-600">{errors.targetPeriod.message}</p>}
      </div>

      <div className="space-y-2">
        <Label htmlFor="owner">Owner *</Label>
        <Select value={selectedOwnerId} onValueChange={handleOwnerChange}>
          <SelectTrigger>
            <SelectValue placeholder="Select owner" />
          </SelectTrigger>
          <SelectContent>
            {users.map(user => (
              <SelectItem key={user.id} value={user.id}>
                {user.name} ({user.email})
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <input type="hidden" {...register('ownerId')} />
        <input type="hidden" {...register('ownerName')} />
        {errors.ownerId && <p className="text-sm text-red-600">{errors.ownerId.message}</p>}
      </div>

      <div className="space-y-2">
        <Label htmlFor="priority">Priority *</Label>
        <Select value={selectedPriority} onValueChange={(value: 'low' | 'medium' | 'high') => setSelectedPriority(value)}>
          <SelectTrigger>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="low">Low</SelectItem>
            <SelectItem value="medium">Medium</SelectItem>
            <SelectItem value="high">High</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {isEditMode && (
        <div className="space-y-2">
          <Label htmlFor="status">Status *</Label>
          <Select value={selectedStatus} onValueChange={(value: 'planned' | 'in-progress' | 'completed' | 'cancelled') => setSelectedStatus(value)}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="planned">Planned</SelectItem>
              <SelectItem value="in-progress">In Progress</SelectItem>
              <SelectItem value="completed">Completed</SelectItem>
              <SelectItem value="cancelled">Cancelled</SelectItem>
            </SelectContent>
          </Select>
        </div>
      )}

      <div className="flex justify-end space-x-2 pt-4">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving...' : (isEditMode ? 'Update Plan' : 'Create Plan')}
        </Button>
      </div>
    </form>
  )
}
