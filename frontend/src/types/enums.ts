// String enums mirroring ECS.Domain.Enums (backend serializes enum -> string).

export const VehicleStatus = ['Active', 'InMaintenance', 'Inactive', 'Retired'] as const
export const TrailerStatus = VehicleStatus
export const DriverStatus = ['Active', 'OnLeave', 'Inactive'] as const
export const WorkOrderStatus = ['Draft', 'Open', 'InProgress', 'Completed', 'Cancelled'] as const
export const WorkOrderType = ['Preventive', 'Corrective', 'Inspection', 'Tire', 'Other'] as const
export const StockMovementType = ['In', 'Out', 'Adjustment', 'Return'] as const
export const AlertType = ['CriticalStock', 'MaintenanceDue', 'DocumentExpiry'] as const
export const AlertPriority = ['Info', 'Warning', 'Critical'] as const
export const AlertStatus = ['Open', 'Acknowledged', 'Resolved', 'Dismissed'] as const
export const TargetType = ['Vehicle', 'Trailer'] as const

export type EnumOption = { value: string; label: string }

export function toOptions(values: readonly string[]): EnumOption[] {
  return values.map((v) => ({ value: v, label: v }))
}

/**
 * Backend stok hareketi enum'ı İngilizce serileşir (In/Out/Adjustment/Return);
 * kullanıcıya Türkçe gösterilir. `hasWorkOrder` true ise çıkış hareketi bir iş
 * emrinde tüketim anlamına gelir ("Bakımda Kullanım").
 */
export const StockMovementTypeLabels: Record<string, string> = {
  In: 'Stok Girişi',
  Out: 'Stok Çıkışı',
  Adjustment: 'Sayım Düzeltmesi',
  Return: 'İade',
}

export function stockMovementTypeLabel(type: string, hasWorkOrder = false): string {
  if (type === 'Out' && hasWorkOrder) return 'Bakımda Kullanım'
  return StockMovementTypeLabels[type] ?? type
}
