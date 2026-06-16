import { httpGet, httpPost } from '@/services/api/http'
import type { Alert } from '@/types/models'

export const alertsApi = {
  open: () => httpGet<Alert[]>('/alerts/open'),
  acknowledge: (id: string) => httpPost<void>(`/alerts/${id}/acknowledge`),
  resolve: (id: string) => httpPost<void>(`/alerts/${id}/resolve`),
  dismiss: (id: string) => httpPost<void>(`/alerts/${id}/dismiss`),
  generateCriticalStock: () => httpPost<number>('/alerts/generate/critical-stock'),
  generateMaintenanceDue: () => httpPost<number>('/alerts/generate/maintenance-due'),
  generateDocumentExpiry: () => httpPost<number>('/alerts/generate/document-expiry'),
}
