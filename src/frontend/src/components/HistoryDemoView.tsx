import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ClockCounterClockwise } from '@phosphor-icons/react'
import FragmentHistoryView from './FragmentHistoryView'

export default function HistoryDemoView() {
  const [showHistory, setShowHistory] = useState(false)
  const [entityType, setEntityType] = useState('DataPoint')
  const [entityId, setEntityId] = useState('')
  const [entityTitle, setEntityTitle] = useState('')

  // Sample entity IDs for testing
  const sampleEntities = {
    DataPoint: [
      { id: 'dp-001', title: 'Total Energy Consumption' },
      { id: 'dp-002', title: 'GHG Scope 1 Emissions' },
      { id: 'dp-003', title: 'Water Usage' }
    ],
    ReportSection: [
      { id: 'section-001', title: 'Energy & Emissions' },
      { id: 'section-002', title: 'Employee Health & Safety' }
    ],
    Gap: [
      { id: 'gap-001', title: 'Missing Scope 3 Emissions Data' },
      { id: 'gap-002', title: 'Incomplete Waste Tracking' }
    ],
    Assumption: [
      { id: 'assumption-001', title: 'Emission Factor Assumption' },
      { id: 'assumption-002', title: 'Extrapolation Method' }
    ]
  }

  function handleViewHistory() {
    if (entityId) {
      setShowHistory(true)
    }
  }

  function handleSampleSelect(sampleId: string) {
    const samples = sampleEntities[entityType as keyof typeof sampleEntities]
    const selected = samples?.find(s => s.id === sampleId)
    if (selected) {
      setEntityId(selected.id)
      setEntityTitle(selected.title)
    }
  }

  if (showHistory && entityId) {
    return (
      <FragmentHistoryView
        entityType={entityType}
        entityId={entityId}
        entityTitle={entityTitle}
        onClose={() => setShowHistory(false)}
      />
    )
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <ClockCounterClockwise size={24} weight="duotone" />
          <div>
            <CardTitle>Fragment Change History Demo</CardTitle>
            <CardDescription>
              View change history and compare versions for any report fragment
            </CardDescription>
          </div>
        </div>
      </CardHeader>

      <CardContent className="space-y-6">
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label>Entity Type</Label>
            <Select value={entityType} onValueChange={(value) => {
              setEntityType(value)
              setEntityId('')
              setEntityTitle('')
            }}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="DataPoint">Data Point</SelectItem>
                <SelectItem value="ReportSection">Report Section</SelectItem>
                <SelectItem value="Gap">Gap</SelectItem>
                <SelectItem value="Assumption">Assumption</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label>Sample Entities (for testing)</Label>
            <Select onValueChange={handleSampleSelect}>
              <SelectTrigger>
                <SelectValue placeholder="Select a sample..." />
              </SelectTrigger>
              <SelectContent>
                {sampleEntities[entityType as keyof typeof sampleEntities]?.map(sample => (
                  <SelectItem key={sample.id} value={sample.id}>
                    {sample.title}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        <div className="space-y-2">
          <Label>Entity ID</Label>
          <Input
            value={entityId}
            onChange={(e) => setEntityId(e.target.value)}
            placeholder="Enter entity ID (e.g., dp-001)"
          />
        </div>

        <div className="space-y-2">
          <Label>Entity Title (Optional)</Label>
          <Input
            value={entityTitle}
            onChange={(e) => setEntityTitle(e.target.value)}
            placeholder="Enter entity title for display"
          />
        </div>

        <Button onClick={handleViewHistory} disabled={!entityId}>
          <ClockCounterClockwise className="mr-2" size={16} />
          View Change History
        </Button>

        <div className="mt-6 p-4 bg-gray-50 rounded-lg">
          <h3 className="font-semibold mb-2">About this feature:</h3>
          <ul className="text-sm space-y-1 text-gray-700">
            <li>• View chronological history of changes for any report fragment</li>
            <li>• Select two versions to see side-by-side comparison</li>
            <li>• See what changed (added, modified, removed)</li>
            <li>• View related evidence, notes, and comments</li>
            <li>• Export history for audit purposes</li>
          </ul>
        </div>
      </CardContent>
    </Card>
  )
}
