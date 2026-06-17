# ECS Fleet — PostgreSQL Veritabanı Tasarımı (PROMPT 2)

Bu klasör, sistemin **PostgreSQL** şemasını sıralı migration dosyaları olarak içerir.
Tüm script'ler **PostgreSQL 16** üzerinde uçtan uca çalıştırılıp doğrulanmıştır
(tablolar + constraint'ler + partial unique index'ler + trigger/function'lar +
view'lar + seed + smoke test).

## Migration sırası

Dosyalar isimlerindeki numara sırasına göre çalıştırılır:

| # | Dosya | İçerik |
|---|-------|--------|
| 0001 | `0001_extensions_and_enums.sql` | Extension'lar (pgcrypto, citext, pg_trgm) + tüm `ENUM` tipleri |
| 0002 | `0002_identity.sql` | `users`, `roles`, `user_roles`, `refresh_tokens` |
| 0003 | `0003_fleet.sql` | `vehicles`, `trailers`, `drivers` |
| 0004 | `0004_assignments.sql` | `vehicle_trailer_assignments`, `driver_vehicle_assignments` (+ partial unique) |
| 0005 | `0005_inventory_base.sql` | `warehouses`, `suppliers`, `parts` |
| 0006 | `0006_maintenance.sql` | `maintenance_work_orders`, `maintenance_tasks` |
| 0007 | `0007_stock_and_work_order_parts.sql` | `stock_movements`, `work_order_parts` |
| 0008 | `0008_operational.sql` | `alerts`, `audit_logs`, `company_settings`, `documents`, `report_files` |
| 0009 | `0009_functions_and_triggers.sql` | Tüm function ve trigger'lar |
| 0010 | `0010_views.sql` | 13 raporlama view'ı |
| 0011 | `0011_seed.sql` | Roller, admin kullanıcı, company_settings, varsayılan depo (idempotent) |
| 0012 | `0012_soft_delete_partial_unique.sql` | Soft-delete uyumlu **partial unique index**'ler (plaka, VIN, TC, parça kodu) |

## Çalıştırma

```bash
createdb ecs_fleet
for f in database/migrations/0*.sql; do
  psql -v ON_ERROR_STOP=1 -d ecs_fleet -f "$f"
done
```

## Tasarım konvansiyonları

- **PK:** Tüm tablolarda `uuid` (`gen_random_uuid()`), backend `Guid` ile birebir.
  İstisna: yüksek hacimli `audit_logs` → `bigint identity`.
- **Audit kolonları:** Ana entity'lerde `created_at/by`, `updated_at/by` +
  **soft delete** (`is_deleted`, `deleted_at/by`). Ledger/log tabloları (örn.
  `stock_movements`, `audit_logs`) append-only'dir.
- **Para/ölçü:** `numeric(14,2)` (para), `numeric(14,3)` (stok miktarı).
- **Kullanıcı bağlamı (audit & stok):** Backend her transaction başında
  oturum değişkeni set eder; trigger'lar bunu okur:
  ```sql
  SET LOCAL ecs.user_id   = '<uuid>';
  SET LOCAL ecs.user_name = '<ad soyad>';
  -- (opsiyonel) SET LOCAL ecs.client_ip = '<ip>';
  ```

## Kritik kuralların nerede uygulandığı

| Kural | Uygulama |
|------|----------|
| Araç ve dorse ayrı | `vehicles` ve `trailers` ayrı tablolar |
| Araç-dorse / şoför-araç history | `*_assignments` tabloları (`started_at`/`ended_at`) |
| Aktif eşleşme tekliği | **Partial unique index** `... WHERE ended_at IS NULL` |
| İş emri tek hedef | `ck_wo_target` CHECK (target_type ↔ vehicle_id XOR trailer_id) |
| Parça + stok birlikte | `add_work_order_part()` + `work_order_parts.stock_movement_id` (1:1, UNIQUE) |
| Stok yalnız hareketle değişir | `fn_guard_part_stock` (INSERT/UPDATE'i bloklar) + `prevent_negative_stock` |
| Stok negatif olamaz | `prevent_negative_stock` trigger'ı + `CHECK (quantity_in_stock >= 0)` |
| Kritik stok | `minimum_stock` + `generate_critical_stock_alerts()` + `vw_critical_stocks` |
| Audit JSONB eski/yeni | `fn_audit` → `audit_logs.old_data/new_data` (`jsonb`) + `changed_columns` |

## View'lar (13)

`vw_vehicle_current_status`, `vw_trailer_current_status`,
`vw_active_vehicle_trailer_assignments`, `vw_active_driver_vehicle_assignments`,
`vw_vehicle_maintenance_history`, `vw_vehicle_cost_summary`,
`vw_trailer_cost_summary`, `vw_critical_stocks`, `vw_upcoming_maintenances`,
`vw_expiring_documents`, `vw_work_order_summary`, `vw_monthly_costs`,
`vw_dashboard_summary`.

## Function & Trigger'lar

**Trigger'lar (veri bütünlüğü / audit / kolaylık):**
`set_updated_at`, `fn_audit`, `fn_guard_part_stock`, `prevent_negative_stock`
(stok hareketinde bakiye hesaplama + negatif engelleme), `fn_set_work_order_no`,
`fn_recalc_work_order_parts_cost`.

**Generator / alert function'ları:**
`generate_work_order_no`, `generate_stock_movement_no`,
`generate_critical_stock_alerts`, `generate_upcoming_maintenance_alerts`
(son ikisi backend BackgroundService tarafından periyodik çağrılır).

**İşlem (operation) function'ları — OPSİYONEL atomik yardımcılar:**
`add_work_order_part`, `complete_work_order`, `assign_vehicle_trailer`,
`assign_driver_vehicle`.

> ### Mimari not (önemli)
> İş süreçlerinin **sahibi veritabanı değildir.** Ana iş kuralları, doğrulama,
> transaction yönetimi, PDF üretimi ve audit bağlamı **backend Application
> Service** katmanındadır (bkz. `docs/ARCHITECTURE.md`). Buradaki DB yapıları
> şu amaçlarla vardır: **veri bütünlüğü** (constraint + partial unique +
> `prevent_negative_stock` + stok guard), **audit**, **performans** (set-based
> alert üretimi) ve **kolaylık** (numara üretimi). `add_work_order_part`,
> `complete_work_order`, `assign_*` function'ları; backend'in çağırabileceği
> **opsiyonel atomik** alternatiflerdir — backend aynı adımları kendi de
> yapabilir; her iki durumda da yukarıdaki bütünlük garantileri korunur.

## Backend (EF Core) ile hizalama

- **Tablolar** → EF entity'leri (PROMPT 1'deki `ECS.Domain` + `Persistence/Configurations`).
- **View'lar** → `ToView("vw_...")` ve `HasNoKey()` ile salt-okunur read model'ler.
- **Function'lar** → `FromSqlInterpolated` / `ExecuteSqlInterpolated` ile çağrılır.
- **Trigger/function/view'lar EF migration'ı ile ifade edilemez**; bu yüzden bu
  SQL script'leri **DB nesneleri için kaynak (source of truth)** kabul edilir.
  Öneri: EF migration'ları tablo şekli için; bu klasördeki script'ler ise
  trigger/function/view ve seed için kullanılsın (ya da tümü bu script'lerle
  yönetilip EF "database-first" eşlensin).

## Doğrulama

`smoke test` ile şunlar doğrulandı: atama/yeniden-atama (tek aktif kayıt),
iş emri numarası üretimi, parça ekleme → stok düşüşü + `balance_after` +
`parts_cost`/`total_cost`, iş emri tamamlama → araç km güncelleme, kritik stok
ve yaklaşan bakım uyarılarının idempotent üretimi, ve dört guard'ın (target
CHECK, negatif stok, stok guard, partial unique) tetiklenmesi.
