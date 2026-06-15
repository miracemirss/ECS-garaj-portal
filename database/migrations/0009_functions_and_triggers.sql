-- ============================================================================
-- Migration 0009 - Functions & triggers
-- ============================================================================
-- DESIGN NOTE (important): the database does NOT own the business process. The
-- canonical orchestration (validation, transactions, PDF, audit context) lives
-- in the backend Application layer. These DB objects exist for:
--   * DATA INTEGRITY  - target/stock/assignment invariants that must hold no
--                        matter who writes (partial unique indexes, checks,
--                        prevent_negative_stock, stock-mutation guard).
--   * AUDIT           - automatic before/after JSONB snapshots.
--   * PERFORMANCE     - set-based alert generation for background jobs.
--   * CONVENIENCE     - human-friendly numbering.
-- The operation functions (add_work_order_part, complete_work_order,
-- assign_*) are provided as OPTIONAL atomic helpers; the backend may call them
-- or perform the same steps itself - either way the constraints above hold.
-- ============================================================================

-- ---------------------------------------------------------------------------
-- Numbering sequences + generators
-- ---------------------------------------------------------------------------
CREATE SEQUENCE IF NOT EXISTS seq_work_order_no;
CREATE SEQUENCE IF NOT EXISTS seq_stock_movement_no;

CREATE OR REPLACE FUNCTION generate_work_order_no() RETURNS text
LANGUAGE sql AS $$
    SELECT 'WO-' || to_char(now(), 'YYYY') || '-' || lpad(nextval('seq_work_order_no')::text, 6, '0');
$$;

CREATE OR REPLACE FUNCTION generate_stock_movement_no() RETURNS text
LANGUAGE sql AS $$
    SELECT 'SM-' || to_char(now(), 'YYYY') || '-' || lpad(nextval('seq_stock_movement_no')::text, 6, '0');
$$;

-- ---------------------------------------------------------------------------
-- set_updated_at - stamp updated_at on every UPDATE
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION set_updated_at() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$;

-- ---------------------------------------------------------------------------
-- fn_audit - write before/after JSONB snapshots to audit_logs.
-- User context is read from transaction-local GUCs the backend sets:
--   SET LOCAL ecs.user_id = '<uuid>'; SET LOCAL ecs.user_name = '<name>';
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_audit() RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
    v_old jsonb;
    v_new jsonb;
    v_record_id uuid;
    v_changed text[];
    v_user uuid      := nullif(current_setting('ecs.user_id',   true), '')::uuid;
    v_user_name text := nullif(current_setting('ecs.user_name', true), '');
    v_ip inet        := nullif(current_setting('ecs.client_ip', true), '')::inet;
BEGIN
    IF TG_OP = 'INSERT' THEN
        v_new := to_jsonb(NEW);
    ELSIF TG_OP = 'UPDATE' THEN
        v_old := to_jsonb(OLD);
        v_new := to_jsonb(NEW);
    ELSE
        v_old := to_jsonb(OLD);
    END IF;

    v_record_id := coalesce(v_new->>'id', v_old->>'id')::uuid;

    IF TG_OP = 'UPDATE' THEN
        SELECT array_agg(n.key) INTO v_changed
        FROM jsonb_each(v_new) n
        WHERE n.value IS DISTINCT FROM (v_old -> n.key);
    END IF;

    INSERT INTO audit_logs(table_name, record_id, action, old_data, new_data,
                           changed_columns, changed_by, changed_by_name, client_ip)
    VALUES (TG_TABLE_NAME, v_record_id, TG_OP, v_old, v_new,
            v_changed, v_user, v_user_name, v_ip);

    RETURN NULL;
END;
$$;

-- ---------------------------------------------------------------------------
-- fn_guard_part_stock - parts.quantity_in_stock may ONLY change via a stock
-- movement (which sets the ecs.allow_stock_mutation flag).
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_guard_part_stock() RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
    v_allowed boolean := coalesce(current_setting('ecs.allow_stock_mutation', true), '0') = '1';
BEGIN
    IF TG_OP = 'INSERT' THEN
        -- Opening stock must be 0; add it via an 'In' stock movement.
        IF NEW.quantity_in_stock <> 0 AND NOT v_allowed THEN
            RAISE EXCEPTION
                'initial parts.quantity_in_stock must be 0; add opening stock via a stock_movements record'
                USING ERRCODE = 'raise_exception';
        END IF;
    ELSE
        IF NEW.quantity_in_stock IS DISTINCT FROM OLD.quantity_in_stock AND NOT v_allowed THEN
            RAISE EXCEPTION
                'parts.quantity_in_stock can only change through a stock_movements record (part %)', NEW.id
                USING ERRCODE = 'raise_exception';
        END IF;
    END IF;
    RETURN NEW;
