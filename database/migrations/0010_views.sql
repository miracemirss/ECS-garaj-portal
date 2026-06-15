-- ============================================================================
-- Migration 0010 - Reporting / read-model views
-- ============================================================================
-- Views encapsulate the common read models for the dashboard, lists and
-- reports. vw_dashboard_summary depends on vw_critical_stocks and
-- vw_upcoming_maintenances, so it is created last.
-- ============================================================================

-- Active links -------------------------------------------------------------
CREATE OR REPLACE VIEW vw_active_vehicle_trailer_assignments AS
SELECT a.id,
       a.vehicle_id, v.plate_no AS vehicle_plate,
       a.trailer_id, t.plate_no AS trailer_plate,
       a.started_at, a.note
FROM vehicle_trailer_assignments a
JOIN vehicles v ON v.id = a.vehicle_id
JOIN trailers t ON t.id = a.trailer_id
WHERE a.ended_at IS NULL;

CREATE OR REPLACE VIEW vw_active_driver_vehicle_assignments AS
SELECT a.id,
       a.driver_id, (d.first_name || ' ' || d.last_name) AS driver_name,
       a.vehicle_id, v.plate_no AS vehicle_plate,
       a.started_at, a.note
FROM driver_vehicle_assignments a
JOIN drivers  d ON d.id = a.driver_id
JOIN vehicles v ON v.id = a.vehicle_id
WHERE a.ended_at IS NULL;

-- Current status -----------------------------------------------------------
CREATE OR REPLACE VIEW vw_vehicle_current_status AS
SELECT v.id, v.plate_no, v.brand, v.model, v.status,
       v.current_odometer_km, v.next_maintenance_km, v.next_maintenance_date,
       vta.trailer_id            AS current_trailer_id,
       tr.plate_no               AS current_trailer_plate,
       dva.driver_id             AS current_driver_id,
       (dr.first_name || ' ' || dr.last_name) AS current_driver_name,
       (SELECT count(*) FROM maintenance_work_orders w
         WHERE w.vehicle_id = v.id AND w.is_deleted = false
           AND w.status IN ('Open', 'InProgress')) AS open_work_orders
FROM vehicles v
LEFT JOIN vehicle_trailer_assignments vta ON vta.vehicle_id = v.id AND vta.ended_at IS NULL
LEFT JOIN trailers tr ON tr.id = vta.trailer_id
LEFT JOIN driver_vehicle_assignments dva ON dva.vehicle_id = v.id AND dva.ended_at IS NULL
LEFT JOIN drivers dr ON dr.id = dva.driver_id
WHERE v.is_deleted = false;

CREATE OR REPLACE VIEW vw_trailer_current_status AS
SELECT t.id, t.plate_no, t.trailer_type, t.status,
       vta.vehicle_id AS current_vehicle_id,
       v.plate_no     AS current_vehicle_plate,
       vta.started_at AS assigned_since
FROM trailers t
LEFT JOIN vehicle_trailer_assignments vta ON vta.trailer_id = t.id AND vta.ended_at IS NULL
LEFT JOIN vehicles v ON v.id = vta.vehicle_id
WHERE t.is_deleted = false;

-- Maintenance history / costs ----------------------------------------------
CREATE OR REPLACE VIEW vw_vehicle_maintenance_history AS
SELECT w.id AS work_order_id, w.work_order_no,
       w.vehicle_id, v.plate_no,
       w.maintenance_type, w.status, w.title,
       w.scheduled_date, w.started_at, w.completed_at,
       w.odometer_before_km, w.odometer_after_km,
       w.labor_cost, w.parts_cost, w.total_cost
FROM maintenance_work_orders w
JOIN vehicles v ON v.id = w.vehicle_id
WHERE w.target_type = 'Vehicle' AND w.is_deleted = false;

CREATE OR REPLACE VIEW vw_vehicle_cost_summary AS
SELECT v.id AS vehicle_id, v.plate_no,
       count(w.id)                         AS work_order_count,
       coalesce(sum(w.labor_cost), 0)      AS total_labor_cost,
       coalesce(sum(w.parts_cost), 0)      AS total_parts_cost,
       coalesce(sum(w.total_cost), 0)      AS total_cost,
       max(w.completed_at)                 AS last_maintenance_at
FROM vehicles v
LEFT JOIN maintenance_work_orders w
       ON w.vehicle_id = v.id AND w.target_type = 'Vehicle'
      AND w.is_deleted = false AND w.status = 'Completed'
WHERE v.is_deleted = false
GROUP BY v.id, v.plate_no;

CREATE OR REPLACE VIEW vw_trailer_cost_summary AS
SELECT t.id AS trailer_id, t.plate_no,
       count(w.id)                         AS work_order_count,
       coalesce(sum(w.labor_cost), 0)      AS total_labor_cost,
       coalesce(sum(w.parts_cost), 0)      AS total_parts_cost,
       coalesce(sum(w.total_cost), 0)      AS total_cost,
       max(w.completed_at)                 AS last_maintenance_at
FROM trailers t
LEFT JOIN maintenance_work_orders w
       ON w.trailer_id = t.id AND w.target_type = 'Trailer'
      AND w.is_deleted = false AND w.status = 'Completed'
WHERE t.is_deleted = false
GROUP BY t.id, t.plate_no;

