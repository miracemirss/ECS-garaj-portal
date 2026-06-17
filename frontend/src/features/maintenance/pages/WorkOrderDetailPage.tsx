import { CheckCircle2, Play, Plus } from 'lucide-react'
import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { Breadcrumb } from '@/components/common/Breadcrumb'
import { PageHeader } from '@/components/common/PageHeader'
import { StatusBadge } from '@/components/common/StatusBadge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { FormInput } from '@/components/common/FormInput'
import { EmptyState } from '@/components/common/EmptyState'
import { formatCurrency } from '@/lib/formatters'
import { getEnumLabel } from '@/types/enums'
import { AddPartDialog } from '../components/AddPartDialog'
import { useCompleteWorkOrder, useStartWorkOrder, useWorkOrder } from '../hooks'

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-0.5 text-sm font-medium text-foreground">{children}</p>
    </div>
  )
}

export function WorkOrderDetailPage() {
  const { id = '' } = useParams()
  const { data: wo, isLoading } = useWorkOrder(id)
  const start = useStartWorkOrder()
  const complete = useCompleteWorkOrder()
  const [addPartOpen, setAddPartOpen] = useState(false)
  const [completeOpen, setCompleteOpen] = useState(false)
  const [odometer, setOdometer] = useState('')

  if (isLoading) return <Skeleton className="h-72 w-full" />
  if (!wo) return <p className="text-sm text-muted-foreground">İş emri bulunamadı.</p>

  const canEdit = wo.status !== 'Completed' && wo.status !== 'Cancelled'
  const canStart = wo.status === 'Open' || wo.status === 'Draft'

  return (
    <div>
      <Breadcrumb items={[{ label: 'İş Emirleri', to: '/maintenance' }, { label: wo.workOrderNo ?? 'İş Emri' }]} />
      <PageHeader
        title={wo.workOrderNo ?? 'İş Emri'}
        description={wo.title}
        actions={
          <>
            {canStart && (
              <Button variant="outline" onClick={() => start.mutate(wo.id)} disabled={start.isPending}>
                <Play className="h-4 w-4" /> Başlat
              </Button>
            )}
            {canEdit && (
              <Button variant="outline" onClick={() => setAddPartOpen(true)}>
                <Plus className="h-4 w-4" /> Parça Ekle
              </Button>
            )}
            {canEdit && (
              <Button onClick={() => setCompleteOpen(true)}>
                <CheckCircle2 className="h-4 w-4" /> Tamamla
              </Button>
            )}
          </>
        }
      />

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-1">
          <CardContent className="grid grid-cols-2 gap-4 p-6">
            <Field label="Durum"><StatusBadge status={wo.status} /></Field>
            <Field label="Tip">{getEnumLabel(wo.maintenanceType)}</Field>
            <Field label="Hedef">{getEnumLabel(wo.targetType)}</Field>
            <Field label="KM (giriş)">{wo.odometerBeforeKm ?? '—'}</Field>
            <Field label="KM (çıkış)">{wo.odometerAfterKm ?? '—'}</Field>
            <Field label="İşçilik">{formatCurrency(wo.laborCost)}</Field>
            <Field label="Parça">{formatCurrency(wo.partsCost)}</Field>
            <Field label="Toplam">{formatCurrency(wo.totalCost)}</Field>
          </CardContent>
        </Card>

        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Kullanılan Parçalar</CardTitle>
          </CardHeader>
          <CardContent>
            {wo.parts.length === 0 ? (
              <EmptyState description="Henüz parça eklenmedi." />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Parça</TableHead>
                    <TableHead>Miktar</TableHead>
                    <TableHead>Birim Maliyet</TableHead>
                    <TableHead>Tutar</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {wo.parts.map((p) => (
                    <TableRow key={p.id}>
                      <TableCell className="font-mono text-xs">{p.partId.slice(0, 8)}</TableCell>
                      <TableCell>{p.quantity}</TableCell>
                      <TableCell>{formatCurrency(p.unitCost)}</TableCell>
                      <TableCell>{formatCurrency(p.lineTotal)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </div>

      <AddPartDialog workOrderId={wo.id} open={addPartOpen} onOpenChange={setAddPartOpen} />

      <Dialog open={completeOpen} onOpenChange={setCompleteOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>İş Emrini Tamamla</DialogTitle>
          </DialogHeader>
          <FormInput label="KM (çıkış)" inputMode="numeric" value={odometer} onChange={(e) => setOdometer(e.target.value)} />
          <DialogFooter>
            <Button variant="outline" onClick={() => setCompleteOpen(false)} disabled={complete.isPending}>Vazgeç</Button>
            <Button
              onClick={async () => {
                await complete.mutateAsync({ id: wo.id, body: { odometerAfterKm: odometer ? Number(odometer) : undefined } })
                setCompleteOpen(false)
              }}
              disabled={complete.isPending}
            >
              {complete.isPending ? 'Tamamlanıyor...' : 'Tamamla'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
