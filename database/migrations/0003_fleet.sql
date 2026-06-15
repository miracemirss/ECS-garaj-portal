-- ============================================================================
-- Migration 0003 - Fleet master data (vehicles, trailers, drivers)
-- ============================================================================
-- Vehicles and trailers are SEPARATE tables (core requirement). Maintenance
-- scheduling fields live on the vehicle so upcoming-maintenance can be derived
-- by either kilometre or date.
-- ============================================================================

CREATE TABLE vehicles (
    id                            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plate_no                      citext NOT NULL UNIQUE,
    vin                           text UNIQUE,                 -- chassis number
    brand                         text NOT NULL,
    model                         text,
    model_year                    int,
    color                         text,
    status                        asset_status NOT NULL DEFAULT 'Active',
    current_odometer_km           int NOT NULL DEFAULT 0,
    purchase_date                 date,

    -- Preventive-maintenance scheduling
    maintenance_interval_km       int,
    maintenance_interval_days     int,
    last_maintenance_date         date,
    last_maintenance_odometer_km  int,
    next_maintenance_km           int,
    next_maintenance_date         date,

    -- Compliance dates (feed vw_expiring_documents / alerts when no document row exists)
    inspection_due_date           date,
    insurance_due_date            date,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    is_deleted  boolean NOT NULL DEFAULT false,
    deleted_at  timestamptz,
    deleted_by  uuid REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT ck_vehicles_odometer        CHECK (current_odometer_km >= 0),
    CONSTRAINT ck_vehicles_model_year      CHECK (model_year IS NULL OR model_year BETWEEN 1950 AND 2100),
    CONSTRAINT ck_vehicles_interval_km     CHECK (maintenance_interval_km IS NULL OR maintenance_interval_km > 0),
    CONSTRAINT ck_vehicles_interval_days   CHECK (maintenance_interval_days IS NULL OR maintenance_interval_days > 0)
);

CREATE TABLE trailers (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    plate_no             citext NOT NULL UNIQUE,
    trailer_type         text,                       -- 'Tent', 'Frigo', 'Lowbed', ...
    brand                text,
    model                text,
    model_year           int,
    status               asset_status NOT NULL DEFAULT 'Active',
    capacity_kg          numeric(12,2),
    purchase_date        date,
    inspection_due_date  date,
    insurance_due_date   date,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    is_deleted  boolean NOT NULL DEFAULT false,
    deleted_at  timestamptz,
    deleted_by  uuid REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT ck_trailers_model_year CHECK (model_year IS NULL OR model_year BETWEEN 1950 AND 2100),
    CONSTRAINT ck_trailers_capacity   CHECK (capacity_kg IS NULL OR capacity_kg >= 0)
);

CREATE TABLE drivers (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    first_name          text NOT NULL,
    last_name           text NOT NULL,
    national_id         text UNIQUE,                 -- TC kimlik no
    phone               text,
    email               citext,
    license_no          text,
    license_class       text,                        -- 'C', 'CE', 'D', ...
    license_expiry_date date,
    status              driver_status NOT NULL DEFAULT 'Active',
    hire_date           date,
    birth_date          date,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    is_deleted  boolean NOT NULL DEFAULT false,
    deleted_at  timestamptz,
    deleted_by  uuid REFERENCES users(id) ON DELETE SET NULL
);

-- Performance / search indexes
CREATE INDEX ix_vehicles_status            ON vehicles (status) WHERE is_deleted = false;
CREATE INDEX ix_vehicles_next_maint_date   ON vehicles (next_maintenance_date) WHERE is_deleted = false;
CREATE INDEX ix_vehicles_plate_trgm        ON vehicles USING gin ((plate_no::text) gin_trgm_ops);
CREATE INDEX ix_trailers_status            ON trailers (status) WHERE is_deleted = false;
CREATE INDEX ix_trailers_plate_trgm        ON trailers USING gin ((plate_no::text) gin_trgm_ops);
CREATE INDEX ix_drivers_status             ON drivers (status) WHERE is_deleted = false;
CREATE INDEX ix_drivers_license_expiry     ON drivers (license_expiry_date) WHERE is_deleted = false;
