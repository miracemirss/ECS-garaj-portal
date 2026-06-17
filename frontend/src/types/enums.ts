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

export const ENUM_LABELS: Record<string, string> = {
  Active: 'Aktif',
  InMaintenance: 'Bakımda',
  Inactive: 'Pasif',
  Retired: 'Emekli',
  OnLeave: 'İzinde',
  Draft: 'Taslak',
  Open: 'Açık',
  InProgress: 'Devam Ediyor',
  Completed: 'Tamamlandı',
  Cancelled: 'İptal',
  Preventive: 'Periyodik Bakım',
  Corrective: 'Arıza Bakımı',
  Inspection: 'Muayene',
  Tire: 'Lastik',
  Other: 'Diğer',
  In: 'Stok Girişi',
  Out: 'Stok Çıkışı',
  Adjustment: 'Sayım Düzeltmesi',
  Return: 'İade',
  CriticalStock: 'Kritik Stok',
  MaintenanceDue: 'Bakım Zamanı',
  DocumentExpiry: 'Belge Süresi',
  Info: 'Bilgi',
  Warning: 'Uyarı',
  Critical: 'Kritik',
  Acknowledged: 'Görüldü',
  Resolved: 'Çözüldü',
  Dismissed: 'Kapatıldı',
  Vehicle: 'Araç',
  Trailer: 'Dorse',
}

export function getEnumLabel(value?: string | null): string {
  if (!value) return '—'
  return ENUM_LABELS[value] ?? value
}

export function toOptions(values: readonly string[]): EnumOption[] {
  return values.map((v) => ({ value: v, label: getEnumLabel(v) }))
}
