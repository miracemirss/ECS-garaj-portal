# Application Servisleri — İş Mantığı Tasarımı (PROMPT 4)

Bu doküman `src/ECS.Application` katmanındaki servislerin sözleşmelerini, iş
akışlarını, transaction sınırlarını, hata senaryolarını, validation kurallarını,
audit noktalarını ve unit test senaryolarını tanımlar. **Tüm çözüm derlenir**
(`dotnet build ECS.sln` → 0 hata).

## Genel konvansiyonlar

- Her servis metodu **`Result` / `Result<T>`** döner. Beklenen sonuçlar
  (`NotFound`, `Validation`, `Conflict`) exception değil `Error` ile taşınır;
  domain invariant ihlalleri (ör. `InsufficientStockException`) güvenlik ağı olarak
  fırlatılır ve `ExceptionHandlingMiddleware` tarafından haritalanır.
- **Validation:** FluentValidation validator'ları metodun başında çalışır
  (`Error.Validation`). Domain factory'leri ikinci savunma hattıdır.
- **EF bağımsızlığı:** Servisler yalnızca `IRepository<T>` / `IUnitOfWork`
  port'larını kullanır; EF tipleri görmez.
- **Transaction:** Çok adımlı kritik işlemler `IUnitOfWork.BeginTransactionAsync()`
  ile sarmalanır (`await using` → hata olursa otomatik rollback).
- **Audit:** Kritik işlemlerde `IAuditLogService.LogAsync(...)` çağrılır.
- **Saat & kullanıcı:** `IDateTimeProvider.UtcNow` ve `ICurrentUserService.UserId`.

## Servis kataloğu

| Servis | Temel sorumluluk |
|--------|------------------|
| `IVehicleService` / `ITrailerService` / `IDriverService` | Ana kayıt CRUD + soft delete |
| `IAssignmentService` | Araç-dorse & şoför-araç history atamaları |
| `IMaintenanceService` | İş emri yaşam döngüsü, parça tüketimi, tamamlama, PDF tetikleme |
| `IInventoryService` | Stok giriş/çıkış/sayım, parça CRUD, kritik stok |
| `IAlertService` | Kritik stok / yaklaşan bakım / belge bitiş uyarıları |
| `IReportService` | Bakım PDF raporu, maliyet özetleri |
| Port'lar | `IPdfService`, `ICurrentUserService`, `IAuditLogService`, `INumberGenerator` |

## Transaction matrisi

