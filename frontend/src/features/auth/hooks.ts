import { useMutation } from '@tanstack/react-query'
import { toast } from 'sonner'
import { extractErrorMessage } from '@/services/api/http'
import { login as loginApi, type LoginRequest } from './api'
import { useAuthStore } from './store'

export function useLogin() {
  const setSession = useAuthStore((s) => s.setSession)
  return useMutation({
    mutationFn: (body: LoginRequest) => loginApi(body),
    onSuccess: (data) => {
      setSession(data.accessToken, data.refreshToken, data.user)
      toast.success('Giriş başarılı')
    },
    onError: (error) => toast.error(extractErrorMessage(error)),
  })
}
