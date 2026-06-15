-- ============================================================================
-- ECS Fleet Maintenance & Inventory Management System
-- Migration 0001 - Extensions and enumerated types
-- ============================================================================
-- Run order: this file MUST run first. Enum types are referenced by later
-- migrations. gen_random_uuid() is core in PG13+, pgcrypto is enabled as a
-- safety net; citext gives case-insensitive plates/emails; pg_trgm powers
-- fuzzy search indexes.
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Asset operational status (shared by vehicles and trailers).
CREATE TYPE asset_status AS ENUM ('Active', 'InMaintenance', 'Inactive', 'Retired');

-- Driver employment status.
CREATE TYPE driver_status AS ENUM ('Active', 'OnLeave', 'Inactive');

-- History-tracked assignment lifecycle (vehicle<->trailer, driver<->vehicle).
CREATE TYPE assignment_status AS ENUM ('Active', 'Ended');

-- Maintenance work-order lifecycle.
CREATE TYPE work_order_status AS ENUM ('Draft', 'Open', 'InProgress', 'Completed', 'Cancelled');

-- A work order targets EITHER a vehicle OR a trailer (never both).
CREATE TYPE work_order_target_type AS ENUM ('Vehicle', 'Trailer');

-- Maintenance category.
CREATE TYPE maintenance_type AS ENUM ('Preventive', 'Corrective', 'Inspection', 'Tire', 'Other');

-- Stock movement direction/kind. Stock changes ONLY through these records.
CREATE TYPE stock_movement_type AS ENUM ('In', 'Out', 'Adjustment', 'Return');

-- System alert categories.
CREATE TYPE alert_type AS ENUM ('CriticalStock', 'MaintenanceDue', 'DocumentExpiry');
CREATE TYPE alert_severity AS ENUM ('Info', 'Warning', 'Critical');
CREATE TYPE alert_status AS ENUM ('Open', 'Acknowledged', 'Resolved', 'Dismissed');

-- Documents can belong to a vehicle, trailer, driver or the company itself.
CREATE TYPE document_owner_type AS ENUM ('Vehicle', 'Trailer', 'Driver', 'Company');
CREATE TYPE document_type AS ENUM ('Insurance', 'Inspection', 'License', 'Registration', 'Contract', 'Other');
