# ECS Fleet Maintenance & Inventory Management System — Mimari Rehber

> **PROMPT 1 çıktısı.** Bu doküman; backend ve frontend dosya yapısını, her
> klasörün sorumluluğunu, dosyaların kullanım amacını, Clean Architecture
> bağımlılık yönünü, ekip geliştirme sırasını ve kod yazarken uyulacak mimari
> kuralları tanımlar.

İçindekiler:

1. [Genel Bakış](#1-genel-bakış)
2. [Backend Klasör Yapısı](#2-backend-klasör-yapısı)
3. [Backend Katman Sorumlulukları](#3-backend-katman-sorumlulukları)
4. [Clean Architecture Bağımlılık Yönü](#4-clean-architecture-bağımlılık-yönü)
5. [Frontend Klasör Yapısı](#5-frontend-klasör-yapısı)
6. [Frontend Klasör Sorumlulukları](#6-frontend-klasör-sorumlulukları)
7. [Mimari Kurallar (Kod Yazarken Uyulacaklar)](#7-mimari-kurallar-kod-yazarken-uyulacaklar)
8. [Ekip Geliştirme Sırası](#8-ekip-geliştirme-sırası)
9. [Bir Özelliğin Uçtan Uca Akışı](#9-bir-özelliğin-uçtan-uca-akışı)

---

## 1. Genel Bakış

Sistem **Clean Architecture + Layered Architecture** prensipleriyle, 6 backend
projesi ve feature-bazlı bir frontend uygulamasından oluşur. Temel hedefler:

- **Bağımsız çekirdek:** İş kuralları (Domain) hiçbir framework'e bağlı değildir.
- **Test edilebilirlik:** Application katmanı, dış dünyayı `interface` (port)
  arkasında gördüğü için izole test edilebilir.
- **Değiştirilebilirlik:** PostgreSQL, QuestPDF, JWT gibi teknik kararlar dış
  katmanlardadır; çekirdeği etkilemeden değiştirilebilir.
- **Tutarlılık:** Her feature aynı dikey dilim (vertical slice) desenini izler.

---

## 2. Backend Klasör Yapısı

```
backend/
├── ECS.sln
├── Directory.Build.props          # Ortak build ayarları (net8.0, nullable, implicit usings)
└── src/
    ├── ECS.Domain/                # (Bağımlılık YOK) En içteki çekirdek
    │   ├── Common/                # BaseEntity, AuditableEntity, AggregateRoot, IDomainEvent
    │   ├── Entities/              # Vehicle, Trailer, Driver, WorkOrder, Part, StockMovement ...
    │   ├── Enums/                 # TargetType, WorkOrderStatus, StockMovementType, AlertType ...
    │   ├── ValueObjects/          # ValueObject (base), Money, PlateNumber
    │   ├── DomainEvents/          # WorkOrderCompletedEvent, StockFellBelowMinimumEvent ...
    │   ├── Interfaces/            # IAuditableEntity, ISoftDeletable
    │   └── Exceptions/            # DomainException, InsufficientStockException ...
    │
    ├── ECS.Shared/                # (Bağımlılık YOK) Cross-cutting primitives
    │   ├── Results/               # Result, Result<T>, Error  (hata/başarı sözleşmesi)
    │   ├── Pagination/            # PaginationRequest, PagedList<T>
    │   ├── Constants/             # Roles, Policies gibi sabitler
    │   ├── Models/                # Katmanlar arası taşınan basit ortak modeller
    │   └── Extensions/            # Genel amaçlı extension method'lar
    │
    ├── ECS.Application/           # → Domain, Shared.  Use-case orchestration
    │   ├── Common/
    │   │   ├── Interfaces/        # PORT'lar: IUnitOfWork, IRepository<T>, IPdfService,
    │   │   │                      #   IJwtTokenService, ICurrentUserService, IDateTimeProvider,
    │   │   │                      #   IExcelService, IAuditLogService
    │   │   ├── Behaviors/         # Pipeline davranışları (validation, transaction, logging)
    │   │   ├── Mapping/           # Mapster IRegister konfigürasyonları
    │   │   ├── Models/            # Application'a özgü ortak modeller
    │   │   └── Exceptions/        # NotFoundException, ValidationException (uygulama seviyesi)
    │   ├── Features/              # Feature-bazlı dikey dilimler
    │   │   ├── Vehicles/          #   IVehicleService + VehicleService + Dtos/ + Validators/
    │   │   ├── Trailers/  Drivers/  Assignments/  Maintenance/  Inventory/
    │   │   ├── Operations/  Reports/  Alerts/  Dashboard/  Auth/
    │   │   └── README.md          # Feature dilim deseni (canonical layout)
    │   └── DependencyInjection.cs # AddApplication(): validator + mapper + servis kaydı
    │
    ├── ECS.Persistence/          # → Application, Domain.  EF Core ile veri erişimi
    │   ├── Contexts/              # ApplicationDbContext
    │   ├── Configurations/        # IEntityTypeConfiguration<T> (Fluent API eşlemeleri)
    │   ├── Repositories/          # Repository<T> (IRepository<T> impl.), UnitOfWork
    │   ├── Interceptors/          # AuditableEntityInterceptor (CreatedAt/By otomatik)
    │   ├── Migrations/            # EF Core migration'ları
    │   └── DependencyInjection.cs # AddPersistence(): DbContext (Npgsql), repo, UoW kaydı
    │
    ├── ECS.Infrastructure/       # → Application, Domain.  Teknik servis implementasyonları
    │   ├── Services/              # DateTimeProvider, e-posta vb.
    │   ├── Pdf/                   # QuestPdfReportService (IPdfService impl.)
    │   ├── Excel/                 # ClosedXmlExportService (IExcelService impl.)
    │   ├── Identity/              # JwtTokenService (IJwtTokenService impl.)
    │   ├── BackgroundJobs/        # Kritik stok / bakım zamanı tarayıcı (BackgroundService)
    │   ├── Logging/               # Serilog enrich/sink yardımcıları
    │   └── DependencyInjection.cs # AddInfrastructure(): teknik servis kaydı
    │
    └── ECS.Api/                  # → Application, Infrastructure, Persistence.  Sunum + composition root
        ├── Controllers/          # ApiControllerBase + feature controller'ları (ince!)
        ├── Middleware/           # ExceptionHandlingMiddleware
        ├── Extensions/           # AddApiServices (Swagger, CORS, Auth, HttpContextAccessor)
        ├── Services/             # CurrentUserService (ICurrentUserService impl., HttpContext)
        ├── Properties/           # launchSettings.json
        ├── appsettings.json      # Connection string, JWT, CORS, Serilog
        └── Program.cs            # Tüm katmanları DI ile birleştiren composition root
```

---

## 3. Backend Katman Sorumlulukları

### ECS.Domain — Çekirdek (hiçbir şeye bağımlı değil)
İş dünyasının kalbidir. Sadece saf C#. Framework, EF, ASP.NET **yasak**.

| Klasör | Ne içerir | Örnek dosya |
| --- | --- | --- |
| `Common/` | Tüm entity'lerin türediği temel sınıflar | `BaseEntity.cs`, `AuditableEntity.cs`, `AggregateRoot.cs`, `IDomainEvent.cs` |
| `Entities/` | İş nesneleri ve davranışları | `Vehicle.cs`, `Trailer.cs`, `MaintenanceWorkOrder.cs` |
| `Enums/` | Sabit kümeler | `WorkOrderStatus.cs`, `TargetType.cs`, `StockMovementType.cs` |
| `ValueObjects/` | Kimliği olmayan değer nesneleri | `PlateNumber.cs`, `Money.cs` |
| `DomainEvents/` | Domain event kayıtları | `WorkOrderCompletedEvent.cs` |
| `Exceptions/` | İş kuralı ihlal istisnaları | `DomainException.cs` |

### ECS.Shared — Ortak Primitives (hiçbir şeye bağımlı değil)
Tüm katmanların kullanabileceği teknolojiden bağımsız küçük tipler. `Result<T>`
deseni metot başarı/başarısızlığını exception fırlatmadan taşır.

### ECS.Application — Use-Case Orchestration (→ Domain, Shared)
Uygulamanın "ne yaptığını" tanımlar. Her senaryo bir **application service**
metoduyla orkestre edilir. Dış dünyaya **port interface'leri** üzerinden bakar;
implementasyonları bilmez.

- `Common/Interfaces/` — **Port'lar.** Persistence ve Infrastructure bunları
  implemente eder (Dependency Inversion). Örn. `IUnitOfWork`, `IRepository<T>`,
  `IPdfService`, `IJwtTokenService`, `ICurrentUserService`, `IDateTimeProvider`.
- `Features/<X>/` — `I<X>Service` (sözleşme) + `<X>Service` (orchestration) +
  `Dtos/` (giriş/çıkış modelleri) + `Validators/` (FluentValidation).
- `Common/Behaviors/` — Tüm akışlara giren çapraz davranışlar
  (örn. `TransactionBehavior`, `ValidationBehavior`).
- **Transaction sınırı burasıdır.** Birden fazla repository'i etkileyen işlem
  `IUnitOfWork.BeginTransactionAsync()` ile sarmalanır.

### ECS.Persistence — Veri Erişimi (→ Application, Domain)
Application'daki veri port'larının EF Core implementasyonu.

- `Contexts/ApplicationDbContext.cs` — `OnModelCreating` tüm
  `IEntityTypeConfiguration` sınıflarını otomatik uygular.
- `Configurations/` — Her entity için Fluent API eşlemesi (entity dosyaları
  attribute kirliliğinden korunur).
- `Repositories/` — `Repository<T>` (generic CRUD) ve `UnitOfWork`
  (`SaveChanges` + transaction).
- `Interceptors/AuditableEntityInterceptor.cs` — `SaveChanges` sırasında
  `CreatedAtUtc/CreatedBy/ModifiedAtUtc/ModifiedBy` alanlarını otomatik doldurur.

### ECS.Infrastructure — Teknik Servisler (→ Application, Domain)
"Nasıl" sorusunun teknik cevapları: PDF üretimi (QuestPDF), Excel (ClosedXML),
JWT üretimi, e-posta, dosya, background job'lar. Hepsi Application'daki bir
port'u implemente eder.

### ECS.Api — Sunum + Composition Root (→ Application, Infrastructure, Persistence)
HTTP dünyası ve **bağımlılıkların birleştirildiği tek yer**.

- `Program.cs` → `AddApplication()` + `AddInfrastructure()` + `AddPersistence()`
  + `AddApiServices()`.
- Controller'lar **incedir**: request'i alır, ilgili service metodunu çağırır,
  `Result`'ı HTTP yanıtına çevirir. **İş mantığı içermez.**

---

## 4. Clean Architecture Bağımlılık Yönü

**Altın kural:** Bağımlılıklar her zaman **içe** (Domain'e doğru) bakar.
Dış katmanlar iç katmanları bilir; iç katmanlar dışı **bilmez**.

```
                ┌─────────────────────────────────────────────┐
                │                  ECS.Api                     │  (composition root)
                │   Controllers / Middleware / Program.cs      │
                └───────┬───────────────┬───────────────┬──────┘
                        │ refs          │ refs          │ refs
            ┌───────────▼──────┐ ┌──────▼───────────┐   │
            │ ECS.Infrastructure│ │ ECS.Persistence  │   │
            │  PDF/Auth/Excel/  │ │  EF Core / Repo  │   │
            │  BackgroundJobs   │ │  / UnitOfWork    │   │
            └───────────┬──────┘ └──────┬───────────┘   │
                        │ implements ports│ implements   │
                        │                 │ ports        │
                        └────────┬────────┴──────────────┘
                                 ▼
                         ┌───────────────┐
                         │ ECS.Application│  → Domain, Shared
                         │  Services/Port │
                         │  DTO/Validator │
                         └───────┬────────┘
                                 ▼
                  ┌──────────────────────────┐
                  │   ECS.Domain   ECS.Shared │  (bağımlılık YOK)
                  └──────────────────────────┘
```

**Project reference matrisi (kod ile zorlanır):**

| Proje | Referans verdiği projeler |
| --- | --- |
| `ECS.Domain` | — (hiçbiri) |
| `ECS.Shared` | — (hiçbiri) |
| `ECS.Application` | `ECS.Domain`, `ECS.Shared` |
| `ECS.Persistence` | `ECS.Application`, `ECS.Domain` |
| `ECS.Infrastructure` | `ECS.Application`, `ECS.Domain` |
| `ECS.Api` | `ECS.Application`, `ECS.Infrastructure`, `ECS.Persistence` |

> **Dependency Inversion:** Persistence ve Infrastructure, Application'a referans
> verir ama Application onlara vermez. Application sadece `interface` (port) tanımlar;
> somut implementasyonlar dış katmanlardadır ve DI ile çalışma zamanında bağlanır.
> Bu sayede iş mantığı, veritabanı/PDF/JWT seçiminden tamamen habersizdir.

---

## 5. Frontend Klasör Yapısı

```
frontend/
├── index.html
├── package.json  vite.config.ts  tsconfig.json  tailwind.config.ts  components.json
├── .env.example                  # VITE_API_BASE_URL
└── src/
    ├── app/                      # Uygulama kabuğu: providers.tsx, router.tsx
    ├── assets/                   # Statik dosyalar (görsel, font)
    ├── components/
    │   ├── ui/                   # shadcn/ui primitives (button, input, dialog ...)
    │   ├── common/               # Projeye özel paylaşılan bileşenler (DataTable, PageHeader)
    │   └── layout/               # AppLayout, Sidebar, Topbar
    ├── features/                 # Feature-bazlı dikey dilimler
    │   ├── dashboard/  vehicles/  trailers/  drivers/  assignments/
    │   ├── maintenance/  inventory/  operations/  reports/  alerts/
    │   ├── settings/  auth/
    │   │     └── her biri: api/  components/  hooks/  pages/  types/
    │   └── README.md             # Feature dilim deseni
    ├── hooks/                    # Global custom hook'lar (useDebounce, useMediaQuery)
    ├── lib/                      # utils.ts (cn), queryClient.ts, env.ts, constants.ts
    ├── services/                 # api/client.ts (axios instance + interceptor'lar)
    ├── types/                    # Global tipler: api.ts, common.ts (backend ile senkron)
    └── styles/                   # globals.css (Tailwind direktifleri)
```

---

## 6. Frontend Klasör Sorumlulukları

| Klasör | Sorumluluk |
| --- | --- |
| `app/` | Uygulamanın tepe kabuğu. `providers.tsx` (React Query, tema), `router.tsx` (rota tanımları). |
| `assets/` | Derlemeye dahil statik dosyalar. |
| `components/ui/` | shadcn/ui ile üretilen, projeye bağımsız primitive bileşenler. |
| `components/common/` | Birden çok feature'ın kullandığı bileşenler (tablo, sayfa başlığı, boş durum). |
| `components/layout/` | Sayfa iskeleti: menü, üst bar, içerik alanı. |
| `features/<x>/api/` | O feature'ın backend çağrıları (axios + React Query key'leri). |
| `features/<x>/components/` | Sadece o feature'a ait bileşenler. |
| `features/<x>/hooks/` | Feature'a özel `useQuery`/`useMutation` sarmalayıcıları. |
| `features/<x>/pages/` | Router'a bağlanan sayfa bileşenleri. |
| `features/<x>/types/` | Feature'a özel DTO/model tipleri. |
| `hooks/` | Global, feature'dan bağımsız hook'lar. |
| `lib/` | Saf yardımcılar ve konfig: `cn`, `queryClient`, `env`, sabitler. |
| `services/` | Paylaşılan HTTP altyapısı: axios instance, auth interceptor, hata normalizasyonu. |
| `types/` | Backend sözleşmeleriyle senkron global tipler (`PagedResult<T>`, enum'lar). |
| `styles/` | Global stil ve Tailwind katmanları. |

**Frontend altın kuralı:** Bileşenler ham `axios` çağırmaz; veri her zaman
`features/<x>/hooks/` içindeki React Query hook'ları üzerinden gelir. Bu hook'lar
`features/<x>/api/` fonksiyonlarını, onlar da `services/api/client.ts`'i kullanır.

---

## 7. Mimari Kurallar (Kod Yazarken Uyulacaklar)

**Backend**

1. **Controller'da iş mantığı olmaz.** Controller: doğrula → service çağır →
   `Result`'ı HTTP'ye çevir. Karar/hesaplama yok.
2. **Entity asla doğrudan dönmez.** API her zaman DTO döner; eşleme Mapster ile
   yapılır. Domain entity'leri sunum sözleşmesi değildir.
3. **Transaction Application katmanında.** Birden çok repository'i değiştiren her
   işlem `IUnitOfWork` transaction'ı içinde çalışır (örn. iş emrine parça ekleme +
   stok düşme + stok hareketi tek transaction).
4. **Teknik servisler Infrastructure'da.** PDF, e-posta, dosya, auth, background
   job'lar Application'daki port'ları implemente eder; Application bunları sadece
   `interface` olarak tanır.
5. **Dış dünyaya port üzerinden eriş.** Application; DbContext, HttpContext,
   `DateTime.Now`, dosya sistemi gibi şeylere doğrudan dokunmaz —
   `IRepository<T>`, `ICurrentUserService`, `IDateTimeProvider` kullanır.
6. **Domain saf kalır.** Domain projesi hiçbir NuGet framework paketine bağımlı
   olamaz (EF, ASP.NET, vb. yasak).
7. **Doğrulama (validation) Application'da.** FluentValidation validator'ları
   feature klasöründe; pipeline/behavior ile servisten önce çalışır.
8. **Hata yönetimi merkezî.** İş kuralı ihlali `DomainException`, bulunamayan
   kayıt `NotFoundException`; hepsi `ExceptionHandlingMiddleware` ile tutarlı HTTP
   yanıtına çevrilir.
9. **Audit log kritik işlemlerde.** Stok düşme, iş emri tamamlama, atama değişimi
   gibi işlemler `IAuditLogService` ile loglanır.
10. **Async her yerde.** I/O yapan tüm metotlar `async` + `CancellationToken` alır.

**Frontend**

11. **Sunucu durumu React Query'de**, istemci/UI durumu Zustand'da. İkisi karışmaz.
12. **Form doğrulaması Zod ile**, React Hook Form `zodResolver` üzerinden.
13. **Tipler backend ile senkron.** Enum ve DTO tipleri `types/` altında backend
    sözleşmesini yansıtır.
14. **Feature izolasyonu.** Bir feature başka feature'ın iç dosyasını import etmez;
    paylaşılan şey `components/common`, `lib`, `hooks` veya `types`'a taşınır.

---

## 8. Ekip Geliştirme Sırası

Aşağıdaki sıra, bağımlılıkları gözeterek paralel çalışmayı mümkün kılar.

**Faz 0 — İskelet (bu commit / PROMPT 1)**
- Çözüm yapısı, katmanlar, project reference'lar, base sınıflar, port interface'leri,
  DI iskeleti, frontend kabuğu ve dokümantasyon.

**Faz 1 — Temel altyapı**
1. Domain `Common` + Auth entity'leri (User, Role, RefreshToken).
2. Persistence: `ApplicationDbContext`, ilk migration, audit interceptor bağlama.
3. Infrastructure: `JwtTokenService`, Serilog kurulumu.
4. Api: Auth controller, JWT middleware, Swagger güvenlik tanımı.
5. Frontend: auth feature (login, token store, axios interceptor, korumalı rota).

**Faz 2 — Ana kayıt (master data)**
6. Vehicles, Trailers, Drivers entity + config + service + DTO + validator + controller.
7. Frontend: ilgili feature'ların liste/form sayfaları (CRUD).

**Faz 3 — İlişki ve geçmiş (history)**
8. Assignments: araç-dorse ve şoför-araç ilişkileri (aktif kayıt + history kuralları).

**Faz 4 — Envanter**
9. Inventory: Part, Warehouse, StockMovement; stok düşme/girişi transaction'ları;
   kritik stok event'i.

**Faz 5 — Bakım**
10. Maintenance: WorkOrder, WorkOrderItem; parça ekleme → otomatik stok düşme;
    bakım öncesi/sonrası KM; tamamlamada `WorkOrderCompletedEvent`.

**Faz 6 — Raporlama ve uyarılar**
11. PDF bakım raporu (QuestPDF), Excel export (ClosedXML).
12. BackgroundService: kritik stok ve yaklaşan bakım taraması → Alert üretimi.
13. Dashboard ve Reports feature'ları (Recharts grafikleri).

**Faz 7 — Operasyonlar ve ayarlar**
14. Operations, Settings; rol/yetki ince ayarı; audit log görüntüleme.

> Her faz; Domain → Persistence → Application → Infrastructure → Api → Frontend
> dikey dilim sırasıyla ilerler.

---

## 9. Bir Özelliğin Uçtan Uca Akışı

Örnek: "İş emrine parça ekle" (Faz 5).

```
HTTP POST /api/workorders/{id}/items
        │
        ▼
[Api] MaintenanceController                # ince: DTO al, service çağır, Result -> HTTP
        │  calls IMaintenanceService.AddPartAsync(...)
        ▼
[Application] MaintenanceService            # ORCHESTRATION + TRANSACTION SINIRI
        │  ├─ Validator (FluentValidation)
        │  ├─ IUnitOfWork.BeginTransactionAsync()
        │  ├─ IRepository<Part>  -> stok yeterli mi? (yetersizse DomainException)
        │  ├─ Domain: WorkOrder.AddItem(...) / Part.DecreaseStock(...)  (iş kuralı)
        │  ├─ IRepository<StockMovement> -> hareket kaydı (stok ancak hareketle değişir)
        │  ├─ IAuditLogService.LogAsync(...)
        │  └─ Commit
        ▼
[Persistence] Repository<T> + UnitOfWork    # EF Core, PostgreSQL
        ▼
DTO (Mapster) -> Result<WorkOrderItemDto> -> HTTP 200
```

Bu akış; controller'ın ince, iş mantığının Application'da, kuralların Domain'de,
teknik detayın dış katmanlarda olduğu mimariyi özetler.
