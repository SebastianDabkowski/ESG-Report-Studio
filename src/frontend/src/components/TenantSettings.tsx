import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { 
  getTenantSettings, 
  updateTenantSettings, 
  getTenantSettingsHistory,
  getStandardsCatalog 
} from '@/lib/api'
import type { TenantSettings, TenantSettingsHistory, StandardsCatalogItem } from '@/lib/types'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { AlertCircle, History, Save } from 'lucide-react'

interface TenantSettingsProps {
  organizationId: string
  userId: string
  userName: string
}

const AVAILABLE_INTEGRATIONS = [
  { id: 'HR', name: 'HR System Integration', description: 'Connect to HR systems for employee data' },
  { id: 'Finance', name: 'Finance Integration', description: 'Connect to financial systems for spending data' },
  { id: 'Utilities', name: 'Utilities Integration', description: 'Connect to utility providers for energy data' },
  { id: 'Webhooks', name: 'Webhook Integration', description: 'Receive real-time data updates via webhooks' }
]

export default function TenantSettingsView({ organizationId, userId, userName }: TenantSettingsProps) {
  const queryClient = useQueryClient()
  const [enabledIntegrations, setEnabledIntegrations] = useState<string[]>([])
  const [enabledStandards, setEnabledStandards] = useState<string[]>([])
  const [applyImmediately, setApplyImmediately] = useState(true)
  const [changeReason, setChangeReason] = useState('')
  const [showHistory, setShowHistory] = useState(false)

  // Fetch current settings
  const { data: settings, isLoading: settingsLoading } = useQuery<TenantSettings>({
    queryKey: ['tenant-settings', organizationId],
    queryFn: () => getTenantSettings(organizationId)
  })

  // Fetch available standards
  const { data: standards } = useQuery<StandardsCatalogItem[]>({
    queryKey: ['standards-catalog'],
    queryFn: () => getStandardsCatalog(false)
  })

  // Fetch settings history
  const { data: history } = useQuery<TenantSettingsHistory[]>({
    queryKey: ['tenant-settings-history', organizationId],
    queryFn: () => getTenantSettingsHistory(organizationId),
    enabled: showHistory
  })

  // Update settings mutation
  const updateMutation = useMutation({
    mutationFn: (payload: { enabledIntegrations: string[]; enabledStandards: string[]; applyImmediately: boolean; changeReason?: string }) =>
      updateTenantSettings(organizationId, {
        ...payload,
        updatedBy: userId,
        updatedByName: userName
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tenant-settings', organizationId] })
      queryClient.invalidateQueries({ queryKey: ['tenant-settings-history', organizationId] })
      setChangeReason('')
    }
  })

  // Initialize state when settings are loaded
  useEffect(() => {
    if (settings) {
      setEnabledIntegrations(settings.enabledIntegrations)
      setEnabledStandards(settings.enabledStandards)
      setApplyImmediately(settings.applyImmediately)
    }
  }, [settings])

  const handleSave = () => {
    updateMutation.mutate({
      enabledIntegrations,
      enabledStandards,
      applyImmediately,
      changeReason: changeReason || undefined
    })
  }

  const hasChanges = settings && (
    JSON.stringify(enabledIntegrations.sort()) !== JSON.stringify(settings.enabledIntegrations.sort()) ||
    JSON.stringify(enabledStandards.sort()) !== JSON.stringify(settings.enabledStandards.sort()) ||
    applyImmediately !== settings.applyImmediately
  )

  if (settingsLoading) {
    return <div className="p-8">Loading tenant settings...</div>
  }

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Tenant Settings</h1>
          <p className="text-muted-foreground">
            Configure integrations and reporting standards for your organization
          </p>
        </div>
        <Button variant="outline" onClick={() => setShowHistory(!showHistory)}>
          <History className="mr-2 h-4 w-4" />
          {showHistory ? 'Hide' : 'Show'} History
        </Button>
      </div>

      {settings && (
        <Card>
          <CardHeader>
            <CardTitle>Current Configuration</CardTitle>
            <CardDescription>
              Version {settings.version} • Effective from {new Date(settings.effectiveDate).toLocaleDateString()}
            </CardDescription>
          </CardHeader>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Integration Settings</CardTitle>
          <CardDescription>
            Enable or disable external system integrations
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {AVAILABLE_INTEGRATIONS.map(integration => (
            <div key={integration.id} className="flex items-start space-x-3">
              <Checkbox
                id={integration.id}
                checked={enabledIntegrations.includes(integration.id)}
                onCheckedChange={(checked) => {
                  if (checked) {
                    setEnabledIntegrations([...enabledIntegrations, integration.id])
                  } else {
                    setEnabledIntegrations(enabledIntegrations.filter(i => i !== integration.id))
                  }
                }}
              />
              <div className="flex-1">
                <Label htmlFor={integration.id} className="font-medium cursor-pointer">
                  {integration.name}
                </Label>
                <p className="text-sm text-muted-foreground">{integration.description}</p>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Reporting Standards</CardTitle>
          <CardDescription>
            Select which reporting frameworks are enabled for this organization
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {standards?.map(standard => (
            <div key={standard.id} className="flex items-start space-x-3">
              <Checkbox
                id={standard.id}
                checked={enabledStandards.includes(standard.id)}
                onCheckedChange={(checked) => {
                  if (checked) {
                    setEnabledStandards([...enabledStandards, standard.id])
                  } else {
                    setEnabledStandards(enabledStandards.filter(s => s !== standard.id))
                  }
                }}
                disabled={standard.isDeprecated}
              />
              <div className="flex-1">
                <div className="flex items-center gap-2">
                  <Label htmlFor={standard.id} className="font-medium cursor-pointer">
                    {standard.title} {standard.version}
                  </Label>
                  {standard.isDeprecated && <Badge variant="secondary">Deprecated</Badge>}
                </div>
                <p className="text-sm text-muted-foreground">{standard.description}</p>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Effective Date</CardTitle>
          <CardDescription>
            When should these changes take effect?
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-start space-x-3">
            <Checkbox
              id="applyImmediately"
              checked={applyImmediately}
              onCheckedChange={(checked) => setApplyImmediately(checked as boolean)}
            />
            <div className="flex-1">
              <Label htmlFor="applyImmediately" className="font-medium cursor-pointer">
                Apply immediately
              </Label>
              <p className="text-sm text-muted-foreground">
                Changes will take effect immediately. Uncheck to apply at the start of the next reporting period.
              </p>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="changeReason">Change Reason (Optional)</Label>
            <Textarea
              id="changeReason"
              placeholder="Explain why you are making these changes..."
              value={changeReason}
              onChange={(e) => setChangeReason(e.target.value)}
              rows={3}
            />
          </div>
        </CardContent>
      </Card>

      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          {hasChanges && (
            <>
              <AlertCircle className="h-4 w-4" />
              <span>You have unsaved changes</span>
            </>
          )}
        </div>
        <Button
          onClick={handleSave}
          disabled={!hasChanges || updateMutation.isPending}
        >
          <Save className="mr-2 h-4 w-4" />
          {updateMutation.isPending ? 'Saving...' : 'Save Changes'}
        </Button>
      </div>

      {showHistory && history && history.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Change History</CardTitle>
            <CardDescription>
              Previous versions of tenant settings
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {history.map((entry) => (
                <div key={entry.id} className="border-l-2 border-primary pl-4 py-2">
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-medium">Version {entry.version}</span>
                    <span className="text-sm text-muted-foreground">
                      {new Date(entry.changedAt).toLocaleString()}
                    </span>
                  </div>
                  <p className="text-sm text-muted-foreground mb-2">
                    Changed by {entry.changedByName}
                  </p>
                  {entry.changeReason && (
                    <p className="text-sm italic">{entry.changeReason}</p>
                  )}
                  <div className="text-sm mt-2">
                    <strong>Integrations:</strong> {entry.enabledIntegrations.join(', ') || 'None'}
                  </div>
                  <div className="text-sm">
                    <strong>Standards:</strong> {entry.enabledStandards.length} enabled
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
