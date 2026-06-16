import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { FormInput } from '@/components/common/FormInput'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { APP_FULL_NAME } from '@/lib/constants'
import { useLogin } from '../hooks'

const schema = z.object({
  email: z.string().min(1, 'E-posta zorunlu').email('Geçerli bir e-posta girin'),
  password: z.string().min(6, 'En az 6 karakter'),
})

type FormValues = z.infer<typeof schema>

export function LoginPage() {
  const navigate = useNavigate()
  const { mutateAsync, isPending } = useLogin()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { email: '', password: '' } })

  async function onSubmit(values: FormValues) {
    try {
      await mutateAsync(values)
      navigate('/dashboard')
    } catch {
      /* error toast handled in the hook */
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface p-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="space-y-2 text-center">
          <div className="mx-auto flex h-11 w-11 items-center justify-center rounded-lg bg-primary text-lg font-bold text-primary-foreground">
            E
          </div>
          <CardTitle className="text-xl text-navy">{APP_FULL_NAME}</CardTitle>
          <CardDescription>Devam etmek için giriş yapın</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <FormInput label="E-posta" type="email" placeholder="admin@ecs.local" error={errors.email?.message} {...register('email')} />
            <FormInput label="Parola" type="password" placeholder="••••••••" error={errors.password?.message} {...register('password')} />
            <Button type="submit" className="w-full" disabled={isPending}>
              {isPending ? 'Giriş yapılıyor...' : 'Giriş Yap'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
