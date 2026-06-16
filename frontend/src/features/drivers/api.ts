import { httpDelete, httpGet, httpPost, httpPut } from '@/services/api/http'
import type { PagedQuery, PagedResponse } from '@/types/api'
import type { Driver } from '@/types/models'

export interface CreateDriverRequest {
  firstName: string
  lastName: string
  nationalId?: string
  phone?: string
  email?: string
}

export interface UpdateDriverRequest {
  phone?: string
  email?: string
  licenseNo?: string
  licenseClass?: string
  licenseExpiryDate?: string
}

export const driversApi = {
  list: (query: PagedQuery) => httpGet<PagedResponse<Driver>>('/drivers', { params: query }),
  get: (id: string) => httpGet<Driver>(`/drivers/${id}`),
  create: (body: CreateDriverRequest) => httpPost<Driver>('/drivers', body),
  update: (id: string, body: UpdateDriverRequest) => httpPut<Driver>(`/drivers/${id}`, body),
  remove: (id: string) => httpDelete(`/drivers/${id}`),
}
