# ECS Fleet — Deployment & Operasyon Rehberi (PROMPT 8)

## 1. Ortamlar

| Ortam | `ASPNETCORE_ENVIRONMENT` | Amaç | Demo seed |
|------|--------------------------|------|-----------|
| local | `Development` | Geliştirici makinesi | İstege bağlı |
| development | `Development` | Paylaşımlı dev | Evet |
| staging | `Staging` | Prod benzeri doğrulama | Evet |
| production | `Production` | Canlı | **Hayır** |

Konfig önceliği: `appsettings.json` → `appsettings.{Environment}.json` →
**environment variable** (en yüksek). Sırlar **yalnızca** env variable / secret
manager ile verilir; repoya yazılmaz.

## 2. Docker ile çalıştırma

```bash
cp .env.example .env          # JWT_SIGNING_KEY ve POSTGRES_PASSWORD'u doldurun
docker compose up -d --build  # db + migrator + backend + frontend
# Adminer (DB UI) gibi yardımcılar:
docker compose --profile tools up -d
```

Servisler:

- **db** — PostgreSQL 16 (volume `pgdata`, healthcheck `pg_isready`).
- **migrator** — tek seferlik; `scripts/db-migrate.sh` ile migration'ları sırayla
  uygular (şema varsa atlar), `RUN_DEMO_SEED=true` ise demo seed'i yükler.
- **backend** — .NET 8 API (`:5080`→`8080`), rapor PDF'leri `reportstorage`
  volume'unda, loglar `./logs`'ta. `db` healthy + `migrator` tamamlanınca başlar.
- **frontend** — nginx + Vite build (`:8080`), `/api` çağrılarını backend'e proxy'ler.

URL'ler: API `http://localhost:5080`, Swagger `…/swagger` (non-prod), uygulama
`http://localhost:8080`, sağlık `http://localhost:5080/health`.

## 3. Environment variable listesi

