import { httpDelete, httpGet, httpPost, httpPut } from '@/services/api/http'
import type { PagedQuery, PagedResponse } from '@/types/api'
import type { Trailer } from '@/types/models'

export interface CreateTrailerRequest {
  plateNo: string
  trailerType?: string
  brand?: string
  capacityKg?: number
  vin?: string
  tireConditionPercent?: number
}

export interface UpdateTrailerRequest {
  trailerType?: string
  brand?: string
  model?: string
  capacityKg?: number
  tireConditionPercent?: number
}

export const trailersApi = {
  list: (query: PagedQuery) => httpGet<PagedResponse<Trailer>>('/trailers', { params: query }),
  get: (id: string) => httpGet<Trailer>(`/trailers/${id}`),
  create: (body: CreateTrailerRequest) => httpPost<Trailer>('/trailers', body),
  update: (id: string, body: UpdateTrailerRequest) => httpPut<Trailer>(`/trailers/${id}`, body),
  remove: (id: string) => httpDelete(`/trailers/${id}`),
}
