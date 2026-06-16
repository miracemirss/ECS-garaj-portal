import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { FormInput } from '@/components/common/FormInput'
import { FormSelect } from '@/components/common/FormSelect'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { TargetType, WorkOrderType, toOptions } from '@/types/enums'
import { useTrailers } from '@/features/trailers/hooks'
import { useVehicles } from '@/features/vehicles/hooks'
import { useCreateWorkOrder } from '../hooks'

const schema = z.object({
  targetType: z.enum(['Vehicle', 'Trailer']),
  targetId: z.string().min(1, 'Hedef seçin'),
  title: z.string().min(1, 'Başlık zorunlu'),
  maintenanceType: z.string().min(1),
  odometerBeforeKm: z.string().optional(),
  laborCost: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function WorkOrderFormDialog({ open, onOpenChange }: Props) {
  const create = useCreateWorkOrder()
  const vehicles = useVehicles({ pageSize: 100 })
  const trailers = useTrailers({ pageSize: 100 })
  const { register, handleSubmit, watch, reset, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { targetType: 'Vehicle', targetId: '', title: '', maintenanceType: 'Corrective', odometerBeforeKm: '', laborCost: '' },
  })

  const targetType = watch('targetType')
  const targetOptions =
    targetType === 'Trailer'
      ? (trailers.data?.items ?? []).map((t) => ({ value: t.id, label: t.plateNo }))
      : (vehicles.data?.items ?? []).map((v) => ({ value: v.id, label: v.plateNo }))

  async function onSubmit(values: FormValues) {
    try {
      await create.mutateAsync({
        targetType: values.targetType,
        targetId: values.targetId,
        title: values.title,
        maintenanceType: values.maintenanceType,
        odometerBeforeKm: values.odometerBeforeKm ? Number(values.odometerBeforeKm) : undefined,
        laborCost: values.laborCost ? Number(values.laborCost) : undefined,
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
          <DialogTitle>Yeni İş Emri</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div className="grid grid-cols-2 gap-3">
            <FormSelect label="Hedef Tipi" options={toOptions(TargetType)} error={errors.targetType?.message} {...register('targetType')} />
            <FormSelect label="Hedef" placeholder="Seçin" options={targetOptions} error={errors.targetId?.message} {...register('targetId')} />
          </div>
          <FormInput label="Başlık" error={errors.title?.message} {...register('title')} />
          <div className="grid grid-cols-3 gap-3">
            <FormSelect label="Bakım Tipi" options={toOptions(WorkOrderType)} error={errors.maintenanceType?.message} {...register('maintenanceType')} />
            <FormInput label="KM (giriş)" inputMode="numeric" error={errors.odometerBeforeKm?.message} {...register('odometerBeforeKm')} />
            <FormInput label="İşçilik Maliyeti" inputMode="decimal" error={errors.laborCost?.message} {...register('laborCost')} />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={create.isPending}>Vazgeç</Button>
            <Button type="submit" disabled={create.isPending}>{create.isPending ? 'Kaydediliyor...' : 'Oluştur'}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
