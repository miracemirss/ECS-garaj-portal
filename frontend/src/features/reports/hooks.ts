import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '@/lib/constants'
import { reportsApi } from './api'

export function useVehicleCosts() {
  return useQuery({ queryKey: queryKeys.vehicleCosts, queryFn: reportsApi.vehicleCosts })
}
export function useMonthlyCosts() {
  return useQuery({ queryKey: queryKeys.monthlyCosts, queryFn: reportsApi.monthlyCosts })
}
