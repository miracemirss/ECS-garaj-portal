-- ============================================================================
-- Migration 0008 - Operational tables
--   alerts, audit_logs, company_settings, documents, report_files
-- ============================================================================

-- System-generated alerts (critical stock, upcoming maintenance, doc expiry).
CREATE TABLE alerts (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    alert_type       alert_type NOT NULL,
    severity         alert_severity NOT NULL DEFAULT 'Warning',
    status           alert_status NOT NULL DEFAULT 'Open',
    title            text NOT NULL,
    message          text,
    -- Optional subjects (nullable; one will typically be set).
    part_id          uuid REFERENCES parts(id)    ON DELETE CASCADE,
    vehicle_id       uuid REFERENCES vehicles(id) ON DELETE CASCADE,
    trailer_id       uuid REFERENCES trailers(id) ON DELETE CASCADE,
    -- Idempotency key so a generator does not raise duplicate OPEN alerts.
    dedup_key        text,

    created_at       timestamptz NOT NULL DEFAULT now(),
    acknowledged_at  timestamptz,
    acknowledged_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    resolved_at      timestamptz,
    resolved_by      uuid REFERENCES users(id) ON DELETE SET NULL
);

-- At most one OPEN alert per dedup_key.
CREATE UNIQUE INDEX ux_alerts_open_dedup ON alerts (dedup_key) WHERE status = 'Open' AND dedup_key IS NOT NULL;
CREATE INDEX ix_alerts_status   ON alerts (status);
CREATE INDEX ix_alerts_type     ON alerts (alert_type);

-- Append-only audit trail. old_data/new_data hold JSONB snapshots.
CREATE TABLE audit_logs (
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    table_name      text NOT NULL,
    record_id       uuid,
    action          text NOT NULL,            -- INSERT | UPDATE | DELETE
    old_data        jsonb,
    new_data        jsonb,
    changed_columns text[],
    changed_by      uuid REFERENCES users(id) ON DELETE SET NULL,
    changed_by_name text,                      -- denormalized for resilience
    changed_at      timestamptz NOT NULL DEFAULT now(),
    client_ip       inet
);

CREATE INDEX ix_audit_table_record ON audit_logs (table_name, record_id);
CREATE INDEX ix_audit_changed_at   ON audit_logs (changed_at);
CREATE INDEX ix_audit_changed_by   ON audit_logs (changed_by);

-- Single-row company configuration (the 'singleton' column guarantees one row).
CREATE TABLE company_settings (
    id                              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    singleton                       boolean NOT NULL DEFAULT true UNIQUE,
    company_name                    text NOT NULL,
    default_currency                text NOT NULL DEFAULT 'TRY',
    maintenance_due_km_threshold    int  NOT NULL DEFAULT 1000,
    maintenance_due_days_threshold  int  NOT NULL DEFAULT 14,
    document_expiry_days_threshold  int  NOT NULL DEFAULT 30,
    low_stock_check_enabled         boolean NOT NULL DEFAULT true,

    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT ck_settings_singleton CHECK (singleton = true),
    CONSTRAINT ck_settings_thresholds CHECK (
        maintenance_due_km_threshold   >= 0 AND
        maintenance_due_days_threshold >= 0 AND
        document_expiry_days_threshold >= 0
    )
);

-- Documents belonging to a vehicle / trailer / driver / company.
CREATE TABLE documents (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_type    document_owner_type NOT NULL,
    vehicle_id    uuid REFERENCES vehicles(id) ON DELETE CASCADE,
    trailer_id    uuid REFERENCES trailers(id) ON DELETE CASCADE,
    driver_id     uuid REFERENCES drivers(id)  ON DELETE CASCADE,
    document_type document_type NOT NULL,
    title         text NOT NULL,
    file_path     text,
    issue_date    date,
    expiry_date   date,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    is_deleted  boolean NOT NULL DEFAULT false,
    deleted_at  timestamptz,
    deleted_by  uuid REFERENCES users(id) ON DELETE SET NULL,

    -- owner_type must agree with exactly the matching FK being set.
    CONSTRAINT ck_doc_owner CHECK (
        (owner_type = 'Vehicle' AND vehicle_id IS NOT NULL AND trailer_id IS NULL AND driver_id IS NULL) OR
        (owner_type = 'Trailer' AND trailer_id IS NOT NULL AND vehicle_id IS NULL AND driver_id IS NULL) OR
        (owner_type = 'Driver'  AND driver_id  IS NOT NULL AND vehicle_id IS NULL AND trailer_id IS NULL) OR
        (owner_type = 'Company' AND vehicle_id IS NULL AND trailer_id IS NULL AND driver_id IS NULL)
    )
);

CREATE INDEX ix_documents_vehicle ON documents (vehicle_id) WHERE is_deleted = false;
CREATE INDEX ix_documents_trailer ON documents (trailer_id) WHERE is_deleted = false;
CREATE INDEX ix_documents_driver  ON documents (driver_id)  WHERE is_deleted = false;
CREATE INDEX ix_documents_expiry  ON documents (expiry_date) WHERE is_deleted = false;

-- Metadata for generated files (maintenance PDF reports, exported Excel, ...).
CREATE TABLE report_files (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    report_type    text NOT NULL,                   -- 'MaintenanceReport', 'MonthlyCost', ...
    work_order_id  uuid REFERENCES maintenance_work_orders(id) ON DELETE SET NULL,
    file_path      text NOT NULL,
    file_name      text NOT NULL,
    content_type   text NOT NULL DEFAULT 'application/pdf',
    size_bytes     bigint,
    parameters     jsonb,
    generated_at   timestamptz NOT NULL DEFAULT now(),
    generated_by   uuid REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT ck_report_size CHECK (size_bytes IS NULL OR size_bytes >= 0)
);

CREATE INDEX ix_report_files_work_order ON report_files (work_order_id);
CREATE INDEX ix_report_files_type       ON report_files (report_type);
