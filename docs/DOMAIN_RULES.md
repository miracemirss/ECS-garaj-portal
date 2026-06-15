# İş Kuralları → Katman Eşlemesi (Domain Rules Mapping)

> Bu doküman, prompt paketinde tanımlanan **ana iş kurallarının** hangi katmanda
> ve hangi mekanizmayla uygulanacağını gösterir. PROMPT 1'de iskelet ve
> sözleşmeler hazırlanır; uygulama sonraki prompt'larda belirtilen yerlere yazılır.

## Entity ve İlişki Modeli (özet)

```
Driver ───(DriverVehicleAssignment: history)──── Vehicle ───(VehicleTrailerAssignment: history)──── Trailer
                                                     │
                                                     │  hedef (target)
                                                     ▼
                                            MaintenanceWorkOrder ──< WorkOrderItem >── Part ──< StockMovement
                                                     │                                   │
                                              (Vehicle XOR Trailer)              (stok yalnız hareketle değişir)
```

- **Vehicle** ve **Trailer** ayrı entity'lerdir.
- **Assignment** entity'leri ilişkiyi *history* olarak tutar: her kayıtta
  `StartDateUtc`, `EndDateUtc?` bulunur; aktif kayıt `EndDateUtc == null` olandır.
- **MaintenanceWorkOrder** hedefi ya Vehicle ya Trailer'dır (ikisi birden olamaz).
- **Part** stok seviyesi yalnızca **StockMovement** üzerinden değişir.

## Kural Matrisi

| # | İş Kuralı | Uygulandığı Katman | Mekanizma / Not |
| --- | --- | --- | --- |
| 1 | Araç ve dorse ayrı entity | Domain | `Vehicle` ve `Trailer` ayrı sınıflar |
| 2 | Araç-dorse ilişkisi history mantığıyla | Domain + Persistence | `VehicleTrailerAssignment` (Start/End tarihli) |
| 3 | Şoför-araç ilişkisi history mantığıyla | Domain + Persistence | `DriverVehicleAssignment` (Start/End tarihli) |
| 4 | Bir araç aynı anda tek aktif dorseye | Application + DB | Atama servisinde kontrol + DB unique filtered index (`EndDateUtc IS NULL`) |
| 5 | Bir dorse aynı anda tek aktif araca | Application + DB | Aynı: servis kontrolü + filtered unique index |
| 6 | Bir şoför aynı anda tek aktif araca | Application + DB | Aynı desen, `DriverVehicleAssignment` üzerinde |
| 7 | İş emri araç **veya** dorse için açılır | Domain | `EntityType` enum + hedef alanları |
| 8 | İş emrinde araç ve dorse aynı anda hedef **olamaz** | Domain | Invariant: `VehicleId` XOR `TrailerId` (factory/guard ile) |
| 9 | Bakım öncesi/sonrası KM tutulur | Domain | `WorkOrder.OdometerBefore` / `OdometerAfter` |
| 10 | Parça eklenince stok otomatik düşer | Application (transaction) | `MaintenanceService.AddPart` → `Part.DecreaseStock` + `StockMovement(Out)` |
| 11 | Stok yalnızca hareketle değişir | Domain + Application | Stok set edilmez; her değişim bir `StockMovement` üretir |
| 12 | Stok yetersizse işlem engellenir | Domain | `Part.DecreaseStock` yetersizse `DomainException` |
| 13 | Min. stok altına düşünce kritik uyarı | Domain event + Background | `StockFellBelowMinimumEvent` → `Alert(CriticalStock)` |
| 14 | Bakım zamanı yaklaşan araçlara uyarı | Infrastructure (BackgroundService) | Periyodik tarama → `Alert(MaintenanceDue)` |
| 15 | Bakım tamamlanınca PDF rapor | Application + Infrastructure | `IPdfService.Render(...)` (QuestPDF impl.) |
| 16 | Kritik işlemler audit log'a yazılır | Application | `IAuditLogService.LogAsync(...)` |
| 17 | Controller'da business logic olmaz | Api | Controller ince; orchestration Application'da |
| 18 | Kritik işlemler transaction içinde | Application | `IUnitOfWork.BeginTransactionAsync()` |

## Atomik İşlem Örnekleri (transaction zorunlu)

- **İş emrine parça ekleme:** stok kontrolü → stok düşme → `StockMovement` →
  iş emri kalemi → audit log. Hepsi tek transaction; biri başarısızsa tümü geri alınır.
- **Aktif atama değiştirme:** mevcut aktif kaydı kapat (`EndDateUtc`) → yeni aktif
  kayıt aç. Tek transaction; "aynı anda tek aktif" invariant'ı korunur.
- **İş emri tamamlama:** durum güncelle → `OdometerAfter` yaz →
  `WorkOrderCompletedEvent` → PDF üret → audit log.

## Veritabanı Seviyesi Güvenceler (Persistence)

Uygulama seviyesi kontrollere ek olarak, yarış koşullarına (race condition) karşı
DB seviyesinde de güvence alınır:

- Aktif atamalar için **filtered unique index** (`WHERE end_date IS NULL`).
- Stok miktarı için **CHECK (quantity >= 0)**.
- Para/KM gibi alanlarda uygun `numeric` precision.

> Bu kurallar PROMPT 1'de **yerleri ve sözleşmeleri** ile hazırlanmıştır; somut
> implementasyon ilgili feature prompt'larında bu tabloya göre yazılacaktır.