END;
$$;

-- ---------------------------------------------------------------------------
-- prevent_negative_stock - BEFORE INSERT on stock_movements:
--   assigns movement_no, locks the part, computes the new balance, blocks the
--   movement if it would go negative, and applies the balance to the part.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION prevent_negative_stock() RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
    v_current     numeric(14,3);
    v_new_balance numeric(14,3);
BEGIN
    IF NEW.movement_no IS NULL THEN
        NEW.movement_no := generate_stock_movement_no();
    END IF;

    SELECT quantity_in_stock INTO v_current
    FROM parts WHERE id = NEW.part_id FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Part % does not exist', NEW.part_id USING ERRCODE = 'foreign_key_violation';
    END IF;

    v_new_balance := v_current + NEW.quantity;

    IF v_new_balance < 0 THEN
        RAISE EXCEPTION 'Insufficient stock for part %: current %, change %',
            NEW.part_id, v_current, NEW.quantity USING ERRCODE = 'check_violation';
    END IF;

    NEW.balance_after := v_new_balance;

    PERFORM set_config('ecs.allow_stock_mutation', '1', true);
    UPDATE parts SET quantity_in_stock = v_new_balance WHERE id = NEW.part_id;
    PERFORM set_config('ecs.allow_stock_mutation', '0', true);

    RETURN NEW;
END;
$$;

-- ---------------------------------------------------------------------------
-- fn_set_work_order_no - assign work_order_no if not supplied.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_set_work_order_no() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.work_order_no IS NULL THEN
        NEW.work_order_no := generate_work_order_no();
    END IF;
    RETURN NEW;
END;
$$;

-- ---------------------------------------------------------------------------
-- fn_recalc_work_order_parts_cost - keep work order parts_cost in sync.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_recalc_work_order_parts_cost() RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
    v_wo uuid := coalesce(NEW.work_order_id, OLD.work_order_id);
BEGIN
    UPDATE maintenance_work_orders w
    SET parts_cost = coalesce(
        (SELECT sum(line_total) FROM work_order_parts WHERE work_order_id = v_wo), 0)
    WHERE w.id = v_wo;
    RETURN NULL;
END;
$$;

-- ---------------------------------------------------------------------------
-- add_work_order_part - atomically consume a part: Out movement + WO part line.
-- (Optional helper; see design note. Stock validation happens in the trigger.)
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION add_work_order_part(
    p_work_order_id uuid,
    p_part_id       uuid,
    p_quantity      numeric,
    p_unit_cost     numeric DEFAULT NULL,
    p_user_id       uuid    DEFAULT NULL
) RETURNS uuid
LANGUAGE plpgsql AS $$
DECLARE
    v_status     work_order_status;
    v_unit_cost  numeric(14,2);
    v_warehouse  uuid;
    v_movement   uuid;
    v_wop        uuid;
BEGIN
    IF p_quantity IS NULL OR p_quantity <= 0 THEN
        RAISE EXCEPTION 'Quantity must be positive';
    END IF;

    SELECT status INTO v_status
    FROM maintenance_work_orders
    WHERE id = p_work_order_id AND is_deleted = false
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Work order % not found', p_work_order_id USING ERRCODE = 'no_data_found';
    END IF;
    IF v_status IN ('Completed', 'Cancelled') THEN
        RAISE EXCEPTION 'Cannot add parts to a % work order', v_status;
    END IF;

    SELECT coalesce(p_unit_cost, unit_cost), warehouse_id
    INTO v_unit_cost, v_warehouse
    FROM parts WHERE id = p_part_id AND is_deleted = false;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Part % not found', p_part_id USING ERRCODE = 'no_data_found';
    END IF;

    INSERT INTO stock_movements(part_id, warehouse_id, movement_type, quantity,
                                unit_cost, work_order_id, created_by, note)
    VALUES (p_part_id, v_warehouse, 'Out', -p_quantity,
            v_unit_cost, p_work_order_id, p_user_id, 'Consumed by work order')
    RETURNING id INTO v_movement;

    INSERT INTO work_order_parts(work_order_id, part_id, quantity, unit_cost,
                                 stock_movement_id, created_by)
    VALUES (p_work_order_id, p_part_id, p_quantity, v_unit_cost, v_movement, p_user_id)
    RETURNING id INTO v_wop;

    RETURN v_wop;
END;
$$;

-- ---------------------------------------------------------------------------
-- complete_work_order - close a work order and roll the vehicle forward.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION complete_work_order(
    p_work_order_id  uuid,
    p_odometer_after int  DEFAULT NULL,
    p_user_id        uuid DEFAULT NULL
) RETURNS void
LANGUAGE plpgsql AS $$
DECLARE
    w maintenance_work_orders;
