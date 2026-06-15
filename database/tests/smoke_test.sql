-- Smoke test for the ECS schema. Run after all migrations on a fresh ecs_test DB.
\set ON_ERROR_STOP on
SET ecs.user_name = 'tester';

-- ---------------------------------------------------------------------------
-- Happy path: master data, assignments, work order, parts, completion, alerts
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    v_vehicle uuid; v_trailer uuid; v_trailer2 uuid; v_driver uuid;
    v_wh uuid; v_sup uuid; v_part uuid;
    v_wo uuid; v_wono text; v_wop uuid;
    v_stock numeric; v_balance numeric; v_parts_cost numeric; v_odo int;
    v_active int; v_cnt int;
BEGIN
    INSERT INTO warehouses(code, name) VALUES ('WH1', 'Test Depo') RETURNING id INTO v_wh;
    INSERT INTO suppliers(name) VALUES ('Test Tedarik') RETURNING id INTO v_sup;

    INSERT INTO parts(part_no, name, unit, quantity_in_stock, minimum_stock, unit_cost, warehouse_id, supplier_id)
    VALUES ('P-001', 'Fren Balatasi', 'pcs', 0, 5, 120.00, v_wh, v_sup) RETURNING id INTO v_part;

    -- Opening stock via In movement (20 units)
    INSERT INTO stock_movements(part_id, warehouse_id, movement_type, quantity, unit_cost)
    VALUES (v_part, v_wh, 'In', 20, 120.00);
    SELECT quantity_in_stock INTO v_stock FROM parts WHERE id = v_part;
    ASSERT v_stock = 20, format('expected stock 20 got %s', v_stock);

    INSERT INTO vehicles(plate_no, brand, model, model_year, current_odometer_km, maintenance_interval_km, maintenance_interval_days)
    VALUES ('34ABC34', 'Mercedes', 'Actros', 2020, 100000, 50000, 365) RETURNING id INTO v_vehicle;
    INSERT INTO trailers(plate_no, trailer_type) VALUES ('34TR01', 'Tent')  RETURNING id INTO v_trailer;
    INSERT INTO trailers(plate_no, trailer_type) VALUES ('34TR02', 'Frigo') RETURNING id INTO v_trailer2;
    INSERT INTO drivers(first_name, last_name, national_id) VALUES ('Ali', 'Veli', '12345678901') RETURNING id INTO v_driver;

    -- Assign trailer, then reassign -> only one active link remains
    PERFORM assign_vehicle_trailer(v_vehicle, v_trailer);
    PERFORM assign_vehicle_trailer(v_vehicle, v_trailer2);
    SELECT count(*) INTO v_active FROM vehicle_trailer_assignments WHERE vehicle_id = v_vehicle AND ended_at IS NULL;
    ASSERT v_active = 1, format('expected 1 active vehicle-trailer got %s', v_active);

    PERFORM assign_driver_vehicle(v_driver, v_vehicle);
    SELECT count(*) INTO v_active FROM driver_vehicle_assignments WHERE driver_id = v_driver AND ended_at IS NULL;
    ASSERT v_active = 1, format('expected 1 active driver-vehicle got %s', v_active);

    -- Work order on the vehicle; number auto-generated
    INSERT INTO maintenance_work_orders(target_type, vehicle_id, title, odometer_before_km, labor_cost, status)
    VALUES ('Vehicle', v_vehicle, 'Periyodik bakim', 100000, 500.00, 'Open')
    RETURNING id, work_order_no INTO v_wo, v_wono;
    ASSERT v_wono IS NOT NULL, 'work_order_no should be generated';

    -- Add part (3 @120): stock 20->17, parts_cost = 360
    v_wop := add_work_order_part(v_wo, v_part, 3, 120.00, NULL);
    SELECT quantity_in_stock INTO v_stock FROM parts WHERE id = v_part;
    ASSERT v_stock = 17, format('expected stock 17 got %s', v_stock);
    SELECT balance_after INTO v_balance FROM stock_movements
      WHERE id = (SELECT stock_movement_id FROM work_order_parts WHERE id = v_wop);
    ASSERT v_balance = 17, format('expected balance_after 17 got %s', v_balance);
    SELECT parts_cost INTO v_parts_cost FROM maintenance_work_orders WHERE id = v_wo;
    ASSERT v_parts_cost = 360.00, format('expected parts_cost 360 got %s', v_parts_cost);

    -- Complete: vehicle odometer rolls to 100500, next maint km = 150500
    PERFORM complete_work_order(v_wo, 100500, NULL);
    SELECT current_odometer_km INTO v_odo FROM vehicles WHERE id = v_vehicle;
    ASSERT v_odo = 100500, format('expected odometer 100500 got %s', v_odo);
    ASSERT (SELECT next_maintenance_km FROM vehicles WHERE id = v_vehicle) = 150500, 'next_maintenance_km should be 150500';

    -- Critical stock alert (idempotent): P-002 opens at 0 with min 10
    INSERT INTO parts(part_no, name, quantity_in_stock, minimum_stock, unit_cost)
    VALUES ('P-002', 'Yag Filtresi', 0, 10, 50);
    SELECT generate_critical_stock_alerts() INTO v_cnt;
    ASSERT v_cnt >= 1, format('expected >=1 critical alert got %s', v_cnt);
    SELECT generate_critical_stock_alerts() INTO v_cnt;
    ASSERT v_cnt = 0, format('expected 0 critical alerts on 2nd run got %s', v_cnt);

    -- Upcoming maintenance alert
    UPDATE vehicles SET next_maintenance_date = current_date + 3 WHERE id = v_vehicle;
    SELECT generate_upcoming_maintenance_alerts() INTO v_cnt;
    ASSERT v_cnt >= 1, format('expected >=1 maintenance alert got %s', v_cnt);

    RAISE NOTICE 'HAPPY PATH OK (stock=%, odometer=%, parts_cost=%, wo=%)', v_stock, v_odo, v_parts_cost, v_wono;
