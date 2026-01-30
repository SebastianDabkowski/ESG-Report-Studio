import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { 
  getBrandingProfiles, 
  createBrandingProfile, 
  updateBrandingProfile, 
  deleteBrandingProfile 
} from '@/lib/api'
import type { BrandingProfile, CreateBrandingProfileRequest, UpdateBrandingProfileRequest } from '@/lib/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Pencil, Trash2, Plus, Check, X } from 'lucide-react'

interface BrandingProfileManagerProps {
  userId: string
  userName: string
}

export function BrandingProfileManager({ userId, userName }: BrandingProfileManagerProps) {
  const queryClient = useQueryClient()
  const [isCreating, setIsCreating] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState<Partial<BrandingProfile>>({})

  const { data: profiles = [], isLoading } = useQuery({
    queryKey: ['branding-profiles'],
    queryFn: getBrandingProfiles
  })

  const createMutation = useMutation({
    mutationFn: (data: CreateBrandingProfileRequest) => createBrandingProfile(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['branding-profiles'] })
      setIsCreating(false)
      setFormData({})
    }
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateBrandingProfileRequest }) => 
      updateBrandingProfile(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['branding-profiles'] })
      setEditingId(null)
      setFormData({})
    }
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteBrandingProfile(id, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['branding-profiles'] })
    }
  })

  const handleCreate = () => {
    if (!formData.name) return
    
    createMutation.mutate({
      name: formData.name,
      description: formData.description,
      subsidiaryName: formData.subsidiaryName,
      primaryColor: formData.primaryColor,
      secondaryColor: formData.secondaryColor,
      accentColor: formData.accentColor,
      footerText: formData.footerText,
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
        subsidiaryName: formData.subsidiaryName,
        primaryColor: formData.primaryColor,
        secondaryColor: formData.secondaryColor,
        accentColor: formData.accentColor,
        footerText: formData.footerText,
        isDefault: formData.isDefault || false,
        isActive: formData.isActive ?? true,
        updatedBy: userId
      }
    })
  }

  const startEdit = (profile: BrandingProfile) => {
    setEditingId(profile.id)
    setFormData(profile)
    setIsCreating(false)
  }

  const cancelEdit = () => {
    setEditingId(null)
    setIsCreating(false)
    setFormData({})
  }

  if (isLoading) {
    return <div className="p-4">Loading branding profiles...</div>
  }

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">Branding Profiles</h2>
          <p className="text-sm text-muted-foreground">
            Manage corporate branding for document exports
          </p>
        </div>
        <Button onClick={() => setIsCreating(true)} disabled={isCreating}>
          <Plus className="w-4 h-4 mr-2" />
          New Profile
        </Button>
      </div>

      {isCreating && (
        <Card>
          <CardHeader>
            <CardTitle>Create Branding Profile</CardTitle>
            <CardDescription>Configure corporate identity for exports</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="name">Name *</Label>
                <Input
                  id="name"
                  value={formData.name || ''}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="e.g., Main Brand"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="subsidiaryName">Subsidiary Name</Label>
                <Input
                  id="subsidiaryName"
                  value={formData.subsidiaryName || ''}
                  onChange={(e) => setFormData({ ...formData, subsidiaryName: e.target.value })}
                  placeholder="e.g., EMEA Division"
                />
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

            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label htmlFor="primaryColor">Primary Color</Label>
                <div className="flex gap-2">
                  <Input
                    id="primaryColor"
                    type="color"
                    value={formData.primaryColor || '#1E40AF'}
                    onChange={(e) => setFormData({ ...formData, primaryColor: e.target.value })}
                    className="w-16 h-10"
                  />
                  <Input
                    value={formData.primaryColor || '#1E40AF'}
                    onChange={(e) => setFormData({ ...formData, primaryColor: e.target.value })}
                    placeholder="#1E40AF"
                  />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="secondaryColor">Secondary Color</Label>
                <div className="flex gap-2">
                  <Input
                    id="secondaryColor"
                    type="color"
                    value={formData.secondaryColor || '#9333EA'}
                    onChange={(e) => setFormData({ ...formData, secondaryColor: e.target.value })}
                    className="w-16 h-10"
                  />
                  <Input
                    value={formData.secondaryColor || '#9333EA'}
                    onChange={(e) => setFormData({ ...formData, secondaryColor: e.target.value })}
                    placeholder="#9333EA"
                  />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="accentColor">Accent Color</Label>
                <div className="flex gap-2">
                  <Input
                    id="accentColor"
                    type="color"
                    value={formData.accentColor || '#10B981'}
                    onChange={(e) => setFormData({ ...formData, accentColor: e.target.value })}
                    className="w-16 h-10"
                  />
                  <Input
                    value={formData.accentColor || '#10B981'}
                    onChange={(e) => setFormData({ ...formData, accentColor: e.target.value })}
                    placeholder="#10B981"
                  />
                </div>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="footerText">Footer Text</Label>
              <Input
                id="footerText"
                value={formData.footerText || ''}
                onChange={(e) => setFormData({ ...formData, footerText: e.target.value })}
                placeholder="e.g., © 2024 Company Inc."
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
                Set as default branding profile
              </Label>
            </div>

            <div className="flex gap-2">
              <Button onClick={handleCreate} disabled={!formData.name || createMutation.isPending}>
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
        {profiles.map((profile) => (
          <Card key={profile.id}>
            <CardHeader>
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle className="flex items-center gap-2">
                    {profile.name}
                    {profile.isDefault && (
                      <Badge variant="default">Default</Badge>
                    )}
                    {!profile.isActive && (
                      <Badge variant="secondary">Inactive</Badge>
                    )}
                  </CardTitle>
                  {profile.description && (
                    <CardDescription>{profile.description}</CardDescription>
                  )}
                  {profile.subsidiaryName && (
                    <p className="text-sm text-muted-foreground mt-1">
                      Subsidiary: {profile.subsidiaryName}
                    </p>
                  )}
                </div>
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => startEdit(profile)}
                    disabled={editingId === profile.id}
                  >
                    <Pencil className="w-4 h-4" />
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      if (confirm(`Delete branding profile "${profile.name}"?`)) {
                        deleteMutation.mutate(profile.id)
                      }
                    }}
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
                </div>
              </div>
            </CardHeader>
            {editingId === profile.id ? (
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
                    <Label>Subsidiary Name</Label>
                    <Input
                      value={formData.subsidiaryName || ''}
                      onChange={(e) => setFormData({ ...formData, subsidiaryName: e.target.value })}
                    />
                  </div>
                </div>

                <div className="grid grid-cols-3 gap-4">
                  <div className="space-y-2">
                    <Label>Primary Color</Label>
                    <div className="flex gap-2">
                      <Input
                        type="color"
                        value={formData.primaryColor || '#1E40AF'}
                        onChange={(e) => setFormData({ ...formData, primaryColor: e.target.value })}
                        className="w-16 h-10"
                      />
                      <Input
                        value={formData.primaryColor || '#1E40AF'}
                        onChange={(e) => setFormData({ ...formData, primaryColor: e.target.value })}
                      />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <Label>Secondary Color</Label>
                    <div className="flex gap-2">
                      <Input
                        type="color"
                        value={formData.secondaryColor || '#9333EA'}
                        onChange={(e) => setFormData({ ...formData, secondaryColor: e.target.value })}
                        className="w-16 h-10"
                      />
                      <Input
                        value={formData.secondaryColor || '#9333EA'}
                        onChange={(e) => setFormData({ ...formData, secondaryColor: e.target.value })}
                      />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <Label>Accent Color</Label>
                    <div className="flex gap-2">
                      <Input
                        type="color"
                        value={formData.accentColor || '#10B981'}
                        onChange={(e) => setFormData({ ...formData, accentColor: e.target.value })}
                        className="w-16 h-10"
                      />
                      <Input
                        value={formData.accentColor || '#10B981'}
                        onChange={(e) => setFormData({ ...formData, accentColor: e.target.value })}
                      />
                    </div>
                  </div>
                </div>

                <div className="space-y-2">
                  <Label>Footer Text</Label>
                  <Input
                    value={formData.footerText || ''}
                    onChange={(e) => setFormData({ ...formData, footerText: e.target.value })}
                  />
                </div>

                <div className="flex items-center gap-4">
                  <div className="flex items-center space-x-2">
                    <input
                      type="checkbox"
                      id={`isDefault-${profile.id}`}
                      checked={formData.isDefault || false}
                      onChange={(e) => setFormData({ ...formData, isDefault: e.target.checked })}
                      className="h-4 w-4"
                    />
                    <Label htmlFor={`isDefault-${profile.id}`} className="cursor-pointer">
                      Default
                    </Label>
                  </div>
                  <div className="flex items-center space-x-2">
                    <input
                      type="checkbox"
                      id={`isActive-${profile.id}`}
                      checked={formData.isActive ?? true}
                      onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                      className="h-4 w-4"
                    />
                    <Label htmlFor={`isActive-${profile.id}`} className="cursor-pointer">
                      Active
                    </Label>
                  </div>
                </div>

                <div className="flex gap-2">
                  <Button 
                    onClick={() => handleUpdate(profile.id)} 
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
            ) : (
              <CardContent className="border-t pt-4">
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <p className="text-muted-foreground">Colors</p>
                    <div className="flex gap-2 mt-1">
                      {profile.primaryColor && (
                        <div className="flex items-center gap-1">
                          <div
                            className="w-6 h-6 rounded border"
                            style={{ backgroundColor: profile.primaryColor }}
                          />
                          <span className="text-xs">{profile.primaryColor}</span>
                        </div>
                      )}
                      {profile.secondaryColor && (
                        <div className="flex items-center gap-1">
                          <div
                            className="w-6 h-6 rounded border"
                            style={{ backgroundColor: profile.secondaryColor }}
                          />
                          <span className="text-xs">{profile.secondaryColor}</span>
                        </div>
                      )}
                    </div>
                  </div>
                  <div>
                    <p className="text-muted-foreground">Footer Text</p>
                    <p className="mt-1">{profile.footerText || 'None'}</p>
                  </div>
                </div>
              </CardContent>
            )}
          </Card>
        ))}
      </div>

      {profiles.length === 0 && !isCreating && (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-muted-foreground">No branding profiles configured</p>
            <Button onClick={() => setIsCreating(true)} className="mt-4">
              <Plus className="w-4 h-4 mr-2" />
              Create First Profile
            </Button>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
