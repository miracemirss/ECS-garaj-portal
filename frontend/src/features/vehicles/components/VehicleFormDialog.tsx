import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { FormInput } from '@/components/common/FormInput'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import type { Vehicle } from '@/types/models'
import { useCreateVehicle, useUpdateVehicle } from '../hooks'

const schema = z.object({
  plateNo: z.string().min(1, 'Plaka zorunlu').max(16),
  brand: z.string().min(1, 'Marka zorunlu').max(100),
  model: z.string().max(100).optional(),
  modelYear: z
    .string()
    .optional()
    .refine((v) => !v || (/^\d{4}$/.test(v) && +v >= 1950 && +v <= 2100), 'Geçerli yıl girin (1950-2100)'),
  vin: z.string().max(32).optional(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  vehicle?: Vehicle | null
}

export function VehicleFormDialog({ open, onOpenChange, vehicle }: Props) {
  const isEdit = !!vehicle
  const create = useCreateVehicle()
  const update = useUpdateVehicle()
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (open) {
      reset({
        plateNo: vehicle?.plateNo ?? '',
        brand: vehicle?.brand ?? '',
        model: vehicle?.model ?? '',
        modelYear: vehicle?.modelYear ? String(vehicle.modelYear) : '',
        vin: vehicle?.vin ?? '',
      })
    }
  }, [open, vehicle, reset])

  async function onSubmit(values: FormValues) {
    const modelYear = values.modelYear ? Number(values.modelYear) : undefined
    try {
      if (isEdit && vehicle) {
        await update.mutateAsync({ id: vehicle.id, body: { brand: values.brand, model: values.model || undefined, modelYear } })
      } else {
        await create.mutateAsync({
          plateNo: values.plateNo,
          brand: values.brand,
          model: values.model || undefined,
          modelYear,
          vin: values.vin || undefined,
        })
      }
      onOpenChange(false)
    } catch {
      /* toast handled in hooks */
    }
  }

  const pending = create.isPending || update.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Aracı Düzenle' : 'Yeni Araç'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <FormInput label="Plaka" disabled={isEdit} error={errors.plateNo?.message} {...register('plateNo')} />
          <FormInput label="Marka" error={errors.brand?.message} {...register('brand')} />
          <div className="grid grid-cols-2 gap-3">
            <FormInput label="Model" error={errors.model?.message} {...register('model')} />
            <FormInput label="Model Yılı" inputMode="numeric" error={errors.modelYear?.message} {...register('modelYear')} />
          </div>
          {!isEdit && <FormInput label="Şasi No (VIN)" error={errors.vin?.message} {...register('vin')} />}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>
              Vazgeç
            </Button>
            <Button type="submit" disabled={pending}>
              {pending ? 'Kaydediliyor...' : 'Kaydet'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
