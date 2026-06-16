import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'

export interface DateRange {
  from?: string
  to?: string
}

interface DateRangePickerProps {
  value: DateRange
  onChange: (value: DateRange) => void
  className?: string
}

export function DateRangePicker({ value, onChange, className }: DateRangePickerProps) {
  return (
    <div className={cn('flex items-center gap-2', className)}>
      <Input
        type="date"
        value={value.from ?? ''}
        onChange={(e) => onChange({ ...value, from: e.target.value })}
        className="w-40"
      />
      <span className="text-muted-foreground">—</span>
      <Input
        type="date"
        value={value.to ?? ''}
        onChange={(e) => onChange({ ...value, to: e.target.value })}
        className="w-40"
      />
    </div>
  )
}
