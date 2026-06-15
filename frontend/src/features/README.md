# Frontend Features — Dikey Dilim (Vertical Slice) Deseni

Her feature kendi içinde bağımsızdır ve şu yapıyı izler. Örnek: `vehicles/`

```
vehicles/
├── api/
│   └── vehicles.api.ts       # apiClient çağrıları + React Query key'leri
├── components/
│   ├── VehicleTable.tsx      # yalnızca bu feature'a ait bileşenler
│   └── VehicleForm.tsx
├── hooks/
│   ├── useVehicles.ts        # useQuery sarmalayıcısı
│   └── useCreateVehicle.ts   # useMutation sarmalayıcısı
├── pages/
│   ├── VehicleListPage.tsx   # router'a bağlanan sayfalar
│   └── VehicleDetailPage.tsx
└── types/
    └── vehicle.ts            # feature'a özgü tipler (backend DTO yansıması)
```

## Kurallar

- Bileşenler **doğrudan axios çağırmaz**; veri `hooks/` (React Query) üzerinden gelir.
- `hooks/` → `api/` → `@/services/api/client` zinciri izlenir.
- Form doğrulaması **Zod** şemaları ile, React Hook Form `zodResolver`'ında yapılır.
- İstemci/UI durumu gerekiyorsa Zustand store'u bu klasörde tutulur (örn. `store.ts`).
- Bir feature başka bir feature'ın iç dosyasını **import etmez**; paylaşılacak şey
  `@/components/common`, `@/lib`, `@/hooks` veya `@/types`'a taşınır.
- Sayfalar `@/app/router.tsx` içinde route olarak kaydedilir.

> Klasörler PROMPT 1'de oluşturuldu; içerik her feature'ın kendi prompt'unda
> bu desene göre doldurulacaktır.
