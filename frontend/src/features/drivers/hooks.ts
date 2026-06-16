import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { extractErrorMessage } from '@/services/api/http'
import type { PagedQuery } from '@/types/api'
import { driversApi, type CreateDriverRequest, type UpdateDriverRequest } from './api'

const KEY = 'drivers'

export function useDrivers(query: PagedQuery) {
  return useQuery({ queryKey: [KEY, query], queryFn: () => driversApi.list(query) })
}
export function useDriver(id: string) {
  return useQuery({ queryKey: [KEY, id], queryFn: () => driversApi.get(id), enabled: !!id })
}
export function useCreateDriver() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateDriverRequest) => driversApi.create(body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: [KEY] }); toast.success('Şoför oluşturuldu') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useUpdateDriver() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateDriverRequest }) => driversApi.update(id, body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: [KEY] }); toast.success('Şoför güncellendi') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useDeleteDriver() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => driversApi.remove(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: [KEY] }); toast.success('Şoför silindi') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
