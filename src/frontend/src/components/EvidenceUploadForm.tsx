import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { WarningCircle, UploadSimple, FileText } from '@phosphor-icons/react'

const evidenceSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  description: z.string().optional(),
  sourceUrl: z.string().optional()
    .refine(
      (val) => {
        if (!val || val === '') return true
        try {
          const url = new URL(val)
          return url.protocol === 'http:' || url.protocol === 'https:'
        } catch {
          return false
        }
      },
      { message: 'Must be a valid HTTP or HTTPS URL' }
    )
    .refine(
      (val) => !val || val.length <= 2048,
      { message: 'URL must not exceed 2048 characters' }
    ),
})

type EvidenceFormData = z.infer<typeof evidenceSchema>

interface EvidenceUploadFormProps {
  sectionId: string
  uploadedBy: string
  onSubmit: (data: EvidenceFormData & { file?: File }) => void | Promise<void>
  onCancel: () => void
  isSubmitting?: boolean
}

export default function EvidenceUploadForm({ 
  sectionId, 
  uploadedBy, 
  onSubmit, 
  onCancel,
  isSubmitting = false 
}: EvidenceUploadFormProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [fileError, setFileError] = useState<string>('')
  
  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
    setError
  } = useForm<EvidenceFormData>({
    resolver: zodResolver(evidenceSchema),
    defaultValues: {
      title: '',
      description: '',
      sourceUrl: '',
    }
  })

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    setFileError('')
    
    if (file) {
      // Validate file size (10MB max)
      const maxSize = 10 * 1024 * 1024
      if (file.size > maxSize) {
        setFileError('File size must not exceed 10MB')
        e.target.value = ''
        setSelectedFile(null)
        return
      }

      // Validate file type
      const allowedTypes = [
        'application/pdf',
        'application/msword',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        'application/vnd.ms-excel',
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        'text/csv',
        'image/png',
        'image/jpeg',
        'image/jpg'
      ]
      
      if (!allowedTypes.includes(file.type)) {
        setFileError('File type not allowed. Accepted formats: PDF, Word, Excel, CSV, PNG, JPEG')
        e.target.value = ''
        setSelectedFile(null)
        return
      }

      setSelectedFile(file)
    }
  }

  const onFormSubmit = (data: EvidenceFormData) => {
    // Validate that either file or URL is provided
    if (!selectedFile && !data.sourceUrl) {
      setError('root', {
        type: 'manual',
        message: 'Please upload a file or provide a source URL'
      })
      return
    }

    onSubmit({
      ...data,
      file: selectedFile || undefined
    })
  }

  return (
    <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-4">
      {/* Title */}
      <div className="space-y-2">
        <Label htmlFor="title">Title *</Label>
        <Input
          id="title"
          {...register('title')}
          placeholder="e.g., Energy Meter Statement 2024"
          className={errors.title ? 'border-red-500' : ''}
        />
        {errors.title && (
          <p className="text-sm text-red-600">{errors.title.message}</p>
        )}
      </div>

      {/* Description */}
      <div className="space-y-2">
        <Label htmlFor="description">Description</Label>
        <Textarea
          id="description"
          {...register('description')}
          placeholder="Brief description of this evidence..."
          rows={3}
        />
      </div>

      {/* File Upload */}
      <div className="space-y-2">
        <Label htmlFor="file">Upload File</Label>
        <div className="flex items-center gap-2">
          <Input
            id="file"
            type="file"
            onChange={handleFileChange}
            className={`flex-1 ${fileError ? 'border-red-500' : ''}`}
            accept=".pdf,.doc,.docx,.xls,.xlsx,.csv,.png,.jpg,.jpeg"
          />
          {selectedFile && (
            <div className="flex items-center gap-1 text-sm text-muted-foreground">
              <FileText size={16} />
              <span>{selectedFile.name}</span>
            </div>
          )}
        </div>
        {fileError && (
          <p className="text-sm text-red-600">{fileError}</p>
        )}
        <p className="text-xs text-muted-foreground">
          Accepted formats: PDF, Word, Excel, CSV, Images (max 10MB)
        </p>
      </div>

      {/* OR divider */}
      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <span className="w-full border-t" />
        </div>
        <div className="relative flex justify-center text-xs uppercase">
          <span className="bg-background px-2 text-muted-foreground">Or</span>
        </div>
      </div>

      {/* Source URL */}
      <div className="space-y-2">
        <Label htmlFor="sourceUrl">Source URL</Label>
        <Input
          id="sourceUrl"
          {...register('sourceUrl')}
          placeholder="https://example.com/document.pdf"
          className={errors.sourceUrl ? 'border-red-500' : ''}
        />
        {errors.sourceUrl && (
          <p className="text-sm text-red-600">{errors.sourceUrl.message}</p>
        )}
        <p className="text-xs text-muted-foreground">
          Link to external document or data source
        </p>
      </div>

      {/* Validation Error Alert */}
      {(Object.keys(errors).length > 0 || errors.root) && (
        <Alert variant="destructive">
          <WarningCircle size={16} />
          <AlertDescription>
            {errors.root?.message || 'Please fix the errors above before submitting.'}
          </AlertDescription>
        </Alert>
      )}

      {/* Form Actions */}
      <div className="flex justify-end gap-2 pt-4 border-t">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          <UploadSimple size={16} className="mr-2" />
          {isSubmitting ? 'Uploading...' : 'Upload Evidence'}
        </Button>
      </div>
    </form>
  )
}