BEGIN
    SELECT * INTO w
    FROM maintenance_work_orders
    WHERE id = p_work_order_id AND is_deleted = false
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Work order % not found', p_work_order_id USING ERRCODE = 'no_data_found';
    END IF;
    IF w.status NOT IN ('Open', 'InProgress') THEN
        RAISE EXCEPTION 'Work order % cannot be completed from status %', p_work_order_id, w.status;
    END IF;
    IF p_odometer_after IS NOT NULL AND w.odometer_before_km IS NOT NULL
       AND p_odometer_after < w.odometer_before_km THEN
        RAISE EXCEPTION 'odometer_after (%) cannot be less than odometer_before (%)',
            p_odometer_after, w.odometer_before_km;
    END IF;

    UPDATE maintenance_work_orders
    SET status            = 'Completed',
        completed_at      = now(),
        odometer_after_km = coalesce(p_odometer_after, odometer_after_km),
        updated_by        = p_user_id
    WHERE id = p_work_order_id;

    IF w.target_type = 'Vehicle' AND p_odometer_after IS NOT NULL THEN
        UPDATE vehicles
        SET current_odometer_km          = greatest(current_odometer_km, p_odometer_after),
            last_maintenance_date         = current_date,
            last_maintenance_odometer_km  = p_odometer_after,
            next_maintenance_km           = CASE WHEN maintenance_interval_km IS NOT NULL
                                                 THEN p_odometer_after + maintenance_interval_km END,
            next_maintenance_date         = CASE WHEN maintenance_interval_days IS NOT NULL
                                                 THEN current_date + maintenance_interval_days END,
            updated_by                    = p_user_id
        WHERE id = w.vehicle_id;
    END IF;
END;
$$;

-- ---------------------------------------------------------------------------
-- assign_vehicle_trailer - end active links on both sides, open a new one.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION assign_vehicle_trailer(
    p_vehicle_id uuid,
    p_trailer_id uuid,
    p_user_id    uuid DEFAULT NULL,
    p_note       text DEFAULT NULL
) RETURNS uuid
LANGUAGE plpgsql AS $$
DECLARE
    v_id uuid;
BEGIN
    UPDATE vehicle_trailer_assignments
    SET status = 'Ended', ended_at = now(), ended_by = p_user_id
    WHERE ended_at IS NULL AND (vehicle_id = p_vehicle_id OR trailer_id = p_trailer_id);

    INSERT INTO vehicle_trailer_assignments(vehicle_id, trailer_id, status, started_at, created_by, note)
    VALUES (p_vehicle_id, p_trailer_id, 'Active', now(), p_user_id, p_note)
    RETURNING id INTO v_id;

    RETURN v_id;
END;
$$;

-- ---------------------------------------------------------------------------
-- assign_driver_vehicle - end active links on both sides, open a new one.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION assign_driver_vehicle(
    p_driver_id  uuid,
    p_vehicle_id uuid,
    p_user_id    uuid DEFAULT NULL,
    p_note       text DEFAULT NULL
) RETURNS uuid
LANGUAGE plpgsql AS $$
DECLARE
    v_id uuid;
BEGIN
    UPDATE driver_vehicle_assignments
    SET status = 'Ended', ended_at = now(), ended_by = p_user_id
    WHERE ended_at IS NULL AND (driver_id = p_driver_id OR vehicle_id = p_vehicle_id);

    INSERT INTO driver_vehicle_assignments(driver_id, vehicle_id, status, started_at, created_by, note)
    VALUES (p_driver_id, p_vehicle_id, 'Active', now(), p_user_id, p_note)
    RETURNING id INTO v_id;

    RETURN v_id;
END;
$$;

-- ---------------------------------------------------------------------------
-- generate_critical_stock_alerts - one OPEN alert per part at/under minimum.
-- Idempotent via the ux_alerts_open_dedup partial unique index. Returns count.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION generate_critical_stock_alerts() RETURNS int
LANGUAGE plpgsql AS $$
DECLARE
    v_count int;
BEGIN
    WITH ins AS (
        INSERT INTO alerts(alert_type, severity, status, title, message, part_id, dedup_key)
        SELECT 'CriticalStock', 'Critical', 'Open',
               'Kritik stok: ' || p.name,
               format('%s (%s) stok %s, minimum %s', p.name, p.part_no, p.quantity_in_stock, p.minimum_stock),
               p.id,
               'stock:' || p.id
        FROM parts p
        WHERE p.is_active AND p.is_deleted = false
          AND p.quantity_in_stock <= p.minimum_stock
        ON CONFLICT (dedup_key) WHERE status = 'Open' AND dedup_key IS NOT NULL DO NOTHING
        RETURNING 1
    )
    SELECT count(*) INTO v_count FROM ins;
    RETURN v_count;
END;
$$;

