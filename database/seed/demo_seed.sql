-- ============================================================================
-- ECS Fleet — DEMO seed data (LOCAL / DEVELOPMENT / STAGING ONLY)
-- ============================================================================
-- NOT for production. Idempotent: safe to run multiple times. Run AFTER the core
-- migrations (0001–0011). Loads example fleet, inventory, assignments, completed
-- work orders (with stock consumption) and alerts using the trigger-safe paths:
--   * opening stock via 'In' stock_movements (never direct quantity updates)
--   * part consumption via add_work_order_part()
--   * completion via complete_work_order()
--   * assignments via assign_vehicle_trailer() / assign_driver_vehicle()
--
--   psql -v ON_ERROR_STOP=1 -d ecs_fleet -f database/seed/demo_seed.sql
-- ============================================================================

DO $$
DECLARE
    v_admin   uuid;
    v_tech    uuid;
    v_wh_main uuid;
    v_wh_ist  uuid;
    v_sup_bosch uuid;
    v_sup_tire  uuid;
    v_v1 uuid; v_v2 uuid; v_v3 uuid; v_v4 uuid;
    v_t1 uuid; v_t2 uuid; v_t3 uuid;
    v_d1 uuid; v_d2 uuid; v_d3 uuid;
    v_p_oil uuid; v_p_air uuid; v_p_moil uuid; v_p_brake uuid; v_p_tire uuid; v_p_fuel uuid;
    v_wo1 uuid; v_wo2 uuid;
