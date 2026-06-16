import { httpGet, httpPut } from '@/services/api/http'
import type { CompanySetting } from '@/types/models'

export interface UpdateSettingsRequest {
  companyName: string
  defaultCurrency: string
  maintenanceDueKmThreshold: number
  maintenanceDueDaysThreshold: number
  documentExpiryDaysThreshold: number
  lowStockCheckEnabled: boolean
}

export const settingsApi = {
  get: () => httpGet<CompanySetting>('/settings'),
  update: (body: UpdateSettingsRequest) => httpPut<CompanySetting>('/settings', body),
}
