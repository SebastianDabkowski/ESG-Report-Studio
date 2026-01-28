import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Card } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { ChatCircle, User as UserIcon } from '@phosphor-icons/react'
import type { DataPointNote } from '@/lib/types'

interface DataPointNotesProps {
  dataPointId: string
  notes: DataPointNote[]
  currentUserId: string
  currentUserName: string
  onAddNote: (content: string) => Promise<void>
}

export default function DataPointNotes({
  dataPointId,
  notes,
  currentUserId,
  currentUserName,
  onAddNote
}: DataPointNotesProps) {
  const [newNoteContent, setNewNoteContent] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!newNoteContent.trim()) {
      setError('Note content cannot be empty')
      return
    }

    setIsSubmitting(true)
    setError(null)

    try {
      await onAddNote(newNoteContent.trim())
      setNewNoteContent('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add note')
    } finally {
      setIsSubmitting(false)
    }
  }

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp)
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffMins = Math.floor(diffMs / 60000)
    const diffHours = Math.floor(diffMs / 3600000)
    const diffDays = Math.floor(diffMs / 86400000)

    if (diffMins < 1) return 'Just now'
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`
    
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric', 
      year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined 
    })
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <ChatCircle size={20} weight="duotone" />
        <h3 className="font-semibold text-sm">Notes & Activity</h3>
        <span className="text-xs text-muted-foreground">({notes.length})</span>
      </div>

      {/* Add New Note Form */}
      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="space-y-2">
          <Label htmlFor="newNote">Add a Note</Label>
          <Textarea
            id="newNote"
            value={newNoteContent}
            onChange={(e) => setNewNoteContent(e.target.value)}
            placeholder="Add a note to track progress, blockers, or important information..."
            rows={3}
            disabled={isSubmitting}
          />
          <p className="text-xs text-muted-foreground">
            Notes are for internal use and will appear in the activity log
          </p>
        </div>

        {error && (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <div className="flex justify-end">
          <Button type="submit" size="sm" disabled={isSubmitting || !newNoteContent.trim()}>
            {isSubmitting ? 'Adding...' : 'Add Note'}
          </Button>
        </div>
      </form>

      {/* Notes List */}
      <div className="space-y-3">
        {notes.length === 0 ? (
          <div className="text-center py-8 border rounded-lg bg-muted/10">
            <ChatCircle size={32} className="mx-auto mb-2 text-muted-foreground/50" weight="duotone" />
            <p className="text-sm text-muted-foreground">No notes yet</p>
            <p className="text-xs text-muted-foreground mt-1">
              Add notes to track progress and communicate with stakeholders
            </p>
          </div>
        ) : (
          notes.map((note) => (
            <Card key={note.id} className="p-4">
              <div className="flex gap-3">
                <div className="flex-shrink-0">
                  <div className="w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center">
                    <UserIcon size={16} weight="duotone" className="text-primary" />
                  </div>
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-baseline gap-2 mb-1">
                    <span className="font-medium text-sm">{note.createdByName}</span>
                    <span className="text-xs text-muted-foreground">
                      {formatTimestamp(note.createdAt)}
                    </span>
                  </div>
                  <p className="text-sm whitespace-pre-wrap break-words">{note.content}</p>
                </div>
              </div>
            </Card>
          ))
        )}
      </div>
    </div>
  )
}
