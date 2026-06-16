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
import { formatNumber } from '@/lib/formatters'
import type { Trailer } from '@/types/models'
import { TrailerFormDialog } from '../components/TrailerFormDialog'
import { useDeleteTrailer, useTrailers } from '../hooks'

export function TrailersPage() {
  const navigate = useNavigate()
  const { page, setPage, search, setSearch, sortBy, sortDescending, toggleSort, query } = useListState()
  const { data, isLoading } = useTrailers(query)
  const remove = useDeleteTrailer()
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Trailer | null>(null)
  const [toDelete, setToDelete] = useState<Trailer | null>(null)

  const columns: Column<Trailer>[] = [
    { key: 'plateNo', header: 'Plaka', sortable: true, cell: (t) => <span className="font-medium">{t.plateNo}</span> },
    { key: 'trailerType', header: 'Tip', sortable: true, cell: (t) => t.trailerType ?? '—' },
    { key: 'capacity', header: 'Kapasite', cell: (t) => (t.capacityKg != null ? `${formatNumber(t.capacityKg)} kg` : '—') },
    { key: 'tire', header: 'Lastik %', cell: (t) => (t.tireConditionPercent != null ? `%${t.tireConditionPercent}` : '—') },
    { key: 'status', header: 'Durum', sortable: true, cell: (t) => <StatusBadge status={t.status} /> },
    {
      key: 'actions', header: '', className: 'w-20 text-right',
      cell: (t) => (
        <div className="flex justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          <Button variant="ghost" size="icon" onClick={() => { setEditing(t); setFormOpen(true) }}><Pencil className="h-4 w-4" /></Button>
          <Button variant="ghost" size="icon" onClick={() => setToDelete(t)}><Trash2 className="h-4 w-4 text-danger" /></Button>
        </div>
      ),
    },
  ]

  return (
    <div>
      <PageHeader title="Dorseler" description="Dorseleri yönetin" actions={<Button onClick={() => { setEditing(null); setFormOpen(true) }}><Plus className="h-4 w-4" /> Yeni Dorse</Button>} />
      <div className="mb-4 max-w-xs"><SearchInput value={search} onChange={setSearch} placeholder="Plaka veya tip ara..." /></div>
      <DataTable
        columns={columns}
        data={data?.items ?? []}
        rowKey={(t) => t.id}
        loading={isLoading}
        onRowClick={(t) => navigate(`/trailers/${t.id}`)}
        page={page}
        totalCount={data?.totalCount}
        totalPages={data?.totalPages}
        onPageChange={setPage}
        sortBy={sortBy}
        sortDescending={sortDescending}
        onSortChange={toggleSort}
        emptyMessage="Dorse bulunamadı."
      />
      <TrailerFormDialog open={formOpen} onOpenChange={setFormOpen} trailer={editing} />
      <ConfirmDialog
        open={!!toDelete}
        onOpenChange={(o) => !o && setToDelete(null)}
        title="Dorseyi sil"
        description={`${toDelete?.plateNo} plakalı dorse silinecek.`}
        loading={remove.isPending}
        onConfirm={async () => { if (toDelete) { await remove.mutateAsync(toDelete.id); setToDelete(null) } }}
      />
    </div>
  )
}
