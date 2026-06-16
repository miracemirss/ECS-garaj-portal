import { Check, RefreshCw, X } from 'lucide-react'
import { DataTable, type Column } from '@/components/common/DataTable'
import { PageHeader } from '@/components/common/PageHeader'
import { StatusBadge } from '@/components/common/StatusBadge'
import { Button } from '@/components/ui/button'
import { formatDateTime } from '@/lib/formatters'
import type { Alert } from '@/types/models'
import { useAlertActions, useOpenAlerts } from '../hooks'

export function AlertsPage() {
  const { data, isLoading } = useOpenAlerts()
  const actions = useAlertActions()

  const columns: Column<Alert>[] = [
    { key: 'type', header: 'Tip', cell: (a) => <StatusBadge status={a.alertType} /> },
    { key: 'priority', header: 'Öncelik', cell: (a) => <StatusBadge status={a.priority} /> },
    { key: 'title', header: 'Başlık', cell: (a) => <span className="font-medium">{a.title}</span> },
    { key: 'message', header: 'Mesaj', cell: (a) => <span className="text-muted-foreground">{a.message}</span> },
    { key: 'createdAt', header: 'Tarih', cell: (a) => formatDateTime(a.createdAt) },
    {
      key: 'actions', header: '', className: 'w-32 text-right',
      cell: (a) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" size="icon" title="Okundu" onClick={() => actions.acknowledge.mutate(a.id)}><Check className="h-4 w-4" /></Button>
          <Button variant="ghost" size="icon" title="Çöz" onClick={() => actions.resolve.mutate(a.id)}><Check className="h-4 w-4 text-success" /></Button>
          <Button variant="ghost" size="icon" title="Kapat" onClick={() => actions.dismiss.mutate(a.id)}><X className="h-4 w-4 text-danger" /></Button>
        </div>
      ),
    },
  ]

  return (
    <div>
      <PageHeader
        title="Uyarılar"
        description="Açık sistem uyarıları"
        actions={
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={() => actions.generateCriticalStock.mutate()}>
              <RefreshCw className="h-4 w-4" /> Kritik Stok
            </Button>
            <Button variant="outline" size="sm" onClick={() => actions.generateMaintenanceDue.mutate()}>
              <RefreshCw className="h-4 w-4" /> Bakım
            </Button>
            <Button variant="outline" size="sm" onClick={() => actions.generateDocumentExpiry.mutate()}>
              <RefreshCw className="h-4 w-4" /> Belge
            </Button>
          </div>
        }
      />
      <DataTable columns={columns} data={data ?? []} rowKey={(a) => a.id} loading={isLoading} emptyMessage="Açık uyarı yok." />
    </div>
  )
}
