import { useState, useEffect, useCallback } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { 
  Dialog, 
  DialogContent, 
  DialogDescription, 
  DialogHeader, 
  DialogTitle,
  DialogFooter 
} from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { 
  Select, 
  SelectContent, 
  SelectItem, 
  SelectTrigger, 
  SelectValue 
} from '@/components/ui/select'
import { Plus, PencilSimple, Trash, ClockCounterClockwise, Copy, XCircle, CheckCircle } from '@phosphor-icons/react'
import type { DataTypeRolloverRule, RolloverRuleHistory, DataTypeRolloverRuleType } from '@/lib/types'
import { 
  getRolloverRules, 
  saveRolloverRule, 
  deleteRolloverRule, 
  getRolloverRuleHistory 
} from '@/lib/api'
import { formatDateTime } from '@/lib/helpers'

// Common data types in ESG reporting
const COMMON_DATA_TYPES = [
  { value: 'narrative', label: 'Narrative', description: 'Text-based descriptions and explanations' },
  { value: 'metric', label: 'Metric', description: 'Quantitative measurements and KPIs' },
  { value: 'kpi', label: 'KPI', description: 'Key Performance Indicators' },
  { value: 'policy', label: 'Policy', description: 'Organizational policies and procedures' },
  { value: 'target', label: 'Target', description: 'Goals and objectives' },
  { value: 'evidence', label: 'Evidence', description: 'Supporting documentation' }
]

const RULE_TYPES: { value: string; label: string; description: string; icon: any }[] = [
  { 
    value: 'copy', 
    label: 'Copy', 
    description: 'Copy all data values to new period',
    icon: Copy
  },
  { 
    value: 'reset', 
    label: 'Reset', 
    description: 'Create empty placeholders (no data copied)',
    icon: XCircle
  },
  { 
    value: 'copy-as-draft', 
    label: 'Copy as Draft', 
    description: 'Copy values but require review',
    icon: CheckCircle
  }
]

interface RolloverRulesConfigProps {
  onClose?: () => void
}

