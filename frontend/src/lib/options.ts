import type { SelectOption } from '@/components/common/FormSelect'

export const PART_UNIT_OPTIONS: SelectOption[] = [
  { value: 'Adet', label: 'Adet' },
  { value: 'Litre', label: 'Litre' },
  { value: 'Kilogram', label: 'Kilogram' },
  { value: 'Metre', label: 'Metre' },
  { value: 'Takim', label: 'Takım' },
  { value: 'Kutu', label: 'Kutu' },
  { value: 'Paket', label: 'Paket' },
  { value: 'Cift', label: 'Çift' },
]

export const TRAILER_TYPE_OPTIONS: SelectOption[] = [
  { value: 'Tenteli Dorse', label: 'Tenteli Dorse' },
  { value: 'Frigorifik Dorse', label: 'Frigorifik Dorse' },
  { value: 'Lowbed', label: 'Lowbed' },
  { value: 'Platform Dorse', label: 'Platform Dorse' },
  { value: 'Konteyner Tasiyici', label: 'Konteyner Taşıyıcı' },
  { value: 'Silobas', label: 'Silobas' },
  { value: 'Tanker', label: 'Tanker' },
  { value: 'Kapali Kasa', label: 'Kapalı Kasa' },
  { value: 'Sal Dorse', label: 'Sal Dorse' },
  { value: 'Mega Dorse', label: 'Mega Dorse' },
]
