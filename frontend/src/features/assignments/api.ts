import { httpGet, httpPost } from '@/services/api/http'
import type { DriverVehicleAssignment, VehicleTrailerAssignment } from '@/types/models'

export interface AssignVehicleTrailerRequest {
  vehicleId: string
  trailerId: string
  note?: string
}
export interface AssignDriverVehicleRequest {
  driverId: string
  vehicleId: string
  note?: string
}

export const assignmentsApi = {
  assignVehicleTrailer: (body: AssignVehicleTrailerRequest) => httpPost<VehicleTrailerAssignment>('/vehicletrailerassignments', body),
  endVehicleTrailer: (id: string) => httpPost<void>(`/vehicletrailerassignments/${id}/end`),
  activeVehicleTrailer: (vehicleId: string) =>
    httpGet<VehicleTrailerAssignment>('/vehicletrailerassignments/active', { params: { vehicleId } }),
  assignDriverVehicle: (body: AssignDriverVehicleRequest) => httpPost<DriverVehicleAssignment>('/drivervehicleassignments', body),
  endDriverVehicle: (id: string) => httpPost<void>(`/drivervehicleassignments/${id}/end`),
  activeDriverVehicle: (driverId: string) =>
    httpGet<DriverVehicleAssignment>('/drivervehicleassignments/active', { params: { driverId } }),
}
