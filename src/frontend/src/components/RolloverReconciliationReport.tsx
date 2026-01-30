import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { CheckCircle, Warning, Info } from '@phosphor-icons/react'
import type { RolloverReconciliation } from '@/lib/types'

interface RolloverReconciliationReportProps {
  reconciliation: RolloverReconciliation
}

export default function RolloverReconciliationReport({ reconciliation }: RolloverReconciliationReportProps) {
  const hasUnmappedItems = reconciliation.unmappedSections > 0

  return (
    <div className="space-y-6">
      {/* Summary */}
      <div className="grid grid-cols-3 gap-4">
        <div className="p-4 bg-gray-50 rounded-lg">
          <div className="text-sm text-gray-600">Total Sections</div>
          <div className="text-2xl font-semibold">{reconciliation.totalSourceSections}</div>
        </div>
        <div className="p-4 bg-green-50 rounded-lg">
          <div className="text-sm text-gray-600">Mapped</div>
          <div className="text-2xl font-semibold text-green-700">{reconciliation.mappedSections}</div>
        </div>
        <div className="p-4 bg-yellow-50 rounded-lg">
          <div className="text-sm text-gray-600">Unmapped</div>
          <div className="text-2xl font-semibold text-yellow-700">{reconciliation.unmappedSections}</div>
        </div>
      </div>

      {/* Alert if there are unmapped items */}
      {hasUnmappedItems && (
        <Alert>
          <Warning className="h-4 w-4" />
          <AlertDescription>
            {reconciliation.unmappedSections} section(s) could not be mapped. Review the unmapped items below and consider providing manual mappings.
          </AlertDescription>
        </Alert>
      )}

      {/* Mapped Sections */}
      {reconciliation.mappedItems.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold mb-3 flex items-center gap-2">
            <CheckCircle className="text-green-600" weight="fill" />
            Successfully Mapped Sections
          </h3>
          <div className="space-y-2">
            {reconciliation.mappedItems.map((item, index) => (
              <div key={index} className="p-4 bg-white border border-gray-200 rounded-lg">
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="font-medium">{item.sourceTitle}</span>
                      {item.mappingType === 'manual' && (
                        <Badge variant="outline" className="text-xs">Manual Mapping</Badge>
                      )}
                    </div>
                    <div className="text-sm text-gray-600">
                      {item.sourceCatalogCode} → {item.targetCatalogCode}
                    </div>
                    {item.dataPointsCopied > 0 && (
                      <div className="text-sm text-gray-500 mt-1">
                        {item.dataPointsCopied} data point(s) copied
                      </div>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Unmapped Sections */}
      {reconciliation.unmappedItems.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold mb-3 flex items-center gap-2">
            <Warning className="text-yellow-600" weight="fill" />
            Unmapped Sections
          </h3>
          <div className="space-y-3">
            {reconciliation.unmappedItems.map((item, index) => (
              <div key={index} className="p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                <div className="flex items-start gap-3">
                  <Warning className="text-yellow-600 flex-shrink-0 mt-1" weight="fill" size={20} />
                  <div className="flex-1">
                    <div className="font-medium mb-1">{item.sourceTitle}</div>
                    {item.sourceCatalogCode && (
                      <div className="text-sm text-gray-600 mb-2">
                        Catalog Code: {item.sourceCatalogCode}
                      </div>
                    )}
                    <div className="text-sm text-gray-700 mb-2">
                      <strong>Reason:</strong> {item.reason}
                    </div>
                    {item.affectedDataPoints > 0 && (
                      <div className="text-sm text-gray-600 mb-2">
                        <Info className="inline mr-1" size={14} />
                        {item.affectedDataPoints} data point(s) were not copied due to this unmapped section
                      </div>
                    )}
                    {item.suggestedActions.length > 0 && (
                      <div className="mt-2">
                        <div className="text-sm font-medium text-gray-700 mb-1">Suggested Actions:</div>
                        <ul className="list-disc list-inside text-sm text-gray-600 space-y-1">
                          {item.suggestedActions.map((action, actionIndex) => (
                            <li key={actionIndex}>{action}</li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
