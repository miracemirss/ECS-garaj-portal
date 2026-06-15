import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'

/**
 * Application routes. Feature routes are registered here as each feature is
 * built (e.g. /vehicles, /maintenance, /inventory).
 */
const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: 'dashboard', element: <div className="p-6">Dashboard (scaffold)</div> },
      { path: '*', element: <div className="p-6">404 — Sayfa bulunamadı</div> },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
