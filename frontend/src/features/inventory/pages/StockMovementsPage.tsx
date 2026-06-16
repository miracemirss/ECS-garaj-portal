import { Minus, Plus, Scale } from 'lucide-react'
import { useState } from 'react'
import { DataTable, type Column } from '@/components/common/DataTable'
import { PageHeader } from '@/components/common/PageHeader'
import { SearchInput } from '@/components/common/SearchInput'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { useListState } from '@/hooks/useListState'
import { formatNumber } from '@/lib/formatters'
import type { Part } from '@/types/models'
import { StockActionDialog, type StockMode } from '../components/StockActionDialog'
import { useParts } from '../hooks'

export function StockMovementsPage() {
  const { page, setPage, search, setSearch, query } = useListState()
  const { data, isLoading } = useParts(query)
  const [stockPart, setStockPart] = useState<Part | null>(null)
  const [stockMode, setStockMode] = useState<StockMode>('receive')

  function openStock(part: Part, mode: StockMode) {
    setStockPart(part)
    setStockMode(mode)
  }

  const columns: Column<Part>[] = [
    { key: 'partNo', header: 'Kod', cell: (p) => <span className="font-medium">{p.partNo}</span> },
    { key: 'name', header: 'Parça', cell: (p) => p.name },
    {
      key: 'quantity', header: 'Mevcut Stok',
      cell: (p) => (
        <span className="flex items-center gap-2">
          {formatNumber(p.quantityInStock)} {p.unit}
          {p.isBelowMinimum && <Badge variant="danger">Kritik</Badge>}
        </span>
      ),
    },
    {
      key: 'actions', header: 'Hareket', className: 'w-56',
      cell: (p) => (
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => openStock(p, 'receive')}><Plus className="h-3.5 w-3.5 text-success" /> Giriş</Button>
          <Button variant="outline" size="sm" onClick={() => openStock(p, 'issue')}><Minus className="h-3.5 w-3.5 text-danger" /> Çıkış</Button>
          <Button variant="outline" size="sm" onClick={() => openStock(p, 'adjust')}><Scale className="h-3.5 w-3.5" /> Sayım</Button>
        </div>
      ),
    },
  ]

  return (
    <div>
      <PageHeader title="Stok Hareketleri" description="Parça seçip stok giriş/çıkış/sayım işlemi yapın" />
      <div className="mb-4 max-w-xs"><SearchInput value={search} onChange={setSearch} placeholder="Parça ara..." /></div>
      <DataTable
        columns={columns}
        data={data?.items ?? []}
        rowKey={(p) => p.id}
        loading={isLoading}
        page={page}
        totalCount={data?.totalCount}
        totalPages={data?.totalPages}
        onPageChange={setPage}
        emptyMessage="Parça bulunamadı."
      />
      <StockActionDialog open={!!stockPart} onOpenChange={(o) => !o && setStockPart(null)} mode={stockMode} part={stockPart} />
    </div>
  )
}
