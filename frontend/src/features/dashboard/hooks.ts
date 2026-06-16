import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '@/lib/constants'
import { dashboardApi } from './api'

export function useDashboard() {
  const vehicles = useQuery({ queryKey: ['dashboard', 'vehicles'], queryFn: dashboardApi.vehicles })
  const trailers = useQuery({ queryKey: ['dashboard', 'trailers'], queryFn: dashboardApi.trailers })
  const drivers = useQuery({ queryKey: ['dashboard', 'drivers'], queryFn: dashboardApi.drivers })
  const criticalStocks = useQuery({ queryKey: queryKeys.criticalStocks, queryFn: dashboardApi.criticalStocks })
  const openAlerts = useQuery({ queryKey: queryKeys.alerts, queryFn: dashboardApi.openAlerts })
  const monthlyCosts = useQuery({ queryKey: queryKeys.monthlyCosts, queryFn: dashboardApi.monthlyCosts })
  return { vehicles, trailers, drivers, criticalStocks, openAlerts, monthlyCosts }
}
