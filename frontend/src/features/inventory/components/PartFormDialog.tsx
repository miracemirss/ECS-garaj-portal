import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { FormInput } from '@/components/common/FormInput'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import type { Part } from '@/types/models'
import { useCreatePart, useUpdatePart } from '../hooks'

const numeric = (msg: string) => z.string().optional().refine((v) => !v || (!Number.isNaN(+v) && +v >= 0), msg)

const schema = z.object({
  partNo: z.string().min(1, 'Parça kodu zorunlu'),
  name: z.string().min(1, 'Parça adı zorunlu'),
  unit: z.string().optional(),
  minimumStock: numeric('Geçerli değer girin'),
  unitCost: numeric('Geçerli değer girin'),
})
type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  part?: Part | null
}

export function PartFormDialog({ open, onOpenChange, part }: Props) {
  const isEdit = !!part
  const create = useCreatePart()
  const update = useUpdatePart()
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (open) {
      reset({
        partNo: part?.partNo ?? '',
        name: part?.name ?? '',
        unit: part?.unit ?? 'pcs',
        minimumStock: part?.minimumStock != null ? String(part.minimumStock) : '0',
        unitCost: part?.unitCost != null ? String(part.unitCost) : '0',
      })
    }
  }, [open, part, reset])

  async function onSubmit(values: FormValues) {
    const minimumStock = values.minimumStock ? Number(values.minimumStock) : 0
    const unitCost = values.unitCost ? Number(values.unitCost) : 0
    try {
      if (isEdit && part) {
        await update.mutateAsync({ id: part.id, body: { name: values.name, minimumStock, unitCost } })
      } else {
        await create.mutateAsync({ partNo: values.partNo, name: values.name, unit: values.unit || 'pcs', minimumStock, unitCost })
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
          <DialogTitle>{isEdit ? 'Parçayı Düzenle' : 'Yeni Parça'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div className="grid grid-cols-2 gap-3">
            <FormInput label="Parça Kodu" disabled={isEdit} error={errors.partNo?.message} {...register('partNo')} />
            <FormInput label="Birim" error={errors.unit?.message} {...register('unit')} />
          </div>
          <FormInput label="Parça Adı" error={errors.name?.message} {...register('name')} />
          <div className="grid grid-cols-2 gap-3">
            <FormInput label="Minimum Stok" inputMode="decimal" error={errors.minimumStock?.message} {...register('minimumStock')} />
            <FormInput label="Birim Maliyet" inputMode="decimal" error={errors.unitCost?.message} {...register('unitCost')} />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>Vazgeç</Button>
            <Button type="submit" disabled={pending}>{pending ? 'Kaydediliyor...' : 'Kaydet'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
