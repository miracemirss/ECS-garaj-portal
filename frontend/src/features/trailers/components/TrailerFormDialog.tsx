import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { FormInput } from '@/components/common/FormInput'
import { FormSelect } from '@/components/common/FormSelect'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { TRAILER_TYPE_OPTIONS } from '@/lib/options'
import type { Trailer } from '@/types/models'
import { useCreateTrailer, useUpdateTrailer } from '../hooks'

const schema = z.object({
  plateNo: z.string().min(1, 'Plaka zorunlu').max(16),
  trailerType: z.string().optional(),
  brand: z.string().optional(),
  capacityKg: z.string().optional().refine((v) => !v || (!Number.isNaN(+v) && +v >= 0), 'Geçerli kapasite girin'),
  vin: z.string().optional(),
  tireConditionPercent: z
    .string()
    .optional()
    .refine((v) => !v || (/^\d+$/.test(v) && +v >= 0 && +v <= 100), 'Lastik ömrü 0-100 arası olmalı'),
})
type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  trailer?: Trailer | null
}

export function TrailerFormDialog({ open, onOpenChange, trailer }: Props) {
  const isEdit = !!trailer
  const create = useCreateTrailer()
  const update = useUpdateTrailer()
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (open) {
      reset({
        plateNo: trailer?.plateNo ?? '',
        trailerType: trailer?.trailerType ?? '',
        brand: trailer?.brand ?? '',
        capacityKg: trailer?.capacityKg != null ? String(trailer.capacityKg) : '',
        vin: trailer?.vin ?? '',
        tireConditionPercent: trailer?.tireConditionPercent != null ? String(trailer.tireConditionPercent) : '',
      })
    }
  }, [open, trailer, reset])

  async function onSubmit(values: FormValues) {
    const capacityKg = values.capacityKg ? Number(values.capacityKg) : undefined
    const tire = values.tireConditionPercent ? Number(values.tireConditionPercent) : undefined
    try {
      if (isEdit && trailer) {
        await update.mutateAsync({ id: trailer.id, body: { trailerType: values.trailerType || undefined, brand: values.brand || undefined, capacityKg, tireConditionPercent: tire } })
      } else {
        await create.mutateAsync({ plateNo: values.plateNo, trailerType: values.trailerType || undefined, brand: values.brand || undefined, capacityKg, vin: values.vin || undefined, tireConditionPercent: tire })
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
          <DialogTitle>{isEdit ? 'Dorseyi Düzenle' : 'Yeni Dorse'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <FormInput label="Plaka" disabled={isEdit} error={errors.plateNo?.message} {...register('plateNo')} />
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <FormSelect label="Dorse Türü" placeholder="Seçin" options={TRAILER_TYPE_OPTIONS} error={errors.trailerType?.message} {...register('trailerType')} />
            <FormInput label="Marka" error={errors.brand?.message} {...register('brand')} />
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <FormInput label="Kapasite (kg)" inputMode="numeric" error={errors.capacityKg?.message} {...register('capacityKg')} />
            <FormInput label="Lastik Ömrü (%)" inputMode="numeric" error={errors.tireConditionPercent?.message} {...register('tireConditionPercent')} />
          </div>
          {!isEdit && <FormInput label="Şasi No (VIN)" error={errors.vin?.message} {...register('vin')} />}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>Vazgeç</Button>
            <Button type="submit" disabled={pending}>{pending ? 'Kaydediliyor...' : 'Kaydet'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
