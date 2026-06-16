import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { extractErrorMessage } from '@/services/api/http'
import type { PagedQuery } from '@/types/api'
import { vehiclesApi, type CreateVehicleRequest, type UpdateVehicleRequest } from './api'

const KEY = 'vehicles'

export function useVehicles(query: PagedQuery) {
  return useQuery({ queryKey: [KEY, query], queryFn: () => vehiclesApi.list(query) })
}

export function useVehicle(id: string) {
  return useQuery({ queryKey: [KEY, id], queryFn: () => vehiclesApi.get(id), enabled: !!id })
}

export function useCreateVehicle() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateVehicleRequest) => vehiclesApi.create(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [KEY] })
      toast.success('Araç oluşturuldu')
    },
    onError: (error) => toast.error(extractErrorMessage(error)),
  })
}

export function useUpdateVehicle() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateVehicleRequest }) => vehiclesApi.update(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [KEY] })
      toast.success('Araç güncellendi')
    },
    onError: (error) => toast.error(extractErrorMessage(error)),
  })
}

export function useDeleteVehicle() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => vehiclesApi.remove(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [KEY] })
      toast.success('Araç silindi')
    },
    onError: (error) => toast.error(extractErrorMessage(error)),
  })
}
