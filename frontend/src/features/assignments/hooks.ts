import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { extractErrorMessage } from '@/services/api/http'
import { assignmentsApi, type AssignDriverVehicleRequest, type AssignVehicleTrailerRequest } from './api'

export function useActiveVehicleTrailer(vehicleId: string) {
  return useQuery({
    queryKey: ['vta-active', vehicleId],
    queryFn: () => assignmentsApi.activeVehicleTrailer(vehicleId),
    enabled: !!vehicleId,
    retry: false,
  })
}
export function useActiveDriverVehicle(driverId: string) {
  return useQuery({
    queryKey: ['dva-active', driverId],
    queryFn: () => assignmentsApi.activeDriverVehicle(driverId),
    enabled: !!driverId,
    retry: false,
  })
}

export function useAssignVehicleTrailer() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: AssignVehicleTrailerRequest) => assignmentsApi.assignVehicleTrailer(body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['vta-active'] }); toast.success('Araç-dorse eşleştirildi') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useEndVehicleTrailer() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => assignmentsApi.endVehicleTrailer(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['vta-active'] }); toast.success('Eşleşme sonlandırıldı') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useAssignDriverVehicle() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: AssignDriverVehicleRequest) => assignmentsApi.assignDriverVehicle(body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['dva-active'] }); toast.success('Şoför-araç atandı') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
export function useEndDriverVehicle() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => assignmentsApi.endDriverVehicle(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['dva-active'] }); toast.success('Atama sonlandırıldı') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
