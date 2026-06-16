import { format, parseISO } from 'date-fns'

function toDate(value: string | Date): Date {
  return typeof value === 'string' ? parseISO(value) : value
}

export function formatDate(value?: string | Date | null): string {
  if (!value) return '—'
  try {
    return format(toDate(value), 'dd.MM.yyyy')
  } catch {
    return '—'
  }
}

export function formatDateTime(value?: string | Date | null): string {
  if (!value) return '—'
  try {
    return format(toDate(value), 'dd.MM.yyyy HH:mm')
  } catch {
    return '—'
  }
}

export function formatCurrency(value?: number | null, currency = 'TRY'): string {
  if (value == null) return '—'
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency }).format(value)
}

export function formatNumber(value?: number | null): string {
  if (value == null) return '—'
  return new Intl.NumberFormat('tr-TR').format(value)
}
