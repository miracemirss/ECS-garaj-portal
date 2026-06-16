import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { LoginPage } from '@/features/auth/pages/LoginPage'
import { DashboardPage } from '@/features/dashboard/pages/DashboardPage'
import { VehiclesPage } from '@/features/vehicles/pages/VehiclesPage'
import { VehicleDetailPage } from '@/features/vehicles/pages/VehicleDetailPage'
import { TrailersPage } from '@/features/trailers/pages/TrailersPage'
import { TrailerDetailPage } from '@/features/trailers/pages/TrailerDetailPage'
import { DriversPage } from '@/features/drivers/pages/DriversPage'
import { DriverDetailPage } from '@/features/drivers/pages/DriverDetailPage'
import { VehicleTrailerAssignmentsPage } from '@/features/assignments/pages/VehicleTrailerAssignmentsPage'
import { DriverVehicleAssignmentsPage } from '@/features/assignments/pages/DriverVehicleAssignmentsPage'
import { MaintenancePage } from '@/features/maintenance/pages/MaintenancePage'
import { WorkOrderDetailPage } from '@/features/maintenance/pages/WorkOrderDetailPage'
import { PartsPage } from '@/features/inventory/pages/PartsPage'
import { StockMovementsPage } from '@/features/inventory/pages/StockMovementsPage'
import { OperationsPage } from '@/features/operations/pages/OperationsPage'
import { ReportsPage } from '@/features/reports/pages/ReportsPage'
import { VehicleCostReportPage } from '@/features/reports/pages/VehicleCostReportPage'
import { PartConsumptionReportPage } from '@/features/reports/pages/PartConsumptionReportPage'
import { MaintenancePerformanceReportPage } from '@/features/reports/pages/MaintenancePerformanceReportPage'
import { AlertsPage } from '@/features/alerts/pages/AlertsPage'
import { SettingsPage } from '@/features/settings/pages/SettingsPage'
import { ProtectedRoute } from './ProtectedRoute'

const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { path: '/', element: <Navigate to="/dashboard" replace /> },
          { path: '/dashboard', element: <DashboardPage /> },
          { path: '/vehicles', element: <VehiclesPage /> },
          { path: '/vehicles/:id', element: <VehicleDetailPage /> },
          { path: '/trailers', element: <TrailersPage /> },
          { path: '/trailers/:id', element: <TrailerDetailPage /> },
          { path: '/drivers', element: <DriversPage /> },
          { path: '/drivers/:id', element: <DriverDetailPage /> },
          { path: '/vehicle-trailer-assignments', element: <VehicleTrailerAssignmentsPage /> },
          { path: '/driver-vehicle-assignments', element: <DriverVehicleAssignmentsPage /> },
          { path: '/maintenance', element: <MaintenancePage /> },
          { path: '/maintenance/:id', element: <WorkOrderDetailPage /> },
          { path: '/parts', element: <PartsPage /> },
          { path: '/stock-movements', element: <StockMovementsPage /> },
          { path: '/operations', element: <OperationsPage /> },
          { path: '/reports', element: <ReportsPage /> },
          { path: '/reports/vehicle-costs', element: <VehicleCostReportPage /> },
          { path: '/reports/part-consumption', element: <PartConsumptionReportPage /> },
          { path: '/reports/maintenance-performance', element: <MaintenancePerformanceReportPage /> },
          { path: '/alerts', element: <AlertsPage /> },
          { path: '/settings', element: <SettingsPage /> },
          { path: '*', element: <div className="p-6">404 — Sayfa bulunamadı</div> },
        ],
      },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
