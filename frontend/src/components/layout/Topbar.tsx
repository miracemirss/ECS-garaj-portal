import { LogOut, Menu } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { APP_NAME } from '@/lib/constants'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/features/auth/store'

interface TopbarProps {
  onMenuClick: () => void
}

export function Topbar({ onMenuClick }: TopbarProps) {
  const navigate = useNavigate()
  const clear = useAuthStore((s) => s.clear)

  function logout() {
    clear()
    navigate('/login')
  }

  return (
    <header className="flex h-14 shrink-0 items-center justify-between border-b bg-card px-3 sm:px-4">
      <div className="flex min-w-0 items-center gap-2">
        <Button variant="ghost" size="icon" className="md:hidden" onClick={onMenuClick} aria-label="Menüyü aç">
          <Menu className="h-5 w-5" />
        </Button>
        <span className="truncate text-base font-semibold text-navy md:hidden">{APP_NAME}</span>
      </div>
      <Button variant="ghost" size="sm" onClick={logout}>
        <LogOut className="h-4 w-4" /> Çıkış
      </Button>
    </header>
  )
}
