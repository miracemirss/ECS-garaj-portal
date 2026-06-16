import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { extractErrorMessage } from '@/services/api/http'
import type { PagedQuery } from '@/types/api'
import { trailersApi, type CreateTrailerRequest, type UpdateTrailerRequest } from './api'

const KEY = 'trailers'

export function useTrailers(query: PagedQuery) {
  return useQuery({ queryKey: [KEY, query], queryFn: () => trailersApi.list(query) })
}
export function useTrailer(id: string) {
  return useQuery({ queryKey: [KEY, id], queryFn: () => trailersApi.get(id), enabled: !!id })
}
export function useCreateTrailer() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateTrailerRequest) => trailersApi.create(body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: [KEY] }); toast.success('Dorse oluşturuldu') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useUpdateTrailer() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateTrailerRequest }) => trailersApi.update(id, body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: [KEY] }); toast.success('Dorse güncellendi') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useDeleteTrailer() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => trailersApi.remove(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: [KEY] }); toast.success('Dorse silindi') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
