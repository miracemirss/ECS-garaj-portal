import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/features/auth/store'

export function ProtectedRoute() {
  const token = useAuthStore((s) => s.accessToken)
  return token ? <Outlet /> : <Navigate to="/login" replace />
}
