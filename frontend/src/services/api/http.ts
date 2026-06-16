import type { AxiosRequestConfig } from 'axios'
import { apiClient } from './client'
import type { ApiResponse } from '@/types/api'

/** Typed helpers that unwrap the standard ApiResponse envelope -> the payload. */

export async function httpGet<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const res = await apiClient.get<ApiResponse<T>>(url, config)
  return res.data.data as T
}

export async function httpPost<T>(url: string, body?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const res = await apiClient.post<ApiResponse<T>>(url, body, config)
  return res.data.data as T
}

export async function httpPut<T>(url: string, body?: unknown): Promise<T> {
  const res = await apiClient.put<ApiResponse<T>>(url, body)
  return res.data.data as T
}

export async function httpDelete<T = void>(url: string): Promise<T> {
  const res = await apiClient.delete<ApiResponse<T>>(url)
  return res.data.data as T
}

export function extractErrorMessage(error: unknown): string {
  const e = error as { response?: { data?: ApiResponse<unknown> } }
  return e?.response?.data?.message ?? 'Bir hata oluştu.'
}
