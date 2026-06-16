import { Pencil, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Breadcrumb } from '@/components/common/Breadcrumb'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { PageHeader } from '@/components/common/PageHeader'
import { StatusBadge } from '@/components/common/StatusBadge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { formatDate, formatNumber } from '@/lib/formatters'
import { VehicleFormDialog } from '../components/VehicleFormDialog'
import { useDeleteVehicle, useVehicle } from '../hooks'

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-0.5 text-sm font-medium text-foreground">{children}</p>
    </div>
  )
}

export function VehicleDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { data: vehicle, isLoading } = useVehicle(id)
  const remove = useDeleteVehicle()
  const [editOpen, setEditOpen] = useState(false)
  const [confirmOpen, setConfirmOpen] = useState(false)

  if (isLoading) {
    return <Skeleton className="h-64 w-full" />
  }
  if (!vehicle) {
    return <p className="text-sm text-muted-foreground">Araç bulunamadı.</p>
  }

  return (
    <div>
      <Breadcrumb items={[{ label: 'Araçlar', to: '/vehicles' }, { label: vehicle.plateNo }]} />
      <PageHeader
        title={vehicle.plateNo}
        description={`${vehicle.brand}${vehicle.model ? ` ${vehicle.model}` : ''}`}
        actions={
          <>
            <Button variant="outline" onClick={() => setEditOpen(true)}>
              <Pencil className="h-4 w-4" /> Düzenle
            </Button>
            <Button variant="destructive" onClick={() => setConfirmOpen(true)}>
              <Trash2 className="h-4 w-4" /> Sil
            </Button>
          </>
        }
      />

      <Card>
        <CardContent className="grid grid-cols-2 gap-5 p-6 sm:grid-cols-3">
          <Field label="Plaka">{vehicle.plateNo}</Field>
          <Field label="Şasi No">{vehicle.vin ?? '—'}</Field>
          <Field label="Durum"><StatusBadge status={vehicle.status} /></Field>
          <Field label="Marka / Model">{vehicle.brand} {vehicle.model ?? ''}</Field>
          <Field label="Model Yılı">{vehicle.modelYear ?? '—'}</Field>
          <Field label="Güncel KM">{formatNumber(vehicle.currentOdometerKm)}</Field>
          <Field label="Sonraki Bakım KM">{vehicle.nextMaintenanceKm ? formatNumber(vehicle.nextMaintenanceKm) : '—'}</Field>
          <Field label="Sonraki Bakım Tarihi">{formatDate(vehicle.nextMaintenanceDate)}</Field>
        </CardContent>
      </Card>

      <VehicleFormDialog open={editOpen} onOpenChange={setEditOpen} vehicle={vehicle} />
      <ConfirmDialog
        open={confirmOpen}
        onOpenChange={setConfirmOpen}
        title="Aracı sil"
        description={`${vehicle.plateNo} plakalı araç silinecek.`}
        loading={remove.isPending}
        onConfirm={async () => {
          await remove.mutateAsync(vehicle.id)
          navigate('/vehicles')
        }}
      />
    </div>
  )
}
