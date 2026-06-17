export const APP_NAME = 'ECS Garaj'
export const APP_FULL_NAME = 'ECS Garaj Bakım ve Stok Yönetimi'

/** TanStack Query cache keys (single source of truth). */
export const queryKeys = {
  auth: ['auth', 'me'] as const,
  dashboard: ['dashboard'] as const,
  vehicles: (params?: unknown) => ['vehicles', params] as const,
  vehicle: (id: string) => ['vehicles', id] as const,
  trailers: (params?: unknown) => ['trailers', params] as const,
  trailer: (id: string) => ['trailers', id] as const,
  drivers: (params?: unknown) => ['drivers', params] as const,
  driver: (id: string) => ['drivers', id] as const,
  workOrders: (params?: unknown) => ['workOrders', params] as const,
  workOrder: (id: string) => ['workOrders', id] as const,
  parts: (params?: unknown) => ['parts', params] as const,
  part: (id: string) => ['parts', id] as const,
  criticalStocks: ['parts', 'critical'] as const,
  alerts: ['alerts', 'open'] as const,
  settings: ['settings'] as const,
  vehicleCosts: ['reports', 'vehicle-costs'] as const,
  monthlyCosts: ['reports', 'monthly-costs'] as const,
}
