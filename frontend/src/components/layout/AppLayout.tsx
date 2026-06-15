import { Outlet } from 'react-router-dom'
import { APP_NAME } from '@/lib/constants'

/**
 * Application shell. Sidebar/topbar navigation is added with the dashboard prompt.
 */
export function AppLayout() {
  return (
    <div className="min-h-screen">
      <header className="border-b px-6 py-4">
        <h1 className="text-lg font-semibold">{APP_NAME}</h1>
      </header>
      <main className="mx-auto max-w-7xl">
        <Outlet />
      </main>
    </div>
  )
}
