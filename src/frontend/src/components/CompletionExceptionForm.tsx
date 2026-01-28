import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { useState, useRef, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { WarningCircle, Check } from '@phosphor-icons/react'
import type { CompletionException, ExceptionType } from '@/lib/types'
import { createCompletionException } from '@/lib/api'

const exceptionSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  exceptionType: z.enum(['missing-data', 'estimated-data', 'simplified-scope', 'other'] as const, {
    errorMap: () => ({ message: 'Exception type is required' })
  }),
  justification: z.string().min(10, 'Justification must be at least 10 characters'),
  expiresAt: z.string().optional().refine((date) => {
    if (!date) return true // Optional field
    const expirationDate = new Date(date)
    const today = new Date()
    today.setHours(0, 0, 0, 0) // Reset to start of day for comparison
    return expirationDate >= today
  }, {
    message: 'Expiration date must be today or in the future'
  })
})

type ExceptionFormData = z.infer<typeof exceptionSchema>

interface CompletionExceptionFormProps {
  sectionId: string
  dataPointId?: string
  requestedBy: string
  onSuccess: (exception: CompletionException) => void
  onCancel: () => void
}

export function CompletionExceptionForm({ 
  sectionId, 
  dataPointId, 
  requestedBy, 
  onSuccess, 
  onCancel 
}: CompletionExceptionFormProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [selectedType, setSelectedType] = useState<ExceptionType>('missing-data')
  const successTimeoutRef = useRef<NodeJS.Timeout | null>(null)

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (successTimeoutRef.current) {
        clearTimeout(successTimeoutRef.current)
      }
    }
  }, [])

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue
  } = useForm<ExceptionFormData>({
    resolver: zodResolver(exceptionSchema),
    defaultValues: {
      title: '',
      exceptionType: 'missing-data',
      justification: '',
      expiresAt: ''
    }
  })

  const onSubmit = async (data: ExceptionFormData) => {
    setIsSubmitting(true)
    setErrorMessage(null)
    setSuccessMessage(null)

    try {
      const exception = await createCompletionException({
        sectionId,
        dataPointId,
        title: data.title,
        exceptionType: data.exceptionType,
        justification: data.justification,
        requestedBy,
        expiresAt: data.expiresAt || undefined
      })

      setSuccessMessage('Exception request created successfully')
      successTimeoutRef.current = setTimeout(() => {
        onSuccess(exception)
      }, 500)
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : 'Failed to create exception')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      <div className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="title">Exception Title</Label>
          <Input
            id="title"
            {...register('title')}
            placeholder="Brief description of the exception"
          />
          {errors.title && (
            <p className="text-sm text-red-600">{errors.title.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="exceptionType">Exception Type</Label>
          <Select 
            value={selectedType}
            onValueChange={(value: ExceptionType) => {
              setSelectedType(value)
              setValue('exceptionType', value)
            }}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select exception type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="missing-data">Missing Data</SelectItem>
              <SelectItem value="estimated-data">Estimated Data</SelectItem>
              <SelectItem value="simplified-scope">Simplified Scope</SelectItem>
              <SelectItem value="other">Other</SelectItem>
            </SelectContent>
          </Select>
          {errors.exceptionType && (
            <p className="text-sm text-red-600">{errors.exceptionType.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="justification">
            Justification
            <span className="text-sm text-gray-500 ml-2">
              (Explain why this exception is necessary and acceptable)
            </span>
          </Label>
          <Textarea
            id="justification"
            {...register('justification')}
            placeholder="Provide detailed justification for this exception..."
            rows={5}
          />
          {errors.justification && (
            <p className="text-sm text-red-600">{errors.justification.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="expiresAt">
            Expiration Date (Optional)
            <span className="text-sm text-gray-500 ml-2">
              (When should this exception be re-evaluated?)
            </span>
          </Label>
          <Input
            id="expiresAt"
            type="date"
            {...register('expiresAt')}
          />
          {errors.expiresAt && (
            <p className="text-sm text-red-600">{errors.expiresAt.message}</p>
          )}
        </div>
      </div>

      {errorMessage && (
        <Alert variant="destructive">
          <WarningCircle className="h-4 w-4" />
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      {successMessage && (
        <Alert className="bg-green-50 text-green-900 border-green-200">
          <Check className="h-4 w-4" />
          <AlertDescription>{successMessage}</AlertDescription>
        </Alert>
      )}

      <div className="flex gap-3 justify-end">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creating...' : 'Create Exception Request'}
        </Button>
      </div>
    </form>
  )
}
