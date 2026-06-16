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
import { formatDate } from '@/lib/formatters'
import type { Driver } from '@/types/models'
import { DriverFormDialog } from '../components/DriverFormDialog'
import { useDeleteDriver, useDrivers } from '../hooks'

export function DriversPage() {
  const navigate = useNavigate()
  const { page, setPage, search, setSearch, sortBy, sortDescending, toggleSort, query } = useListState()
  const { data, isLoading } = useDrivers(query)
  const remove = useDeleteDriver()
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Driver | null>(null)
  const [toDelete, setToDelete] = useState<Driver | null>(null)

  const columns: Column<Driver>[] = [
    { key: 'lastName', header: 'Ad Soyad', sortable: true, cell: (d) => <span className="font-medium">{d.fullName}</span> },
    { key: 'nationalId', header: 'TC Kimlik', cell: (d) => d.nationalId ?? '—' },
    { key: 'phone', header: 'Telefon', cell: (d) => d.phone ?? '—' },
    { key: 'licenseExpiry', header: 'Ehliyet Bitiş', cell: (d) => formatDate(d.licenseExpiryDate) },
    { key: 'status', header: 'Durum', sortable: true, cell: (d) => <StatusBadge status={d.status} /> },
    {
      key: 'actions', header: '', className: 'w-20 text-right',
      cell: (d) => (
        <div className="flex justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          <Button variant="ghost" size="icon" onClick={() => { setEditing(d); setFormOpen(true) }}><Pencil className="h-4 w-4" /></Button>
          <Button variant="ghost" size="icon" onClick={() => setToDelete(d)}><Trash2 className="h-4 w-4 text-danger" /></Button>
        </div>
      ),
    },
  ]

  return (
    <div>
      <PageHeader title="Şoförler" description="Şoförleri yönetin" actions={<Button onClick={() => { setEditing(null); setFormOpen(true) }}><Plus className="h-4 w-4" /> Yeni Şoför</Button>} />
      <div className="mb-4 max-w-xs"><SearchInput value={search} onChange={setSearch} placeholder="Ad veya soyad ara..." /></div>
      <DataTable
        columns={columns}
        data={data?.items ?? []}
        rowKey={(d) => d.id}
        loading={isLoading}
        onRowClick={(d) => navigate(`/drivers/${d.id}`)}
        page={page}
        totalCount={data?.totalCount}
        totalPages={data?.totalPages}
        onPageChange={setPage}
        sortBy={sortBy}
        sortDescending={sortDescending}
        onSortChange={toggleSort}
        emptyMessage="Şoför bulunamadı."
      />
      <DriverFormDialog open={formOpen} onOpenChange={setFormOpen} driver={editing} />
      <ConfirmDialog
        open={!!toDelete}
        onOpenChange={(o) => !o && setToDelete(null)}
        title="Şoförü sil"
        description={`${toDelete?.fullName} silinecek.`}
        loading={remove.isPending}
        onConfirm={async () => { if (toDelete) { await remove.mutateAsync(toDelete.id); setToDelete(null) } }}
      />
    </div>
  )
}
