import { httpGet } from '@/services/api/http'
import type { MonthlyCost, VehicleCostSummary } from '@/types/models'

export const reportsApi = {
  vehicleCosts: () => httpGet<VehicleCostSummary[]>('/reports/vehicle-costs'),
  monthlyCosts: () => httpGet<MonthlyCost[]>('/reports/monthly-costs'),
}
