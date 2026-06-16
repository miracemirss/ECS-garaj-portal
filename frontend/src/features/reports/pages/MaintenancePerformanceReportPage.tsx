import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { Breadcrumb } from '@/components/common/Breadcrumb'
import { PageHeader } from '@/components/common/PageHeader'
import { StatCard } from '@/components/common/StatCard'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { EmptyState } from '@/components/common/EmptyState'
import { formatCurrency } from '@/lib/formatters'
import { useMonthlyCosts } from '../hooks'

export function MaintenancePerformanceReportPage() {
  const { data } = useMonthlyCosts()
  const rows = data ?? []
  const totalWorkOrders = rows.reduce((sum, r) => sum + r.workOrderCount, 0)
  const totalCost = rows.reduce((sum, r) => sum + r.totalCost, 0)
  const chartData = rows.map((r) => ({ name: `${String(r.month).padStart(2, '0')}.${r.year}`, total: r.totalCost, count: r.workOrderCount }))

  return (
    <div>
      <Breadcrumb items={[{ label: 'Raporlar', to: '/reports' }, { label: 'Bakım Performans' }]} />
      <PageHeader title="Bakım Performans Raporu" description="Aylık bakım maliyeti ve iş emri sayısı" />

      <div className="mb-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <StatCard title="Toplam Tamamlanan İş Emri" value={totalWorkOrders} />
        <StatCard title="Toplam Maliyet" value={formatCurrency(totalCost)} />
      </div>

      <Card>
        <CardHeader><CardTitle>Aylık Eğilim</CardTitle></CardHeader>
        <CardContent>
          {chartData.length === 0 ? (
            <EmptyState description="Veri yok." />
          ) : (
            <div className="h-72">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(220 13% 91%)" vertical={false} />
                  <XAxis dataKey="name" tick={{ fontSize: 12 }} stroke="hsl(220 9% 46%)" />
                  <YAxis tick={{ fontSize: 12 }} stroke="hsl(220 9% 46%)" />
                  <Tooltip formatter={(v: number) => formatCurrency(v)} />
                  <Line type="monotone" dataKey="total" stroke="hsl(216 90% 44%)" strokeWidth={2} dot={false} />
                </LineChart>
              </ResponsiveContainer>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