-- ---------------------------------------------------------------------------
-- generate_upcoming_maintenance_alerts - alert vehicles whose next maintenance
-- (km or date) is within the company thresholds. Idempotent. Returns count.
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION generate_upcoming_maintenance_alerts() RETURNS int
LANGUAGE plpgsql AS $$
DECLARE
    v_count          int;
    v_km_threshold   int;
    v_days_threshold int;
BEGIN
    SELECT maintenance_due_km_threshold, maintenance_due_days_threshold
    INTO v_km_threshold, v_days_threshold
    FROM company_settings WHERE singleton = true;

    v_km_threshold   := coalesce(v_km_threshold, 1000);
    v_days_threshold := coalesce(v_days_threshold, 14);

    WITH ins AS (
        INSERT INTO alerts(alert_type, severity, status, title, message, vehicle_id, dedup_key)
        SELECT 'MaintenanceDue',
               (CASE WHEN (v.next_maintenance_date IS NOT NULL AND v.next_maintenance_date <= current_date)
                       OR (v.next_maintenance_km IS NOT NULL AND v.next_maintenance_km <= v.current_odometer_km)
                     THEN 'Critical' ELSE 'Warning' END)::alert_severity,
               'Open',
               'Yaklasan bakim: ' || v.plate_no,
               format('Plaka %s - sonraki bakim km %s (mevcut %s), tarih %s',
                      v.plate_no, v.next_maintenance_km, v.current_odometer_km, v.next_maintenance_date),
               v.id,
               'maint:' || v.id
        FROM vehicles v
        WHERE v.is_deleted = false AND v.status <> 'Retired'
          AND (
              (v.next_maintenance_date IS NOT NULL AND v.next_maintenance_date <= current_date + v_days_threshold)
              OR
              (v.next_maintenance_km IS NOT NULL AND (v.next_maintenance_km - v.current_odometer_km) <= v_km_threshold)
          )
        ON CONFLICT (dedup_key) WHERE status = 'Open' AND dedup_key IS NOT NULL DO NOTHING
        RETURNING 1
    )
    SELECT count(*) INTO v_count FROM ins;
    RETURN v_count;
END;
$$;

-- ===========================================================================
-- TRIGGERS
-- ===========================================================================

-- updated_at stamping
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON users                       FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON roles                       FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON vehicles                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON trailers                    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON drivers                     FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON warehouses                  FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON suppliers                   FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON parts                       FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON maintenance_work_orders     FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON maintenance_tasks           FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON vehicle_trailer_assignments FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON driver_vehicle_assignments  FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON company_settings            FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON documents                   FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- numbering / cost / stock guards
CREATE TRIGGER trg_set_work_order_no BEFORE INSERT ON maintenance_work_orders FOR EACH ROW EXECUTE FUNCTION fn_set_work_order_no();
CREATE TRIGGER trg_guard_part_stock  BEFORE INSERT OR UPDATE ON parts        FOR EACH ROW EXECUTE FUNCTION fn_guard_part_stock();
CREATE TRIGGER trg_prevent_neg_stock BEFORE INSERT ON stock_movements         FOR EACH ROW EXECUTE FUNCTION prevent_negative_stock();
CREATE TRIGGER trg_recalc_wo_cost    AFTER INSERT OR UPDATE OR DELETE ON work_order_parts FOR EACH ROW EXECUTE FUNCTION fn_recalc_work_order_parts_cost();

-- audit (business tables only; users/refresh_tokens excluded to avoid logging secrets)
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON vehicles                    FOR EACH ROW EXECUTE FUNCTION fn_audit();
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON trailers                    FOR EACH ROW EXECUTE FUNCTION fn_audit();
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON drivers                     FOR EACH ROW EXECUTE FUNCTION fn_audit();
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON vehicle_trailer_assignments FOR EACH ROW EXECUTE FUNCTION fn_audit();
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON driver_vehicle_assignments  FOR EACH ROW EXECUTE FUNCTION fn_audit();
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON parts                       FOR EACH ROW EXECUTE FUNCTION fn_audit();
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON stock_movements             FOR EACH ROW EXECUTE FUNCTION fn_audit();
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON maintenance_work_orders     FOR EACH ROW EXECUTE FUNCTION fn_audit();
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON maintenance_tasks           FOR EACH ROW EXECUTE FUNCTION fn_audit();
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON work_order_parts            FOR EACH ROW EXECUTE FUNCTION fn_audit();
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON documents                   FOR EACH ROW EXECUTE FUNCTION fn_audit();
CREATE TRIGGER trg_audit AFTER INSERT OR UPDATE OR DELETE ON company_settings            FOR EACH ROW EXECUTE FUNCTION fn_audit();
