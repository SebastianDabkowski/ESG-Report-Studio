import { useState } from 'react'
import { Plus, X } from '@phosphor-icons/react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import type { NarrativeSourceReference } from '@/lib/types'

interface SourceReferencesManagerProps {
  sourceReferences: NarrativeSourceReference[]
  onChange: (sourceReferences: NarrativeSourceReference[]) => void
  disabled?: boolean
}

const sourceTypeOptions = [
  { value: 'data-point', label: 'Data Point' },
  { value: 'evidence', label: 'Evidence File' },
  { value: 'assumption', label: 'Assumption' },
  { value: 'external-system', label: 'External System' },
  { value: 'uploaded-file', label: 'Uploaded File' },
  { value: 'other', label: 'Other' }
]

export function SourceReferencesManager({ sourceReferences, onChange, disabled }: SourceReferencesManagerProps) {
  const [isAdding, setIsAdding] = useState(false)
  const [newSource, setNewSource] = useState<Partial<NarrativeSourceReference>>({
    sourceType: 'data-point',
    sourceReference: '',
    description: '',
    originSystem: '',
    ownerId: '',
    ownerName: ''
  })

  const handleAddSource = () => {
    if (!newSource.sourceReference || !newSource.description) {
      return
    }

    const source: NarrativeSourceReference = {
      sourceType: newSource.sourceType || 'other',
      sourceReference: newSource.sourceReference,
      description: newSource.description,
      originSystem: newSource.originSystem || undefined,
      ownerId: newSource.ownerId || undefined,
      ownerName: newSource.ownerName || undefined,
      lastUpdated: new Date().toISOString()
    }

    onChange([...sourceReferences, source])
    setNewSource({
      sourceType: 'data-point',
      sourceReference: '',
      description: '',
      originSystem: '',
      ownerId: '',
      ownerName: ''
    })
    setIsAdding(false)
  }

  const handleRemoveSource = (index: number) => {
    const updated = sourceReferences.filter((_, i) => i !== index)
    onChange(updated)
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-medium">Source References</h3>
          <p className="text-xs text-muted-foreground mt-1">
            Link this statement to underlying source data for traceability
          </p>
        </div>
        {!isAdding && !disabled && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => setIsAdding(true)}
            className="gap-2"
          >
            <Plus className="h-4 w-4" />
            Add Source
          </Button>
        )}
      </div>

      {/* Existing source references */}
      {sourceReferences.length > 0 && (
        <div className="space-y-2">
          {sourceReferences.map((source, index) => (
            <Card key={index} className="relative">
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="inline-flex items-center rounded-md bg-blue-50 px-2 py-1 text-xs font-medium text-blue-700 ring-1 ring-inset ring-blue-700/10">
                        {sourceTypeOptions.find(opt => opt.value === source.sourceType)?.label || source.sourceType}
                      </span>
                      <code className="text-xs bg-muted px-2 py-1 rounded">
                        {source.sourceReference}
                      </code>
                    </div>
                    <CardDescription className="mt-2">
                      {source.description}
                    </CardDescription>
                  </div>
                  {!disabled && (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => handleRemoveSource(index)}
                      className="h-8 w-8 p-0"
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </CardHeader>
              {(source.originSystem || source.ownerName) && (
                <CardContent className="pt-0 text-xs text-muted-foreground space-y-1">
                  {source.originSystem && (
                    <div className="flex gap-2">
                      <span className="font-medium">Origin:</span>
                      <span>{source.originSystem}</span>
                    </div>
                  )}
                  {source.ownerName && (
                    <div className="flex gap-2">
                      <span className="font-medium">Owner:</span>
                      <span>{source.ownerName}</span>
                    </div>
                  )}
                  {source.lastUpdated && (
                    <div className="flex gap-2">
                      <span className="font-medium">Last Updated:</span>
                      <span>{new Date(source.lastUpdated).toLocaleDateString()}</span>
                    </div>
                  )}
                </CardContent>
              )}
            </Card>
          ))}
        </div>
      )}

      {/* Add new source form */}
      {isAdding && (
        <Card className="border-dashed">
          <CardHeader>
            <CardTitle className="text-sm">Add Source Reference</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4">
              <div className="space-y-2">
                <Label htmlFor="sourceType" className="text-xs">Source Type *</Label>
                <Select
                  value={newSource.sourceType}
                  onValueChange={(value) => setNewSource({ ...newSource, sourceType: value })}
                >
                  <SelectTrigger id="sourceType">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {sourceTypeOptions.map((option) => (
                      <SelectItem key={option.value} value={option.value}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="sourceReference" className="text-xs">
                  Source Reference ID *
                </Label>
                <Input
                  id="sourceReference"
                  value={newSource.sourceReference}
                  onChange={(e) => setNewSource({ ...newSource, sourceReference: e.target.value })}
                  placeholder="e.g., DP-2024-001, EV-042, FILE-789"
                  className="text-sm"
                />
                <p className="text-xs text-muted-foreground">
                  Unique identifier for the source (data point ID, evidence ID, file reference, etc.)
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="description" className="text-xs">Description *</Label>
                <Textarea
                  id="description"
                  value={newSource.description}
                  onChange={(e) => setNewSource({ ...newSource, description: e.target.value })}
                  placeholder="Describe what data this source provides..."
                  rows={2}
                  className="text-sm"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="originSystem" className="text-xs">Origin System (Optional)</Label>
                <Input
                  id="originSystem"
                  value={newSource.originSystem}
                  onChange={(e) => setNewSource({ ...newSource, originSystem: e.target.value })}
                  placeholder="e.g., HR System, Energy Management System, operations_data.csv"
                  className="text-sm"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="ownerName" className="text-xs">Source Owner (Optional)</Label>
                <Input
                  id="ownerName"
                  value={newSource.ownerName}
                  onChange={(e) => setNewSource({ ...newSource, ownerName: e.target.value })}
                  placeholder="e.g., Energy Manager, Sustainability Lead"
                  className="text-sm"
                />
              </div>
            </div>

            <div className="flex gap-2">
              <Button
                type="button"
                onClick={handleAddSource}
                size="sm"
                disabled={!newSource.sourceReference || !newSource.description}
              >
                Add Source
              </Button>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => {
                  setIsAdding(false)
                  setNewSource({
                    sourceType: 'data-point',
                    sourceReference: '',
                    description: '',
                    originSystem: '',
                    ownerId: '',
                    ownerName: ''
                  })
                }}
              >
                Cancel
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {sourceReferences.length === 0 && !isAdding && (
        <div className="text-center py-6 border border-dashed rounded-lg">
          <p className="text-sm text-muted-foreground">
            No source references yet. Add sources to improve traceability.
          </p>
        </div>
      )}
    </div>
  )
}
