import { useState } from 'react'
import { FormSelect } from '@/components/common/FormSelect'
import { PageHeader } from '@/components/common/PageHeader'
import { StatusBadge } from '@/components/common/StatusBadge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useTrailers } from '@/features/trailers/hooks'
import { useVehicles } from '@/features/vehicles/hooks'
import { formatDateTime } from '@/lib/formatters'
import { useActiveVehicleTrailer, useAssignVehicleTrailer, useEndVehicleTrailer } from '../hooks'

export function VehicleTrailerAssignmentsPage() {
  const vehicles = useVehicles({ pageSize: 100 })
  const trailers = useTrailers({ pageSize: 100 })
  const assign = useAssignVehicleTrailer()
  const end = useEndVehicleTrailer()

  const [vehicleId, setVehicleId] = useState('')
  const [trailerId, setTrailerId] = useState('')
  const [lookupVehicle, setLookupVehicle] = useState('')
  const active = useActiveVehicleTrailer(lookupVehicle)

  const vehicleOptions = (vehicles.data?.items ?? []).map((v) => ({ value: v.id, label: v.plateNo }))
  const trailerOptions = (trailers.data?.items ?? []).map((t) => ({ value: t.id, label: t.plateNo }))

  return (
    <div>
      <PageHeader title="Araç-Dorse Eşleştirme" description="Aktif eşleşmeyi yönetin (history korunur)" />

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader><CardTitle>Yeni Eşleştirme</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <FormSelect label="Araç" placeholder="Seçin" options={vehicleOptions} value={vehicleId} onChange={(e) => setVehicleId(e.target.value)} />
            <FormSelect label="Dorse" placeholder="Seçin" options={trailerOptions} value={trailerId} onChange={(e) => setTrailerId(e.target.value)} />
            <Button
              disabled={!vehicleId || !trailerId || assign.isPending}
              onClick={() => assign.mutate({ vehicleId, trailerId })}
            >
              {assign.isPending ? 'Eşleştiriliyor...' : 'Eşleştir'}
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>Aktif Eşleşme Sorgula</CardTitle></CardHeader>
          <CardContent className="space-y-4">
            <FormSelect label="Araç" placeholder="Seçin" options={vehicleOptions} value={lookupVehicle} onChange={(e) => setLookupVehicle(e.target.value)} />
            {lookupVehicle && active.isError && <p className="text-sm text-muted-foreground">Aktif dorse yok.</p>}
            {active.data && (
              <div className="flex items-center justify-between rounded-md border p-3">
                <div>
                  <p className="text-sm font-medium">Aktif dorse atanmış</p>
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
