import { useEffect, useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { useKV } from '@github/spark/hooks'
import { Building, FloppyDisk, PencilSimple } from '@phosphor-icons/react'
import type { User, Organization } from '@/lib/types'
import { getOrganization, createOrganization, updateOrganization } from '@/lib/api'

interface OrganizationViewProps {
  currentUser: User
}

export default function OrganizationView({ currentUser }: OrganizationViewProps) {
  const [organization, setOrganization] = useKV<Organization | null>('organization', null)
  const [isEditing, setIsEditing] = useState(false)
  const [syncError, setSyncError] = useState<string | null>(null)

  // Form state
  const [name, setName] = useState('')
  const [legalForm, setLegalForm] = useState('')
  const [country, setCountry] = useState('')
  const [identifier, setIdentifier] = useState('')

  // Validation state
  const [errors, setErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    let isActive = true

    const loadFromApi = async () => {
      try {
        const org = await getOrganization()
        if (!isActive) return

        setOrganization(org)
        setName(org.name)
        setLegalForm(org.legalForm)
        setCountry(org.country)
        setIdentifier(org.identifier)
        setSyncError(null)
      } catch (error) {
        if (!isActive) return
        // Organization not found is expected initially
        setSyncError(null)
      }
    }

    loadFromApi()

    return () => {
      isActive = false
    }
  }, [setOrganization])

  useEffect(() => {
    if (organization && !isEditing) {
      setName(organization.name)
      setLegalForm(organization.legalForm)
      setCountry(organization.country)
      setIdentifier(organization.identifier)
    }
  }, [organization, isEditing])

  useEffect(() => {
    if (!organization) {
      setIsEditing(true)
    }
  }, [organization])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!name.trim()) {
      newErrors.name = 'Company name is required'
    }
    if (!legalForm.trim()) {
      newErrors.legalForm = 'Legal form is required'
    }
    if (!country.trim()) {
      newErrors.country = 'Country is required'
    }
    if (!identifier.trim()) {
      newErrors.identifier = 'Company identifier is required'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSave = async () => {
    if (!validateForm()) {
      return
    }

    try {
      if (organization) {
        // Update existing
        const updated = await updateOrganization(organization.id, {
          name,
          legalForm,
          country,
          identifier
        })
        setOrganization(updated)
      } else {
        // Create new
        const created = await createOrganization({
          name,
          legalForm,
          country,
          identifier,
          createdBy: currentUser.id
        })
        setOrganization(created)
      }
      setIsEditing(false)
      setSyncError(null)
    } catch (error) {
      setSyncError('Failed to save organization. Please try again.')
    }
  }

  const handleCancel = () => {
    if (organization) {
      setName(organization.name)
      setLegalForm(organization.legalForm)
      setCountry(organization.country)
      setIdentifier(organization.identifier)
      setIsEditing(false)
      setErrors({})
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-semibold tracking-tight text-foreground">
            Organization Configuration
          </h2>
          <p className="text-sm text-muted-foreground mt-1">
            Define basic company information for ESG reporting
          </p>
          {syncError && (
            <p className="text-xs text-destructive mt-1">
              {syncError}
            </p>
          )}
        </div>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                <Building size={20} weight="bold" className="text-primary" />
              </div>
              <div>
                <CardTitle>Company Basic Information</CardTitle>
                <CardDescription>
                  Enter the legal entity details for your ESG reports
                </CardDescription>
              </div>
            </div>
            {organization && !isEditing && currentUser.role !== 'auditor' && (
              <Button variant="outline" onClick={() => setIsEditing(true)} className="gap-2">
                <PencilSimple size={16} weight="bold" />
                Edit
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {!organization && !isEditing ? (
            <div className="text-center py-8">
              <p className="text-sm text-muted-foreground mb-4">
                No organization configured. Please set up your company information to continue.
              </p>
              <Button onClick={() => setIsEditing(true)} className="gap-2">
                <Building size={16} weight="bold" />
                Configure Organization
              </Button>
            </div>
          ) : isEditing ? (
            <div className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="company-name">
                    Company Name <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="company-name"
                    placeholder="e.g., Acme Corporation"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    className={errors.name ? 'border-destructive' : ''}
                  />
                  {errors.name && (
                    <p className="text-xs text-destructive">{errors.name}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="legal-form">
                    Legal Form <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="legal-form"
                    placeholder="e.g., GmbH, Inc., Ltd."
                    value={legalForm}
                    onChange={(e) => setLegalForm(e.target.value)}
                    className={errors.legalForm ? 'border-destructive' : ''}
                  />
                  {errors.legalForm && (
                    <p className="text-xs text-destructive">{errors.legalForm}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="country">
                    Country <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="country"
                    placeholder="e.g., Germany, United States"
                    value={country}
                    onChange={(e) => setCountry(e.target.value)}
                    className={errors.country ? 'border-destructive' : ''}
                  />
                  {errors.country && (
                    <p className="text-xs text-destructive">{errors.country}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="identifier">
                    Company Identifier <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="identifier"
                    placeholder="e.g., Tax ID, Registration Number"
                    value={identifier}
                    onChange={(e) => setIdentifier(e.target.value)}
                    className={errors.identifier ? 'border-destructive' : ''}
                  />
                  {errors.identifier && (
                    <p className="text-xs text-destructive">{errors.identifier}</p>
                  )}
                </div>
              </div>

              <div className="flex gap-2 pt-4">
                <Button onClick={handleSave} className="gap-2">
                  <FloppyDisk size={16} weight="bold" />
                  Save Organization
                </Button>
                {organization && (
                  <Button variant="outline" onClick={handleCancel}>
                    Cancel
                  </Button>
                )}
              </div>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <Label className="text-muted-foreground">Company Name</Label>
                  <p className="text-sm font-medium mt-1">{organization?.name}</p>
                </div>

                <div>
                  <Label className="text-muted-foreground">Legal Form</Label>
                  <p className="text-sm font-medium mt-1">{organization?.legalForm}</p>
                </div>

                <div>
                  <Label className="text-muted-foreground">Country</Label>
                  <p className="text-sm font-medium mt-1">{organization?.country}</p>
                </div>

                <div>
                  <Label className="text-muted-foreground">Company Identifier</Label>
                  <p className="text-sm font-medium mt-1">{organization?.identifier}</p>
                </div>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {!organization && (
        <Card className="border-amber-500/50 bg-amber-500/5">
          <CardContent className="pt-6">
            <p className="text-sm text-amber-700 dark:text-amber-400">
              <strong>Required:</strong> You must configure your organization information before creating reporting periods.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
