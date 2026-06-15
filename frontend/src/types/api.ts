// Mirrors the backend ECS.Shared result/pagination contracts.

export interface ApiError {
  code: string
  message: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface PaginationRequest {
  page: number
  pageSize: number
}
