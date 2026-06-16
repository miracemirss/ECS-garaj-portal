import { useEffect, useState } from 'react'
import { FormInput } from '@/components/common/FormInput'
import { FormTextarea } from '@/components/common/FormTextarea'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import type { Part } from '@/types/models'
import { useAdjustStock, useIssueStock, useReceiveStock } from '../hooks'

export type StockMode = 'receive' | 'issue' | 'adjust'

const TITLES: Record<StockMode, string> = {
  receive: 'Stok Girişi',
  issue: 'Stok Çıkışı',
  adjust: 'Sayım Düzeltmesi',
}

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  mode: StockMode
  part: Part | null
}

export function StockActionDialog({ open, onOpenChange, mode, part }: Props) {
  const receive = useReceiveStock()
  const issue = useIssueStock()
  const adjust = useAdjustStock()
  const [quantity, setQuantity] = useState('')
  const [unitCost, setUnitCost] = useState('')
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (open) {
      setQuantity('')
      setUnitCost(part?.unitCost != null ? String(part.unitCost) : '')
      setNote('')
      setError(null)
    }
  }, [open, part])

  const pending = receive.isPending || issue.isPending || adjust.isPending

  async function submit() {
    if (!part) return
    const qty = Number(quantity)
    if (Number.isNaN(qty) || qty === 0) {
      setError('Geçerli bir miktar girin.')
      return
    }
    if (mode !== 'adjust' && qty <= 0) {
      setError('Miktar pozitif olmalıdır.')
      return
    }
    try {
      if (mode === 'receive') await receive.mutateAsync({ partId: part.id, quantity: qty, unitCost: Number(unitCost) || 0, note: note || undefined })
      else if (mode === 'issue') await issue.mutateAsync({ partId: part.id, quantity: qty, note: note || undefined })
      else await adjust.mutateAsync({ partId: part.id, signedQuantity: qty, note: note || undefined })
      onOpenChange(false)
    } catch {
      /* toast handled */
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{TITLES[mode]}{part ? ` — ${part.name}` : ''}</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <FormInput
            label={mode === 'adjust' ? 'Miktar (+/-)' : 'Miktar'}
            inputMode="decimal"
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            error={error ?? undefined}
          />
          {mode === 'receive' && (
            <FormInput label="Birim Maliyet" inputMode="decimal" value={unitCost} onChange={(e) => setUnitCost(e.target.value)} />
          )}
          <FormTextarea label="Not" value={note} onChange={(e) => setNote(e.target.value)} />
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>Vazgeç</Button>
          <Button onClick={submit} disabled={pending}>{pending ? 'İşleniyor...' : 'Onayla'}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
