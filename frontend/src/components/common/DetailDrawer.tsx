import { X } from 'lucide-react'
import type { ReactNode } from 'react'

interface DetailDrawerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  children: ReactNode
  footer?: ReactNode
}

/** Right-side slide-over panel for record details. */
export function DetailDrawer({ open, onOpenChange, title, children, footer }: DetailDrawerProps) {
  if (!open) return null
  return (
    <div className="fixed inset-0 z-50">
      <div className="fixed inset-0 bg-black/40" onClick={() => onOpenChange(false)} aria-hidden />
      <aside className="fixed right-0 top-0 z-50 flex h-full w-full max-w-md flex-col border-l bg-card shadow-xl">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <h2 className="font-semibold text-foreground">{title}</h2>
          <button
            type="button"
            onClick={() => onOpenChange(false)}
            className="rounded-md p-1 text-muted-foreground hover:bg-muted hover:text-foreground"
            aria-label="Kapat"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
        <div className="flex-1 overflow-y-auto p-4">{children}</div>
        {footer && <div className="border-t p-4">{footer}</div>}
      </aside>
    </div>
  )
}
