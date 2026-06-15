-- ============================================================================
-- Migration 0006 - Maintenance work orders and tasks
-- ============================================================================
-- A work order targets EXACTLY ONE of vehicle/trailer, enforced by a CHECK that
-- ties target_type to which FK is populated. parts_cost is maintained by a
-- trigger (0009) from work_order_parts; total_cost is a generated column.
-- ============================================================================

CREATE TABLE maintenance_work_orders (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    work_order_no      text UNIQUE,                       -- auto-generated (trigger) e.g. WO-2026-000123
    target_type        work_order_target_type NOT NULL,
    vehicle_id         uuid REFERENCES vehicles(id) ON DELETE RESTRICT,
    trailer_id         uuid REFERENCES trailers(id) ON DELETE RESTRICT,
    maintenance_type   maintenance_type NOT NULL DEFAULT 'Corrective',
    status             work_order_status NOT NULL DEFAULT 'Open',
    title              text NOT NULL,
    description        text,

    odometer_before_km int,
    odometer_after_km  int,

    scheduled_date     date,
    started_at         timestamptz,
    completed_at       timestamptz,

    supplier_id        uuid REFERENCES suppliers(id) ON DELETE SET NULL,  -- external service provider
    assigned_to        uuid REFERENCES users(id)     ON DELETE SET NULL,  -- technician

    labor_cost         numeric(14,2) NOT NULL DEFAULT 0,
    parts_cost         numeric(14,2) NOT NULL DEFAULT 0,                   -- maintained by trigger
    total_cost         numeric(14,2) GENERATED ALWAYS AS (labor_cost + parts_cost) STORED,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    is_deleted  boolean NOT NULL DEFAULT false,
    deleted_at  timestamptz,
    deleted_by  uuid REFERENCES users(id) ON DELETE SET NULL,

    -- Target integrity: Vehicle => vehicle_id set & trailer_id null, and vice versa.
    CONSTRAINT ck_wo_target CHECK (
        (target_type = 'Vehicle' AND vehicle_id IS NOT NULL AND trailer_id IS NULL) OR
        (target_type = 'Trailer' AND trailer_id IS NOT NULL AND vehicle_id IS NULL)
    ),
    CONSTRAINT ck_wo_costs        CHECK (labor_cost >= 0 AND parts_cost >= 0),
    CONSTRAINT ck_wo_odometer     CHECK (odometer_before_km IS NULL OR odometer_before_km >= 0),
    CONSTRAINT ck_wo_odometer2    CHECK (
        odometer_after_km IS NULL OR odometer_before_km IS NULL OR odometer_after_km >= odometer_before_km
    ),
    -- A completed work order must record its completion time.
    CONSTRAINT ck_wo_completed    CHECK (status <> 'Completed' OR completed_at IS NOT NULL)
);

CREATE TABLE maintenance_tasks (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    work_order_id   uuid NOT NULL REFERENCES maintenance_work_orders(id) ON DELETE CASCADE,
    description     text NOT NULL,
    is_completed    boolean NOT NULL DEFAULT false,
    labor_hours     numeric(8,2),
    sort_order      int NOT NULL DEFAULT 0,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT ck_task_hours CHECK (labor_hours IS NULL OR labor_hours >= 0)
);

CREATE INDEX ix_wo_vehicle        ON maintenance_work_orders (vehicle_id) WHERE is_deleted = false;
CREATE INDEX ix_wo_trailer        ON maintenance_work_orders (trailer_id) WHERE is_deleted = false;
CREATE INDEX ix_wo_status         ON maintenance_work_orders (status)     WHERE is_deleted = false;
CREATE INDEX ix_wo_scheduled_date ON maintenance_work_orders (scheduled_date);
CREATE INDEX ix_wo_completed_at   ON maintenance_work_orders (completed_at);
CREATE INDEX ix_tasks_work_order  ON maintenance_tasks (work_order_id);
