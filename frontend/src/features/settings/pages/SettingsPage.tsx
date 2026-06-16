import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { FormInput } from '@/components/common/FormInput'
import { PageHeader } from '@/components/common/PageHeader'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { useSettings, useUpdateSettings } from '../hooks'

interface FormValues {
  companyName: string
  defaultCurrency: string
  maintenanceDueKmThreshold: string
  maintenanceDueDaysThreshold: string
  documentExpiryDaysThreshold: string
  lowStockCheckEnabled: boolean
}

export function SettingsPage() {
  const { data, isLoading } = useSettings()
  const update = useUpdateSettings()
  const { register, handleSubmit, reset } = useForm<FormValues>()

  useEffect(() => {
    if (data) {
      reset({
        companyName: data.companyName,
        defaultCurrency: data.defaultCurrency,
        maintenanceDueKmThreshold: String(data.maintenanceDueKmThreshold),
        maintenanceDueDaysThreshold: String(data.maintenanceDueDaysThreshold),
        documentExpiryDaysThreshold: String(data.documentExpiryDaysThreshold),
        lowStockCheckEnabled: data.lowStockCheckEnabled,
      })
    }
  }, [data, reset])

  async function onSubmit(values: FormValues) {
    await update.mutateAsync({
      companyName: values.companyName,
      defaultCurrency: values.defaultCurrency,
      maintenanceDueKmThreshold: Number(values.maintenanceDueKmThreshold) || 0,
      maintenanceDueDaysThreshold: Number(values.maintenanceDueDaysThreshold) || 0,
      documentExpiryDaysThreshold: Number(values.documentExpiryDaysThreshold) || 0,
      lowStockCheckEnabled: values.lowStockCheckEnabled,
    })
  }

  if (isLoading) return <Skeleton className="h-64 w-full" />

  return (
    <div>
      <PageHeader title="Ayarlar" description="Şirket ve eşik ayarları" />
      <Card className="max-w-2xl">
        <CardHeader><CardTitle>Şirket Ayarları</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <FormInput label="Şirket Adı" {...register('companyName')} />
              <FormInput label="Para Birimi" {...register('defaultCurrency')} />
            </div>
            <div className="grid grid-cols-3 gap-3">
              <FormInput label="Bakım KM Eşiği" inputMode="numeric" {...register('maintenanceDueKmThreshold')} />
              <FormInput label="Bakım Gün Eşiği" inputMode="numeric" {...register('maintenanceDueDaysThreshold')} />
              <FormInput label="Belge Bitiş Eşiği (gün)" inputMode="numeric" {...register('documentExpiryDaysThreshold')} />
            </div>
            <label className="flex items-center gap-2">
              <input type="checkbox" className="h-4 w-4 rounded border-input" {...register('lowStockCheckEnabled')} />
              <Label>Düşük stok kontrolü aktif</Label>
            </label>
            <Button type="submit" disabled={update.isPending}>{update.isPending ? 'Kaydediliyor...' : 'Kaydet'}</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
