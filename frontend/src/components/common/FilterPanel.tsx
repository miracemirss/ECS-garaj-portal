import type { ReactNode } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'

interface FilterPanelProps {
  children: ReactNode
  onClear?: () => void
}

export function FilterPanel({ children, onClear }: FilterPanelProps) {
  return (
    <Card className="mb-4">
      <CardContent className="flex flex-wrap items-end gap-3 p-4">
        {children}
        {onClear && (
          <Button variant="ghost" size="sm" onClick={onClear}>
            Temizle
          </Button>
        )}
      </CardContent>
    </Card>
  )
}
