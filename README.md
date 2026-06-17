# ECS Fleet Maintenance & Inventory Management System

Lojistik / taşımacılık firmaları için geliştirilen **stok entegreli araç, dorse,
şoför, bakım-onarım ve envanter yönetim sistemi**.

> Bu repo **parçalı geliştirme prompt paketi** ile kurulmuştur: mimari (P1), DB
> şeması (P2), domain (P3), iş servisleri (P4), REST API (P5), frontend (P6),
> **PDF bakım raporu (P7)** ve **test / seed / Docker / deployment (P8)**.

## Teknoloji Yığını (Stack)

| Katman | Teknoloji |
| --- | --- |
| Backend | .NET 8 Web API, Clean Architecture |
| ORM / DB | Entity Framework Core + PostgreSQL (Npgsql) |
| Auth | JWT + Refresh Token |
| Validation | FluentValidation |
| Mapping | Mapster |
| Logging | Serilog |
| PDF | QuestPDF |
| Excel | ClosedXML |
| Frontend | React 18 + TypeScript + Vite |
| UI | Tailwind CSS + shadcn/ui |
| Routing | React Router |
| Data | TanStack React Query + Axios |
| State | Zustand |
| Forms | React Hook Form + Zod |
| Charts | Recharts |

## Repo Yapısı

```
ECS-garaj-portal/
├── backend/          # .NET 8 çözümü (Clean Architecture)
│   ├── ECS.sln
│   ├── Directory.Build.props
│   └── src/
│       ├── ECS.Domain/          # Çekirdek: entity, enum, value object, event
│       ├── ECS.Shared/          # Cross-cutting primitives (Result, paging, sabitler)
│       ├── ECS.Application/      # Use-case'ler, servisler, DTO, validator, port interface'leri
│       ├── ECS.Persistence/      # EF Core DbContext, konfigürasyon, repository, migration
│       ├── ECS.Infrastructure/   # Teknik servisler: PDF, Excel, Auth, background job
│       └── ECS.Api/              # Controller, middleware, DI composition root
├── frontend/         # React + TypeScript + Vite uygulaması
│   └── src/
│       ├── app/  assets/  components/  features/  hooks/
│       ├── lib/  services/  types/  styles/
└── docs/
    ├── ARCHITECTURE.md   # Mimari rehber: katmanlar, sorumluluklar, kurallar, geliştirme sırası
    └── DOMAIN_RULES.md   # İş kurallarının hangi katmanda nasıl uygulanacağı
```

Belgeler:
**[ARCHITECTURE](docs/ARCHITECTURE.md)** ·
**[DOMAIN_RULES](docs/DOMAIN_RULES.md)** ·
**[API](docs/API.md)** ·
**[SERVICES](docs/SERVICES.md)** ·
**[FRONTEND](docs/FRONTEND.md)** ·
**[REPORTING (PDF)](docs/REPORTING.md)** ·
**[TESTING](docs/TESTING.md)** ·
**[DEPLOYMENT](docs/DEPLOYMENT.md)**.

## Gereksinimler

- .NET 8 SDK
- Node.js 20+ / npm 10+
- PostgreSQL 14+
- (opsiyonel) Docker + Docker Compose

## Docker ile Hızlı Başlangıç

Tüm yığını (PostgreSQL + migration + backend + frontend) tek komutla çalıştırın:

```bash
cp .env.example .env          # JWT_SIGNING_KEY ve POSTGRES_PASSWORD'u doldurun
docker compose up -d --build
# Demo veriyle (yalnızca non-prod): RUN_DEMO_SEED=true docker compose up -d --build
```

- Uygulama: http://localhost:8080  ·  API: http://localhost:5080  ·  Sağlık: `/health`
- `migrator` servisi `database/migrations/*.sql`'i sırayla uygular (şema varsa atlar).
- İlk giriş: `admin@ecslog.com` / `Admin123!` (`admin@ecs.local` local alias olarak da çalışır; hemen değiştirin).

Detaylar: **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)**.

## Çalıştırma

### Backend

```bash
cd backend
dotnet restore
# Bağlantı cümlesini ayarlayın: src/ECS.Api/appsettings.json -> ConnectionStrings:Default
dotnet build
dotnet run --project src/ECS.Api      # http://localhost:5080 (Swagger: /swagger)
```

> EF Core migration'ları ve entity'ler sonraki prompt'larda eklenecektir.
> Migration komutu (entity'ler tanımlandıktan sonra):
> `dotnet ef migrations add Init --project src/ECS.Persistence --startup-project src/ECS.Api`

### Frontend

```bash
cd frontend
cp .env.example .env          # VITE_API_BASE_URL değerini gerekirse güncelleyin
npm install
npm run dev                   # http://localhost:5173
```

### Test

```bash
cd backend
dotnet test                                    # domain + application birim testleri
dotnet test --collect:"XPlat Code Coverage"    # kapsama (coverlet)
```

Test planı ve kritik senaryo eşlemesi: **[docs/TESTING.md](docs/TESTING.md)**.

## Geliştirme Dalı

Bu paketteki tüm geliştirme `claude/modest-turing-sj9bdz` dalında yapılır.

## Mimari Altın Kurallar (özet)

1. Controller içinde iş mantığı **yazılmaz** — orchestration Application katmanındadır.
2. Entity **asla** doğrudan API response olarak dönmez — DTO kullanılır.
3. Transaction gerektiren işlemler Application katmanında `IUnitOfWork` ile yönetilir.
4. PDF, e-posta, dosya, auth gibi teknik servisler Infrastructure katmanındadır.
5. Bağımlılıklar **her zaman içe** (Domain'e doğru) bakar.

Tam liste: [docs/ARCHITECTURE.md → Mimari Kurallar](docs/ARCHITECTURE.md#7-mimari-kurallar-kod-yazarken-uyulacaklar).
