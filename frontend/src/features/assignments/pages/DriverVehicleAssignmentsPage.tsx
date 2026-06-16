import { useState } from 'react'
import { FormSelect } from '@/components/common/FormSelect'
import { PageHeader } from '@/components/common/PageHeader'
import { StatusBadge } from '@/components/common/StatusBadge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useDrivers } from '@/features/drivers/hooks'
import { useVehicles } from '@/features/vehicles/hooks'
import { formatDateTime } from '@/lib/formatters'
import { useActiveDriverVehicle, useAssignDriverVehicle, useEndDriverVehicle } from '../hooks'

export function DriverVehicleAssignmentsPage() {
  const drivers = useDrivers({ pageSize: 100 })
  const vehicles = useVehicles({ pageSize: 100 })
  const assign = useAssignDriverVehicle()
  const end = useEndDriverVehicle()

  const [driverId, setDriverId] = useState('')
  const [vehicleId, setVehicleId] = useState('')
  const [lookupDriver, setLookupDriver] = useState('')
  const active = useActiveDriverVehicle(lookupDriver)

  const driverOptions = (drivers.data?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))
  const vehicleOptions = (vehicles.data?.items ?? []).map((v) => ({ value: v.id, label: v.plateNo }))

  return (
    <div>
      <PageHeader title="Şoför-Araç Atama" description="Aktif atamayı yönetin (history korunur)" />

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader><CardTitle>Yeni Atama</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <FormSelect label="Şoför" placeholder="Seçin" options={driverOptions} value={driverId} onChange={(e) => setDriverId(e.target.value)} />
            <FormSelect label="Araç" placeholder="Seçin" options={vehicleOptions} value={vehicleId} onChange={(e) => setVehicleId(e.target.value)} />
            <Button disabled={!driverId || !vehicleId || assign.isPending} onClick={() => assign.mutate({ driverId, vehicleId })}>
              {assign.isPending ? 'Atanıyor...' : 'Ata'}
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>Aktif Atama Sorgula</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <FormSelect label="Şoför" placeholder="Seçin" options={driverOptions} value={lookupDriver} onChange={(e) => setLookupDriver(e.target.value)} />
            {lookupDriver && active.isError && <p className="text-sm text-muted-foreground">Aktif araç yok.</p>}
            {active.data && (
              <div className="flex items-center justify-between rounded-md border p-3">
                <div>
                  <p className="text-sm font-medium">Aktif araç atanmış</p>
                  <p className="text-xs text-muted-foreground">Başlangıç: {formatDateTime(active.data.startedAt)}</p>
                </div>
                <div className="flex items-center gap-2">
                  <StatusBadge status={active.data.status} />
                  <Button variant="destructive" size="sm" disabled={end.isPending} onClick={() => end.mutate(active.data!.id)}>
                    Sonlandır
                  </Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
