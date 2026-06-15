# ECS.Domain — Çekirdek Katman (PROMPT 3)

Saf C# domain modeli. **Hiçbir framework bağımlılığı yoktur** (EF Core, ASP.NET yok).
Tüm çözümle birlikte derlenip doğrulanmıştır (`dotnet build ECS.sln` → 0 hata).

## Klasörler

| Klasör | İçerik |
|--------|--------|
| `Common/` | `BaseEntity` (Guid Id), `AuditableEntity` (audit + soft delete), `AggregateRoot` (+ domain event), `IDomainEvent`/`DomainEvent`/`IHasDomainEvents`, `IAggregateRoot` |
| `Interfaces/` | `IAuditableEntity`, `ISoftDeletable` |
| `Entities/` | 20 entity (aşağıda) |
| `Enums/` | 13 enum |
| `ValueObjects/` | `ValueObject` (eşitlik tabanı), `Money`, `PlateNumber` |
| `DomainEvents/` | `WorkOrderCompletedEvent`, `StockFellBelowMinimumEvent`, atama event'leri |
| `Exceptions/` | `DomainException` + `InsufficientStockException`, `InvalidWorkOrderStateException`, `InvalidWorkOrderTargetException`, `AssignmentConflictException` |

## Base sınıf hiyerarşisi

```
BaseEntity (Guid Id)
 └── AuditableEntity (CreatedAt/By, UpdatedAt/By, IsDeleted/DeletedAt/By)  : IAuditableEntity, ISoftDeletable
      └── AggregateRoot (domain events)  : IAggregateRoot, IHasDomainEvents
```

- **Soft delete:** `AuditableEntity.MarkDeleted(...)` / `Restore()`; setter'lar `private`.
- **Aggregate roots:** Vehicle, Trailer, Driver, VehicleTrailerAssignment,
  DriverVehicleAssignment, MaintenanceWorkOrder, Part, User.
- **Auditable (event'siz):** Role, Warehouse, Supplier, CompanySetting, Document, MaintenanceTask.
- **BaseEntity (append-only / kendi zaman damgası):** RefreshToken, WorkOrderPart,
  StockMovement, Alert, ReportFile. `AuditLog` ise yüksek hacim nedeniyle `long Id` ile bağımsızdır.

## Tasarım kuralları (PROMPT 3)

- EF Core bağımlılığı **yok**; entity'ler doğrudan API response olarak kullanılmaz (DTO katmanı ayrı).
- Ortak alanlar (`CreatedAt/UpdatedAt/DeletedAt/IsDeleted`) tek base sınıftan gelir.
- **Para alanları `decimal`**, **tarih/saat UTC** (`DateTime` UTC; saf tarihler için `DateOnly`).
- Unique alanlar: `Vehicle.PlateNo`/`Vin`, `Trailer.PlateNo`, `Driver.NationalId`,
  `Part.PartNo`, `User.Email`, `Warehouse.Code`, `Role.Name`, `RefreshToken.TokenHash`.
- **Zengin model:** `private set` + `static` factory metotları + davranış metotları.
  Örn. `Part.DecreaseStock` negatif stoğu engeller ve minimumun altına düşünce
  `StockFellBelowMinimumEvent` yayar; `MaintenanceWorkOrder.AddPart` durumu kontrol
  eder ve `parts_cost`'u yeniden hesaplar; `Complete(...)` `WorkOrderCompletedEvent` yayar.
- İş emri hedefi factory ile garanti: `CreateForVehicle` / `CreateForTrailer`
  (araç **veya** dorse — ikisi birden değil).

## PROMPT 2 (DB) ile hizalama notları

Domain, doğrulanmış PostgreSQL şemasıyla uyumludur; birkaç bilinçli incelik:

- `TargetType` (Vehicle/Trailer) ≙ DB `work_order_target_type`. (PROMPT 1'deki
  `EntityType` bununla değiştirildi.)
- `VehicleStatus`/`TrailerStatus` ayrı enum'lardır; DB'de tek `asset_status`'a
  eşlenir (aynı üyeler). `WorkOrderType` ≙ `maintenance_type`,
  `AlertPriority` ≙ `alert_severity`.
- `User.Status` (`UserStatus`) zengin modeldir; DB'deki `users.is_active` ile
  Persistence katmanında eşlenir (Active ↔ true) **veya** kolon `user_status`'a yükseltilir.
- `Warehouse`/`Supplier` ve assignment'lar `AuditableEntity`'den soft-delete
  alanları taşır; bu tablolarda DB'de soft-delete kolonu yoksa Persistence
  konfigürasyonu bu alanları `Ignore` eder (ya da kolonlar eklenir).
- `total_cost`/`line_total` domain'de hesaplanan (computed) property; DB'de
  `GENERATED` kolon. `work_order_no`/`movement_no`/`balance_after` üretimi
  Persistence/servis (veya DB trigger) tarafında `Assign*`/`StampBalance` ile set edilir.

> Bu eşleme detayları, EF Core entity konfigürasyonları (ör. value converter,
> `Ignore`, `HasColumnName`) ile sonraki Persistence prompt'unda kesinleştirilir.
