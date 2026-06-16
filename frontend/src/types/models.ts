// Response models mirroring the backend DTOs (statuses are serialized strings).

export interface AuthUser {
  id: string
  email: string
  fullName: string
  roles: string[]
}

export interface AuthResult {
  accessToken: string
  expiresAtUtc: string
  refreshToken: string
  user: AuthUser
}

export interface Vehicle {
  id: string
  plateNo: string
  vin?: string | null
  brand: string
  model?: string | null
  modelYear?: number | null
  status: string
  currentOdometerKm: number
  nextMaintenanceKm?: number | null
  nextMaintenanceDate?: string | null
}

export interface Trailer {
  id: string
  plateNo: string
  vin?: string | null
  trailerType?: string | null
  brand?: string | null
  status: string
  capacityKg?: number | null
  tireConditionPercent?: number | null
}

export interface Driver {
  id: string
  firstName: string
  lastName: string
  fullName: string
  nationalId?: string | null
  phone?: string | null
  email?: string | null
  status: string
  licenseNo?: string | null
  licenseExpiryDate?: string | null
}

export interface WorkOrderPart {
  id: string
  partId: string
  quantity: number
  unitCost: number
  lineTotal: number
}

export interface WorkOrder {
  id: string
  workOrderNo?: string | null
  targetType: string
  vehicleId?: string | null
  trailerId?: string | null
  status: string
  maintenanceType: string
  title: string
  description?: string | null
  odometerBeforeKm?: number | null
  odometerAfterKm?: number | null
  laborCost: number
  partsCost: number
  totalCost: number
  scheduledDate?: string | null
  completedAt?: string | null
  parts: WorkOrderPart[]
}

export interface Part {
  id: string
  partNo: string
  name: string
  category?: string | null
  unit: string
  quantityInStock: number
  minimumStock: number
  unitCost: number
  isBelowMinimum: boolean
  warehouseId?: string | null
  supplierId?: string | null
}

export interface StockMovement {
  id: string
  movementNo?: string | null
  partId: string
  movementType: string
  quantity: number
  balanceAfter: number
  unitCost: number
  createdAt: string
}

export interface Alert {
  id: string
  alertType: string
  priority: string
  status: string
  title: string
  message?: string | null
  partId?: string | null
  vehicleId?: string | null
  trailerId?: string | null
  createdAt: string
}

export interface VehicleTrailerAssignment {
  id: string
  vehicleId: string
  trailerId: string
  status: string
  startedAt: string
  endedAt?: string | null
}

export interface DriverVehicleAssignment {
  id: string
  driverId: string
  vehicleId: string
  status: string
  startedAt: string
  endedAt?: string | null
}

export interface CompanySetting {
  id: string
  companyName: string
  defaultCurrency: string
  maintenanceDueKmThreshold: number
  maintenanceDueDaysThreshold: number
  documentExpiryDaysThreshold: number
  lowStockCheckEnabled: boolean
}

export interface VehicleCostSummary {
  vehicleId: string
  plateNo: string
  workOrderCount: number
  totalCost: number
}

export interface MonthlyCost {
  year: number
  month: number
  workOrderCount: number
  totalCost: number
}
