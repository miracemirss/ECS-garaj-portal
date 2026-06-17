import { useParams } from 'react-router-dom'
import { Breadcrumb } from '@/components/common/Breadcrumb'
import { PageHeader } from '@/components/common/PageHeader'
import { StatusBadge } from '@/components/common/StatusBadge'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { formatDate } from '@/lib/formatters'
import { useDriver } from '../hooks'

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-0.5 text-sm font-medium text-foreground">{children}</p>
    </div>
  )
}

export function DriverDetailPage() {
  const { id = '' } = useParams()
  const { data: driver, isLoading } = useDriver(id)

  if (isLoading) return <Skeleton className="h-64 w-full" />
  if (!driver) return <p className="text-sm text-muted-foreground">Şoför bulunamadı.</p>

  return (
    <div>
      <Breadcrumb items={[{ label: 'Şoförler', to: '/drivers' }, { label: driver.fullName }]} />
      <PageHeader title={driver.fullName} description="Şoför detayı" />
      <Card>
        <CardContent className="grid grid-cols-1 gap-5 p-6 sm:grid-cols-2 lg:grid-cols-3">
          <Field label="Ad Soyad">{driver.fullName}</Field>
          <Field label="TC Kimlik">{driver.nationalId ?? '—'}</Field>
          <Field label="Durum"><StatusBadge status={driver.status} /></Field>
          <Field label="Telefon">{driver.phone ?? '—'}</Field>
          <Field label="E-posta">{driver.email ?? '—'}</Field>
          <Field label="Ehliyet No">{driver.licenseNo ?? '—'}</Field>
          <Field label="Ehliyet Sınıfı">{driver.licenseClass ?? '—'}</Field>
          <Field label="Ehliyet Başlangıç">{formatDate(driver.licenseStartDate)}</Field>
          <Field label="Ehliyet Bitiş">{formatDate(driver.licenseExpiryDate)}</Field>
          <Field label="SRC Başlangıç">{formatDate(driver.srcStartDate)}</Field>
          <Field label="SRC Bitiş">{formatDate(driver.srcEndDate)}</Field>
          <Field label="Psikoteknik Başlangıç">{formatDate(driver.psychotechnicalStartDate)}</Field>
          <Field label="Psikoteknik Bitiş">{formatDate(driver.psychotechnicalEndDate)}</Field>
          <Field label="Vize Başlangıç">{formatDate(driver.visaStartDate)}</Field>
          <Field label="Vize Bitiş">{formatDate(driver.visaEndDate)}</Field>
          <Field label="Pasaport Başlangıç">{formatDate(driver.passportStartDate)}</Field>
          <Field label="Pasaport Bitiş">{formatDate(driver.passportEndDate)}</Field>
          <Field label="Belge Notu">{driver.documentNote ?? '—'}</Field>
        </CardContent>
      </Card>
    </div>
  )
}
