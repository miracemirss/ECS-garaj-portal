import { toast } from 'sonner'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { Breadcrumb } from '@/components/common/Breadcrumb'
import { DataTable, type Column } from '@/components/common/DataTable'
import { ExportButtons } from '@/components/common/ExportButtons'
import { PageHeader } from '@/components/common/PageHeader'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { EmptyState } from '@/components/common/EmptyState'
import { formatCurrency } from '@/lib/formatters'
import type { VehicleCostSummary } from '@/types/models'
import { useVehicleCosts } from '../hooks'

export function VehicleCostReportPage() {
  const { data, isLoading } = useVehicleCosts()
  const rows = data ?? []
  const chartData = rows.slice(0, 10).map((r) => ({ name: r.plateNo, total: r.totalCost }))

  const columns: Column<VehicleCostSummary>[] = [
    { key: 'plateNo', header: 'Plaka', cell: (r) => <span className="font-medium">{r.plateNo}</span> },
    { key: 'count', header: 'İş Emri', cell: (r) => r.workOrderCount },
    { key: 'total', header: 'Toplam Maliyet', cell: (r) => formatCurrency(r.totalCost) },
  ]

  return (
    <div>
      <Breadcrumb items={[{ label: 'Raporlar', to: '/reports' }, { label: 'Araç Maliyet' }]} />
      <PageHeader
        title="Araç Maliyet Raporu"
        description="Tamamlanmış iş emirlerine göre araç maliyetleri"
        actions={<ExportButtons onExportExcel={() => toast.message('Excel dışa aktarma yakında')} onExportPdf={() => toast.message('PDF dışa aktarma yakında')} />}
      />

      <Card className="mb-4">
        <CardHeader><CardTitle>En Yüksek Maliyetli Araçlar</CardTitle></CardHeader>
        <CardContent>
          {chartData.length === 0 ? (
            <EmptyState description="Veri yok." />
          ) : (
            <div className="h-72">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(220 13% 91%)" vertical={false} />
                  <XAxis dataKey="name" tick={{ fontSize: 12 }} stroke="hsl(220 9% 46%)" />
                  <YAxis tick={{ fontSize: 12 }} stroke="hsl(220 9% 46%)" />
                  <Tooltip formatter={(v: number) => formatCurrency(v)} />
                  <Bar dataKey="total" fill="hsl(216 90% 44%)" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}
        </CardContent>
      </Card>

      <DataTable columns={columns} data={rows} rowKey={(r) => r.vehicleId} loading={isLoading} emptyMessage="Maliyet verisi yok." />
    </div>
  )
}
