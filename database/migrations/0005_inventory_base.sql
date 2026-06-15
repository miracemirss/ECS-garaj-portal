-- ============================================================================
-- Migration 0005 - Inventory master data (warehouses, suppliers, parts)
-- ============================================================================
-- parts.quantity_in_stock is a CACHED running balance. It must only ever change
-- as the result of a stock_movements row (enforced by triggers in 0009). The
-- CHECK (quantity_in_stock >= 0) is the final backstop against negative stock.
-- ============================================================================

CREATE TABLE warehouses (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code        text NOT NULL UNIQUE,
    name        text NOT NULL,
    address     text,
    is_active   boolean NOT NULL DEFAULT true,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE suppliers (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name        text NOT NULL,
    tax_no      text UNIQUE,
    phone       text,
    email       citext,
    address     text,
    is_active   boolean NOT NULL DEFAULT true,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE parts (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    part_no             text NOT NULL UNIQUE,          -- SKU
    name                text NOT NULL,
    category            text,
    unit                text NOT NULL DEFAULT 'pcs',   -- unit of measure
    quantity_in_stock   numeric(14,3) NOT NULL DEFAULT 0,
    minimum_stock       numeric(14,3) NOT NULL DEFAULT 0,
    unit_cost           numeric(14,2) NOT NULL DEFAULT 0,
    warehouse_id        uuid REFERENCES warehouses(id) ON DELETE SET NULL,
    supplier_id         uuid REFERENCES suppliers(id)  ON DELETE SET NULL,
    is_active           boolean NOT NULL DEFAULT true,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    is_deleted  boolean NOT NULL DEFAULT false,
    deleted_at  timestamptz,
    deleted_by  uuid REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT ck_parts_qty_nonneg   CHECK (quantity_in_stock >= 0),
    CONSTRAINT ck_parts_min_nonneg   CHECK (minimum_stock >= 0),
    CONSTRAINT ck_parts_cost_nonneg  CHECK (unit_cost >= 0)
);

CREATE INDEX ix_parts_warehouse   ON parts (warehouse_id);
CREATE INDEX ix_parts_supplier    ON parts (supplier_id);
CREATE INDEX ix_parts_name_trgm   ON parts USING gin (name gin_trgm_ops);
-- Partial index to quickly find parts at/under their minimum (critical stock).
CREATE INDEX ix_parts_critical    ON parts (id) WHERE is_active AND is_deleted = false AND quantity_in_stock <= minimum_stock;
