# ECS Fleet — PDF Bakım/Tamir Raporu (PROMPT 7)

Bir bakım iş emri **tamamlandığında** otomatik olarak kurumsal bir PDF
bakım/tamir raporu üretilir, dosya sisteminde saklanır ve metadata'sı
`report_files` tablosuna yazılır. Rapor sonradan tekrar indirilebilir.

> PDF motoru **QuestPDF** (Community lisans). Üretim Infrastructure katmanında,
> orkestrasyon Application katmanındadır (mimari altın kurallara uygun).

## 1. Mimari yerleşim

| Bileşen | Katman | Dosya |
|--------|--------|------|
| `IPdfService` / `IPdfDocumentModel` | Application (port) | `Common/Interfaces/IPdfService.cs` |
| `IFileStorage` (dosya saklama portu) | Application (port) | `Common/Interfaces/IFileStorage.cs` |
| `IQrCodeGenerator` (QR portu) | Application (port) | `Common/Interfaces/IQrCodeGenerator.cs` |
| `ReportingOptions` (firma + KDV + logo) | Application (config) | `Common/Options/ReportingOptions.cs` |
| `MaintenanceReportModel` (DTO) | Application | `Features/Reports/Models/MaintenanceReportModel.cs` |
| `ReportService` (model kurma + saklama) | Application | `Features/Reports/ReportService.cs` |
| `MaintenanceReportDocument` (QuestPDF layout) | Infrastructure | `Pdf/MaintenanceReportDocument.cs` |
| `QuestPdfReportService` (`IPdfService` impl) | Infrastructure | `Pdf/QuestPdfReportService.cs` |
| `QrCodeGenerator` (QRCoder impl) | Infrastructure | `Pdf/QrCodeGenerator.cs` |
| `ReportBrandAssets` (logo yükleme) | Infrastructure | `Pdf/ReportBrandAssets.cs` |
| `LocalFileStorage` (`IFileStorage` impl) | Infrastructure | `Storage/LocalFileStorage.cs` |
| `ReportsController` (üret / listele / indir) | API | `Controllers/ReportsController.cs` |

## 2. PDF layout tasarımı

A4 · beyaz arka plan · lacivert (`#0B2A5B`) başlıklar · tablolu kurumsal düzen ·
her sayfada tekrar eden header/footer · sayfa numarası · QR doğrulama alanı.

```
┌──────────────────────────────────────────────────────────────────────┐
│ [ECS LOGO]   ECS Europe Cargo Service              Rapor No : RPR-...  │
│              Araç Bakım / Tamir Raporu             İş Emri No: WO-...   │
│              ECS ... Lojistik A.Ş.                 Tarih     : ...      │
│ ───────────────────────────────────────────────────────────────────── │ (lacivert çizgi)
│ ┌── Firma Bilgileri ───────────┐  ┌── Rapor Bilgileri ───────┐         │
│ │ Ünvan / Adres / Vergi / Tel  │  │ Rapor No / Tarih / İş Emri│ [QR]  │
│ └──────────────────────────────┘  │ Durum                     │       │
│ ┌── Araç Bilgileri ────────────┐  ┌── Dorse Bilgileri ───────┐ (varsa)│
│ │ Plaka/Marka/Model/Şasi/KM    │  │ Plaka / Tip / Şasi        │        │
│ └──────────────────────────────┘  └───────────────────────────┘        │
│ ┌── İş Emri Bilgileri ─────────────────────────────────────────────┐   │
│ │ İşlem tipi · Servise giriş · Tamamlanma · Süre · Usta/Servis      │   │
│ │ Açıklama ...                                                      │   │
│ └──────────────────────────────────────────────────────────────────┘   │
│ ┌── Kilometre Bilgileri ──┐                                             │
│ │ Öncesi | Sonrası | Fark │  (3 stat kutusu, yalnızca araç iş emrinde) │
│ Yapılan İşlemler           (lacivert başlıklı tablo: #, açıklama, saat) │
│ Kullanılan Parçalar        (tablo: #, no, ad, miktar, birim, fiyat, ₺) │
│ Maliyet Özeti                       Parça / İşçilik / Diğer / Ara top.  │
│                                     KDV %20 · GENEL TOPLAM (lacivert)   │
│ Sonraki Bakım Önerisi: 250.000 km veya 15.08.2026                      │
│ ____________      ____________      ____________                       │
│ Usta/Teknisyen    Servis Sorumlusu  Araç Yetkilisi/Şoför               │
│ ───────────────────────────────────────────────────────────────────── │
│ ECS ... A.Ş. | Bu rapor sistem tarafından üretilmiştir.   Sayfa 1 / 1  │
└──────────────────────────────────────────────────────────────────────┘
```

## 3. DTO modeli

`MaintenanceReportModel` immutable bir kayıttır; Application kurar, Infrastructure
sadece çizer. Bölümler: `Header`, `Company`, `Vehicle?`, `Trailer?`, `WorkOrder`,
`Odometer`, `Tasks[]`, `Parts[]`, `Cost` (KDV dahil), `NextMaintenanceRecommendation`,
`VerificationPayload` (QR içeriği).

## 4. PDF oluşturma methodu

`ReportService.GenerateMaintenanceReportAsync(workOrderId)`:

1. İş emrini yükle; **yalnızca `Completed`** ise devam et (aksi halde `Conflict`).
2. `BuildModelAsync` ile araç/dorse/usta/servis/parça/işlem/firma verisini topla,
   KDV ve genel toplamı hesapla, sonraki bakım önerisini ve QR payload'unu üret.
