import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { queryKeys } from '@/lib/constants'
import { extractErrorMessage } from '@/services/api/http'
import { alertsApi } from './api'

export function useOpenAlerts() {
  return useQuery({ queryKey: queryKeys.alerts, queryFn: alertsApi.open })
}

export function useAlertActions() {
  const qc = useQueryClient()
  const invalidate = () => qc.invalidateQueries({ queryKey: queryKeys.alerts })
  const onError = (e: unknown) => toast.error(extractErrorMessage(e))

  const acknowledge = useMutation({ mutationFn: (id: string) => alertsApi.acknowledge(id), onSuccess: () => { invalidate(); toast.success('Uyarı okundu işaretlendi') }, onError })
  const resolve = useMutation({ mutationFn: (id: string) => alertsApi.resolve(id), onSuccess: () => { invalidate(); toast.success('Uyarı çözüldü') }, onError })
  const dismiss = useMutation({ mutationFn: (id: string) => alertsApi.dismiss(id), onSuccess: () => { invalidate(); toast.success('Uyarı kapatıldı') }, onError })

  const generateCriticalStock = useMutation({ mutationFn: alertsApi.generateCriticalStock, onSuccess: (n) => { invalidate(); toast.success(`${n} kritik stok uyarısı üretildi`) }, onError })
  const generateMaintenanceDue = useMutation({ mutationFn: alertsApi.generateMaintenanceDue, onSuccess: (n) => { invalidate(); toast.success(`${n} bakım uyarısı üretildi`) }, onError })
  const generateDocumentExpiry = useMutation({ mutationFn: alertsApi.generateDocumentExpiry, onSuccess: (n) => { invalidate(); toast.success(`${n} belge uyarısı üretildi`) }, onError })

  return { acknowledge, resolve, dismiss, generateCriticalStock, generateMaintenanceDue, generateDocumentExpiry }
}
