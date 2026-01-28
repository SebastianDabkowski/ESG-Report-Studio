import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { 
  User, 
  CalendarBlank, 
  Article, 
  Target,
  Link as LinkIcon
} from '@phosphor-icons/react'
import type { DataPoint, Assumption } from '@/lib/types'

interface EstimateProvenanceProps {
  dataPoint: DataPoint
}

export function EstimateProvenance({ dataPoint }: EstimateProvenanceProps) {
  if (dataPoint.informationType !== 'estimate') {
    return null
  }

  return (
    <Card className="border-slate-200">
      <CardHeader>
        <CardTitle className="text-base font-semibold flex items-center gap-2">
          <Article className="h-5 w-5 text-slate-600" />
          Estimate Provenance
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Method, Type, and Confidence */}
        <div className="grid gap-3">
          <div>
            <span className="text-sm font-medium text-slate-700">Type:</span>
            <Badge variant="outline" className="ml-2">
              {dataPoint.estimateType || 'Not specified'}
            </Badge>
          </div>
          
          <div>
            <span className="text-sm font-medium text-slate-700">Confidence Level:</span>
            <Badge 
              variant="outline" 
              className={`ml-2 ${
                dataPoint.confidenceLevel === 'high' ? 'border-green-500 text-green-700' :
                dataPoint.confidenceLevel === 'medium' ? 'border-yellow-500 text-yellow-700' :
                'border-red-500 text-red-700'
              }`}
            >
              {dataPoint.confidenceLevel || 'Not specified'}
            </Badge>
          </div>

          {dataPoint.estimateMethod && (
            <div>
              <span className="text-sm font-medium text-slate-700">Method:</span>
              <p className="text-sm text-slate-600 mt-1 whitespace-pre-wrap">
                {dataPoint.estimateMethod}
              </p>
            </div>
          )}
        </div>

        {/* Inputs and Sources */}
        {dataPoint.estimateInputs && (
          <div className="border-t pt-4">
            <span className="text-sm font-medium text-slate-700">Input Data & Sources:</span>
            <p className="text-sm text-slate-600 mt-1 whitespace-pre-wrap">
              {dataPoint.estimateInputs}
            </p>
          </div>
        )}

        {/* Input Sources */}
        {dataPoint.estimateInputSources && dataPoint.estimateInputSources.length > 0 && (
          <div className="border-t pt-4">
            <span className="text-sm font-medium text-slate-700 flex items-center gap-2">
              <LinkIcon className="h-4 w-4" />
              Referenced Sources:
            </span>
            <div className="mt-2 space-y-2">
              {dataPoint.estimateInputSources.map((source, idx) => (
                <div key={idx} className="bg-slate-50 p-3 rounded border border-slate-200">
                  <div className="flex items-center gap-2">
                    <Badge variant="secondary" className="text-xs">
                      {source.sourceType}
                    </Badge>
                  </div>
                  <p className="text-sm text-slate-600 mt-1">{source.description}</p>
                  {source.sourceReference && (
                    <p className="text-xs text-slate-500 mt-1 font-mono">
                      Ref: {source.sourceReference}
                    </p>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Author and Timestamp */}
        {(dataPoint.estimateAuthor || dataPoint.estimateCreatedAt) && (
          <div className="border-t pt-4 grid gap-2">
            {dataPoint.estimateAuthor && (
              <div className="flex items-center gap-2 text-sm text-slate-600">
                <User className="h-4 w-4" />
                <span className="font-medium">Author:</span>
                <span>{dataPoint.estimateAuthor}</span>
              </div>
            )}
            {dataPoint.estimateCreatedAt && (
              <div className="flex items-center gap-2 text-sm text-slate-600">
                <CalendarBlank className="h-4 w-4" />
                <span className="font-medium">Created:</span>
                <span>{new Date(dataPoint.estimateCreatedAt).toLocaleString()}</span>
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

interface AssumptionProvenanceProps {
  assumption: Assumption
}

export function AssumptionProvenance({ assumption }: AssumptionProvenanceProps) {
  return (
    <Card className="border-slate-200">
      <CardHeader>
        <CardTitle className="text-base font-semibold flex items-center gap-2">
          <Article className="h-5 w-5 text-slate-600" />
          Assumption Provenance
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Core Info */}
        <div className="grid gap-3">
          <div>
            <span className="text-sm font-medium text-slate-700">Status:</span>
            <Badge 
              variant="outline" 
              className={`ml-2 ${
                assumption.status === 'active' ? 'border-green-500 text-green-700' :
                assumption.status === 'deprecated' ? 'border-yellow-500 text-yellow-700' :
                'border-red-500 text-red-700'
              }`}
            >
              {assumption.status}
            </Badge>
            <span className="text-xs text-slate-500 ml-2">v{assumption.version}</span>
          </div>

          <div>
            <span className="text-sm font-medium text-slate-700">Scope:</span>
            <p className="text-sm text-slate-600 mt-1">{assumption.scope}</p>
          </div>

          <div>
            <span className="text-sm font-medium text-slate-700">Validity Period:</span>
            <p className="text-sm text-slate-600 mt-1">
              {new Date(assumption.validityStartDate).toLocaleDateString()} - {new Date(assumption.validityEndDate).toLocaleDateString()}
            </p>
          </div>
        </div>

        {/* Methodology */}
        <div className="border-t pt-4">
          <span className="text-sm font-medium text-slate-700">Methodology:</span>
          <p className="text-sm text-slate-600 mt-1 whitespace-pre-wrap">
            {assumption.methodology}
          </p>
        </div>

        {/* Rationale */}
        {assumption.rationale && (
          <div className="border-t pt-4">
            <span className="text-sm font-medium text-slate-700">Rationale:</span>
            <p className="text-sm text-slate-600 mt-1 whitespace-pre-wrap bg-blue-50 p-3 rounded border border-blue-200">
              {assumption.rationale}
            </p>
          </div>
        )}

        {/* Limitations */}
        {assumption.limitations && (
          <div className="border-t pt-4">
            <span className="text-sm font-medium text-slate-700">Limitations:</span>
            <p className="text-sm text-slate-600 mt-1 whitespace-pre-wrap">
              {assumption.limitations}
            </p>
          </div>
        )}

        {/* Sources */}
        {assumption.sources && assumption.sources.length > 0 && (
          <div className="border-t pt-4">
            <span className="text-sm font-medium text-slate-700 flex items-center gap-2">
              <LinkIcon className="h-4 w-4" />
              Supporting Sources:
            </span>
            <div className="mt-2 space-y-2">
              {assumption.sources.map((source, idx) => (
                <div key={idx} className="bg-slate-50 p-3 rounded border border-slate-200">
                  <div className="flex items-center gap-2">
                    <Badge variant="secondary" className="text-xs">
                      {source.sourceType}
                    </Badge>
                  </div>
                  <p className="text-sm text-slate-600 mt-1">{source.description}</p>
                  {source.sourceReference && (
                    <p className="text-xs text-slate-500 mt-1 font-mono">
                      Ref: {source.sourceReference}
                    </p>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Linked Disclosures */}
        {assumption.linkedDataPointIds && assumption.linkedDataPointIds.length > 0 && (
          <div className="border-t pt-4">
            <span className="text-sm font-medium text-slate-700 flex items-center gap-2">
              <Target className="h-4 w-4" />
              Linked Disclosures:
            </span>
            <div className="mt-2">
              <Badge variant="outline" className="text-xs">
                {assumption.linkedDataPointIds.length} data point(s)
              </Badge>
            </div>
          </div>
        )}

        {/* Audit Trail */}
        <div className="border-t pt-4 grid gap-2">
          <div className="flex items-center gap-2 text-sm text-slate-600">
            <User className="h-4 w-4" />
            <span className="font-medium">Created by:</span>
            <span>{assumption.createdBy}</span>
          </div>
          <div className="flex items-center gap-2 text-sm text-slate-600">
            <CalendarBlank className="h-4 w-4" />
            <span className="font-medium">Created:</span>
            <span>{new Date(assumption.createdAt).toLocaleString()}</span>
          </div>
          {assumption.updatedBy && (
            <>
              <div className="flex items-center gap-2 text-sm text-slate-600">
                <User className="h-4 w-4" />
                <span className="font-medium">Last updated by:</span>
                <span>{assumption.updatedBy}</span>
              </div>
              {assumption.updatedAt && (
                <div className="flex items-center gap-2 text-sm text-slate-600">
                  <CalendarBlank className="h-4 w-4" />
                  <span className="font-medium">Updated:</span>
                  <span>{new Date(assumption.updatedAt).toLocaleString()}</span>
                </div>
              )}
            </>
          )}
        </div>
      </CardContent>
    </Card>
  )
}