3. `IPdfService.Render(model)` → QuestPDF `byte[]`.
4. `IFileStorage.SaveAsync("reports/yyyy/MM/<wo>-<timestamp>.pdf", bytes)`.
5. `report_files`'a metadata satırı ekle (path, ad, content-type, boyut, üreten).
6. Audit log: `MaintenanceReportGenerated`.

## 5. Logo ekleme yöntemi

`Reporting:LogoPath` ile PNG/JPG verilir; `ReportBrandAssets` startup'ta bir kez
okuyup cache'ler. Dosya yoksa **tipografik ECS rozeti** (lacivert kutu) fallback
çizilir — eksik asset raporu asla bozmaz. QuestPDF içinde `.Image(bytes).FitArea()`.

## 6. Tablo tasarımları

`table.ColumnsDefinition` ile sabit/oransal kolonlar; `table.Header` lacivert zemin
beyaz yazı; gövde satırlarında zebra (`#F4F6F9`) ve ince alt çizgi. İşçilik/fiyat
kolonları sağa, sıra/birim/durum ortaya hizalı. Boş tablolarda kibar bir not.

## 7. Sayfa numarası yapısı

Footer her sayfada tekrar eder: `Sayfa <CurrentPageNumber> / <TotalPages>`
(QuestPDF `text.CurrentPageNumber()` / `text.TotalPages()`).

## 8. QR / barkod alanı önerisi

- **QR (varsayılan):** `QRCoder` `PngByteQRCode` ile üretilir — `System.Drawing`
  gerektirmez, headless Linux container'da çalışır. İçerik:
  `"{VerificationBaseUrl}?wo={workOrderNo}"`. Rapor bilgileri kartının sağ üstünde.
- **Barkod alternatifi:** İş emri numarasını Code128 olarak basmak istenirse
  `IQrCodeGenerator` portunun yanına bir `IBarcodeGenerator` portu eklenebilir
  (örn. `BarcodeLib`/`ZXing.Net`), aynı şekilde Infrastructure'da implemente edilir.

## 9. Dosya kaydetme stratejisi

`report_files` tablosu **metadata** tutar (`file_path`, `file_name`, `content_type`,
`size_bytes`, `generated_by`); ikili içerik **`IFileStorage`** kökünde
(`FileStorage:RootPath`, varsayılan `storage/`) `reports/yyyy/MM/...pdf` olarak
saklanır. Bu sayede DB şişmez, dosyalar yedeklenebilir/CDN'e taşınabilir ve port
ileride S3/Azure Blob ile değiştirilebilir. Yol gezinme (path traversal) reddedilir.

## 10. Tekrar indirilebilirlik

`GET /api/reports/files/{reportFileId}/download` dosyayı depodan okur. Dosya
silinmişse ve iş emri hâlâ `Completed` ise **yeniden render edilip** aynı yola
yazılır (best-effort dayanıklılık), böylece indirme kesintisiz çalışır.

## 11. Transaction stratejisi (kritik)

> İş emri tamamlanma **transaction'ı PDF üretimine bağlı değildir.**

`MaintenanceService.CompleteWorkOrderAsync`:

1. `BeginTransaction` → iş emrini `Complete`, araç KM güncelle → `SaveChanges` →
   audit (`WorkOrderCompleted`) → **`Commit`**.
2. **Commit sonrası** (transaction dışında) `GenerateMaintenanceReportAsync` çağrılır.
   - Başarısızsa: `ILogger` ile loglanır + `MaintenanceReportGenerationFailed` audit
     kaydı düşülür; **iş emri tamamlanmış kalır**.
   - Rapor sonradan `POST /api/reports/maintenance/{id}` ile tekrar üretilebilir.

Gerekçe: PDF üretimi IO'dur ve nadiren de olsa başarısız olabilir; tamamlama gibi
kritik bir iş kuralının PDF'e bağımlı olması (ya da PDF hatası yüzünden rollback
olması) kabul edilemez. Idempotent yeniden üretim bu riski tamamen ortadan kaldırır.

## 12. API uçları

| Metot | Yol | Açıklama |
|------|-----|---------|
| POST | `/api/reports/maintenance/{workOrderId}` | Raporu üret/yeniden üret (201 + metadata) |
| GET  | `/api/reports/maintenance/{workOrderId}/files` | İş emrinin raporlarını listele |
| GET  | `/api/reports/files/{reportFileId}/download` | PDF'i indir (`application/pdf`) |

```bash
# Üret
curl -X POST http://localhost:5080/api/reports/maintenance/<WO_ID> \
     -H "Authorization: Bearer <token>"

# İndir
curl -OJ http://localhost:5080/api/reports/files/<REPORT_ID>/download \
     -H "Authorization: Bearer <token>"
```

## 13. Konfigürasyon

```jsonc
"FileStorage": { "RootPath": "storage" },
"Reporting": {
  "VatRate": 0.20, "DefaultCurrency": "TRY", "LogoPath": "",
  "StorageFolder": "reports",
  "VerificationBaseUrl": "https://portal.ecs.local/reports/verify",
  "Company": { "Name": "ECS Europe Cargo Service", "LegalName": "...", "Address": "...",
               "TaxOffice": "...", "TaxNo": "...", "Phone": "...", "Email": "...", "Website": "..." }
}
```

Container'da Türkçe tarih/saat için `tzdata` (Europe/Istanbul) kurulu olmalıdır;
yoksa raporda UTC değeri gösterilir (rapor yine üretilir).
