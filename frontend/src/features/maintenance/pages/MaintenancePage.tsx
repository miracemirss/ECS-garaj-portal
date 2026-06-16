import { Plus } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { DataTable, type Column } from '@/components/common/DataTable'
import { PageHeader } from '@/components/common/PageHeader'
import { SearchInput } from '@/components/common/SearchInput'
import { StatusBadge } from '@/components/common/StatusBadge'
import { Button } from '@/components/ui/button'
import { useListState } from '@/hooks/useListState'
import { formatCurrency, formatDate } from '@/lib/formatters'
import type { WorkOrder } from '@/types/models'
import { WorkOrderFormDialog } from '../components/WorkOrderFormDialog'
import { useWorkOrders } from '../hooks'

export function MaintenancePage() {
  const navigate = useNavigate()
  const { page, setPage, search, setSearch, sortBy, sortDescending, toggleSort, query } = useListState()
  const { data, isLoading } = useWorkOrders(query)
  const [formOpen, setFormOpen] = useState(false)

  const columns: Column<WorkOrder>[] = [
    { key: 'workOrderNo', header: 'No', sortable: true, cell: (w) => <span className="font-medium">{w.workOrderNo ?? '—'}</span> },
    { key: 'title', header: 'Başlık', cell: (w) => w.title },
    { key: 'targetType', header: 'Hedef', cell: (w) => w.targetType },
    { key: 'maintenanceType', header: 'Tip', cell: (w) => w.maintenanceType },
    { key: 'totalCost', header: 'Toplam', cell: (w) => formatCurrency(w.totalCost) },
    { key: 'scheduledDate', header: 'Planlanan', sortable: true, cell: (w) => formatDate(w.scheduledDate) },
    { key: 'status', header: 'Durum', sortable: true, cell: (w) => <StatusBadge status={w.status} /> },
  ]

  return (
    <div>
      <PageHeader
        title="İş Emirleri"
        description="Bakım iş emirlerini yönetin"
        actions={<Button onClick={() => setFormOpen(true)}><Plus className="h-4 w-4" /> Yeni İş Emri</Button>}
      />
      <div className="mb-4 max-w-xs"><SearchInput value={search} onChange={setSearch} placeholder="No veya başlık ara..." /></div>
      <DataTable
        columns={columns}
        data={data?.items ?? []}
        rowKey={(w) => w.id}
        loading={isLoading}
        onRowClick={(w) => navigate(`/maintenance/${w.id}`)}
        page={page}
        totalCount={data?.totalCount}
        totalPages={data?.totalPages}
        onPageChange={setPage}
        sortBy={sortBy}
        sortDescending={sortDescending}
        onSortChange={toggleSort}
        emptyMessage="İş emri bulunamadı."
      />
      <WorkOrderFormDialog open={formOpen} onOpenChange={setFormOpen} />
    </div>
  )
}
