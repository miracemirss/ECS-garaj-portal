import { BarChart3, PackageX, TrendingUp } from 'lucide-react'
import { Link } from 'react-router-dom'
import { PageHeader } from '@/components/common/PageHeader'
import { Card, CardContent } from '@/components/ui/card'

const REPORTS = [
  { to: '/reports/vehicle-costs', title: 'Araç Maliyet Raporu', description: 'Araç bazında bakım maliyetleri', icon: BarChart3 },
  { to: '/reports/part-consumption', title: 'Parça Tüketim Raporu', description: 'Stok ve kritik parça durumu', icon: PackageX },
  { to: '/reports/maintenance-performance', title: 'Bakım Performans Raporu', description: 'Aylık bakım eğilimi', icon: TrendingUp },
]

export function ReportsPage() {
  return (
    <div>
      <PageHeader title="Raporlar" description="Operasyonel raporlar" />
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {REPORTS.map((r) => (
          <Link key={r.to} to={r.to}>
            <Card className="transition-colors hover:border-primary/40">
              <CardContent className="flex items-center gap-4 p-5">
                <div className="rounded-lg bg-primary/10 p-3 text-primary">
                  <r.icon className="h-5 w-5" />
                </div>
                <div>
                  <p className="font-medium text-foreground">{r.title}</p>
                  <p className="text-sm text-muted-foreground">{r.description}</p>
                </div>
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  )
}
