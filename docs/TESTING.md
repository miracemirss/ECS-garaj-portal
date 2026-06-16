# ECS Fleet — Test Stratejisi (PROMPT 8)

Test piramidi: çok sayıda **hızlı birim testi** (domain + application), daha az
**entegrasyon testi** (gerçek PostgreSQL), en üstte birkaç **E2E** senaryosu.

```
        /\        E2E (Playwright)            — kritik kullanıcı akışları
       /  \       Integration (Testcontainers) — API + gerçek DB + trigger'lar
      /----\      Application unit (NSubstitute) — iş kuralları, mock port'lar
     /------\     Domain unit (saf)              — entity invariantları
```

## 1. Projeler

| Proje | Tür | Araçlar | Durum |
|------|-----|---------|-------|
| `backend/tests/ECS.Domain.UnitTests` | Domain birim | xUnit + FluentAssertions | **Uygulandı** |
| `backend/tests/ECS.Application.UnitTests` | Application birim | xUnit + NSubstitute | **Uygulandı** |
| `backend/tests/ECS.Api.IntegrationTests` | Entegrasyon | `WebApplicationFactory` + Testcontainers (PostgreSQL) | Planlandı (bkz. §5) |
| `frontend` (Vitest) | Component | Vitest + React Testing Library | Önerildi (§6) |
| `e2e` (Playwright) | E2E | Playwright | Önerildi (§7) |

Çalıştırma:

```bash
cd backend && dotnet test                       # tüm birim testleri
dotnet test --collect:"XPlat Code Coverage"     # kapsama (coverlet)
```

## 2. Backend unit test planı — Domain

Saf C#, altyapı yok; en hızlı ve en güvenilir katman. Uygulanan testler:

- **Vehicle** — plaka normalizasyonu, marka/model-yıl doğrulaması, KM geri gitmenin
  reddi, `RecordMaintenanceCompletion` ile sonraki bakım planlaması.
- **Trailer** — plaka normalizasyonu, kapasite/lastik aralık kontrolü.
- **Part (stok)** — `DecreaseStock` düşüşü, **yetersiz stokta `InsufficientStockException`
  + stok değişmemesi**, minimum altına inince `StockFellBelowMinimumEvent`, negatif
  ayarlamanın reddi.
- **MaintenanceWorkOrder** — hedef tipi, `AddPart` ile `parts_cost`/`total_cost`
  birikimi, `Complete` durum + zaman + event, KM sonrası < öncesi reddi, çift
  tamamlama ve tamamlanmış emre parça eklemenin reddi, `Start` geçişi.
- **Assignment (VT/DV)** — başlangıç aktif, `End` ile pasifleşme, çift `End`
  → `AssignmentConflictException`, bitişin başlangıçtan önce olamaması.
- **Value objects** — `Money` yuvarlama/negatif/para birimi; `PlateNumber` normalizasyon.

## 3. Backend unit test planı — Application

Servisler, port'lar (`IRepository<>`, `IUnitOfWork`, `IDateTimeProvider`, ...)
NSubstitute ile taklit edilerek test edilir. Uygulanan testler:

- **MaintenanceService**
  - Stok yetersizse `AddPart` **Conflict** döner ve stok **değişmez**, stok hareketi
    **oluşmaz** (`movements.AddAsync` çağrılmaz).
  - `AddPart` başarıda stok **otomatik düşer** (10→7) ve **`Out` stok hareketi** (-3)
    oluşur.
  - `Complete` iş emrini tamamlar, **araç KM'sini ileri alır** ve **PDF üretimini tetikler**.
  - PDF üretimi başarısız olsa bile **tamamlama başarılı kalır** ve
    `MaintenanceReportGenerationFailed` audit'i düşülür.
- **AssignmentService**
  - Araç-dorse atamasında **mevcut aktif bağ kapatılır**, yeni aktif bağ açılır
    (aynı araca ikinci aktif dorse engellenir).
  - Şoför-araç atamasında **mevcut aktif bağ kapatılır** (aynı şoföre ikinci aktif araç engellenir).
  - Araç yoksa atama **NotFound** döner.

## 4. Kritik senaryo eşlemesi