BEGIN
    SELECT id INTO v_admin FROM users WHERE email = 'admin@ecs.local';

    -- ---- Demo users (placeholder hash; set a real password via the app to log in) ----
    INSERT INTO users(email, full_name, password_hash, is_active) VALUES
        ('fleet@ecs.local', 'Filo Yöneticisi',   'REPLACE_WITH_BCRYPT_HASH', true),
        ('tech@ecs.local',  'Servis Teknisyeni', 'REPLACE_WITH_BCRYPT_HASH', true),
        ('depo@ecs.local',  'Depo Sorumlusu',    'REPLACE_WITH_BCRYPT_HASH', true)
    ON CONFLICT (email) DO NOTHING;

    INSERT INTO user_roles(user_id, role_id)
    SELECT u.id, r.id FROM users u, roles r WHERE u.email='fleet@ecs.local' AND r.name='FleetManager'
    ON CONFLICT DO NOTHING;
    INSERT INTO user_roles(user_id, role_id)
    SELECT u.id, r.id FROM users u, roles r WHERE u.email='tech@ecs.local' AND r.name='Technician'
    ON CONFLICT DO NOTHING;
    INSERT INTO user_roles(user_id, role_id)
    SELECT u.id, r.id FROM users u, roles r WHERE u.email='depo@ecs.local' AND r.name='WarehouseManager'
    ON CONFLICT DO NOTHING;

    SELECT id INTO v_tech FROM users WHERE email = 'tech@ecs.local';

    -- ---- Warehouses ----
    INSERT INTO warehouses(code, name, is_active) VALUES ('IST-1', 'İstanbul Anadolu Depo', true)
    ON CONFLICT (code) DO NOTHING;
    SELECT id INTO v_wh_main FROM warehouses WHERE code = 'MAIN';
    SELECT id INTO v_wh_ist  FROM warehouses WHERE code = 'IST-1';

    -- ---- Suppliers ----
    INSERT INTO suppliers(name, tax_no, phone, is_active) VALUES
        ('Bosch Yetkili Servis', '1112223334', '+90 212 000 00 01', true),
        ('Lastik Dünyası A.Ş.',  '5556667778', '+90 212 000 00 02', true)
    ON CONFLICT (tax_no) DO NOTHING;
    SELECT id INTO v_sup_bosch FROM suppliers WHERE tax_no = '1112223334';
    SELECT id INTO v_sup_tire  FROM suppliers WHERE tax_no = '5556667778';

    -- ---- Vehicles ----
    INSERT INTO vehicles(plate_no, vin, brand, model, model_year, current_odometer_km,
                         maintenance_interval_km, maintenance_interval_days) VALUES
        ('34ECS001', 'WDB9634031L900001', 'Mercedes', 'Actros 1845', 2021, 480000, 40000, 365),
        ('34ECS002', 'YV2RT40A8LB800002', 'Volvo',    'FH 460',      2020, 615000, 40000, 365),
        ('06ECS003', 'XLER4X20005900003', 'Scania',   'R 450',       2022, 210000, 45000, 365),
        ('35ECS004', 'WMA06XZZ7KP000004', 'MAN',      'TGX 18.500',  2019, 720000, 40000, 365)
    ON CONFLICT (plate_no) DO NOTHING;
    SELECT id INTO v_v1 FROM vehicles WHERE plate_no = '34ECS001';
    SELECT id INTO v_v2 FROM vehicles WHERE plate_no = '34ECS002';
    SELECT id INTO v_v3 FROM vehicles WHERE plate_no = '06ECS003';
    SELECT id INTO v_v4 FROM vehicles WHERE plate_no = '35ECS004';

    -- ---- Trailers ----
    INSERT INTO trailers(plate_no, vin, trailer_type, brand, capacity_kg, tire_condition_percent) VALUES
        ('34TR100', 'WKE000000000T0100', 'Tent',   'Krone',   24000, 80),
        ('34TR200', 'WSM000000000T0200', 'Frigo',  'Schmitz', 22000, 65),
        ('06TR300', 'WLB000000000T0300', 'Lowbed', 'Nooteboom', 40000, 90)
    ON CONFLICT (plate_no) DO NOTHING;
    SELECT id INTO v_t1 FROM trailers WHERE plate_no = '34TR100';
    SELECT id INTO v_t2 FROM trailers WHERE plate_no = '34TR200';
    SELECT id INTO v_t3 FROM trailers WHERE plate_no = '06TR300';

    -- ---- Drivers ----
    INSERT INTO drivers(first_name, last_name, national_id, phone, license_class) VALUES
        ('Ahmet', 'Yılmaz', '10000000001', '+90 532 000 00 01', 'CE'),
        ('Mehmet','Demir',  '10000000002', '+90 532 000 00 02', 'CE'),
        ('Hasan', 'Kaya',   '10000000003', '+90 532 000 00 03', 'C')
    ON CONFLICT (national_id) DO NOTHING;
    SELECT id INTO v_d1 FROM drivers WHERE national_id = '10000000001';
    SELECT id INTO v_d2 FROM drivers WHERE national_id = '10000000002';
    SELECT id INTO v_d3 FROM drivers WHERE national_id = '10000000003';

    -- ---- Parts (opening stock is 0; loaded via stock movements below) ----
    INSERT INTO parts(part_no, name, category, unit, minimum_stock, unit_cost, warehouse_id, supplier_id) VALUES
        ('FLT-OIL-001',  'Yağ Filtresi',          'Filtre', 'adet',  10,  180, v_wh_main, v_sup_bosch),
        ('FLT-AIR-002',  'Hava Filtresi',         'Filtre', 'adet',   8,  240, v_wh_main, v_sup_bosch),
        ('OIL-15W40-205','Motor Yağı 15W40',      'Yağ',    'litre', 100,  95, v_wh_main, v_sup_bosch),
        ('BRK-PAD-010',  'Fren Balatası (takım)', 'Fren',   'takım',   6, 1450, v_wh_main, v_sup_bosch),
        ('TIRE-315-80',  'Lastik 315/80 R22.5',   'Lastik', 'adet',    8, 6200, v_wh_ist,  v_sup_tire),
        ('FLT-FUEL-003', 'Yakıt Filtresi',        'Filtre', 'adet',   12,  320, v_wh_main, v_sup_bosch)
    ON CONFLICT (part_no) DO NOTHING;
    SELECT id INTO v_p_oil   FROM parts WHERE part_no = 'FLT-OIL-001';
    SELECT id INTO v_p_air   FROM parts WHERE part_no = 'FLT-AIR-002';
    SELECT id INTO v_p_moil  FROM parts WHERE part_no = 'OIL-15W40-205';
    SELECT id INTO v_p_brake FROM parts WHERE part_no = 'BRK-PAD-010';
    SELECT id INTO v_p_tire  FROM parts WHERE part_no = 'TIRE-315-80';
    SELECT id INTO v_p_fuel  FROM parts WHERE part_no = 'FLT-FUEL-003';

    -- ---- Opening stock (once). Fuel filter intentionally below minimum -> critical alert ----
    IF NOT EXISTS (SELECT 1 FROM stock_movements WHERE note = 'DEMO opening stock') THEN
        INSERT INTO stock_movements(part_id, warehouse_id, movement_type, quantity, unit_cost, created_by, note) VALUES
            (v_p_oil,   v_wh_main, 'In', 40,  180, v_admin, 'DEMO opening stock'),
            (v_p_air,   v_wh_main, 'In', 30,  240, v_admin, 'DEMO opening stock'),
            (v_p_moil,  v_wh_main, 'In', 600,  95, v_admin, 'DEMO opening stock'),
            (v_p_brake, v_wh_main, 'In', 12, 1450, v_admin, 'DEMO opening stock'),
            (v_p_tire,  v_wh_ist,  'In', 16, 6200, v_admin, 'DEMO opening stock'),
            (v_p_fuel,  v_wh_main, 'In',  5,  320, v_admin, 'DEMO opening stock');
    END IF;

    -- ---- Active assignments (one active per side) ----
    IF NOT EXISTS (SELECT 1 FROM vehicle_trailer_assignments WHERE vehicle_id = v_v1 AND ended_at IS NULL) THEN
        PERFORM assign_vehicle_trailer(v_v1, v_t1, v_admin, 'DEMO');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM vehicle_trailer_assignments WHERE vehicle_id = v_v2 AND ended_at IS NULL) THEN
        PERFORM assign_vehicle_trailer(v_v2, v_t2, v_admin, 'DEMO');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM driver_vehicle_assignments WHERE driver_id = v_d1 AND ended_at IS NULL) THEN
        PERFORM assign_driver_vehicle(v_d1, v_v1, v_admin, 'DEMO');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM driver_vehicle_assignments WHERE driver_id = v_d2 AND ended_at IS NULL) THEN
        PERFORM assign_driver_vehicle(v_d2, v_v2, v_admin, 'DEMO');
    END IF;

    -- ---- Work orders (with part consumption + completion). Created once ----
    IF NOT EXISTS (SELECT 1 FROM maintenance_work_orders WHERE title LIKE 'DEMO:%') THEN
        -- WO1: completed preventive service consuming filters + oil
        INSERT INTO maintenance_work_orders(target_type, vehicle_id, maintenance_type, status, title,
                                            odometer_before_km, labor_cost, supplier_id, assigned_to)
        VALUES ('Vehicle', v_v1, 'Preventive', 'Open', 'DEMO: 480.000 km periyodik bakım',
                480000, 1500, v_sup_bosch, v_tech)
        RETURNING id INTO v_wo1;
        PERFORM add_work_order_part(v_wo1, v_p_oil,  1, NULL, v_admin);
        PERFORM add_work_order_part(v_wo1, v_p_air,  1, NULL, v_admin);
        PERFORM add_work_order_part(v_wo1, v_p_moil, 40, NULL, v_admin);
        PERFORM complete_work_order(v_wo1, 480500, v_admin);

        -- WO2: completed corrective brake job
        INSERT INTO maintenance_work_orders(target_type, vehicle_id, maintenance_type, status, title,
                                            odometer_before_km, labor_cost, supplier_id, assigned_to)
        VALUES ('Vehicle', v_v2, 'Corrective', 'Open', 'DEMO: Fren balata değişimi',
                615000, 800, v_sup_bosch, v_tech)
        RETURNING id INTO v_wo2;
        PERFORM add_work_order_part(v_wo2, v_p_brake, 1, NULL, v_admin);
        PERFORM complete_work_order(v_wo2, 615200, v_admin);

        -- WO3: in-progress trailer inspection (no parts) for variety
        INSERT INTO maintenance_work_orders(target_type, trailer_id, maintenance_type, status, title, labor_cost)
        VALUES ('Trailer', v_t2, 'Inspection', 'InProgress', 'DEMO: Frigo ünitesi kontrolü', 500);
    END IF;

    -- ---- Alerts ----
    -- Nudge one vehicle into the upcoming-maintenance window so a demo alert is produced.
    UPDATE vehicles
       SET next_maintenance_date = current_date + 5,
           next_maintenance_km   = current_odometer_km + 800
     WHERE plate_no = '06ECS003';

    PERFORM generate_critical_stock_alerts();      -- fuel filter is below minimum
    PERFORM generate_upcoming_maintenance_alerts();
END $$;
