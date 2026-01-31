import { useState, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { 
  getStandardsCatalog, 
  getReportingPeriods,
  getStandardCoverageAnalysis,
  type StandardsCatalogItem,
  type ReportingPeriod,
  type StandardCoverageAnalysis,
  type DisclosureCoverageDetail
} from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
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
import { Loader2, Download, AlertCircle, CheckCircle2, Circle } from 'lucide-react'
import { Progress } from '@/components/ui/progress'

export default function StandardsCoverageView() {
  const [selectedStandardId, setSelectedStandardId] = useState<string>('')
  const [selectedPeriodId, setSelectedPeriodId] = useState<string>('')
  const [categoryFilter, setCategoryFilter] = useState<string>('')
  const [topicFilter, setTopicFilter] = useState<string>('')

  // Fetch standards catalog
  const { data: standards = [], isLoading: isLoadingStandards } = useQuery({
    queryKey: ['standards-catalog', false],
    queryFn: () => getStandardsCatalog(false)
  })

  // Fetch reporting periods
  const { data: periods = [], isLoading: isLoadingPeriods } = useQuery({
    queryKey: ['reporting-periods'],
    queryFn: getReportingPeriods
  })

  // Fetch coverage analysis when both standard and period are selected
  const { data: coverageAnalysis, isLoading: isLoadingCoverage, error: coverageError } = useQuery({
    queryKey: ['coverage-analysis', selectedStandardId, selectedPeriodId, categoryFilter, topicFilter],
    queryFn: () => getStandardCoverageAnalysis(
      selectedStandardId, 
      selectedPeriodId, 
      categoryFilter || undefined, 
      topicFilter || undefined
    ),
    enabled: !!selectedStandardId && !!selectedPeriodId
  })

  // Auto-select first standard and period if available
  useEffect(() => {
    if (standards.length > 0 && !selectedStandardId) {
      setSelectedStandardId(standards[0].id)
    }
  }, [standards, selectedStandardId])

  useEffect(() => {
    if (periods.length > 0 && !selectedPeriodId) {
      setSelectedPeriodId(periods[0].id)
    }
  }, [periods, selectedPeriodId])

  // Get unique topics from coverage data for filtering
  const availableTopics = coverageAnalysis?.disclosureDetails
    .map(d => d.topic)
    .filter((t): t is string => !!t) // Type guard to ensure non-null
    .filter((t, i, arr) => arr.indexOf(t) === i) // Remove duplicates
    .sort() || []

  const handleExportCoverage = () => {
    if (!coverageAnalysis) return

    // Convert to CSV
    const headers = ['Disclosure Code', 'Title', 'Category', 'Topic', 'Mandatory', 'Coverage Status', 'Mapped Sections', 'Mapped Data Points']
    const rows = coverageAnalysis.disclosureDetails.map(d => [
      d.disclosureCode,
      d.title,
      d.category,
      d.topic || '',
      d.isMandatory ? 'Yes' : 'No',
      d.coverageStatus,
      d.mappedSections.length.toString(),
      d.mappedDataPoints.length.toString()
    ])

    const csv = [
      headers.join(','),
      ...rows.map(row => row.map(cell => `"${cell}"`).join(','))
    ].join('\n')

    // Download
    const blob = new Blob([csv], { type: 'text/csv' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `coverage-analysis-${selectedStandardId}-${selectedPeriodId}-${new Date().toISOString().split('T')[0]}.csv`
    a.click()
    URL.revokeObjectURL(url)
  }

  const getCoverageStatusIcon = (status: string) => {
    switch (status) {
      case 'full':
        return <CheckCircle2 className="h-4 w-4 text-green-600" />
      case 'partial':
        return <Circle className="h-4 w-4 text-yellow-600" />
      case 'missing':
        return <AlertCircle className="h-4 w-4 text-red-600" />
      default:
        return <Circle className="h-4 w-4 text-gray-400" />
    }
  }

  const getCoverageStatusBadge = (status: string) => {
    const variants: Record<string, 'default' | 'secondary' | 'destructive'> = {
      full: 'default',
      partial: 'secondary',
      missing: 'destructive'
    }
    
    return (
      <Badge variant={variants[status] || 'secondary'}>
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </Badge>
    )
  }

  if (isLoadingStandards || isLoadingPeriods) {
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
          <h1 className="text-3xl font-bold">Standards Coverage</h1>
          <p className="text-gray-500 mt-1">
            Analyze which standard disclosures are covered by your report sections and data points
          </p>
        </div>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Analysis Filters</CardTitle>
          <CardDescription>
            Select a standard and reporting period to analyze coverage
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Standard</label>
              <Select value={selectedStandardId} onValueChange={setSelectedStandardId}>
                <SelectTrigger>
                  <SelectValue placeholder="Select standard" />
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

            <div className="space-y-2">
              <label className="text-sm font-medium">Reporting Period</label>
              <Select value={selectedPeriodId} onValueChange={setSelectedPeriodId}>
                <SelectTrigger>
                  <SelectValue placeholder="Select period" />
                </SelectTrigger>
                <SelectContent>
                  {periods.map((period) => (
                    <SelectItem key={period.id} value={period.id}>
                      {period.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Category</label>
              <Select value={categoryFilter} onValueChange={setCategoryFilter}>
                <SelectTrigger>
                  <SelectValue placeholder="All categories" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">All categories</SelectItem>
                  <SelectItem value="environmental">Environmental</SelectItem>
                  <SelectItem value="social">Social</SelectItem>
                  <SelectItem value="governance">Governance</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Topic</label>
              <Select value={topicFilter} onValueChange={setTopicFilter}>
                <SelectTrigger>
                  <SelectValue placeholder="All topics" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">All topics</SelectItem>
                  {availableTopics.map((topic) => (
                    <SelectItem key={topic} value={topic}>
                      {topic}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Coverage Summary */}
      {coverageAnalysis && (
        <>
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle>Coverage Summary</CardTitle>
                  <CardDescription>
                    {coverageAnalysis.standardTitle} - {periods.find(p => p.id === selectedPeriodId)?.name}
                  </CardDescription>
                </div>
                <Button onClick={handleExportCoverage} variant="outline" size="sm">
                  <Download className="h-4 w-4 mr-2" />
                  Export CSV
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium">Overall Coverage</span>
                  <span className="text-2xl font-bold">{coverageAnalysis.coveragePercentage.toFixed(1)}%</span>
                </div>
                <Progress value={coverageAnalysis.coveragePercentage} className="h-2" />
                
                <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mt-4">
                  <div className="p-4 border rounded-lg">
                    <div className="text-sm text-gray-500">Total Disclosures</div>
                    <div className="text-2xl font-bold mt-1">{coverageAnalysis.totalDisclosures}</div>
                  </div>
                  <div className="p-4 border rounded-lg bg-green-50">
                    <div className="text-sm text-green-700">Fully Covered</div>
                    <div className="text-2xl font-bold text-green-700 mt-1">{coverageAnalysis.fullyCovered}</div>
                  </div>
                  <div className="p-4 border rounded-lg bg-yellow-50">
                    <div className="text-sm text-yellow-700">Partially Covered</div>
                    <div className="text-2xl font-bold text-yellow-700 mt-1">{coverageAnalysis.partiallyCovered}</div>
                  </div>
                  <div className="p-4 border rounded-lg bg-red-50">
                    <div className="text-sm text-red-700">Not Covered</div>
                    <div className="text-2xl font-bold text-red-700 mt-1">{coverageAnalysis.notCovered}</div>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Disclosure Details */}
          <Card>
            <CardHeader>
              <CardTitle>Disclosure Details</CardTitle>
              <CardDescription>
                {coverageAnalysis.disclosureDetails.length} disclosures
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-[100px]">Status</TableHead>
                    <TableHead>Code</TableHead>
                    <TableHead>Title</TableHead>
                    <TableHead>Category</TableHead>
                    <TableHead>Topic</TableHead>
                    <TableHead className="text-center">Mandatory</TableHead>
                    <TableHead className="text-center">Sections</TableHead>
                    <TableHead className="text-center">Data Points</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {coverageAnalysis.disclosureDetails.map((disclosure) => (
                    <TableRow key={disclosure.disclosureId}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          {getCoverageStatusIcon(disclosure.coverageStatus)}
                          {getCoverageStatusBadge(disclosure.coverageStatus)}
                        </div>
                      </TableCell>
                      <TableCell className="font-mono text-sm">{disclosure.disclosureCode}</TableCell>
                      <TableCell className="max-w-md">
                        <div className="font-medium">{disclosure.title}</div>
                        {(disclosure.mappedSections.length > 0 || disclosure.mappedDataPoints.length > 0) && (
                          <div className="text-xs text-gray-500 mt-1">
                            {disclosure.mappedSections.length > 0 && (
                              <div>Sections: {disclosure.mappedSections.map(s => s.sectionTitle).join(', ')}</div>
                            )}
                            {disclosure.mappedDataPoints.length > 0 && (
                              <div>Data Points: {disclosure.mappedDataPoints.map(d => d.dataPointTitle).join(', ')}</div>
                            )}
                          </div>
                        )}
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline">
                          {disclosure.category.charAt(0).toUpperCase() + disclosure.category.slice(1)}
                        </Badge>
                      </TableCell>
                      <TableCell>{disclosure.topic || '-'}</TableCell>
                      <TableCell className="text-center">
                        {disclosure.isMandatory ? (
                          <Badge variant="default" className="bg-blue-600">Yes</Badge>
                        ) : (
                          <Badge variant="outline">No</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-center">{disclosure.mappedSections.length}</TableCell>
                      <TableCell className="text-center">{disclosure.mappedDataPoints.length}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </>
      )}

      {isLoadingCoverage && (
        <div className="flex items-center justify-center p-8">
          <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
        </div>
      )}

      {coverageError && (
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center gap-2 text-red-600">
              <AlertCircle className="h-5 w-5" />
              <span>Error loading coverage analysis. Please try again.</span>
            </div>
          </CardContent>
        </Card>
      )}

      {!selectedStandardId || !selectedPeriodId ? (
        <Card>
          <CardContent className="p-6">
            <div className="text-center text-gray-500">
              Please select both a standard and reporting period to view coverage analysis.
            </div>
          </CardContent>
        </Card>
      ) : null}
    </div>
  )
}
