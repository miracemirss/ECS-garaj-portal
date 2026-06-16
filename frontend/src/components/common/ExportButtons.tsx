import { FileSpreadsheet, FileText } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface ExportButtonsProps {
  onExportPdf?: () => void
  onExportExcel?: () => void
}

export function ExportButtons({ onExportPdf, onExportExcel }: ExportButtonsProps) {
  if (!onExportPdf && !onExportExcel) return null
  return (
    <div className="flex gap-2">
      {onExportExcel && (
        <Button variant="outline" size="sm" onClick={onExportExcel}>
          <FileSpreadsheet className="h-4 w-4" /> Excel
        </Button>
      )}
      {onExportPdf && (
        <Button variant="outline" size="sm" onClick={onExportPdf}>
          <FileText className="h-4 w-4" /> PDF
        </Button>
      )}
    </div>
  )
}
