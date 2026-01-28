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
import type { RemediationAction, User } from '@/lib/types'
import { createRemediationAction, updateRemediationAction, type CreateRemediationActionPayload, type UpdateRemediationActionPayload } from '@/lib/api'

const remediationActionSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  description: z.string().min(1, 'Description is required'),
  ownerId: z.string().min(1, 'Owner is required'),
  ownerName: z.string().min(1, 'Owner name is required'),
  dueDate: z.string().min(1, 'Due date is required'),
  status: z.enum(['pending', 'in-progress', 'completed', 'cancelled']).optional()
})

type RemediationActionFormData = z.infer<typeof remediationActionSchema>

interface RemediationActionFormProps {
  remediationPlanId: string
  action?: RemediationAction
  users?: User[]
  onSuccess: (action: RemediationAction) => void
  onCancel: () => void
}

export function RemediationActionForm({ 
  remediationPlanId, 
  action, 
  users = [],
  onSuccess, 
  onCancel 
}: RemediationActionFormProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [selectedStatus, setSelectedStatus] = useState<'pending' | 'in-progress' | 'completed' | 'cancelled'>(action?.status || 'pending')
  const [selectedOwnerId, setSelectedOwnerId] = useState<string>(action?.ownerId || '')

  const isEditMode = !!action

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue
  } = useForm<RemediationActionFormData>({
    resolver: zodResolver(remediationActionSchema),
    defaultValues: action ? {
      title: action.title,
      description: action.description,
      ownerId: action.ownerId,
      ownerName: action.ownerName,
      dueDate: action.dueDate.split('T')[0], // Convert ISO to date input format
      status: action.status
    } : {
      title: '',
      description: '',
      ownerId: '',
      ownerName: '',
      dueDate: '',
      status: 'pending'
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

  const onSubmit = async (data: RemediationActionFormData) => {
    setIsSubmitting(true)
    setErrorMessage(null)
    setSuccessMessage(null)

    try {
      if (isEditMode) {
        const payload: UpdateRemediationActionPayload = {
          title: data.title,
          description: data.description,
          ownerId: data.ownerId,
          ownerName: data.ownerName,
          dueDate: new Date(data.dueDate).toISOString(),
          status: selectedStatus
        }
        const updated = await updateRemediationAction(action.id, payload)
        setSuccessMessage('Action updated successfully')
        onSuccess(updated)
      } else {
        const payload: CreateRemediationActionPayload = {
          remediationPlanId,
          title: data.title,
          description: data.description,
          ownerId: data.ownerId,
          ownerName: data.ownerName,
          dueDate: new Date(data.dueDate).toISOString()
        }
        const created = await createRemediationAction(payload)
        setSuccessMessage('Action created successfully')
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
      
      {successMessage && (
        <Alert>
          <Check className="h-4 w-4" />
          <AlertDescription>{successMessage}</AlertDescription>
        </Alert>
      )}

      <div className="space-y-2">
        <Label htmlFor="title">Title *</Label>
        <Input
          id="title"
          {...register('title')}
          placeholder="e.g., Request utility bills from facility management"
        />
        {errors.title && <p className="text-sm text-red-600">{errors.title.message}</p>}
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Description *</Label>
        <Textarea
          id="description"
          {...register('description')}
          placeholder="Describe what needs to be done..."
          rows={3}
        />
        {errors.description && <p className="text-sm text-red-600">{errors.description.message}</p>}
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
        <Label htmlFor="dueDate">Due Date *</Label>
        <Input
          id="dueDate"
          type="date"
          {...register('dueDate')}
        />
        {errors.dueDate && <p className="text-sm text-red-600">{errors.dueDate.message}</p>}
      </div>

      {isEditMode && (
        <div className="space-y-2">
          <Label htmlFor="status">Status *</Label>
          <Select value={selectedStatus} onValueChange={(value: 'pending' | 'in-progress' | 'completed' | 'cancelled') => setSelectedStatus(value)}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="pending">Pending</SelectItem>
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
          {isSubmitting ? 'Saving...' : (isEditMode ? 'Update Action' : 'Create Action')}
        </Button>
      </div>
    </form>
  )
}