| Değişken | Zorunlu | Açıklama |
|---------|:------:|---------|
| `ASPNETCORE_ENVIRONMENT` | | local/Development/Staging/Production |
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | ✔ | DB adı/kullanıcı/parola |
| `ConnectionStrings__Default` | ✔ | Backend bağlantı cümlesi (compose otomatik kurar) |
| `Jwt__SigningKey` | ✔ | **≥ 32 karakter** rastgele sır (`openssl rand -base64 48`) |
| `Jwt__Issuer` / `Jwt__Audience` | | JWT issuer/audience |
| `FileStorage__RootPath` | | PDF/dosya kök dizini (container'da `/app/storage`) |
| `Reporting__VatRate` | | KDV oranı (örn. `0.20`) |
| `Reporting__Company__*` | | Rapor başlığı firma bilgileri |
| `Cors__Origins__0` | | İzin verilen frontend origin'i |
| `VITE_API_BASE_URL` | | SPA'ya build anında gömülen API kökü (`/api`) |
| `RUN_DEMO_SEED` | | `true` ⇒ demo seed (yalnız non-prod) |

> ASP.NET Core iç içe anahtarları `__` ile eşler: `Jwt:SigningKey` → `Jwt__SigningKey`.

## 4. Migration stratejisi

Şemanın kaynağı `database/migrations/*.sql` (tablolar + constraint + trigger +
function + view). EF Core "database-first" eşlenir.

- **local/dev/staging:** `migrator` servisi otomatik uygular; demo seed opsiyonel.
- **production (dikkat):**
  1. Migration'ı **staging'de** uçtan uca doğrula.
  2. **Önce yedek al** (`scripts/backup.sh`), tercihen bakım penceresinde.
  3. Migration'ı **tek transaction** + `ON_ERROR_STOP=1` ile uygula; geri alma
     planı (yedekten restore) hazır olsun.
  4. Üretimde **demo seed asla** çalıştırılmaz (`RUN_DEMO_SEED=false`).
  5. İlk kurulumdan sonra bootstrap admin parolasını değiştir
     (`admin@ecslog.com` / `Admin123!` yalnızca ilk giriş içindir).

## 5. Logging stratejisi

Serilog ile **hem console hem dosya**ya yazılır (loglar `./logs` volume'unda kalıcı):

- **Development/Staging:** insan-okunur metin (console) + günlük rolling dosya
  (`logs/ecs-YYYYMMDD.log`, 14 gün saklanır).
- **Production** (`appsettings.Production.json`): **Compact JSON** (CLEF) formatı —
  console + `logs/ecs-*.json` (30 gün). Bu format merkezi log sistemlerine
  (Elastic/Seq/Loki/CloudWatch) doğrudan ingest edilebilir; `RequestId`,
  `SourceContext`, exception alanları yapılandırılmış olarak taşınır.
- HTTP istek logları `UseSerilogRequestLogging` ile tek satır/özet halinde.
- **Kritik hatalar** `ILogger.LogError` ile; ayrıca iş açısından kritik olaylar
  (örn. başarısız PDF üretimi) hem log'a hem **audit_logs** tablosuna düşer.
- Konteyner stdout'u (console JSON) orkestratör (Docker/K8s) tarafından da toplanabilir.

## 6. Backup stratejisi (PostgreSQL)

- **Mantıksal yedek:** `scripts/backup.sh` → `pg_dump -Fc` (sıkıştırılmış, restore
  edilebilir), bütünlük kontrolü (`pg_restore -l`), `RETENTION_DAYS` ile rotasyon.
  Nightly cron:
  ```cron
  0 2 * * *  PGHOST=db PGPASSWORD=... BACKUP_DIR=/var/backups/ecs RETENTION_DAYS=14 /opt/ecs/scripts/backup.sh >> /var/log/ecs-backup.log 2>&1
  ```
- **Restore:** `scripts/restore.sh <dump>` (`--clean --if-exists --single-transaction`).
  Geri yüklemeyi **periyodik olarak staging'de test edin** — test edilmemiş yedek
  yedek değildir.
- **Off-site:** dump'ları nesne depolamaya (S3/Backblaze) kopyalayın; şifreleyin.
- **PITR (öneri):** kritik üretimde `pg_dump`'a ek olarak WAL arşivleme +
  base backup (pgBackRest) ile noktasal kurtarma.
- **Rapor PDF'leri:** `reportstorage` volume'u da yedeklenmeli (ya da PDF'ler her
  zaman iş emrinden yeniden üretilebilir — bkz. `docs/REPORTING.md §10`).

## 7. Deployment checklist

**Yayın öncesi**
- [ ] `JWT_SIGNING_KEY` güçlü ve ortama özel; repoda değil.
- [ ] `POSTGRES_PASSWORD` güçlü; bağlantı cümlesi env'den geliyor.
- [ ] `ASPNETCORE_ENVIRONMENT=Production`; Swagger kapalı (sadece Development'ta açık).
- [ ] `Cors__Origins` yalnızca gerçek frontend origin(ler)i.
- [ ] HTTPS terminasyonu (reverse proxy/ingress) ve HSTS aktif.
- [ ] `RUN_DEMO_SEED=false`.
- [ ] DB yedeği alındı; migration staging'de doğrulandı.

**Yayın**
- [ ] `docker compose pull/build` ve `up -d`; `migrator` başarıyla tamamlandı.
- [ ] `/health` 200 dönüyor; frontend açılıyor; login çalışıyor.
- [ ] Admin parolası atandı; örnek bir iş emri akışı + PDF indirme denendi.

**Yayın sonrası**
- [ ] Loglar merkezi sisteme akıyor; hata oranı normal.
- [ ] Nightly backup cron'u doğrulandı; restore tatbikatı planlandı.
- [ ] Kaynak/limit (CPU/RAM), disk (storage + logs) izleniyor.

## 8. Ölçekleme notları

- Backend stateless'tir (PDF'ler volume/nesne deposunda, oturum JWT'de) → yatay
  ölçeklenebilir; paylaşılan dosya depolama için `IFileStorage`'ı S3/Blob ile
  değiştirin.
- DB tek yazıcı; okuma replikası + bağlantı havuzu (PgBouncer) ile okuma ölçeklenir.
- Arka plan uyarı job'ları (kritik stok / yaklaşan bakım) tek örnekte çalışmalı
  (idempotent üretim dedup index'lerle korunur).
