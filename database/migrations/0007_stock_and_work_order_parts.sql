-- ============================================================================
-- Migration 0007 - Stock movements (ledger) and work-order parts
-- ============================================================================
-- stock_movements is an append-only ledger. quantity is SIGNED:
--   In / Return  -> positive   |   Out -> negative   |   Adjustment -> non-zero
-- balance_after is the resulting parts.quantity_in_stock, stamped by the trigger
-- in 0009 (which also blocks the movement if it would drive stock negative).
--
-- work_order_parts has a 1:1 link to the Out movement that consumed the part,
-- making "a part line and its stock movement are created together" a data fact.
-- ============================================================================

CREATE TABLE stock_movements (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    movement_no    text UNIQUE,                          -- auto-generated (trigger) e.g. SM-2026-000123
    part_id        uuid NOT NULL REFERENCES parts(id) ON DELETE RESTRICT,
    warehouse_id   uuid REFERENCES warehouses(id) ON DELETE SET NULL,
    movement_type  stock_movement_type NOT NULL,
    quantity       numeric(14,3) NOT NULL,               -- signed; see header
    unit_cost      numeric(14,2) NOT NULL DEFAULT 0,
    balance_after  numeric(14,3) NOT NULL DEFAULT 0,     -- stamped by trigger
    work_order_id  uuid REFERENCES maintenance_work_orders(id) ON DELETE SET NULL,
    supplier_id    uuid REFERENCES suppliers(id) ON DELETE SET NULL,
    reference_no   text,
    note           text,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT ck_sm_unit_cost CHECK (unit_cost >= 0),
    -- Sign of quantity must match the movement direction.
    CONSTRAINT ck_sm_direction CHECK (
        (movement_type IN ('In', 'Return') AND quantity > 0) OR
        (movement_type = 'Out'        AND quantity < 0) OR
        (movement_type = 'Adjustment' AND quantity <> 0)
    )
);

CREATE INDEX ix_sm_part        ON stock_movements (part_id);
CREATE INDEX ix_sm_work_order  ON stock_movements (work_order_id);
CREATE INDEX ix_sm_created_at  ON stock_movements (created_at);
CREATE INDEX ix_sm_type        ON stock_movements (movement_type);

CREATE TABLE work_order_parts (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    work_order_id      uuid NOT NULL REFERENCES maintenance_work_orders(id) ON DELETE CASCADE,
    part_id            uuid NOT NULL REFERENCES parts(id) ON DELETE RESTRICT,
    quantity           numeric(14,3) NOT NULL,
    unit_cost          numeric(14,2) NOT NULL,           -- captured at time of consumption
    line_total         numeric(14,2) GENERATED ALWAYS AS (quantity * unit_cost) STORED,
    stock_movement_id  uuid UNIQUE REFERENCES stock_movements(id) ON DELETE RESTRICT,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT ck_wop_qty  CHECK (quantity > 0),
    CONSTRAINT ck_wop_cost CHECK (unit_cost >= 0)
);

CREATE INDEX ix_wop_work_order ON work_order_parts (work_order_id);
CREATE INDEX ix_wop_part       ON work_order_parts (part_id);
