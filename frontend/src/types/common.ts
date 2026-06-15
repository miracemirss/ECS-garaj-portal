export type Id = string

// Mirrors ECS.Domain.Enums — keep these in sync with the backend.
export enum EntityType {
  Vehicle = 'Vehicle',
  Trailer = 'Trailer',
}

export enum WorkOrderStatus {
  Draft = 'Draft',
  Open = 'Open',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
}

export enum AlertType {
  CriticalStock = 'CriticalStock',
  MaintenanceDue = 'MaintenanceDue',
}
