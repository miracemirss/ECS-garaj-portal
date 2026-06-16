import { httpGet } from '@/services/api/http'
import type { PagedResponse } from '@/types/api'
import type { Alert, Driver, MonthlyCost, Part, Trailer, Vehicle } from '@/types/models'

const countParams = { params: { page: 1, pageSize: 1 } }

export const dashboardApi = {
  vehicles: () => httpGet<PagedResponse<Vehicle>>('/vehicles', countParams),
  trailers: () => httpGet<PagedResponse<Trailer>>('/trailers', countParams),
  drivers: () => httpGet<PagedResponse<Driver>>('/drivers', countParams),
  criticalStocks: () => httpGet<Part[]>('/parts/critical'),
  openAlerts: () => httpGet<Alert[]>('/alerts/open'),
  monthlyCosts: () => httpGet<MonthlyCost[]>('/reports/monthly-costs'),
}