| Kritik senaryo | Katman | Test |
|---|---|---|
| Araç / dorse / şoför oluşturma | Domain | `VehicleTests`, `TrailerTests` (+ entegrasyon) |
| Araç-dorse eşleştirme | Application/DB | `AssignmentServiceTests`, partial unique index |
| **Aynı araca 2. aktif dorse engeli** | Application/DB | `AssignmentServiceTests` + `ux_*_active` index |
| **Aynı dorseye 2. aktif araç engeli** | DB | partial unique index (entegrasyon) |
| **Aynı şoföre 2. aktif araç engeli** | Application/DB | `AssignmentServiceTests` + index |
| İş emri oluşturma / parça ekleme | Application | `MaintenanceServiceTests` |
| **Parçanın stoktan otomatik düşmesi** | Application | `AddPart_decrements_stock...` |
| **Stok yetersizse engelleme** | Application/Domain | `AddPart_blocks...`, `PartStockTests` |
| **Stok hareketi olmadan stok değişmemesi** | DB | `fn_guard_part_stock` (entegrasyon) |
| İş emri tamamlama + KM güncelleme | Application | `Complete_marks_completed...` |
| PDF raporu oluşturma | Application/Infra | `Complete_..._triggers_report` (+ entegrasyon) |
| Kritik stok uyarısı | DB/Application | demo seed + `generate_critical_stock_alerts` |
| Yaklaşan bakım uyarısı | DB/Application | demo seed + `generate_upcoming_maintenance_alerts` |
| Audit log oluşması | DB | `fn_audit` trigger (entegrasyon) |
| Yetkisiz kullanıcı engeli | API | endpoint testleri (§5) |

## 5. Entegrasyon test planı (önerilen)

`ECS.Api.IntegrationTests`: `WebApplicationFactory<Program>` + **Testcontainers
PostgreSQL** (her koşuda gerçek DB; migration script'leri uygulanır). Böylece
trigger'lar, partial unique index'ler ve `fn_guard_part_stock` gibi DB-seviyesi
garantileri **gerçekten** test edilir.

> Not: Entegrasyon testleri, EF Core entity konfigürasyonlarının tamamlanmasına
> bağlıdır (DbContext eşlemeleri). Şema kaynağı `database/migrations/*.sql`'dir;
> testler bu script'leri container'a uygular.

API endpoint testleri:

- `POST /api/auth/login` → token; **token'sız** çağrı 401; **yetkisiz rol** 403.
- `POST /api/maintenance` → iş emri; `POST /.../parts` stok düşürür; yetersizse 409.
- `POST /api/maintenance/{id}/complete` → 200 + araç KM; ardından
  `POST /api/reports/maintenance/{id}` 201 ve `GET /.../download` `application/pdf`.
- `POST /api/assignments/vehicle-trailer` ikinci aktif → önceki kapanır; DB tek aktif.
- Kritik/ yaklaşan uyarı üreticileri çağrılır, `GET /api/alerts` döner.

## 6. Frontend component test önerileri

`Vitest + @testing-library/react + jsdom` (devDependency olarak eklenir):

- Form validasyonu (React Hook Form + Zod): zorunlu alanlar, plaka maskesi,
  negatif KM reddi.
- Veri tablosu/sayfalama bileşenleri: boş durum, yükleniyor, hata durumu.
- React Query hook'ları: MSW (Mock Service Worker) ile API yanıtları taklit edilir.
- Yetki yönlendirme: token yokken korumalı route → login'e yönlenme.

## 7. E2E test senaryoları (Playwright)

1. Login → dashboard'da özet kartlarının görünmesi.
2. Araç + dorse oluştur → eşleştir → ikinci eşleştirmede öncekinin kapanması.
3. İş emri oluştur → parça ekle (stok düşer) → tamamla → **PDF indir**.
4. Stok yetersiz parça eklemeye çalış → hata mesajı, stok sabit.
5. Kritik stok ve yaklaşan bakım uyarılarının uyarılar sayfasında listelenmesi.
6. Viewer rolüyle giriş → yazma işlemlerinin gizli/yasak olması.

## 8. CI önerisi

GitHub Actions: `dotnet test` (birim) her PR'da; entegrasyon testleri Docker
mevcutsa (`services: postgres` veya Testcontainers) çalışır; kapsama raporu
(coverlet → Cobertura) PR'a eklenir. Frontend için `npm run typecheck && npm run lint`.
