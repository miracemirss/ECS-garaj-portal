import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { queryKeys } from '@/lib/constants'
import { extractErrorMessage } from '@/services/api/http'
import { settingsApi, type UpdateSettingsRequest } from './api'

export function useSettings() {
  return useQuery({ queryKey: queryKeys.settings, queryFn: settingsApi.get })
}

export function useUpdateSettings() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateSettingsRequest) => settingsApi.update(body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: queryKeys.settings }); toast.success('Ayarlar kaydedildi') },
    onError: (e) => toast.error(extractErrorMessage(e)),
  })
}
