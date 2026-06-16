import { httpGet, httpPost } from '@/services/api/http'
import type { PagedQuery, PagedResponse } from '@/types/api'
import type { WorkOrder, WorkOrderPart } from '@/types/models'

export interface CreateWorkOrderRequest {
  targetType: string
  targetId: string
  title: string
  maintenanceType?: string
  odometerBeforeKm?: number
  laborCost?: number
  scheduledDate?: string
  description?: string
}
export interface AddWorkOrderPartRequest {
  partId: string
  quantity: number
  unitCost?: number
}
export interface CompleteWorkOrderRequest {
  odometerAfterKm?: number
}

export const workOrdersApi = {
  list: (query: PagedQuery) => httpGet<PagedResponse<WorkOrder>>('/maintenanceworkorders', { params: query }),
  get: (id: string) => httpGet<WorkOrder>(`/maintenanceworkorders/${id}`),
  create: (body: CreateWorkOrderRequest) => httpPost<WorkOrder>('/maintenanceworkorders', body),
  start: (id: string) => httpPost<void>(`/maintenanceworkorders/${id}/start`),
  addPart: (id: string, body: AddWorkOrderPartRequest) => httpPost<WorkOrderPart>(`/maintenanceworkorders/${id}/parts`, body),
  complete: (id: string, body: CompleteWorkOrderRequest) => httpPost<WorkOrder>(`/maintenanceworkorders/${id}/complete`, body),
}
