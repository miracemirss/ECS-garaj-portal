# ECS Fleet — REST API Tasarımı (PROMPT 5)

`ECS.Api` (controller/sunum) + `ECS.Application` (servis/DTO/validation) katmanları.
Tüm çözüm derlenir (`dotnet build ECS.sln` → 0 uyarı, 0 hata). Controller'lar
**incedir**: request alır → servis çağırır → standart response döner.

## Standart response zarfı

```jsonc
{
  "success": true,
  "message": null,
  "data": { /* payload | null */ },
  "errors": null,            // hata/validation durumunda dolu
  "statusCode": 200,
  "traceId": "0HN..."        // her isteğe özgü iz
}
```
- Başarılı: `success=true`, `data` dolu (veya boş işlemde null).
- Hatalı: `success=false`, `message` + `errors`, uygun `statusCode`, `traceId`.

## Pagination payload (`data` içinde)

```jsonc
{ "page": 1, "pageSize": 20, "totalCount": 134, "totalPages": 7, "items": [ /* ... */ ] }
```
Query: `?page=1&pageSize=20` (pageSize üst sınır 200, geçersiz değerler güvenli aralığa çekilir).

## Hata standardı (Error.Code → HTTP)

| Code | HTTP | Anlam |
|------|------|-------|
| Validation | 400 | Girdi hatalı (`errors` = mesaj listesi) |
| Unauthorized | 401 | Kimlik doğrulama başarısız |
| (policy) | 403 | Rol yetersiz |
| NotFound | 404 | Kayıt yok |
| Conflict | 409 | İş kuralı/çakışma (ör. yetersiz stok) |
| Failure | 500 | Beklenmeyen |

Tüm beklenmeyen hatalar **global `ExceptionHandlingMiddleware`** ile aynı zarfa
(+`traceId`) çevrilir; `ValidationException` 400, `NotFoundException` 404,
`DomainException` 409.

## Kimlik doğrulama akışı

1. `POST /api/auth/login` → `accessToken` (JWT, kısa ömür) + `refreshToken` (rotasyonlu).
2. İsteklerde `Authorization: Bearer {accessToken}`.
3. `POST /api/auth/refresh` → access süresi dolunca yenile (eski refresh revoke edilir).
4. `POST /api/auth/logout` → refresh token iptal.

## Yetki politikaları (role bazlı)

| Policy | İzinli roller |
|--------|---------------|
| `AdminOnly` | Admin |
| `ManageFleet` | Admin, FleetManager |
| `ManageMaintenance` | Admin, FleetManager, Technician |
| `ManageInventory` | Admin, WarehouseManager |
| `ViewReports` | Kimliği doğrulanmış herkes |

## Endpoint listesi

### AuthController — `api/auth`
| Method | Route | Yetki |
|--------|-------|-------|
| POST | `/login` | Anonim |
| POST | `/refresh` | Anonim |
| POST | `/logout` | Authenticated |
| GET | `/me` | Authenticated |

### VehiclesController — `api/vehicles` · TrailersController — `api/trailers` · DriversController — `api/drivers`
| Method | Route | Yetki |
|--------|-------|-------|
| GET | `/?page&pageSize` | Authenticated |
| GET | `/{id}` | Authenticated |
| POST | `/` | ManageFleet |
| PUT | `/{id}` | ManageFleet |
| DELETE | `/{id}` | ManageFleet |

### VehicleTrailerAssignmentsController — `api/vehicletrailerassignments` (ManageFleet)
| Method | Route | Açıklama |
|--------|-------|----------|
| POST | `/` | Eşleştir (eski aktifi kapatır) |
| POST | `/{id}/end` | İlişkiyi kapat |
| GET | `/active?vehicleId=` | Aktif dorse |

### DriverVehicleAssignmentsController — `api/drivervehicleassignments` (ManageFleet)
| POST `/` | atama | · | POST `/{id}/end` | kapat | · | GET `/active?driverId=` | aktif araç |

### MaintenanceWorkOrdersController — `api/maintenanceworkorders`
| Method | Route | Yetki |
|--------|-------|-------|
| GET | `/?page&pageSize` · `/{id}` | Authenticated |
| POST | `/` | ManageMaintenance |
| PUT | `/{id}` | ManageMaintenance |
| POST | `/{id}/start` · `/{id}/tasks` · `/{id}/parts` · `/{id}/complete` | ManageMaintenance |

