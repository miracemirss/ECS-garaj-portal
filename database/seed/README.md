# ECS Fleet — Demo Seed

`demo_seed.sql` **örnek** veri yükler: depolar, tedarikçiler, araçlar, dorseler,
şoförler, parçalar (açılış stoğu ile), araç-dorse & şoför-araç atamaları,
tamamlanmış bakım iş emirleri (parça tüketimi + stok hareketi) ve uyarılar.

> **Yalnızca local / development / staging.** Üretimde çalıştırmayın.

İdempotenttir (tekrar çalıştırılabilir) ve **trigger-güvenli** yollar kullanır:

- Açılış stoğu `quantity_in_stock`'u doğrudan yazmaz; `In` **stok hareketi** ekler
  (`prevent_negative_stock` bakiyeyi damgalar).
- Parça tüketimi `add_work_order_part()`, tamamlama `complete_work_order()`,
  atamalar `assign_vehicle_trailer()` / `assign_driver_vehicle()` ile yapılır.
- Yakıt filtresi bilinçli olarak minimumun altında bırakılır → **kritik stok uyarısı**.
- Bir araç yaklaşan bakım penceresine çekilir → **yaklaşan bakım uyarısı**.

## Çalıştırma

```bash
# Çekirdek migration'lardan (0001–0011) sonra:
psql -v ON_ERROR_STOP=1 -d ecs_fleet -f database/seed/demo_seed.sql

# Docker Compose ile:
RUN_DEMO_SEED=true docker compose up -d --build   # migrator otomatik yükler
```

## Demo hesaplar

`admin@ecs.local` (Admin) çekirdek seed'de gelir; `fleet@ecs.local`,
`tech@ecs.local`, `depo@ecs.local` demo seed'de eklenir. Tümünün parola hash'i
**placeholder**'dır — giriş için parolayı uygulama üzerinden atayın.
