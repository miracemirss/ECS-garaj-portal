// Mirrors the backend standard response envelope and pagination payload.

export interface ApiResponse<T> {
  success: boolean
  message?: string | null
  data?: T | null
  errors?: unknown
  statusCode: number
  traceId?: string | null
}

export interface PagedResponse<T> {
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  items: T[]
}

export interface PagedQuery {
  page?: number
  pageSize?: number
  search?: string
  sortBy?: string
  sortDescending?: boolean
}
