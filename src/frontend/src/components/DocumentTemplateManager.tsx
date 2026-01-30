import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { 
  getDocumentTemplates, 
  createDocumentTemplate, 
  updateDocumentTemplate, 
  deleteDocumentTemplate,
  getTemplateUsageHistory
} from '@/lib/api'
import type { DocumentTemplate, CreateDocumentTemplateRequest, UpdateDocumentTemplateRequest } from '@/lib/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Pencil, Trash2, Plus, Check, X, History } from 'lucide-react'

interface DocumentTemplateManagerProps {
  userId: string
  userName: string
}

export function DocumentTemplateManager({ userId, userName }: DocumentTemplateManagerProps) {
  const queryClient = useQueryClient()
  const [isCreating, setIsCreating] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [viewingHistoryId, setViewingHistoryId] = useState<string | null>(null)
  const [formData, setFormData] = useState<Partial<DocumentTemplate>>({})

  const { data: templates = [], isLoading } = useQuery({
    queryKey: ['document-templates'],
    queryFn: () => getDocumentTemplates()
  })

  const { data: usageHistory = [] } = useQuery({
    queryKey: ['template-usage', viewingHistoryId],
    queryFn: () => viewingHistoryId ? getTemplateUsageHistory(viewingHistoryId) : Promise.resolve([]),
    enabled: !!viewingHistoryId
  })

  const createMutation = useMutation({
    mutationFn: (data: CreateDocumentTemplateRequest) => createDocumentTemplate(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['document-templates'] })
      setIsCreating(false)
      setFormData({})
    }
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateDocumentTemplateRequest }) => 
      updateDocumentTemplate(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['document-templates'] })
      setEditingId(null)
      setFormData({})
    }
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteDocumentTemplate(id, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['document-templates'] })
    }
  })

  const handleCreate = () => {
    if (!formData.name || !formData.templateType) return
    
    createMutation.mutate({
      name: formData.name,
      description: formData.description,
      templateType: formData.templateType as 'pdf' | 'docx' | 'excel',
      configuration: formData.configuration || '{}',
      isDefault: formData.isDefault || false,
      createdBy: userId
    })
  }

  const handleUpdate = (id: string) => {
    if (!formData.name) return
    
    updateMutation.mutate({
      id,
      data: {
        name: formData.name,
        description: formData.description,
        configuration: formData.configuration || '{}',
        isDefault: formData.isDefault || false,
        isActive: formData.isActive ?? true,
        updatedBy: userId
      }
    })
  }

  const startEdit = (template: DocumentTemplate) => {
    setEditingId(template.id)
    setFormData(template)
    setIsCreating(false)
  }

  const cancelEdit = () => {
    setEditingId(null)
    setIsCreating(false)
    setFormData({})
  }

  if (isLoading) {
    return <div className="p-4">Loading document templates...</div>
  }

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">Document Templates</h2>
          <p className="text-sm text-muted-foreground">
            Manage versioned export templates for reports
          </p>
        </div>
        <Button onClick={() => setIsCreating(true)} disabled={isCreating}>
          <Plus className="w-4 h-4 mr-2" />
          New Template
        </Button>
      </div>

      {isCreating && (
        <Card>
          <CardHeader>
            <CardTitle>Create Document Template</CardTitle>
            <CardDescription>Configure export template with versioning</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="name">Name *</Label>
                <Input
                  id="name"
                  value={formData.name || ''}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="e.g., Standard PDF Template"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="templateType">Template Type *</Label>
                <select
                  id="templateType"
                  value={formData.templateType || 'pdf'}
                  onChange={(e) => setFormData({ ...formData, templateType: e.target.value as any })}
                  className="w-full px-3 py-2 border rounded-md"
                >
                  <option value="pdf">PDF</option>
                  <option value="docx">DOCX</option>
                  <option value="excel">Excel</option>
                </select>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={formData.description || ''}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                placeholder="Optional description"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="configuration">Configuration (JSON)</Label>
              <Textarea
                id="configuration"
                value={formData.configuration || '{}'}
                onChange={(e) => setFormData({ ...formData, configuration: e.target.value })}
                placeholder='{"pageSize": "A4", "margins": 20}'
                className="font-mono text-sm"
                rows={4}
              />
            </div>

            <div className="flex items-center space-x-2">
              <input
                type="checkbox"
                id="isDefault"
                checked={formData.isDefault || false}
                onChange={(e) => setFormData({ ...formData, isDefault: e.target.checked })}
                className="h-4 w-4"
              />
              <Label htmlFor="isDefault" className="cursor-pointer">
                Set as default template for this type
              </Label>
            </div>

            <div className="flex gap-2">
              <Button onClick={handleCreate} disabled={!formData.name || !formData.templateType || createMutation.isPending}>
                <Check className="w-4 h-4 mr-2" />
                Create
              </Button>
              <Button variant="outline" onClick={cancelEdit}>
                <X className="w-4 h-4 mr-2" />
                Cancel
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4">
        {templates.map((template) => (
          <Card key={template.id}>
            <CardHeader>
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle className="flex items-center gap-2">
                    {template.name}
                    <Badge variant="outline">{template.templateType.toUpperCase()}</Badge>
                    <Badge variant="secondary">v{template.version}</Badge>
                    {template.isDefault && (
                      <Badge variant="default">Default</Badge>
                    )}
                    {!template.isActive && (
                      <Badge variant="secondary">Inactive</Badge>
                    )}
                  </CardTitle>
                  {template.description && (
                    <CardDescription>{template.description}</CardDescription>
                  )}
                </div>
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setViewingHistoryId(viewingHistoryId === template.id ? null : template.id)}
                  >
                    <History className="w-4 h-4" />
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => startEdit(template)}
                    disabled={editingId === template.id}
                  >
                    <Pencil className="w-4 h-4" />
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      if (confirm(`Delete template "${template.name}"?`)) {
                        deleteMutation.mutate(template.id)
                      }
                    }}
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
                </div>
              </div>
            </CardHeader>
            {editingId === template.id ? (
              <CardContent className="space-y-4 border-t pt-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Name *</Label>
                    <Input
                      value={formData.name || ''}
                      onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label>Template Type</Label>
                    <Input value={template.templateType.toUpperCase()} disabled />
                  </div>
                </div>

                <div className="space-y-2">
                  <Label>Description</Label>
                  <Textarea
                    value={formData.description || ''}
                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  />
                </div>

                <div className="space-y-2">
                  <Label>Configuration (JSON)</Label>
                  <Textarea
                    value={formData.configuration || '{}'}
                    onChange={(e) => setFormData({ ...formData, configuration: e.target.value })}
                    className="font-mono text-sm"
                    rows={4}
                  />
                  <p className="text-xs text-muted-foreground">
                    Note: Changing configuration will create a new version
                  </p>
                </div>

                <div className="flex items-center gap-4">
                  <div className="flex items-center space-x-2">
                    <input
                      type="checkbox"
                      id={`isDefault-${template.id}`}
                      checked={formData.isDefault || false}
                      onChange={(e) => setFormData({ ...formData, isDefault: e.target.checked })}
                      className="h-4 w-4"
                    />
                    <Label htmlFor={`isDefault-${template.id}`} className="cursor-pointer">
                      Default
                    </Label>
                  </div>
                  <div className="flex items-center space-x-2">
                    <input
                      type="checkbox"
                      id={`isActive-${template.id}`}
                      checked={formData.isActive ?? true}
                      onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                      className="h-4 w-4"
                    />
                    <Label htmlFor={`isActive-${template.id}`} className="cursor-pointer">
                      Active
                    </Label>
                  </div>
                </div>

                <div className="flex gap-2">
                  <Button 
                    onClick={() => handleUpdate(template.id)} 
                    disabled={!formData.name || updateMutation.isPending}
                  >
                    <Check className="w-4 h-4 mr-2" />
                    Save
                  </Button>
                  <Button variant="outline" onClick={cancelEdit}>
                    <X className="w-4 h-4 mr-2" />
                    Cancel
                  </Button>
                </div>
              </CardContent>
            ) : viewingHistoryId === template.id ? (
              <CardContent className="border-t pt-4">
                <h4 className="font-semibold mb-3">Usage History</h4>
                {usageHistory.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No usage history yet</p>
                ) : (
                  <div className="space-y-2">
                    {usageHistory.map((record) => (
                      <div key={record.id} className="text-sm border-l-2 border-muted pl-3 py-1">
                        <p>
                          <span className="font-medium">Version {record.templateVersion}</span>
                          {' - '}
                          {record.exportType.toUpperCase()} export
                        </p>
                        <p className="text-muted-foreground text-xs">
                          {new Date(record.generatedAt).toLocaleString()}
                        </p>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            ) : (
              <CardContent className="border-t pt-4">
                <div className="text-sm">
                  <p className="text-muted-foreground">Configuration</p>
                  <pre className="mt-1 p-2 bg-muted rounded text-xs overflow-x-auto">
                    {(() => {
                      try {
                        return JSON.stringify(JSON.parse(template.configuration || '{}'), null, 2)
                      } catch {
                        return 'Invalid JSON configuration'
                      }
                    })()}
                  </pre>
                </div>
              </CardContent>
            )}
          </Card>
        ))}
      </div>

      {templates.length === 0 && !isCreating && (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-muted-foreground">No document templates configured</p>
            <Button onClick={() => setIsCreating(true)} className="mt-4">
              <Plus className="w-4 h-4 mr-2" />
              Create First Template
            </Button>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