export default function RolloverRulesConfig({ onClose }: RolloverRulesConfigProps) {
  const [rules, setRules] = useState<DataTypeRolloverRule[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [editingRule, setEditingRule] = useState<DataTypeRolloverRule | null>(null)
  const [viewingHistory, setViewingHistory] = useState<string | null>(null)
  const [history, setHistory] = useState<RolloverRuleHistory[]>([])
  const [historyLoading, setHistoryLoading] = useState(false)
  const [showNewRuleDialog, setShowNewRuleDialog] = useState(false)
  
  // Form state
  const [formDataType, setFormDataType] = useState('')
  const [formRuleType, setFormRuleType] = useState('copy')
  const [formDescription, setFormDescription] = useState('')
  const [formSaving, setFormSaving] = useState(false)

  const loadRules = useCallback(async () => {
    try {
      setLoading(true)
      const data = await getRolloverRules()
      setRules(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load rollover rules')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadRules()
  }, [loadRules])

  async function handleSaveRule() {
    if (!formDataType || !formRuleType) {
      setError('Data type and rule type are required')
      return
    }

    try {
      setFormSaving(true)
      setError(null)
      
      await saveRolloverRule({
        dataType: formDataType,
        ruleType: formRuleType,
        description: formDescription || undefined,
        savedBy: 'current-user' // In production, this would come from auth context
      })
      
      await loadRules()
      handleCloseDialog()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save rollover rule')
    } finally {
      setFormSaving(false)
    }
  }

  async function handleDelete(dataType: string) {
    if (!confirm(`Reset rollover rule for '${dataType}' to default (Copy)? This will delete the custom rule.`)) {
      return
    }

    try {
      await deleteRolloverRule(dataType, 'current-user')
      await loadRules()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete rollover rule')
    }
  }

  function handleEdit(rule: DataTypeRolloverRule) {
    setEditingRule(rule)
    setFormDataType(rule.dataType)
    setFormRuleType(rule.ruleType.toLowerCase().replace('asdraft', '-as-draft'))
    setFormDescription(rule.description || '')
    setShowNewRuleDialog(true)
  }

  function handleNewRule() {
    setEditingRule(null)
    setFormDataType('')
    setFormRuleType('copy')
    setFormDescription('')
    setShowNewRuleDialog(true)
  }

  function handleCloseDialog() {
    setShowNewRuleDialog(false)
    setEditingRule(null)
    setFormDataType('')
    setFormRuleType('copy')
    setFormDescription('')
  }

  async function handleViewHistory(dataType: string) {
    try {
      setHistoryLoading(true)
      setViewingHistory(dataType)
      const data = await getRolloverRuleHistory(dataType)
      setHistory(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load rule history')
    } finally {
      setHistoryLoading(false)
    }
  }

  function handleCloseHistory() {
    setViewingHistory(null)
    setHistory([])
  }

  function getRuleTypeInfo(ruleType: DataTypeRolloverRuleType) {
    const normalizedType = ruleType.toLowerCase().replace('asdraft', '-as-draft')
    return RULE_TYPES.find(rt => rt.value === normalizedType) || RULE_TYPES[0]
  }

  function getRuleTypeColor(ruleType: DataTypeRolloverRuleType): string {
    const normalizedType = ruleType.toLowerCase()
    if (normalizedType === 'copy') return 'bg-blue-100 text-blue-800'
    if (normalizedType === 'reset') return 'bg-orange-100 text-orange-800'
    if (normalizedType.includes('draft')) return 'bg-purple-100 text-purple-800'
    return 'bg-gray-100 text-gray-800'
  }

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Rollover Rules Configuration</CardTitle>
          <CardDescription>Loading...</CardDescription>
        </CardHeader>
      </Card>
    )
  }

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Rollover Rules Configuration</CardTitle>
          <CardDescription>
            Define how different data types are handled during period rollover
          </CardDescription>
        </CardHeader>
        <CardContent>
          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-md text-sm text-red-800">
              {error}
            </div>
          )}

          <div className="mb-4">
            <Button onClick={handleNewRule}>
              <Plus className="mr-2 h-4 w-4" />
              Add Rule
            </Button>
          </div>

          {rules.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              <p className="mb-2">No custom rollover rules configured</p>
              <p className="text-sm">All data types will use the default "Copy" behavior</p>
            </div>
          ) : (
            <div className="space-y-3">
              {rules.map((rule) => {
                const ruleTypeInfo = getRuleTypeInfo(rule.ruleType)
                const Icon = ruleTypeInfo.icon
                
                return (
                  <div
                    key={rule.id}
                    className="border rounded-lg p-4 hover:bg-gray-50 transition-colors"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-2">
                          <h3 className="font-medium text-lg capitalize">{rule.dataType}</h3>
                          <Badge className={getRuleTypeColor(rule.ruleType)}>
                            <Icon className="mr-1 h-3 w-3" />
                            {ruleTypeInfo.label}
                          </Badge>
                          <span className="text-sm text-gray-500">v{rule.version}</span>
                        </div>
                        
                        {rule.description && (
                          <p className="text-sm text-gray-600 mb-2">{rule.description}</p>
                        )}
                        
                        <p className="text-xs text-gray-500">
                          {ruleTypeInfo.description}
                        </p>
                        
                        <div className="mt-2 text-xs text-gray-400">
                          Created {formatDateTime(rule.createdAt)}
                          {rule.updatedAt && ` • Updated ${formatDateTime(rule.updatedAt)}`}
                        </div>
                      </div>
                      
                      <div className="flex gap-2 ml-4">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleEdit(rule)}
                          title="Edit rule"
                        >
                          <PencilSimple className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleViewHistory(rule.dataType)}
                          title="View history"
                        >
                          <ClockCounterClockwise className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(rule.dataType)}
                          className="text-red-600 hover:text-red-700"
                          title="Delete rule"
                        >
                          <Trash className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          )}

          {onClose && (
            <div className="mt-6 flex justify-end">
              <Button variant="outline" onClick={onClose}>
                Close
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* New/Edit Rule Dialog */}
      <Dialog open={showNewRuleDialog} onOpenChange={handleCloseDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editingRule ? 'Edit Rollover Rule' : 'New Rollover Rule'}</DialogTitle>
            <DialogDescription>
              Configure how this data type should be handled during period rollover
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="dataType">Data Type</Label>
              {editingRule ? (
                <div className="px-3 py-2 bg-gray-50 rounded-md text-sm capitalize">
                  {editingRule.dataType}
                </div>
              ) : (
                <Select value={formDataType} onValueChange={setFormDataType}>
                  <SelectTrigger id="dataType">
                    <SelectValue placeholder="Select data type" />
                  </SelectTrigger>
                  <SelectContent>
                    {COMMON_DATA_TYPES.map(dt => (
                      <SelectItem key={dt.value} value={dt.value}>
                        <div>
                          <div className="font-medium">{dt.label}</div>
                          <div className="text-xs text-gray-500">{dt.description}</div>
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="ruleType">Rule Type</Label>
              <Select value={formRuleType} onValueChange={setFormRuleType}>
                <SelectTrigger id="ruleType">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {RULE_TYPES.map(rt => {
                    const Icon = rt.icon
                    return (
                      <SelectItem key={rt.value} value={rt.value}>
                        <div className="flex items-center gap-2">
                          <Icon className="h-4 w-4" />
                          <div>
                            <div className="font-medium">{rt.label}</div>
                            <div className="text-xs text-gray-500">{rt.description}</div>
                          </div>
                        </div>
                      </SelectItem>
                    )
                  })}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">Description (Optional)</Label>
              <Textarea
                id="description"
                value={formDescription}
                onChange={(e) => setFormDescription(e.target.value)}
                placeholder="Explain why this rule is configured this way..."
                rows={3}
              />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={handleCloseDialog} disabled={formSaving}>
              Cancel
            </Button>
            <Button onClick={handleSaveRule} disabled={formSaving}>
              {formSaving ? 'Saving...' : editingRule ? 'Update Rule' : 'Create Rule'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* History Dialog */}
      <Dialog open={viewingHistory !== null} onOpenChange={handleCloseHistory}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              Rule History: <span className="capitalize">{viewingHistory}</span>
            </DialogTitle>
            <DialogDescription>
              Version history of changes to this rollover rule
            </DialogDescription>
          </DialogHeader>

          <div className="max-h-96 overflow-y-auto">
            {historyLoading ? (
              <div className="text-center py-8 text-gray-500">Loading history...</div>
            ) : history.length === 0 ? (
              <div className="text-center py-8 text-gray-500">No history available</div>
            ) : (
              <div className="space-y-3">
                {history.map((entry) => {
                  const ruleTypeInfo = getRuleTypeInfo(entry.ruleType)
                  const Icon = ruleTypeInfo.icon
                  
                  return (
                    <div key={entry.id} className="border rounded-lg p-3 text-sm">
                      <div className="flex items-center gap-2 mb-2">
                        <Badge variant="outline" className="capitalize">
                          {entry.changeType}
                        </Badge>
                        <Badge className={getRuleTypeColor(entry.ruleType)}>
                          <Icon className="mr-1 h-3 w-3" />
                          {ruleTypeInfo.label}
                        </Badge>
                        <span className="text-xs text-gray-500">v{entry.version}</span>
                      </div>
                      
                      {entry.description && (
                        <p className="text-gray-600 mb-2">{entry.description}</p>
                      )}
                      
                      <div className="text-xs text-gray-500">
                        {entry.changedByName} • {formatDateTime(entry.changedAt)}
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={handleCloseHistory}>
              Close
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