END $$;

-- ---------------------------------------------------------------------------
-- Negative tests: each guard must block. ASSERT false (assert_failure) is used
-- on the should-not-happen path so it is NOT swallowed by the EXCEPTION handler.
-- ---------------------------------------------------------------------------
DO $$
DECLARE v uuid; t uuid;
BEGIN
    SELECT id INTO v FROM vehicles WHERE plate_no = '34ABC34';
    SELECT id INTO t FROM trailers WHERE plate_no = '34TR01';
    BEGIN
        INSERT INTO maintenance_work_orders(target_type, vehicle_id, trailer_id, title)
        VALUES ('Vehicle', v, t, 'mixed target');
        ASSERT false, 'target CHECK did not fire';
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE 'OK: target CHECK blocked Vehicle+trailer_id';
    END;
END $$;

DO $$
DECLARE p uuid;
BEGIN
    SELECT id INTO p FROM parts WHERE part_no = 'P-001';   -- stock 17
    BEGIN
        INSERT INTO stock_movements(part_id, movement_type, quantity, unit_cost) VALUES (p, 'Out', -100, 0);
        ASSERT false, 'prevent_negative_stock did not fire';
    EXCEPTION WHEN check_violation THEN
        RAISE NOTICE 'OK: prevent_negative_stock blocked oversized Out';
    END;
END $$;

DO $$
DECLARE p uuid;
BEGIN
    SELECT id INTO p FROM parts WHERE part_no = 'P-001';
    BEGIN
        UPDATE parts SET quantity_in_stock = 999 WHERE id = p;
        ASSERT false, 'stock guard did not block manual update';
    EXCEPTION WHEN raise_exception THEN
        RAISE NOTICE 'OK: stock guard blocked manual quantity_in_stock update';
    END;
END $$;

DO $$
DECLARE v uuid; t uuid;
BEGIN
    SELECT id INTO v FROM vehicles WHERE plate_no = '34ABC34';
    SELECT id INTO t FROM trailers WHERE plate_no = '34TR01';
    BEGIN
        INSERT INTO vehicle_trailer_assignments(vehicle_id, trailer_id, status) VALUES (v, t, 'Active');
        ASSERT false, 'partial unique (active vehicle) did not fire';
    EXCEPTION WHEN unique_violation THEN
        RAISE NOTICE 'OK: partial unique blocked a 2nd active trailer for the vehicle';
    END;
END $$;

-- ---------------------------------------------------------------------------
-- View sanity + audit verification
-- ---------------------------------------------------------------------------
\echo '--- vw_dashboard_summary ---'
SELECT * FROM vw_dashboard_summary;
\echo '--- vw_vehicle_current_status ---'
SELECT plate_no, current_trailer_plate, current_driver_name, open_work_orders FROM vw_vehicle_current_status;
\echo '--- vw_critical_stocks ---'
SELECT part_no, name, quantity_in_stock, minimum_stock, deficit FROM vw_critical_stocks ORDER BY part_no;
\echo '--- vw_work_order_summary ---'
SELECT work_order_no, target_plate, status, parts_cost, total_cost, part_lines FROM vw_work_order_summary;
\echo '--- vw_vehicle_cost_summary ---'
SELECT plate_no, work_order_count, total_cost FROM vw_vehicle_cost_summary;
\echo '--- vw_monthly_costs ---'
SELECT to_char(month,'YYYY-MM') AS month, work_order_count, total_cost FROM vw_monthly_costs;
\echo '--- audit_logs ---'
SELECT count(*) AS audit_rows, count(*) FILTER (WHERE changed_by_name = 'tester') AS by_tester FROM audit_logs;
\echo 'SMOKE TEST COMPLETE'