| Metod | Transaction | Adımlar |
|-------|:-----------:|---------|
| `MaintenanceService.AddPartAsync` | ✅ | stok düş → `StockMovement(Out)` → `WorkOrderPart` (movement'a 1:1 bağlı) → kritik stok uyarısı → audit |
| `MaintenanceService.CompleteWorkOrderAsync` | ✅ | iş emri tamamla → araç KM/sonraki bakım güncelle → maliyet finalize → audit → (commit sonrası) PDF |
| `AssignmentService.AssignVehicleTrailerAsync` | ✅ | aktif kayıtları kapat (flush) → yeni aktif kayıt aç |
| `AssignmentService.AssignDriverVehicleAsync` | ✅ | aynı desen |
| `InventoryService.ReceiveStockAsync` | ✅ | `StockMovement(In)` + stok artır |
| `InventoryService.IssueStockAsync` | ✅ | stok düş + `StockMovement(Out)` + kritik uyarı |
| `InventoryService.AdjustStockAsync` | ✅ | sayım düzeltme + `StockMovement(Adjustment)` + kritik uyarı |
| Basit CRUD (create/update/delete) | tek `SaveChanges` (atomik) | — |

## Kritik akışlar (pseudo-code)

### AddPartAsync (iş emrine parça ekleme)
```
validate(request)                              -> Error.Validation
wo   = workOrders.GetById(id)                  -> NotFound | Conflict (Completed/Cancelled)
part = parts.GetById(request.PartId)           -> NotFound
unitCost = request.UnitCost ?? part.UnitCost
if part.QuantityInStock < qty                  -> Conflict("Insufficient stock")   # parça eklenmeden engelle
BEGIN TRANSACTION
    part.DecreaseStock(qty)                     # domain: negatif olursa fırlatır (güvenlik ağı)
    mv = StockMovement.Out(part, qty, unitCost, workOrderId)
    mv.StampBalance(part.QuantityInStock)       # bakiye anlık görüntü
    mv.AssignNumber(numbers.NextStockMovementNo())
    movements.Add(mv)
    line = wo.AddPart(part.Id, qty, unitCost, mv.Id)   # PartsCost += lineTotal; movement'a 1:1 bağlı
    workOrderParts.Add(line); parts.Update(part); workOrders.Update(wo)
    if part.IsBelowMinimum: alerts.EnsureCriticalStockAlert(part)   # aynı tx içinde
    SaveChanges()                               # tek atomik kayıt
    audit("WorkOrderPartAdded")
COMMIT
```
> Garanti: stok düşmeden `work_order_part` oluşmaz; `stock_movement` oluşmadan stok
> değişmez; herhangi bir adım patlarsa **tümü** geri alınır.

### CompleteWorkOrderAsync (iş emri tamamlama)
```
wo = workOrders.GetById(id)                    -> NotFound
BEGIN TRANSACTION
    wo.Complete(odometerAfter, now)            -> Conflict (geçersiz durum) | Validation (km<önce)
    if target=Vehicle && odometerAfter:        vehicle.RecordMaintenanceCompletion(odo, today)
    SaveChanges(); audit("WorkOrderCompleted")
COMMIT
# PDF, IO olduğu için DB transaction'ı DIŞINDA, commit sonrası üretilir (best-effort,
# tekrar üretilebilir). Tamamlama PDF'e bağlı değildir.
reportService.GenerateMaintenanceReport(wo.Id)
```

### AssignVehicleTrailerAsync (eşleşme değiştirme)
```
validate; vehicle & trailer var mı            -> Validation | NotFound
BEGIN TRANSACTION
    actives = vta.List(ended==null && (vehicleId|trailerId))
    foreach a in actives: a.End(now, user)
    if actives.Any: SaveChanges()              # ÖNCE kapat ve flush -> partial unique çakışmaz
    new = VehicleTrailerAssignment.Start(vehicleId, trailerId, now)
    vta.Add(new); SaveChanges(); audit("VehicleTrailerAssigned")
COMMIT
```
> DB tarafındaki **partial unique index** yarış koşullarına karşı nihai garanti.

### Alert üretimi (background job tetikler)
```
GenerateCriticalStockAlerts():    parts(stok<=min) için dedup 'stock:{id}' ile Open alert
GenerateUpcomingMaintenanceAlerts(): company_settings eşiğine göre vehicles, dedup 'maint:{id}'
GenerateDocumentExpiryAlerts():   expiry<=today+esik documents, dedup 'doc:{id}'
# Hepsi idempotent: aynı subject için ikinci kez Open alert üretmez.
```

## Hata senaryoları

| Senaryo | Sonuç |
|---------|-------|
| Geçersiz girdi | `Error.Validation` (400) |
| Kayıt yok | `Error.NotFound` (404) |
| Yetersiz stok | `Error.Conflict` (409) — işlem başlamadan engellenir |
| Tamamlanmış/iptal iş emrine parça | `Error.Conflict` (409) |
| Zaten kapalı atamayı kapatma | `Error.Conflict` (409) |
| Plaka/parça no/e-posta tekrarı | `Error.Conflict` (409) + DB unique backstop |
| Geçersiz iş emri durumu geçişi | `InvalidWorkOrderStateException` → Conflict |
| Beklenmeyen hata | transaction rollback, middleware 500 |

## Audit log noktaları

`*Created/Updated/Deleted`, `WorkOrderStarted/PartAdded/Completed`,
`StockReceived/Issued/Adjusted`, `VehicleTrailerAssigned`, `DriverVehicleAssigned`,
`*AssignmentEnded`, `AlertResolved`, `MaintenanceReportGenerated`,
`*AlertsGenerated`. Satır seviyesi eski/yeni değerler ayrıca **DB audit trigger**'ı
ile `audit_logs` tablosuna yazılır.

## Stok otoritesi (PROMPT 2 ile uyum — önemli)

PROMPT 4'te **stok otoritesi Application'dır**: `Part` + `InventoryService` /
`MaintenanceService` stoğu hesaplar ve `QuantityInStock` ile `StockMovement.BalanceAfter`
değerlerini yazar; `INumberGenerator` numara üretir. PROMPT 2'deki **stok uygulayan**
trigger (`prevent_negative_stock`'un `parts` UPDATE'i) ve guard, doğrudan-SQL kullanımı
için **opsiyonel alternatif** yoldur. Uygulama üzerinden çalışırken çift sayımı önlemek
için bu trigger **validate-only** moda alınmalı (negatiflik/`CHECK (quantity_in_stock >= 0)`
korunur). `INumberGenerator` PostgreSQL sequence'lerini (`seq_work_order_no`,
`seq_stock_movement_no`) kullanır.

## Unit test senaryoları

Servisler saf port'lara bağlı olduğundan repository/uow/clock/audit **mock'lanarak**
izole test edilir.

**MaintenanceService.AddPartAsync**
- ✅ Yeterli stok → stok `qty` kadar düşer, `StockMovement(Out, -qty)` üretilir,
  `WorkOrderPart.StockMovementId` set edilir, `PartsCost` artar, audit çağrılır.
- ✅ Yetersiz stok → `Conflict`, hiçbir kayıt oluşmaz (Add* çağrılmaz), commit yok.
- ✅ Tamamlanmış iş emri → `Conflict`.
- ✅ Parça yok → `NotFound`.
- ✅ Stok minimuma düşerse → `EnsureCriticalStockAlert` çağrılır.

**MaintenanceService.CompleteWorkOrderAsync**
- ✅ Open/InProgress → `Completed`, `CompletedAt` set, araç KM ileri alınır,
  `next_maintenance` yeniden hesaplanır, PDF üretimi tetiklenir.
- ✅ odometerAfter < odometerBefore → `Validation`.
- ✅ Zaten Completed → `Conflict`.
- ✅ PDF üretimi patlasa bile tamamlama başarılı kalır (best-effort).

**InventoryService**
- ✅ Receive → stok artar, `StockMovement(In)` ve `BalanceAfter` doğru.
- ✅ Issue yetersiz stok → `Conflict`.
- ✅ Adjust negatife düşürür → `Conflict`.

**AssignmentService**
- ✅ Yeni eşleşme → eski aktif kayıt(lar) kapanır, tek aktif kayıt kalır.
- ✅ Zaten kapalı atamayı kapatma → `Conflict`.
- ✅ Var olmayan araç/dorse/şoför → `NotFound`.

**AlertService**
- ✅ İlk üretim alert oluşturur; ikinci üretim (aynı subject) idempotent (0 yeni).
- ✅ Acknowledge/Resolve/Dismiss durum geçişleri.