-- Inventory ----------------------------------------------------------------
CREATE OR REPLACE VIEW vw_critical_stocks AS
SELECT p.id AS part_id, p.part_no, p.name, p.category, p.unit,
       p.quantity_in_stock, p.minimum_stock,
       (p.minimum_stock - p.quantity_in_stock) AS deficit,
       p.unit_cost,
       w.name AS warehouse_name,
       s.name AS supplier_name
FROM parts p
LEFT JOIN warehouses w ON w.id = p.warehouse_id
LEFT JOIN suppliers  s ON s.id = p.supplier_id
WHERE p.is_active AND p.is_deleted = false
  AND p.quantity_in_stock <= p.minimum_stock;

-- Upcoming maintenance / expiring documents (thresholds from company_settings)
CREATE OR REPLACE VIEW vw_upcoming_maintenances AS
SELECT v.id AS vehicle_id, v.plate_no, v.status,
       v.current_odometer_km, v.next_maintenance_km, v.next_maintenance_date,
       (v.next_maintenance_km - v.current_odometer_km) AS km_remaining,
       (v.next_maintenance_date - current_date)        AS days_remaining
FROM vehicles v
CROSS JOIN company_settings cs
WHERE v.is_deleted = false AND v.status <> 'Retired'
  AND (
      (v.next_maintenance_date IS NOT NULL AND v.next_maintenance_date <= current_date + cs.maintenance_due_days_threshold)
      OR
      (v.next_maintenance_km IS NOT NULL AND (v.next_maintenance_km - v.current_odometer_km) <= cs.maintenance_due_km_threshold)
  );

CREATE OR REPLACE VIEW vw_expiring_documents AS
SELECT d.id AS document_id, d.owner_type, d.document_type, d.title,
       d.issue_date, d.expiry_date,
       (d.expiry_date - current_date) AS days_to_expiry,
       d.vehicle_id, d.trailer_id, d.driver_id,
       coalesce(v.plate_no::text, t.plate_no::text, (dr.first_name || ' ' || dr.last_name)) AS owner_label
FROM documents d
LEFT JOIN vehicles v  ON v.id = d.vehicle_id
LEFT JOIN trailers t  ON t.id = d.trailer_id
LEFT JOIN drivers  dr ON dr.id = d.driver_id
CROSS JOIN company_settings cs
WHERE d.is_deleted = false AND d.expiry_date IS NOT NULL
  AND d.expiry_date <= current_date + cs.document_expiry_days_threshold;

-- Work orders --------------------------------------------------------------
CREATE OR REPLACE VIEW vw_work_order_summary AS
SELECT w.id AS work_order_id, w.work_order_no, w.target_type,
       coalesce(v.plate_no::text, t.plate_no::text) AS target_plate,
       w.maintenance_type, w.status, w.title,
       w.scheduled_date, w.started_at, w.completed_at,
       w.labor_cost, w.parts_cost, w.total_cost,
       (SELECT count(*) FROM work_order_parts wp WHERE wp.work_order_id = w.id) AS part_lines,
       (SELECT count(*) FROM maintenance_tasks mt WHERE mt.work_order_id = w.id) AS task_count,
       (SELECT count(*) FROM maintenance_tasks mt WHERE mt.work_order_id = w.id AND mt.is_completed) AS completed_task_count,
       u.full_name AS technician
FROM maintenance_work_orders w
LEFT JOIN vehicles v ON v.id = w.vehicle_id
LEFT JOIN trailers t ON t.id = w.trailer_id
LEFT JOIN users    u ON u.id = w.assigned_to
WHERE w.is_deleted = false;

CREATE OR REPLACE VIEW vw_monthly_costs AS
SELECT date_trunc('month', w.completed_at)::date AS month,
       count(*)                        AS work_order_count,
       coalesce(sum(w.labor_cost), 0)  AS labor_cost,
       coalesce(sum(w.parts_cost), 0)  AS parts_cost,
       coalesce(sum(w.total_cost), 0)  AS total_cost
FROM maintenance_work_orders w
WHERE w.is_deleted = false AND w.status = 'Completed' AND w.completed_at IS NOT NULL
GROUP BY date_trunc('month', w.completed_at);

-- Dashboard (depends on the views above) -----------------------------------
CREATE OR REPLACE VIEW vw_dashboard_summary AS
SELECT
    (SELECT count(*) FROM vehicles WHERE is_deleted = false)                                  AS total_vehicles,
    (SELECT count(*) FROM vehicles WHERE is_deleted = false AND status = 'Active')            AS active_vehicles,
    (SELECT count(*) FROM trailers WHERE is_deleted = false)                                  AS total_trailers,
    (SELECT count(*) FROM drivers  WHERE is_deleted = false)                                  AS total_drivers,
    (SELECT count(*) FROM maintenance_work_orders
       WHERE is_deleted = false AND status IN ('Open', 'InProgress'))                         AS open_work_orders,
    (SELECT count(*) FROM vw_critical_stocks)                                                 AS critical_stock_count,
    (SELECT count(*) FROM vw_upcoming_maintenances)                                           AS upcoming_maintenance_count,
    (SELECT count(*) FROM vw_expiring_documents)                                              AS expiring_document_count,
    (SELECT count(*) FROM alerts WHERE status = 'Open')                                       AS open_alerts;
