import { useState } from 'react'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Progress } from '@/components/ui/progress'
import { UploadSimple, Download, CheckCircle, Warning, X } from '@phosphor-icons/react'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'

interface ImportRowResult {
  rowNumber: number
  dataPointId?: string
  title: string
  errorMessage?: string
}

interface ImportResult {
  totalRows: number
  successCount: number
  errorCount: number
  successfulRows: ImportRowResult[]
  failedRows: ImportRowResult[]
}

interface ImportDataDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onImportComplete: () => void
}

export default function ImportDataDialog({ open, onOpenChange, onImportComplete }: ImportDataDialogProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [importing, setImporting] = useState(false)
  const [importResult, setImportResult] = useState<ImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (file) {
      if (!file.name.endsWith('.csv')) {
        setError('Only CSV files are supported')
        setSelectedFile(null)
        return
      }
      setSelectedFile(file)
      setError(null)
      setImportResult(null)
    }
  }

  const handleDownloadTemplate = async () => {
    try {
      const response = await fetch('/api/import/template')
      if (!response.ok) {
        throw new Error('Failed to download template')
      }
      
      const blob = await response.blob()
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = 'data-points-template.csv'
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      window.URL.revokeObjectURL(url)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to download template')
    }
  }

  const handleImport = async () => {
    if (!selectedFile) {
      setError('Please select a file to import')
      return
    }

    setImporting(true)
    setError(null)
    setImportResult(null)

    try {
      const formData = new FormData()
      formData.append('file', selectedFile)

      const response = await fetch('/api/import/data-points', {
        method: 'POST',
        body: formData,
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.error || 'Import failed')
      }

      const result: ImportResult = await response.json()
      setImportResult(result)

      // If all rows succeeded, notify parent and close after a short delay
      if (result.errorCount === 0) {
        setTimeout(() => {
          onImportComplete()
          handleClose()
        }, 2000)
      } else {
        // Partially succeeded, notify parent but keep dialog open to show errors
        onImportComplete()
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Import failed')
    } finally {
      setImporting(false)
    }
  }

  const handleClose = () => {
    setSelectedFile(null)
    setImportResult(null)
    setError(null)
    onOpenChange(false)
  }

  const getSuccessRate = () => {
    if (!importResult) return 0
    if (importResult.totalRows === 0) return 0
    return (importResult.successCount / importResult.totalRows) * 100
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] flex flex-col">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <UploadSimple size={24} weight="duotone" />
            Import Data Points from CSV
          </DialogTitle>
          <DialogDescription>
            Upload a CSV file to bulk import ESG data points. Download the template to see the required format.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 flex-1 overflow-y-auto">
          {/* Template download */}
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={handleDownloadTemplate}
              className="flex items-center gap-2"
            >
              <Download size={16} />
              Download CSV Template
            </Button>
          </div>

          {/* File upload */}
          <div className="space-y-2">
            <label className="block text-sm font-medium">Select CSV File</label>
            <div className="flex items-center gap-2">
              <input
                type="file"
                accept=".csv"
                onChange={handleFileChange}
                className="block w-full text-sm text-gray-500
                  file:mr-4 file:py-2 file:px-4
                  file:rounded-md file:border-0
                  file:text-sm file:font-semibold
                  file:bg-primary file:text-primary-foreground
                  hover:file:bg-primary/90
                  cursor-pointer"
                disabled={importing}
              />
            </div>
            {selectedFile && (
              <p className="text-sm text-muted-foreground">
                Selected: {selectedFile.name} ({(selectedFile.size / 1024).toFixed(2)} KB)
              </p>
            )}
          </div>

          {/* Error display */}
          {error && (
            <Alert variant="destructive">
              <Warning size={16} />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {/* Import results */}
          {importResult && (
            <div className="space-y-4 border rounded-lg p-4">
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <h3 className="text-sm font-semibold">Import Summary</h3>
                  <Badge variant={importResult.errorCount === 0 ? 'default' : 'secondary'}>
                    {importResult.successCount} / {importResult.totalRows} successful
                  </Badge>
                </div>
                <Progress value={getSuccessRate()} className="h-2" />
              </div>

              {/* Success summary */}
              {importResult.successCount > 0 && (
                <Alert>
                  <CheckCircle size={16} className="text-green-600" />
                  <AlertDescription>
                    Successfully imported {importResult.successCount} data point{importResult.successCount !== 1 ? 's' : ''}.
                  </AlertDescription>
                </Alert>
              )}

              {/* Failed rows */}
              {importResult.failedRows.length > 0 && (
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <X size={16} className="text-red-600" />
                    <h4 className="text-sm font-semibold text-red-600">
                      Failed Rows ({importResult.failedRows.length})
                    </h4>
                  </div>
                  <ScrollArea className="h-48 border rounded-md">
                    <div className="p-2 space-y-2">
                      {importResult.failedRows.map((row, index) => (
                        <div key={index} className="text-sm border-l-2 border-red-500 pl-3 py-1">
                          <div className="font-medium">Row {row.rowNumber}: {row.title}</div>
                          <div className="text-xs text-muted-foreground">{row.errorMessage}</div>
                        </div>
                      ))}
                    </div>
                  </ScrollArea>
                </div>
              )}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={handleClose} disabled={importing}>
            {importResult ? 'Close' : 'Cancel'}
          </Button>
          {!importResult && (
            <Button
              onClick={handleImport}
              disabled={!selectedFile || importing}
              className="flex items-center gap-2"
            >
              {importing ? (
                <>
                  <div className="animate-spin rounded-full h-4 w-4 border-2 border-primary border-t-transparent" />
                  Importing...
                </>
              ) : (
                <>
                  <UploadSimple size={16} />
                  Import
                </>
              )}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
