import { AlertTriangle, Bell, PackageX, Truck, Users, Container } from 'lucide-react'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { PageHeader } from '@/components/common/PageHeader'
import { StatCard } from '@/components/common/StatCard'
import { StatusBadge } from '@/components/common/StatusBadge'
import { EmptyState } from '@/components/common/EmptyState'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { formatCurrency } from '@/lib/formatters'
import { useDashboard } from '../hooks'

export function DashboardPage() {
  const { vehicles, trailers, drivers, criticalStocks, openAlerts, monthlyCosts } = useDashboard()

  const chartData = (monthlyCosts.data ?? []).map((m) => ({
    name: `${String(m.month).padStart(2, '0')}.${m.year}`,
    total: m.totalCost,
  }))

  return (
    <div>
      <PageHeader title="Dashboard" description="Filo ve operasyon özeti" />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <StatCard title="Araçlar" value={vehicles.data?.totalCount ?? '—'} icon={Truck} />
        <StatCard title="Dorseler" value={trailers.data?.totalCount ?? '—'} icon={Container} />
        <StatCard title="Şoförler" value={drivers.data?.totalCount ?? '—'} icon={Users} />
        <StatCard title="Kritik Stok" value={criticalStocks.data?.length ?? '—'} icon={PackageX} tone="danger" />
        <StatCard title="Açık Uyarı" value={openAlerts.data?.length ?? '—'} icon={Bell} tone="warning" />
        <StatCard title="Toplam Uyarı" value={openAlerts.data?.length ?? '—'} icon={AlertTriangle} tone="info" />
      </div>

      <div className="mt-6 grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Aylık Bakım Maliyeti</CardTitle>
          </CardHeader>
          <CardContent>
            {chartData.length === 0 ? (
              <EmptyState description="Henüz tamamlanmış iş emri yok." />
            ) : (
              <div className="h-72">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={chartData} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
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

        <Card>
          <CardHeader>
            <CardTitle>Açık Uyarılar</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {(openAlerts.data ?? []).length === 0 ? (
              <EmptyState description="Açık uyarı yok." />
            ) : (
              (openAlerts.data ?? []).slice(0, 6).map((alert) => (
                <div key={alert.id} className="flex items-start justify-between gap-2 border-b pb-2 last:border-0">
                  <div>
                    <p className="text-sm font-medium text-foreground">{alert.title}</p>
                    <p className="text-xs text-muted-foreground">{alert.message}</p>
                  </div>
                  <StatusBadge status={alert.priority} />
                </div>
              ))
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
