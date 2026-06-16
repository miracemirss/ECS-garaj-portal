import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { queryKeys } from '@/lib/constants'
import { extractErrorMessage } from '@/services/api/http'
import type { PagedQuery } from '@/types/api'
import {
  partsApi,
  stockApi,
  type AdjustStockRequest,
  type CreatePartRequest,
  type IssueStockRequest,
  type ReceiveStockRequest,
  type UpdatePartRequest,
} from './api'

const KEY = 'parts'

function invalidate(qc: ReturnType<typeof useQueryClient>) {
  qc.invalidateQueries({ queryKey: [KEY] })
  qc.invalidateQueries({ queryKey: queryKeys.criticalStocks })
}

export function useParts(query: PagedQuery) {
  return useQuery({ queryKey: [KEY, query], queryFn: () => partsApi.list(query) })
}
export function useCriticalStocks() {
  return useQuery({ queryKey: queryKeys.criticalStocks, queryFn: partsApi.critical })
}
export function useCreatePart() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreatePartRequest) => partsApi.create(body),
    onSuccess: () => { invalidate(qc); toast.success('Parça oluşturuldu') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useUpdatePart() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdatePartRequest }) => partsApi.update(id, body),
    onSuccess: () => { invalidate(qc); toast.success('Parça güncellendi') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useReceiveStock() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: ReceiveStockRequest) => stockApi.receive(body),
    onSuccess: () => { invalidate(qc); toast.success('Stok girişi yapıldı') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useIssueStock() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: IssueStockRequest) => stockApi.issue(body),
    onSuccess: () => { invalidate(qc); toast.success('Stok çıkışı yapıldı') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useAdjustStock() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: AdjustStockRequest) => stockApi.adjust(body),
    onSuccess: () => { invalidate(qc); toast.success('Sayım düzeltmesi yapıldı') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
