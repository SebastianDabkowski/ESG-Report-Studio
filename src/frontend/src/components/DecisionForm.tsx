import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import type { Decision, CreateDecisionRequest, UpdateDecisionRequest } from '@/lib/types'
import { createDecision, updateDecision } from '@/lib/api'

interface DecisionFormProps {
  sectionId?: string
  decision?: Decision
  onSuccess: () => void
  onCancel: () => void
}

export default function DecisionForm({ sectionId, decision, onSuccess, onCancel }: DecisionFormProps) {
  const isEdit = !!decision
  const [formData, setFormData] = useState({
    title: decision?.title || '',
    context: decision?.context || '',
    decisionText: decision?.decisionText || '',
    alternatives: decision?.alternatives || '',
    consequences: decision?.consequences || '',
    changeNote: ''
  })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    // Validation
    if (!formData.title.trim()) {
      setError('Title is required')
      return
    }
    if (!formData.context.trim()) {
      setError('Context is required')
      return
    }
    if (!formData.decisionText.trim()) {
      setError('Decision text is required')
      return
    }
    if (!formData.alternatives.trim()) {
      setError('Alternatives are required')
      return
    }
    if (!formData.consequences.trim()) {
      setError('Consequences are required')
      return
    }
    if (isEdit && !formData.changeNote.trim()) {
      setError('Change note is required when updating a decision')
      return
    }

    try {
      setLoading(true)
      setError(null)

      if (isEdit && decision) {
        const payload: UpdateDecisionRequest = {
          title: formData.title.trim(),
          context: formData.context.trim(),
          decisionText: formData.decisionText.trim(),
          alternatives: formData.alternatives.trim(),
          consequences: formData.consequences.trim(),
          changeNote: formData.changeNote.trim()
        }
        await updateDecision(decision.id, payload)
      } else {
        const payload: CreateDecisionRequest = {
          sectionId,
          title: formData.title.trim(),
          context: formData.context.trim(),
          decisionText: formData.decisionText.trim(),
          alternatives: formData.alternatives.trim(),
          consequences: formData.consequences.trim()
        }
        await createDecision(payload)
      }

      onSuccess()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save decision')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{isEdit ? 'Edit Decision' : 'Create Decision'}</CardTitle>
        <CardDescription>
          {isEdit 
            ? 'Update this decision. A new version will be created and the old version will be preserved.'
            : 'Record a new decision using ADR-like structure (Context, Decision, Alternatives, Consequences).'}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="title">Title *</Label>
            <Input
              id="title"
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              placeholder="Brief summary of the decision"
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="context">Context *</Label>
            <Textarea
              id="context"
              value={formData.context}
              onChange={(e) => setFormData({ ...formData, context: e.target.value })}
              placeholder="Background and circumstances leading to this decision"
              rows={4}
              required
            />
            <p className="text-sm text-muted-foreground">
              Describe the situation, problem, or question that required this decision.
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="decisionText">Decision *</Label>
            <Textarea
              id="decisionText"
              value={formData.decisionText}
              onChange={(e) => setFormData({ ...formData, decisionText: e.target.value })}
              placeholder="The actual decision made and rationale"
              rows={4}
              required
            />
            <p className="text-sm text-muted-foreground">
              Explain what was decided and why this choice was made.
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="alternatives">Alternatives Considered *</Label>
            <Textarea
              id="alternatives"
              value={formData.alternatives}
              onChange={(e) => setFormData({ ...formData, alternatives: e.target.value })}
              placeholder="Other options that were considered but not chosen"
              rows={4}
              required
            />
            <p className="text-sm text-muted-foreground">
              List and briefly describe other approaches that were evaluated.
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="consequences">Consequences *</Label>
            <Textarea
              id="consequences"
              value={formData.consequences}
              onChange={(e) => setFormData({ ...formData, consequences: e.target.value })}
              placeholder="Expected impacts and implications of this decision"
              rows={4}
              required
            />
            <p className="text-sm text-muted-foreground">
              Describe both positive outcomes and potential risks or limitations.
            </p>
          </div>

          {isEdit && (
            <div className="space-y-2">
              <Label htmlFor="changeNote">Change Note *</Label>
              <Textarea
                id="changeNote"
                value={formData.changeNote}
                onChange={(e) => setFormData({ ...formData, changeNote: e.target.value })}
                placeholder="Explain what changed in this version"
                rows={3}
                required
              />
              <p className="text-sm text-muted-foreground">
                Required when updating a decision. Describe what was modified and why.
              </p>
            </div>
          )}

          {error && (
            <div className="bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded">
              {error}
            </div>
          )}

          <div className="flex gap-2">
            <Button type="submit" disabled={loading}>
              {loading ? 'Saving...' : isEdit ? 'Update Decision' : 'Create Decision'}
            </Button>
            <Button type="button" variant="outline" onClick={onCancel} disabled={loading}>
              Cancel
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}
