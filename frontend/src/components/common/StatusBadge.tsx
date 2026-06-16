import { Badge, type BadgeProps } from '@/components/ui/badge'

type Variant = NonNullable<BadgeProps['variant']>

// Maps backend enum string values to a semantic badge color.
const VARIANT_BY_STATUS: Record<string, Variant> = {
  // active / positive
  Active: 'success',
  Completed: 'success',
  Resolved: 'success',
  // in-progress / info
  Open: 'info',
  InProgress: 'info',
  Acknowledged: 'info',
  // warning
  InMaintenance: 'warning',
  OnLeave: 'warning',
  Warning: 'warning',
  MaintenanceDue: 'warning',
  DocumentExpiry: 'warning',
  // neutral
  Draft: 'neutral',
  Inactive: 'neutral',
  Ended: 'neutral',
  Dismissed: 'neutral',
  Retired: 'neutral',
  Info: 'info',
  // danger
  Cancelled: 'danger',
  Critical: 'danger',
  CriticalStock: 'danger',
}

export function StatusBadge({ status }: { status?: string | null }) {
  if (!status) return <Badge variant="neutral">—</Badge>
  return <Badge variant={VARIANT_BY_STATUS[status] ?? 'neutral'}>{status}</Badge>
}
