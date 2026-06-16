import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { queryKeys } from '@/lib/constants'
import { extractErrorMessage } from '@/services/api/http'
import type { PagedQuery } from '@/types/api'
import { workOrdersApi, type AddWorkOrderPartRequest, type CompleteWorkOrderRequest, type CreateWorkOrderRequest } from './api'

const KEY = 'workOrders'

export function useWorkOrders(query: PagedQuery) {
  return useQuery({ queryKey: [KEY, query], queryFn: () => workOrdersApi.list(query) })
}
export function useWorkOrder(id: string) {
  return useQuery({ queryKey: [KEY, id], queryFn: () => workOrdersApi.get(id), enabled: !!id })
}
export function useCreateWorkOrder() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateWorkOrderRequest) => workOrdersApi.create(body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: [KEY] }); toast.success('İş emri oluşturuldu') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useStartWorkOrder() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => workOrdersApi.start(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: [KEY] }); toast.success('İş emri başlatıldı') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useAddWorkOrderPart() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: AddWorkOrderPartRequest }) => workOrdersApi.addPart(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [KEY] })
      qc.invalidateQueries({ queryKey: ['parts'] })
      qc.invalidateQueries({ queryKey: queryKeys.criticalStocks })
      toast.success('Parça eklendi, stoktan düşüldü')
    },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useCompleteWorkOrder() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: CompleteWorkOrderRequest }) => workOrdersApi.complete(id, body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: [KEY] }); toast.success('İş emri tamamlandı') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
