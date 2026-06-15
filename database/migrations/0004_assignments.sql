-- ============================================================================
-- Migration 0004 - History-tracked assignments
-- ============================================================================
-- Relationships are kept as full history: each row has started_at/ended_at.
-- The ACTIVE row is the one with ended_at IS NULL. Partial unique indexes make
-- "only one active link per side" a hard, race-proof database guarantee.
-- ============================================================================

CREATE TABLE vehicle_trailer_assignments (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    vehicle_id  uuid NOT NULL REFERENCES vehicles(id) ON DELETE RESTRICT,
    trailer_id  uuid NOT NULL REFERENCES trailers(id) ON DELETE RESTRICT,
    status      assignment_status NOT NULL DEFAULT 'Active',
    started_at  timestamptz NOT NULL DEFAULT now(),
    ended_at    timestamptz,
    note        text,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    ended_by    uuid REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT ck_vta_period       CHECK (ended_at IS NULL OR ended_at >= started_at),
    CONSTRAINT ck_vta_status_state CHECK (
        (status = 'Active' AND ended_at IS NULL) OR
        (status = 'Ended'  AND ended_at IS NOT NULL)
    )
);

-- A vehicle can have at most ONE active trailer; a trailer at most ONE active vehicle.
CREATE UNIQUE INDEX ux_vta_active_vehicle ON vehicle_trailer_assignments (vehicle_id) WHERE ended_at IS NULL;
CREATE UNIQUE INDEX ux_vta_active_trailer ON vehicle_trailer_assignments (trailer_id) WHERE ended_at IS NULL;
CREATE INDEX ix_vta_vehicle ON vehicle_trailer_assignments (vehicle_id);
CREATE INDEX ix_vta_trailer ON vehicle_trailer_assignments (trailer_id);

CREATE TABLE driver_vehicle_assignments (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    driver_id   uuid NOT NULL REFERENCES drivers(id)  ON DELETE RESTRICT,
    vehicle_id  uuid NOT NULL REFERENCES vehicles(id) ON DELETE RESTRICT,
    status      assignment_status NOT NULL DEFAULT 'Active',
    started_at  timestamptz NOT NULL DEFAULT now(),
    ended_at    timestamptz,
    note        text,

    created_at  timestamptz NOT NULL DEFAULT now(),
    created_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at  timestamptz,
    updated_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    ended_by    uuid REFERENCES users(id) ON DELETE SET NULL,

    CONSTRAINT ck_dva_period       CHECK (ended_at IS NULL OR ended_at >= started_at),
    CONSTRAINT ck_dva_status_state CHECK (
        (status = 'Active' AND ended_at IS NULL) OR
        (status = 'Ended'  AND ended_at IS NOT NULL)
    )
);

-- Required: a driver can be actively assigned to at most ONE vehicle.
CREATE UNIQUE INDEX ux_dva_active_driver  ON driver_vehicle_assignments (driver_id)  WHERE ended_at IS NULL;
-- Sensible extra (one active driver per vehicle). Drop this index if co-driver
-- (team driving) on the same vehicle must be supported.
CREATE UNIQUE INDEX ux_dva_active_vehicle ON driver_vehicle_assignments (vehicle_id) WHERE ended_at IS NULL;
CREATE INDEX ix_dva_driver  ON driver_vehicle_assignments (driver_id);
CREATE INDEX ix_dva_vehicle ON driver_vehicle_assignments (vehicle_id);
