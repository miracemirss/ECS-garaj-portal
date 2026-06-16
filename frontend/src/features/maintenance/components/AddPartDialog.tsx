import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { FormInput } from '@/components/common/FormInput'
import { FormSelect } from '@/components/common/FormSelect'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { useParts } from '@/features/inventory/hooks'
import { useAddWorkOrderPart } from '../hooks'

const schema = z.object({
  partId: z.string().min(1, 'Parça seçin'),
  quantity: z.string().refine((v) => !Number.isNaN(+v) && +v > 0, 'Miktar pozitif olmalı'),
  unitCost: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

interface Props {
  workOrderId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function AddPartDialog({ workOrderId, open, onOpenChange }: Props) {
  const addPart = useAddWorkOrderPart()
  const parts = useParts({ pageSize: 200 })
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { partId: '', quantity: '', unitCost: '' },
  })

  const partOptions = (parts.data?.items ?? []).map((p) => ({
    value: p.id,
    label: `${p.partNo} — ${p.name} (stok: ${p.quantityInStock})`,
  }))

  async function onSubmit(values: FormValues) {
    try {
      await addPart.mutateAsync({
        id: workOrderId,
        body: { partId: values.partId, quantity: Number(values.quantity), unitCost: values.unitCost ? Number(values.unitCost) : undefined },
      })
      reset()
      onOpenChange(false)
    } catch {
      /* toast handled */
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Parça Ekle</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <FormSelect label="Parça" placeholder="Seçin" options={partOptions} error={errors.partId?.message} {...register('partId')} />
          <div className="grid grid-cols-2 gap-3">
            <FormInput label="Miktar" inputMode="decimal" error={errors.quantity?.message} {...register('quantity')} />
            <FormInput label="Birim Maliyet (ops.)" inputMode="decimal" error={errors.unitCost?.message} {...register('unitCost')} />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={addPart.isPending}>Vazgeç</Button>
            <Button type="submit" disabled={addPart.isPending}>{addPart.isPending ? 'Ekleniyor...' : 'Ekle'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