### PartsController — `api/parts`
| GET `/?page&pageSize` · `/critical` · `/{id}` | Authenticated | · | POST `/` · PUT `/{id}` | ManageInventory |

### StockMovementsController — `api/stockmovements` (ManageInventory)
| POST `/receive` · `/issue` · `/adjust` | stok giriş / çıkış / sayım |

### ReportsController — `api/reports` (ViewReports)
| POST `/maintenance/{workOrderId}` | bakım PDF | · | GET `/vehicle-costs` · `/monthly-costs` | özet |

### AlertsController — `api/alerts`
| GET `/open` · POST `/{id}/acknowledge` · `/resolve` · `/dismiss` | Authenticated |
| POST `/generate/critical-stock` | ManageInventory |
| POST `/generate/maintenance-due` · `/generate/document-expiry` | ManageFleet |

### SettingsController — `api/settings`
| GET `/` | Authenticated | · | PUT `/` | AdminOnly |

## DTO ayrımı

- **Create vs Update:** ayrı record'lar (ör. `CreateVehicleRequest` / `UpdateVehicleRequest`).
- **List vs Detail:** liste sayfaları temel alanlı DTO döner; detay (ör. iş emri)
  alt koleksiyonları (parça satırları) içerir. Zengin liste/rapor görünümleri için
  PROMPT 2 view'ları kullanılır.
- **Response DTO'ları** entity'leri asla doğrudan sızdırmaz.

## Validation kuralları (FluentValidation)

| Alan | Kural | Konum |
|------|------|-------|
| Araç plaka | zorunlu + unique | `CreateVehicleRequestValidator` + servis unique kontrolü + DB unique |
| Araç şasi (VIN) | unique | DB unique + servis |
| Araç güncel KM | negatif olamaz | Domain (`Vehicle`) |
| Sonraki bakım KM | mevcut KM'den küçük olamaz | Domain (`RecordMaintenanceCompletion`) |
| Dorse plaka | zorunlu + unique | `CreateTrailerRequestValidator` + servis + DB |
| Dorse kapasite | negatif olamaz | validator + domain |
| Şoför ad/soyad | zorunlu | `CreateDriverRequestValidator` |
| Şoför e-posta | format | validator (`EmailAddress`) |
| İş emri hedef | araç **veya** dorse, ikisi değil | Domain factory (`CreateForVehicle/Trailer`) + DB CHECK |
| İş emri KM giriş | negatif olamaz | validator + domain |
| İş emri KM çıkış | girişten küçük olamaz | Domain (`Complete`) |
| Parça adı | zorunlu | `CreatePartRequestValidator` |
| Parça kodu | unique | servis + DB unique |
| Stok miktarı | negatif olamaz | Domain + DB CHECK |
| Stok çıkış | mevcuttan fazla olamaz | servis ön-kontrol + Domain (`DecreaseStock`) |

> Not: Bazı kurallar (KM, stok negatifliği) doğal olarak **domain seviyesinde**
> uygulanır çünkü ilgili alanlar create/update DTO girişinde yoktur; API
> validator'ları girdi-seviyesi kuralları, domain ise invariant'ları korur.

## Controller örneği (ince)

```csharp
[Authorize(Policy = ApiPolicies.ManageMaintenance)]
[HttpPost("{id:guid}/parts")]
public async Task<IActionResult> AddPart(Guid id, AddWorkOrderPartRequest request, CancellationToken ct)
    => Respond(await _service.AddPartAsync(id, request, ct), StatusCodes.Status201Created);
```

## Çalıştırma notu

API katmanı yapısal olarak tamamlandı ve derleniyor. Uçtan uca **çalışma**
(DB'ye karşı) için EF Core `DbContext` eşlemesi (DbSet'ler + `IEntityTypeConfiguration`
+ migration) gerekir — bu ayrı bir Persistence prompt'unda tamamlanacaktır.
JWT için `appsettings.json > Jwt:SigningKey` üretimde güçlü bir değerle
değiştirilmelidir; seed admin parolası uygulamanın PBKDF2 hasher'ı ile set edilmelidir.
