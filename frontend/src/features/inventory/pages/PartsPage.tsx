import { Minus, Pencil, Plus, Scale } from 'lucide-react'
import { useState } from 'react'
import { DataTable, type Column } from '@/components/common/DataTable'
import { PageHeader } from '@/components/common/PageHeader'
import { SearchInput } from '@/components/common/SearchInput'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { useListState } from '@/hooks/useListState'
import { formatCurrency, formatNumber } from '@/lib/formatters'
import type { Part } from '@/types/models'
import { PartFormDialog } from '../components/PartFormDialog'
import { StockActionDialog, type StockMode } from '../components/StockActionDialog'
import { useParts } from '../hooks'

export function PartsPage() {
  const { page, setPage, search, setSearch, sortBy, sortDescending, toggleSort, query } = useListState()
  const { data, isLoading } = useParts(query)
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Part | null>(null)
  const [stockPart, setStockPart] = useState<Part | null>(null)
  const [stockMode, setStockMode] = useState<StockMode>('receive')

  function openStock(part: Part, mode: StockMode) {
    setStockPart(part)
    setStockMode(mode)
  }

  const columns: Column<Part>[] = [
    { key: 'partNo', header: 'Kod', sortable: true, cell: (p) => <span className="font-medium">{p.partNo}</span> },
    { key: 'name', header: 'Ad', sortable: true, cell: (p) => p.name },
    {
      key: 'quantity', header: 'Stok', sortable: true,
      cell: (p) => (
        <span className="flex items-center gap-2">
          {formatNumber(p.quantityInStock)} {p.unit}
          {p.isBelowMinimum && <Badge variant="danger">Kritik</Badge>}
        </span>
      ),
    },
    { key: 'minimumStock', header: 'Min.', cell: (p) => formatNumber(p.minimumStock) },
    { key: 'unitCost', header: 'Birim Maliyet', cell: (p) => formatCurrency(p.unitCost) },
    {
      key: 'actions', header: '', className: 'w-44 text-right',
      cell: (p) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" size="icon" title="Giriş" onClick={() => openStock(p, 'receive')}><Plus className="h-4 w-4 text-success" /></Button>
          <Button variant="ghost" size="icon" title="Çıkış" onClick={() => openStock(p, 'issue')}><Minus className="h-4 w-4 text-danger" /></Button>
          <Button variant="ghost" size="icon" title="Sayım" onClick={() => openStock(p, 'adjust')}><Scale className="h-4 w-4" /></Button>
          <Button variant="ghost" size="icon" title="Düzenle" onClick={() => { setEditing(p); setFormOpen(true) }}><Pencil className="h-4 w-4" /></Button>
        </div>
      ),
    },
  ]

  return (
    <div>
      <PageHeader title="Parçalar" description="Stok kalemlerini yönetin" actions={<Button onClick={() => { setEditing(null); setFormOpen(true) }}><Plus className="h-4 w-4" /> Yeni Parça</Button>} />
      <div className="mb-4 max-w-xs"><SearchInput value={search} onChange={setSearch} placeholder="Kod veya ad ara..." /></div>
      <DataTable
        columns={columns}
        data={data?.items ?? []}
        rowKey={(p) => p.id}
        loading={isLoading}
        page={page}
        totalCount={data?.totalCount}
        totalPages={data?.totalPages}
        onPageChange={setPage}
        sortBy={sortBy}
        sortDescending={sortDescending}
        onSortChange={toggleSort}
        emptyMessage="Parça bulunamadı."
      />
      <PartFormDialog open={formOpen} onOpenChange={setFormOpen} part={editing} />
      <StockActionDialog open={!!stockPart} onOpenChange={(o) => !o && setStockPart(null)} mode={stockMode} part={stockPart} />
    </div>
  )
}
