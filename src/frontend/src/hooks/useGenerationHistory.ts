import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { 
  getGenerationHistory, 
  getGeneration,
  markGenerationFinal,
  compareGenerations,
  getExportHistory
} from '@/lib/api'
import type { 
  GenerationHistoryEntry, 
  ExportHistoryEntry,
  MarkGenerationFinalRequest,
  CompareGenerationsRequest,
  GenerationComparison
} from '@/lib/types'

export function useGenerationHistory(periodId: string | undefined) {
  return useQuery({
    queryKey: ['generationHistory', periodId],
    queryFn: () => {
      if (!periodId) throw new Error('Period ID is required')
      return getGenerationHistory(periodId)
    },
    enabled: !!periodId
  })
}

export function useGeneration(generationId: string | undefined) {
  return useQuery({
    queryKey: ['generation', generationId],
    queryFn: () => {
      if (!generationId) throw new Error('Generation ID is required')
      return getGeneration(generationId)
    },
    enabled: !!generationId
  })
}

export function useMarkGenerationFinal() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: ({ generationId, payload }: { generationId: string; payload: MarkGenerationFinalRequest }) =>
      markGenerationFinal(generationId, payload),
    onSuccess: (data, variables) => {
      // Invalidate and refetch generation history
      queryClient.invalidateQueries({ queryKey: ['generationHistory', data.periodId] })
      queryClient.invalidateQueries({ queryKey: ['generation', variables.generationId] })
    }
  })
}

export function useCompareGenerations() {
  return useMutation({
    mutationFn: (payload: CompareGenerationsRequest) => compareGenerations(payload)
  })
}

export function useExportHistory(periodId: string | undefined) {
  return useQuery({
    queryKey: ['exportHistory', periodId],
    queryFn: () => {
      if (!periodId) throw new Error('Period ID is required')
      return getExportHistory(periodId)
    },
    enabled: !!periodId
  })
}
