import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getStandardsCatalog,
  getStandardDisclosures,
  createStandardDisclosure,
  updateStandardDisclosure,
  deleteStandardDisclosure,
  type StandardsCatalogItem,
  type StandardDisclosure,
  type CreateStandardDisclosureRequest
} from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Plus, Edit, Trash2, Loader2 } from 'lucide-react'

export default function StandardDisclosuresManagement() {
  const [selectedStandardId, setSelectedStandardId] = useState<string>('')
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [editingDisclosure, setEditingDisclosure] = useState<StandardDisclosure | null>(null)
  const [formData, setFormData] = useState<CreateStandardDisclosureRequest>({
    standardId: '',
    disclosureCode: '',
    title: '',
    description: '',
    category: 'environmental',
    topic: '',
    isMandatory: true
  })

  const queryClient = useQueryClient()

  // Fetch standards catalog
  const { data: standards = [], isLoading: isLoadingStandards } = useQuery({
    queryKey: ['standards-catalog', false],
    queryFn: () => getStandardsCatalog(false)
  })

  // Fetch disclosures for selected standard
  const { data: disclosures = [], isLoading: isLoadingDisclosures } = useQuery({
    queryKey: ['standard-disclosures', selectedStandardId],
    queryFn: () => getStandardDisclosures(selectedStandardId),
    enabled: !!selectedStandardId
  })

  // Create disclosure mutation
  const createMutation = useMutation({
    mutationFn: createStandardDisclosure,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['standard-disclosures'] })
      setIsCreateDialogOpen(false)
      resetForm()
    }
  })

  // Update disclosure mutation
  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => updateStandardDisclosure(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['standard-disclosures'] })
      setEditingDisclosure(null)
      resetForm()
    }
  })

  // Delete disclosure mutation
  const deleteMutation = useMutation({
    mutationFn: deleteStandardDisclosure,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['standard-disclosures'] })
    }
  })

  const resetForm = () => {
    setFormData({
      standardId: selectedStandardId,
      disclosureCode: '',
      title: '',
      description: '',
      category: 'environmental',
      topic: '',
      isMandatory: true
    })
  }

  const handleCreate = () => {
    createMutation.mutate({ ...formData, standardId: selectedStandardId })
  }

  const handleUpdate = () => {
    if (!editingDisclosure) return
    updateMutation.mutate({
      id: editingDisclosure.id,
      data: {
        title: formData.title,
        description: formData.description,
        category: formData.category,
        topic: formData.topic,
        isMandatory: formData.isMandatory
      }
    })
  }

  const handleEdit = (disclosure: StandardDisclosure) => {
    setEditingDisclosure(disclosure)
    setFormData({
      standardId: disclosure.standardId,
      disclosureCode: disclosure.disclosureCode,
      title: disclosure.title,
      description: disclosure.description,
      category: disclosure.category,
      topic: disclosure.topic || '',
      isMandatory: disclosure.isMandatory
    })
  }

  const handleDelete = (id: string) => {
    if (confirm('Are you sure you want to delete this disclosure? This will also remove all associated mappings.')) {
      deleteMutation.mutate(id)
    }
  }

  const handleOpenCreateDialog = () => {
    resetForm()
    setIsCreateDialogOpen(true)
  }

  if (isLoadingStandards) {
    return (
      <div className="flex items-center justify-center p-8">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
      </div>
    )
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Standard Disclosures</h1>
          <p className="text-gray-500 mt-1">
            Manage individual disclosure requirements within reporting standards
          </p>
        </div>
      </div>

      {/* Standard Selection */}
      <Card>
        <CardHeader>
          <CardTitle>Select Standard</CardTitle>
          <CardDescription>Choose a standard to manage its disclosures</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-4">
            <div className="flex-1">
              <Select value={selectedStandardId} onValueChange={setSelectedStandardId}>
                <SelectTrigger>
                  <SelectValue placeholder="Select a standard" />
                </SelectTrigger>
                <SelectContent>
                  {standards.map((standard) => (
                    <SelectItem key={standard.id} value={standard.id}>
                      {standard.title} ({standard.version})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            {selectedStandardId && (
              <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
                <DialogTrigger asChild>
                  <Button onClick={handleOpenCreateDialog}>
                    <Plus className="h-4 w-4 mr-2" />
                    Add Disclosure
                  </Button>
                </DialogTrigger>
                <DialogContent className="max-w-2xl">
                  <DialogHeader>
                    <DialogTitle>Add New Disclosure</DialogTitle>
                    <DialogDescription>
                      Create a new disclosure requirement for this standard
                    </DialogDescription>
                  </DialogHeader>
                  <div className="space-y-4 py-4">
                    <div className="space-y-2">
                      <Label htmlFor="code">Disclosure Code*</Label>
                      <Input
                        id="code"
                        placeholder="e.g., ESRS E1-1, GRI 305-1"
                        value={formData.disclosureCode}
                        onChange={(e) => setFormData({ ...formData, disclosureCode: e.target.value })}
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="title">Title*</Label>
                      <Input
                        id="title"
                        placeholder="Enter disclosure title"
                        value={formData.title}
                        onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="description">Description*</Label>
                      <Textarea
                        id="description"
                        placeholder="Enter disclosure description"
                        value={formData.description}
                        onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                        rows={3}
                      />
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-2">
                        <Label htmlFor="category">Category*</Label>
                        <Select
                          value={formData.category}
                          onValueChange={(value: any) => setFormData({ ...formData, category: value })}
                        >
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="environmental">Environmental</SelectItem>
                            <SelectItem value="social">Social</SelectItem>
                            <SelectItem value="governance">Governance</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="topic">Topic (Optional)</Label>
                        <Input
                          id="topic"
                          placeholder="e.g., Climate Change, Workforce"
                          value={formData.topic}
                          onChange={(e) => setFormData({ ...formData, topic: e.target.value })}
                        />
                      </div>
                    </div>
                    <div className="flex items-center space-x-2">
                      <Checkbox
                        id="mandatory"
                        checked={formData.isMandatory}
                        onCheckedChange={(checked) => setFormData({ ...formData, isMandatory: !!checked })}
                      />
                      <Label htmlFor="mandatory" className="cursor-pointer">
                        This disclosure is mandatory for compliance
                      </Label>
                    </div>
                  </div>
                  <DialogFooter>
                    <Button variant="outline" onClick={() => setIsCreateDialogOpen(false)}>
                      Cancel
                    </Button>
                    <Button onClick={handleCreate} disabled={createMutation.isPending}>
                      {createMutation.isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                      Create Disclosure
                    </Button>
                  </DialogFooter>
                </DialogContent>
              </Dialog>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Disclosures Table */}
      {selectedStandardId && (
        <Card>
          <CardHeader>
            <CardTitle>Disclosures</CardTitle>
            <CardDescription>
              {disclosures.length} disclosure{disclosures.length !== 1 ? 's' : ''} defined
            </CardDescription>
          </CardHeader>
          <CardContent>
            {isLoadingDisclosures ? (
              <div className="flex items-center justify-center p-8">
                <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
              </div>
            ) : disclosures.length === 0 ? (
              <div className="text-center text-gray-500 py-8">
                No disclosures defined for this standard yet. Click "Add Disclosure" to create one.
              </div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Code</TableHead>
                    <TableHead>Title</TableHead>
                    <TableHead>Category</TableHead>
                    <TableHead>Topic</TableHead>
                    <TableHead className="text-center">Mandatory</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {disclosures.map((disclosure) => (
                    <TableRow key={disclosure.id}>
                      <TableCell className="font-mono text-sm">{disclosure.disclosureCode}</TableCell>
                      <TableCell className="max-w-md">
                        <div className="font-medium">{disclosure.title}</div>
                        <div className="text-sm text-gray-500 mt-1">{disclosure.description}</div>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline">
                          {disclosure.category.charAt(0).toUpperCase() + disclosure.category.slice(1)}
                        </Badge>
                      </TableCell>
                      <TableCell>{disclosure.topic || '-'}</TableCell>
                      <TableCell className="text-center">
                        {disclosure.isMandatory ? (
                          <Badge className="bg-blue-600">Yes</Badge>
                        ) : (
                          <Badge variant="outline">No</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex items-center justify-end gap-2">
                          <Dialog>
                            <DialogTrigger asChild>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleEdit(disclosure)}
                              >
                                <Edit className="h-4 w-4" />
                              </Button>
                            </DialogTrigger>
                            <DialogContent className="max-w-2xl">
                              <DialogHeader>
                                <DialogTitle>Edit Disclosure</DialogTitle>
                                <DialogDescription>
                                  Update disclosure details (code cannot be changed)
                                </DialogDescription>
                              </DialogHeader>
                              <div className="space-y-4 py-4">
                                <div className="space-y-2">
                                  <Label>Disclosure Code</Label>
                                  <Input value={formData.disclosureCode} disabled />
                                </div>
                                <div className="space-y-2">
                                  <Label htmlFor="edit-title">Title*</Label>
                                  <Input
                                    id="edit-title"
                                    value={formData.title}
                                    onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                                  />
                                </div>
                                <div className="space-y-2">
                                  <Label htmlFor="edit-description">Description*</Label>
                                  <Textarea
                                    id="edit-description"
                                    value={formData.description}
                                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                                    rows={3}
                                  />
                                </div>
                                <div className="grid grid-cols-2 gap-4">
                                  <div className="space-y-2">
                                    <Label>Category*</Label>
                                    <Select
                                      value={formData.category}
                                      onValueChange={(value: any) => setFormData({ ...formData, category: value })}
                                    >
                                      <SelectTrigger>
                                        <SelectValue />
                                      </SelectTrigger>
                                      <SelectContent>
                                        <SelectItem value="environmental">Environmental</SelectItem>
                                        <SelectItem value="social">Social</SelectItem>
                                        <SelectItem value="governance">Governance</SelectItem>
                                      </SelectContent>
                                    </Select>
                                  </div>
                                  <div className="space-y-2">
                                    <Label htmlFor="edit-topic">Topic (Optional)</Label>
                                    <Input
                                      id="edit-topic"
                                      value={formData.topic}
                                      onChange={(e) => setFormData({ ...formData, topic: e.target.value })}
                                    />
                                  </div>
                                </div>
                                <div className="flex items-center space-x-2">
                                  <Checkbox
                                    id="edit-mandatory"
                                    checked={formData.isMandatory}
                                    onCheckedChange={(checked) => setFormData({ ...formData, isMandatory: !!checked })}
                                  />
                                  <Label htmlFor="edit-mandatory" className="cursor-pointer">
                                    This disclosure is mandatory for compliance
                                  </Label>
                                </div>
                              </div>
                              <DialogFooter>
                                <Button variant="outline" onClick={() => setEditingDisclosure(null)}>
                                  Cancel
                                </Button>
                                <Button onClick={handleUpdate} disabled={updateMutation.isPending}>
                                  {updateMutation.isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                                  Update Disclosure
                                </Button>
                              </DialogFooter>
                            </DialogContent>
                          </Dialog>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDelete(disclosure.id)}
                            disabled={deleteMutation.isPending}
                          >
                            <Trash2 className="h-4 w-4 text-red-600" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      )}

      {!selectedStandardId && (
        <Card>
          <CardContent className="p-8">
            <div className="text-center text-gray-500">
              Please select a standard to view and manage its disclosures.
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
