import { PageHeader } from '@/components/common/PageHeader'
import { StatusBadge } from '@/components/common/StatusBadge'
import { EmptyState } from '@/components/common/EmptyState'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useOpenAlerts } from '@/features/alerts/hooks'
import { useCriticalStocks } from '@/features/inventory/hooks'
import { useWorkOrders } from '@/features/maintenance/hooks'
import { formatNumber } from '@/lib/formatters'

export function OperationsPage() {
  const workOrders = useWorkOrders({ page: 1, pageSize: 8 })
  const criticalStocks = useCriticalStocks()
  const openAlerts = useOpenAlerts()

  return (
    <div>
      <PageHeader title="Operasyon" description="Günlük operasyon görünümü" />
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Card>
          <CardHeader><CardTitle>Son İş Emirleri</CardTitle></CardHeader>
          <CardContent className="space-y-2">
            {(workOrders.data?.items ?? []).length === 0 ? (
              <EmptyState description="İş emri yok." />
            ) : (
              (workOrders.data?.items ?? []).map((w) => (
                <div key={w.id} className="flex items-center justify-between border-b pb-2 last:border-0">
                  <span className="text-sm">{w.workOrderNo ?? w.title}</span>
                  <StatusBadge status={w.status} />
                </div>
              ))
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>Kritik Stok</CardTitle></CardHeader>
          <CardContent className="space-y-2">
            {(criticalStocks.data ?? []).length === 0 ? (
              <EmptyState description="Kritik stok yok." />
            ) : (
              (criticalStocks.data ?? []).map((p) => (
                <div key={p.id} className="flex items-center justify-between border-b pb-2 last:border-0">
                  <span className="text-sm">{p.name}</span>
                  <span className="text-sm text-danger">{formatNumber(p.quantityInStock)} {p.unit}</span>
                </div>
              ))
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>Açık Uyarılar</CardTitle></CardHeader>
          <CardContent className="space-y-2">
            {(openAlerts.data ?? []).length === 0 ? (
              <EmptyState description="Uyarı yok." />
            ) : (
              (openAlerts.data ?? []).map((a) => (
                <div key={a.id} className="flex items-center justify-between border-b pb-2 last:border-0">
                  <span className="text-sm">{a.title}</span>
                  <StatusBadge status={a.priority} />
                </div>
              ))
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
