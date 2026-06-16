import { Breadcrumb } from '@/components/common/Breadcrumb'
import { DataTable, type Column } from '@/components/common/DataTable'
import { PageHeader } from '@/components/common/PageHeader'
import { Badge } from '@/components/ui/badge'
import { useCriticalStocks } from '@/features/inventory/hooks'
import { formatCurrency, formatNumber } from '@/lib/formatters'
import type { Part } from '@/types/models'

export function PartConsumptionReportPage() {
  const { data, isLoading } = useCriticalStocks()

  const columns: Column<Part>[] = [
    { key: 'partNo', header: 'Kod', cell: (p) => <span className="font-medium">{p.partNo}</span> },
    { key: 'name', header: 'Parça', cell: (p) => p.name },
    { key: 'stock', header: 'Stok', cell: (p) => `${formatNumber(p.quantityInStock)} ${p.unit}` },
    { key: 'min', header: 'Min.', cell: (p) => formatNumber(p.minimumStock) },
    { key: 'cost', header: 'Birim Maliyet', cell: (p) => formatCurrency(p.unitCost) },
    { key: 'flag', header: '', cell: () => <Badge variant="danger">Kritik</Badge> },
  ]

  return (
    <div>
      <Breadcrumb items={[{ label: 'Raporlar', to: '/reports' }, { label: 'Parça Tüketim' }]} />
      <PageHeader title="Parça Tüketim Raporu" description="Minimum seviyenin altındaki parçalar (kritik stok)" />
      <DataTable columns={columns} data={data ?? []} rowKey={(p) => p.id} loading={isLoading} emptyMessage="Kritik stok yok." />
    </div>
  )
}
