export const APP_NAME = 'ECS Fleet Maintenance & Inventory'

/** Feature keys — kept in sync with the routed feature folders. */
export const FEATURES = [
  'dashboard',
  'vehicles',
  'trailers',
  'drivers',
  'assignments',
  'maintenance',
  'inventory',
  'operations',
  'reports',
  'alerts',
  'settings',
  'auth',
] as const

export type FeatureKey = (typeof FEATURES)[number]
