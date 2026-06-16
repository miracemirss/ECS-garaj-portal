import { useParams } from 'react-router-dom'
import { Breadcrumb } from '@/components/common/Breadcrumb'
import { PageHeader } from '@/components/common/PageHeader'
import { StatusBadge } from '@/components/common/StatusBadge'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { formatNumber } from '@/lib/formatters'
import { useTrailer } from '../hooks'

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-0.5 text-sm font-medium text-foreground">{children}</p>
    </div>
  )
}

export function TrailerDetailPage() {
  const { id = '' } = useParams()
  const { data: trailer, isLoading } = useTrailer(id)

  if (isLoading) return <Skeleton className="h-64 w-full" />
  if (!trailer) return <p className="text-sm text-muted-foreground">Dorse bulunamadı.</p>

  return (
    <div>
      <Breadcrumb items={[{ label: 'Dorseler', to: '/trailers' }, { label: trailer.plateNo }]} />
      <PageHeader title={trailer.plateNo} description={trailer.trailerType ?? 'Dorse'} />
      <Card>
        <CardContent className="grid grid-cols-2 gap-5 p-6 sm:grid-cols-3">
          <Field label="Plaka">{trailer.plateNo}</Field>
          <Field label="Şasi No">{trailer.vin ?? '—'}</Field>
          <Field label="Durum"><StatusBadge status={trailer.status} /></Field>
          <Field label="Tip">{trailer.trailerType ?? '—'}</Field>
          <Field label="Marka">{trailer.brand ?? '—'}</Field>
          <Field label="Kapasite">{trailer.capacityKg != null ? `${formatNumber(trailer.capacityKg)} kg` : '—'}</Field>
          <Field label="Lastik Ömrü">{trailer.tireConditionPercent != null ? `%${trailer.tireConditionPercent}` : '—'}</Field>
        </CardContent>
      </Card>
    </div>
  )
}
