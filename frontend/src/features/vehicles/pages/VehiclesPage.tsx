import { Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ConfirmDialog } from '@/components/common/ConfirmDialog'
import { DataTable, type Column } from '@/components/common/DataTable'
import { PageHeader } from '@/components/common/PageHeader'
import { SearchInput } from '@/components/common/SearchInput'
import { StatusBadge } from '@/components/common/StatusBadge'
import { Button } from '@/components/ui/button'
import { useListState } from '@/hooks/useListState'
import { formatDate, formatNumber } from '@/lib/formatters'
import type { Vehicle } from '@/types/models'
import { VehicleFormDialog } from '../components/VehicleFormDialog'
import { useDeleteVehicle, useVehicles } from '../hooks'

export function VehiclesPage() {
  const navigate = useNavigate()
  const { page, setPage, search, setSearch, sortBy, sortDescending, toggleSort, query } = useListState()
  const { data, isLoading } = useVehicles(query)
  const remove = useDeleteVehicle()

  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Vehicle | null>(null)
  const [toDelete, setToDelete] = useState<Vehicle | null>(null)

  const columns: Column<Vehicle>[] = [
    { key: 'plateNo', header: 'Plaka', sortable: true, cell: (v) => <span className="font-medium">{v.plateNo}</span> },
    { key: 'brand', header: 'Marka', sortable: true, cell: (v) => v.brand },
    { key: 'model', header: 'Model', cell: (v) => v.model ?? '—' },
    { key: 'odometer', header: 'KM', sortable: true, cell: (v) => formatNumber(v.currentOdometerKm) },
    { key: 'nextMaintenance', header: 'Sonraki Bakım', cell: (v) => formatDate(v.nextMaintenanceDate) },
    { key: 'status', header: 'Durum', sortable: true, cell: (v) => <StatusBadge status={v.status} /> },
    {
      key: 'actions',
      header: '',
      className: 'w-20 text-right',
      cell: (v) => (
        <div className="flex justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          <Button variant="ghost" size="icon" onClick={() => { setEditing(v); setFormOpen(true) }}>
            <Pencil className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="icon" onClick={() => setToDelete(v)}>
            <Trash2 className="h-4 w-4 text-danger" />
          </Button>
        </div>
      ),
    },
  ]

  return (
    <div>
      <PageHeader
        title="Araçlar"
        description="Filo araçlarını yönetin"
        actions={
          <Button onClick={() => { setEditing(null); setFormOpen(true) }}>
            <Plus className="h-4 w-4" /> Yeni Araç
          </Button>
        }
      />

      <div className="mb-4 max-w-xs">
        <SearchInput value={search} onChange={setSearch} placeholder="Plaka veya marka ara..." />
      </div>

      <DataTable
        columns={columns}
        data={data?.items ?? []}
        rowKey={(v) => v.id}
        loading={isLoading}
        onRowClick={(v) => navigate(`/vehicles/${v.id}`)}
        page={page}
        totalCount={data?.totalCount}
        totalPages={data?.totalPages}
        onPageChange={setPage}
        sortBy={sortBy}
        sortDescending={sortDescending}
        onSortChange={toggleSort}
        emptyMessage="Araç bulunamadı."
      />

      <VehicleFormDialog open={formOpen} onOpenChange={setFormOpen} vehicle={editing} />
      <ConfirmDialog
        open={!!toDelete}
        onOpenChange={(o) => !o && setToDelete(null)}
        title="Aracı sil"
        description={`${toDelete?.plateNo} plakalı araç silinecek.`}
        loading={remove.isPending}
        onConfirm={async () => {
          if (toDelete) {
            await remove.mutateAsync(toDelete.id)
            setToDelete(null)
          }
        }}
      />
    </div>
  )
}
