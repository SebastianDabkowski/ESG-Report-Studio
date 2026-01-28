import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { WarningCircle, Check } from '@phosphor-icons/react'
import type { Assumption } from '@/lib/types'
import { deprecateAssumption, type DeprecateAssumptionPayload } from '@/lib/api'

const deprecateSchema = z.object({
  replacementAssumptionId: z.string().optional(),
  justification: z.string().optional()
}).refine((data) => {
  // Either replacement (non-empty) or justification (non-empty) is required
  const hasReplacement = !!data.replacementAssumptionId && data.replacementAssumptionId.trim().length > 0
  const hasJustification = !!data.justification && data.justification.trim().length > 0
  return hasReplacement || hasJustification
}, {
  message: "Either a replacement assumption or justification is required",
  path: ['justification']
})

type DeprecateFormData = z.infer<typeof deprecateSchema>

interface DeprecateAssumptionDialogProps {
  assumption: Assumption
  availableAssumptions: Assumption[]
  onSuccess: () => void
  onCancel: () => void
}

export function DeprecateAssumptionDialog({ assumption, availableAssumptions, onSuccess, onCancel }: DeprecateAssumptionDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [useReplacement, setUseReplacement] = useState(true)

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors }
  } = useForm<DeprecateFormData>({
    resolver: zodResolver(deprecateSchema),
    defaultValues: {
      replacementAssumptionId: '',
      justification: ''
    }
  })

  const replacementAssumptionId = watch('replacementAssumptionId')

  const onSubmit = async (data: DeprecateFormData) => {
    setIsSubmitting(true)
    setErrorMessage(null)

    try {
      const payload: DeprecateAssumptionPayload = useReplacement
        ? { 
            replacementAssumptionId: data.replacementAssumptionId && data.replacementAssumptionId.trim() 
              ? data.replacementAssumptionId 
              : undefined
          }
        : { justification: data.justification }

      await deprecateAssumption(assumption.id, payload)
      onSuccess()
    } catch (error) {
      console.error('Error deprecating assumption:', error)
      setErrorMessage(error instanceof Error ? error.message : 'Failed to deprecate assumption')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog open={true} onOpenChange={() => onCancel()}>
      <DialogContent className="sm:max-w-[600px]">
        <DialogHeader>
          <DialogTitle>Deprecate Assumption</DialogTitle>
          <DialogDescription>
            When deprecating an assumption, you must either provide a replacement assumption or justify why it's being marked as invalid.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div className="space-y-4">
            <div className="bg-slate-50 p-4 rounded-lg">
              <h4 className="font-semibold text-slate-900 mb-1">{assumption.title}</h4>
              <p className="text-sm text-slate-600">{assumption.description}</p>
              {assumption.linkedDataPointIds.length > 0 && (
                <p className="text-sm text-slate-500 mt-2">
                  Linked to {assumption.linkedDataPointIds.length} data point(s)
                </p>
              )}
            </div>

            <div className="space-y-3">
              <Label>Deprecation Method</Label>
              <div className="flex gap-4">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    checked={useReplacement}
                    onChange={() => setUseReplacement(true)}
                    className="w-4 h-4"
                  />
                  <span className="text-sm">Provide Replacement</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    checked={!useReplacement}
                    onChange={() => setUseReplacement(false)}
                    className="w-4 h-4"
                  />
                  <span className="text-sm">Mark as Invalid</span>
                </label>
              </div>
            </div>

            {useReplacement ? (
              <div>
                <Label htmlFor="replacementAssumptionId">Replacement Assumption</Label>
                <Select
                  value={replacementAssumptionId}
                  onValueChange={(value) => setValue('replacementAssumptionId', value)}
                >
                  <SelectTrigger className={errors.replacementAssumptionId ? 'border-red-500' : ''}>
                    <SelectValue placeholder="Select replacement assumption" />
                  </SelectTrigger>
                  <SelectContent>
                    {availableAssumptions.length === 0 ? (
                      <div className="p-4 text-sm text-slate-500 text-center">
                        No active assumptions available
                      </div>
                    ) : (
                      availableAssumptions.map((a) => (
                        <SelectItem key={a.id} value={a.id}>
                          {a.title}
                        </SelectItem>
                      ))
                    )}
                  </SelectContent>
                </Select>
                {errors.replacementAssumptionId && (
                  <p className="text-sm text-red-600 mt-1">{errors.replacementAssumptionId.message}</p>
                )}
                <p className="text-sm text-slate-500 mt-1">
                  Select an active assumption to replace this one. All linked data points will reference the new version.
                </p>
              </div>
            ) : (
              <div>
                <Label htmlFor="justification">Justification *</Label>
                <Textarea
                  id="justification"
                  {...register('justification')}
                  placeholder="Explain why this assumption is being marked as invalid"
                  rows={4}
                  className={errors.justification ? 'border-red-500' : ''}
                />
                {errors.justification && (
                  <p className="text-sm text-red-600 mt-1">{errors.justification.message}</p>
                )}
                <p className="text-sm text-slate-500 mt-1">
                  Provide a clear justification for why this assumption is no longer valid.
                </p>
              </div>
            )}
          </div>

          {errorMessage && (
            <Alert variant="destructive">
              <WarningCircle className="h-4 w-4" />
              <AlertDescription>{errorMessage}</AlertDescription>
            </Alert>
          )}

          <div className="flex gap-3 pt-4">
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Deprecating...' : 'Deprecate Assumption'}
            </Button>
            <Button type="button" variant="outline" onClick={onCancel}>
              Cancel
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
