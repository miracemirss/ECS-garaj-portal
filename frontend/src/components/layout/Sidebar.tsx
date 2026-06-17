import {
  ArrowLeftRight,
  BarChart3,
  Bell,
  Boxes,
  ClipboardList,
  Container,
  LayoutDashboard,
  Link2,
  Package,
  Settings,
  Truck,
  Users,
  Wrench,
  X,
  type LucideIcon,
} from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { APP_NAME } from '@/lib/constants'
import { cn } from '@/lib/utils'

interface NavItem {
  to: string
  label: string
  icon: LucideIcon
}

interface SidebarProps {
  mobileOpen?: boolean
  onMobileOpenChange?: (open: boolean) => void
}

const NAV: { group: string; items: NavItem[] }[] = [
  { group: 'Genel', items: [{ to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard }] },
  {
    group: 'Filo',
    items: [
      { to: '/vehicles', label: 'Araçlar', icon: Truck },
      { to: '/trailers', label: 'Dorseler', icon: Container },
      { to: '/drivers', label: 'Şoförler', icon: Users },
      { to: '/vehicle-trailer-assignments', label: 'Araç-Dorse', icon: Link2 },
      { to: '/driver-vehicle-assignments', label: 'Şoför-Araç', icon: ArrowLeftRight },
    ],
  },
  {
    group: 'Bakım & Stok',
    items: [
      { to: '/maintenance', label: 'İş Emirleri', icon: Wrench },
      { to: '/parts', label: 'Parçalar', icon: Package },
      { to: '/stock-movements', label: 'Stok Hareketleri', icon: Boxes },
    ],
  },
  {
    group: 'Analiz',
    items: [
      { to: '/operations', label: 'Operasyon', icon: ClipboardList },
      { to: '/reports', label: 'Raporlar', icon: BarChart3 },
      { to: '/alerts', label: 'Uyarılar', icon: Bell },
    ],
  },
  { group: 'Sistem', items: [{ to: '/settings', label: 'Ayarlar', icon: Settings }] },
]

function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  return (
    <>
      <div className="flex h-14 items-center gap-2 border-b px-4">
        <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-sm font-bold text-primary-foreground">
          E
        </div>
        <span className="text-base font-semibold text-navy">{APP_NAME}</span>
      </div>
      <nav className="flex-1 space-y-4 overflow-y-auto p-3">
        {NAV.map((section) => (
          <div key={section.group}>
            <p className="px-2 pb-1 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
              {section.group}
            </p>
            <div className="space-y-0.5">
              {section.items.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  onClick={onNavigate}
                  className={({ isActive }) =>
                    cn(
                      'flex min-h-10 items-center gap-2.5 rounded-md px-2.5 py-2 text-sm font-medium transition-colors',
                      isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-muted hover:text-foreground',
                    )
                  }
                >
                  <item.icon className="h-4 w-4 shrink-0" />
                  <span className="truncate">{item.label}</span>
                </NavLink>
              ))}
            </div>
          </div>
        ))}
      </nav>
    </>
  )
}

export function Sidebar({ mobileOpen = false, onMobileOpenChange }: SidebarProps) {
  return (
    <>
      <aside className="hidden w-60 shrink-0 flex-col border-r bg-card md:flex">
        <SidebarContent />
      </aside>

      {mobileOpen && (
        <div className="fixed inset-0 z-50 md:hidden">
          <div className="absolute inset-0 bg-black/40" onClick={() => onMobileOpenChange?.(false)} aria-hidden />
          <aside className="relative flex h-full w-[min(18rem,85vw)] flex-col border-r bg-card shadow-xl">
            <Button
              variant="ghost"
              size="icon"
              className="absolute right-2 top-2"
              onClick={() => onMobileOpenChange?.(false)}
              aria-label="Menüyü kapat"
            >
              <X className="h-5 w-5" />
            </Button>
            <SidebarContent onNavigate={() => onMobileOpenChange?.(false)} />
          </aside>
        </div>
      )}
    </>
  )
}
