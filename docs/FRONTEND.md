# ECS Fleet — Frontend Mimarisi (PROMPT 6)

React + TypeScript + Vite uygulaması. **Beyaz tema, sol sidebar, lacivert/mavi
vurgu**, kart + tablo odaklı operasyon arayüzü. Üretim derlemesi doğrulanmıştır
(`npm run build` → 0 hata).

## Stack
React 18, TypeScript, Vite, Tailwind CSS, shadcn tarzı UI, React Router v6,
TanStack React Query, Axios, Zustand, React Hook Form + Zod, Recharts, date-fns, sonner.

## Dosya yapısı
```
frontend/src/
├── app/                  # providers.tsx (QueryClient + Toaster), router.tsx, ProtectedRoute.tsx
├── components/
│   ├── ui/               # shadcn tarzı primitive'ler (button, card, input, table, dialog, badge ...)
│   ├── common/           # AppLayout yardımcıları: DataTable, PageHeader, StatCard, StatusBadge,
│   │                     #   ConfirmDialog, EmptyState, SearchInput, Form*, DateRangePicker,
│   │                     #   ExportButtons, DetailDrawer, FilterPanel, Breadcrumb
│   └── layout/           # AppLayout, Sidebar, Topbar
├── features/<feature>/   # dashboard, vehicles, trailers, drivers, assignments, maintenance,
│   ├── api.ts            #   inventory, reports, alerts, operations, settings, auth
│   ├── hooks.ts          #   React Query query/mutation hook'ları
│   ├── components/       #   feature'a özel bileşenler (form dialog'ları vb.)
│   └── pages/            #   route'a bağlanan sayfalar
├── hooks/                # global: useListState (page/search/sort)
├── lib/                  # utils (cn), formatters (date-fns), constants (queryKeys), env, queryClient
├── services/api/         # client.ts (axios + interceptor + refresh), http.ts (zarf çözen tipli helper'lar)
├── types/                # api.ts (ApiResponse/PagedResponse), enums.ts, models.ts (backend DTO yansıması)
└── styles/globals.css    # tasarım token'ları (CSS değişkenleri) + Tailwind katmanları
```

## Route yapısı
`/login` (public) → diğer her şey `ProtectedRoute` + `AppLayout` altında:
`/dashboard`, `/vehicles` + `/vehicles/:id`, `/trailers` + `/:id`, `/drivers` + `/:id`,
`/vehicle-trailer-assignments`, `/driver-vehicle-assignments`, `/maintenance` + `/maintenance/:id`,
`/parts`, `/stock-movements`, `/operations`, `/reports` (+ `/vehicle-costs`,
`/part-consumption`, `/maintenance-performance`), `/alerts`, `/settings`, `*` (404).

## Layout
Sabit sol **Sidebar** (logo + gruplu navigasyon, aktif link mavi vurgulu) + üst **Topbar**
(çıkış) + kaydırılabilir içerik. Tema `globals.css`'teki CSS değişkenleriyle
(palet: primary `#0B5ED7`, navy `#1E3A8A`, surface `#F8FAFC`, border `#E5E7EB` ...).

## API service katmanı
- `services/api/client.ts`: tek Axios instance. İstek interceptor'ı Bearer token ekler;
  yanıt interceptor'ı **401'de refresh token ile yeniler** ve isteği tekrarlar, olmazsa `/login`'e atar.
- `services/api/http.ts`: `httpGet/Post/Put/Delete` standart `ApiResponse` zarfını açıp **payload** döner.
- Her feature `api.ts`'i bu helper'ları kullanır. **Bileşenler asla doğrudan axios çağırmaz.**

## TypeScript modelleri
`types/models.ts` backend DTO'larını yansıtır (Vehicle, Trailer, Driver, WorkOrder,
Part, Alert, ...). `types/api.ts` `ApiResponse<T>`/`PagedResponse<T>`/`PagedQuery`.
`types/enums.ts` enum değer kümeleri + `toOptions`.

## React Query
- Query'ler `feature/hooks.ts` içinde; cache anahtarları `lib/constants.ts → queryKeys`.
- Mutasyonlar başarıda ilgili anahtarları `invalidateQueries` ile tazeler ve **toast** gösterir.
- Sunucu durumu React Query'de; **Zustand yalnızca auth** (token + user) için (`features/auth/store.ts`).

## Form validation
React Hook Form + Zod (`zodResolver`). `Form*` bileşenleri `register()` ile çalışır,
hata mesajını gösterir. Örn. `VehicleFormDialog`, `TrailerFormDialog`, `DriverFormDialog`,
`PartFormDialog`, `WorkOrderFormDialog`, `LoginPage`.

## Tablo: filtre / sıralama / pagination
Reusable **`DataTable<T>`** + **`useListState`**: `page`, `search`, `sortBy`,
`sortDescending` durumunu yönetir ve backend'in kabul ettiği `PagedQuery`'yi üretir.
Sıralanabilir kolon başlıkları tıklanınca yön değişir; alt barda sayfa gezinme.
Arama `SearchInput` ile; hepsi backend'in `?search=&sortBy=&sortDescending=&page=&pageSize=` API'siyle eşleşir.

## Sayfa akışları (özet)
- **Liste sayfaları** (Araç/Dorse/Şoför/Parça/İş Emri): PageHeader + arama + DataTable +
  satır → detay, "Yeni" → form dialog, satır içi düzenle/sil (**sil → ConfirmDialog**).
- **Detay sayfaları**: Breadcrumb + alan kartı + düzenle/sil.
- **İş Emri detayı**: durum/maliyet kartı + parça tablosu + Başlat / **Parça Ekle** (stoktan düşer) / **Tamamla** (KM).
- **Stok Hareketleri**: parça seç → Giriş/Çıkış/Sayım dialog'u (toast ile sonuç).
- **Atamalar**: araç-dorse & şoför-araç eşleştir + aktif sorgula/sonlandır.
- **Raporlar**: Recharts grafikler + tablolar (araç maliyet, aylık performans, kritik stok).
- **Uyarılar**: açık uyarı listesi + okundu/çöz/kapat + üreteç butonları.
- **Dashboard**: StatCard'lar + aylık maliyet grafiği + açık uyarılar.

## Responsive davranış
Ana hedef **masaüstü operasyon**. Sidebar `md` altında gizlenir (Topbar marka gösterir).
StatCard/grafik grid'leri `sm/lg/xl` kırılımlarında sütun azaltır. Tablolar yatay
kaydırılabilir (`overflow-auto`). Form dialog'ları `max-w-lg`, mobilde kenar boşluklu.

## Önemli kurallar (uygulanan)
Bileşende API yok → service + React Query · Zustand yalnız auth · RHF+Zod formlar ·
reusable DataTable · StatusBadge enum'a göre renk · sil → ConfirmDialog · kritik işlemde toast.
