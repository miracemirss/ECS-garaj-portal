import { LogOut } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { APP_NAME } from '@/lib/constants'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/features/auth/store'

export function Topbar() {
  const navigate = useNavigate()
  const clear = useAuthStore((s) => s.clear)

  function logout() {
    clear()
    navigate('/login')
  }

  return (
    <header className="flex h-14 shrink-0 items-center justify-between border-b bg-card px-4">
      <span className="text-base font-semibold text-navy md:hidden">{APP_NAME}</span>
      <div className="flex-1" />
      <Button variant="ghost" size="sm" onClick={logout}>
        <LogOut className="h-4 w-4" /> Çıkış
      </Button>
    </header>
  )
}
