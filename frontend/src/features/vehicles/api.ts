import { httpDelete, httpGet, httpPost, httpPut } from '@/services/api/http'
import type { PagedQuery, PagedResponse } from '@/types/api'
import type { Vehicle } from '@/types/models'

export interface CreateVehicleRequest {
  plateNo: string
  brand: string
  model?: string
  modelYear?: number
  vin?: string
}

export interface UpdateVehicleRequest {
  brand: string
  model?: string
  modelYear?: number
  color?: string
  maintenanceIntervalKm?: number
  maintenanceIntervalDays?: number
}

export const vehiclesApi = {
  list: (query: PagedQuery) => httpGet<PagedResponse<Vehicle>>('/vehicles', { params: query }),
  get: (id: string) => httpGet<Vehicle>(`/vehicles/${id}`),
  create: (body: CreateVehicleRequest) => httpPost<Vehicle>('/vehicles', body),
  update: (id: string, body: UpdateVehicleRequest) => httpPut<Vehicle>(`/vehicles/${id}`, body),
  remove: (id: string) => httpDelete(`/vehicles/${id}`),
}
