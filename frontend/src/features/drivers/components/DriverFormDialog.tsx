import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { FormInput } from '@/components/common/FormInput'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import type { Driver } from '@/types/models'
import { useCreateDriver, useUpdateDriver } from '../hooks'

const phoneRegex = /^\+?[0-9\s\-()]{7,20}$/

const schema = z.object({
  firstName: z.string().min(1, 'Ad zorunlu'),
  lastName: z.string().min(1, 'Soyad zorunlu'),
  nationalId: z.string().optional(),
  phone: z.string().optional().refine((v) => !v || phoneRegex.test(v), 'Geçerli telefon girin'),
  email: z.string().optional().refine((v) => !v || z.string().email().safeParse(v).success, 'Geçerli e-posta girin'),
  licenseNo: z.string().optional(),
  licenseExpiryDate: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  driver?: Driver | null
}

export function DriverFormDialog({ open, onOpenChange, driver }: Props) {
  const isEdit = !!driver
  const create = useCreateDriver()
  const update = useUpdateDriver()
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (open) {
      reset({
        firstName: driver?.firstName ?? '',
        lastName: driver?.lastName ?? '',
        nationalId: driver?.nationalId ?? '',
        phone: driver?.phone ?? '',
        email: driver?.email ?? '',
        licenseNo: driver?.licenseNo ?? '',
        licenseExpiryDate: driver?.licenseExpiryDate ?? '',
      })
    }
  }, [open, driver, reset])

  async function onSubmit(values: FormValues) {
    try {
      if (isEdit && driver) {
        await update.mutateAsync({
          id: driver.id,
          body: { phone: values.phone || undefined, email: values.email || undefined, licenseNo: values.licenseNo || undefined, licenseExpiryDate: values.licenseExpiryDate || undefined },
        })
      } else {
        await create.mutateAsync({ firstName: values.firstName, lastName: values.lastName, nationalId: values.nationalId || undefined, phone: values.phone || undefined, email: values.email || undefined })
      }
      onOpenChange(false)
    } catch {
      /* toast handled */
    }
  }

  const pending = create.isPending || update.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Şoförü Düzenle' : 'Yeni Şoför'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div className="grid grid-cols-2 gap-3">
            <FormInput label="Ad" disabled={isEdit} error={errors.firstName?.message} {...register('firstName')} />
            <FormInput label="Soyad" disabled={isEdit} error={errors.lastName?.message} {...register('lastName')} />
          </div>
          <FormInput label="TC Kimlik No" disabled={isEdit} error={errors.nationalId?.message} {...register('nationalId')} />
          <div className="grid grid-cols-2 gap-3">
            <FormInput label="Telefon" error={errors.phone?.message} {...register('phone')} />
            <FormInput label="E-posta" type="email" error={errors.email?.message} {...register('email')} />
          </div>
          {isEdit && (
            <div className="grid grid-cols-2 gap-3">
              <FormInput label="Ehliyet No" error={errors.licenseNo?.message} {...register('licenseNo')} />
              <FormInput label="Ehliyet Bitiş" type="date" error={errors.licenseExpiryDate?.message} {...register('licenseExpiryDate')} />
            </div>
          )}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>Vazgeç</Button>
            <Button type="submit" disabled={pending}>{pending ? 'Kaydediliyor...' : 'Kaydet'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
