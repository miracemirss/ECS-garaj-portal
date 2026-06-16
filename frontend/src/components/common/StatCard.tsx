import type { LucideIcon } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { cn } from '@/lib/utils'

type Tone = 'default' | 'success' | 'warning' | 'danger' | 'info'

const TONE_CLASS: Record<Tone, string> = {
  default: 'bg-primary/10 text-primary',
  success: 'bg-success/10 text-success',
  warning: 'bg-warning/10 text-[hsl(38_92%_38%)]',
  danger: 'bg-danger/10 text-danger',
  info: 'bg-info/10 text-info',
}

interface StatCardProps {
  title: string
  value: string | number
  icon?: LucideIcon
  tone?: Tone
  hint?: string
}

export function StatCard({ title, value, icon: Icon, tone = 'default', hint }: StatCardProps) {
  return (
    <Card>
      <CardContent className="flex items-center justify-between p-5">
        <div>
          <p className="text-sm text-muted-foreground">{title}</p>
          <p className="mt-1 text-2xl font-semibold tracking-tight text-foreground">{value}</p>
          {hint && <p className="mt-1 text-xs text-muted-foreground">{hint}</p>}
        </div>
        {Icon && (
          <div className={cn('rounded-lg p-2.5', TONE_CLASS[tone])}>
            <Icon className="h-5 w-5" />
          </div>
        )}
      </CardContent>
    </Card>
  )
}
