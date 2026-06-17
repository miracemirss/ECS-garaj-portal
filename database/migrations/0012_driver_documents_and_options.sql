-- ============================================================================
-- Migration 0012 - Driver document date fields and first-stage option guards
-- ============================================================================

ALTER TABLE drivers
    ADD COLUMN IF NOT EXISTS license_start_date date,
    ADD COLUMN IF NOT EXISTS src_start_date date,
    ADD COLUMN IF NOT EXISTS src_end_date date,
    ADD COLUMN IF NOT EXISTS psychotechnical_start_date date,
    ADD COLUMN IF NOT EXISTS psychotechnical_end_date date,
    ADD COLUMN IF NOT EXISTS visa_start_date date,
    ADD COLUMN IF NOT EXISTS visa_end_date date,
    ADD COLUMN IF NOT EXISTS passport_start_date date,
    ADD COLUMN IF NOT EXISTS passport_end_date date,
    ADD COLUMN IF NOT EXISTS document_note text;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_drivers_license_dates') THEN
        ALTER TABLE drivers
            ADD CONSTRAINT ck_drivers_license_dates
            CHECK (license_start_date IS NULL OR license_expiry_date IS NULL OR license_expiry_date >= license_start_date);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_drivers_src_dates') THEN
        ALTER TABLE drivers
            ADD CONSTRAINT ck_drivers_src_dates
            CHECK (src_start_date IS NULL OR src_end_date IS NULL OR src_end_date >= src_start_date);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_drivers_psychotechnical_dates') THEN
        ALTER TABLE drivers
            ADD CONSTRAINT ck_drivers_psychotechnical_dates
            CHECK (psychotechnical_start_date IS NULL OR psychotechnical_end_date IS NULL OR psychotechnical_end_date >= psychotechnical_start_date);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_drivers_visa_dates') THEN
        ALTER TABLE drivers
            ADD CONSTRAINT ck_drivers_visa_dates
            CHECK (visa_start_date IS NULL OR visa_end_date IS NULL OR visa_end_date >= visa_start_date);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_drivers_passport_dates') THEN
        ALTER TABLE drivers
            ADD CONSTRAINT ck_drivers_passport_dates
            CHECK (passport_start_date IS NULL OR passport_end_date IS NULL OR passport_end_date >= passport_start_date);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_drivers_src_end ON drivers (src_end_date) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_drivers_psychotechnical_end ON drivers (psychotechnical_end_date) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_drivers_visa_end ON drivers (visa_end_date) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_drivers_passport_end ON drivers (passport_end_date) WHERE is_deleted = false;
