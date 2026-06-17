-- ============================================================================
-- Migration 0012 - Soft-delete aware uniqueness
-- ============================================================================
-- PROBLEM: vehicles.plate_no, vehicles.vin, trailers.plate_no, trailers.vin,
-- drivers.national_id and parts.part_no were declared with a GLOBAL UNIQUE
-- constraint. Because deleted rows are kept (soft delete: is_deleted = true),
-- re-adding a record with the same plate / TC / chassis / part code after a
-- delete hit the unique constraint and surfaced as a 409 ("already exists") or
-- a 500 (DbUpdateException) even though the previous record was logically gone.
--
-- FIX: drop the global UNIQUE constraints and recreate them as PARTIAL UNIQUE
-- indexes filtered on `is_deleted = false`. Uniqueness is now enforced only for
-- the rows the application actually treats as live, so a deleted record never
-- blocks re-adding the same key. VIN / chassis numbers are also filtered on
-- `IS NOT NULL` so the (optional) chassis number stays non-unique when absent.
--
-- Idempotent: safe to run multiple times.
-- ============================================================================

-- --- VEHICLES -------------------------------------------------------------
ALTER TABLE vehicles DROP CONSTRAINT IF EXISTS vehicles_plate_no_key;
ALTER TABLE vehicles DROP CONSTRAINT IF EXISTS vehicles_vin_key;

CREATE UNIQUE INDEX IF NOT EXISTS ux_vehicles_plate_no_active
    ON vehicles (plate_no)
    WHERE is_deleted = false;

CREATE UNIQUE INDEX IF NOT EXISTS ux_vehicles_vin_active
    ON vehicles (vin)
    WHERE is_deleted = false AND vin IS NOT NULL;

-- --- TRAILERS -------------------------------------------------------------
ALTER TABLE trailers DROP CONSTRAINT IF EXISTS trailers_plate_no_key;
ALTER TABLE trailers DROP CONSTRAINT IF EXISTS trailers_vin_key;

CREATE UNIQUE INDEX IF NOT EXISTS ux_trailers_plate_no_active
    ON trailers (plate_no)
    WHERE is_deleted = false;

CREATE UNIQUE INDEX IF NOT EXISTS ux_trailers_vin_active
    ON trailers (vin)
    WHERE is_deleted = false AND vin IS NOT NULL;

-- --- DRIVERS --------------------------------------------------------------
ALTER TABLE drivers DROP CONSTRAINT IF EXISTS drivers_national_id_key;

CREATE UNIQUE INDEX IF NOT EXISTS ux_drivers_national_id_active
    ON drivers (national_id)
    WHERE is_deleted = false AND national_id IS NOT NULL;

-- --- PARTS ----------------------------------------------------------------
ALTER TABLE parts DROP CONSTRAINT IF EXISTS parts_part_no_key;

CREATE UNIQUE INDEX IF NOT EXISTS ux_parts_part_no_active
    ON parts (part_no)
    WHERE is_deleted = false;
