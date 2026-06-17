import { httpGet, httpPost, httpPut } from '@/services/api/http'
import type { PagedQuery, PagedResponse } from '@/types/api'
import type { Part, StockMovement } from '@/types/models'

export interface CreatePartRequest {
  partNo: string
  name: string
  unit?: string
  initialStock?: number
  minimumStock?: number
  unitCost?: number
}
export interface UpdatePartRequest {
  name: string
  category?: string
  minimumStock: number
  unitCost: number
}
export interface ReceiveStockRequest {
  partId: string
  quantity: number
  unitCost: number
  note?: string
}
export interface IssueStockRequest {
  partId: string
  quantity: number
  note?: string
}
export interface AdjustStockRequest {
  partId: string
  signedQuantity: number
  note?: string
}

export const partsApi = {
  list: (query: PagedQuery) => httpGet<PagedResponse<Part>>('/parts', { params: query }),
  get: (id: string) => httpGet<Part>(`/parts/${id}`),
  create: (body: CreatePartRequest) => httpPost<Part>('/parts', body),
  update: (id: string, body: UpdatePartRequest) => httpPut<Part>(`/parts/${id}`, body),
  critical: () => httpGet<Part[]>('/parts/critical'),
}

export const stockApi = {
  receive: (body: ReceiveStockRequest) => httpPost<StockMovement>('/stockmovements/receive', body),
  issue: (body: IssueStockRequest) => httpPost<StockMovement>('/stockmovements/issue', body),
  adjust: (body: AdjustStockRequest) => httpPost<StockMovement>('/stockmovements/adjust', body),
}
