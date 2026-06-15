# ECS Fleet Maintenance & Inventory Management System

Lojistik / taşımacılık firmaları için geliştirilen **stok entegreli araç, dorse,
şoför, bakım-onarım ve envanter yönetim sistemi**.

> Bu repo **parçalı geliştirme prompt paketi** ile kurulmaktadır.
> Bu commit **PROMPT 1 — Proje Mimarisi ve Dosya Yapısı** çıktısıdır:
> tüm katmanlar, klasör yapısı, temel soyutlamalar (base entity, Result, port
> interface'leri, DI iskeleti) ve mimari dokümantasyon oluşturulmuştur.
> İş kuralları, entity'ler ve özellikler sonraki prompt'larda bu iskelet
> üzerine inşa edilecektir.

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

Detaylı mimari için **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**,
iş kurallarının katman eşlemesi için **[docs/DOMAIN_RULES.md](docs/DOMAIN_RULES.md)**.

## Gereksinimler

- .NET 8 SDK
- Node.js 20+ / npm 10+
- PostgreSQL 14+

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

## Geliştirme Dalı

Bu paketteki tüm geliştirme `claude/modest-turing-sj9bdz` dalında yapılır.

## Mimari Altın Kurallar (özet)

1. Controller içinde iş mantığı **yazılmaz** — orchestration Application katmanındadır.
2. Entity **asla** doğrudan API response olarak dönmez — DTO kullanılır.
3. Transaction gerektiren işlemler Application katmanında `IUnitOfWork` ile yönetilir.
4. PDF, e-posta, dosya, auth gibi teknik servisler Infrastructure katmanındadır.
5. Bağımlılıklar **her zaman içe** (Domain'e doğru) bakar.

Tam liste: [docs/ARCHITECTURE.md → Mimari Kurallar](docs/ARCHITECTURE.md#7-mimari-kurallar-kod-yazarken-uyulacaklar).
