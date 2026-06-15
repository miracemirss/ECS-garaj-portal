-- ============================================================================
-- Migration 0011 - Seed data (idempotent)
-- ============================================================================
-- Minimal data required to boot the system: roles, an admin user, the company
-- settings singleton and a default warehouse. Safe to run multiple times.
--
-- SECURITY: the admin password_hash below is a PLACEHOLDER. Replace it by
-- setting the password through the application (or update with a real bcrypt
-- hash) before going live. Login will fail until a valid hash is set.
-- ============================================================================

INSERT INTO roles (name, description, is_system) VALUES
    ('Admin',            'Full access',                       true),
    ('FleetManager',     'Manage vehicles, trailers, drivers',true),
    ('Technician',       'Maintenance work orders',           true),
    ('WarehouseManager', 'Inventory and stock',               true),
    ('Viewer',           'Read-only access',                  true)
ON CONFLICT (name) DO NOTHING;

INSERT INTO users (email, full_name, password_hash, is_active)
VALUES ('admin@ecs.local', 'System Administrator', 'REPLACE_WITH_BCRYPT_HASH', true)
ON CONFLICT (email) DO NOTHING;

INSERT INTO user_roles (user_id, role_id)
SELECT u.id, r.id
FROM users u, roles r
WHERE u.email = 'admin@ecs.local' AND r.name = 'Admin'
ON CONFLICT DO NOTHING;

INSERT INTO company_settings (
    singleton, company_name, default_currency,
    maintenance_due_km_threshold, maintenance_due_days_threshold,
    document_expiry_days_threshold, low_stock_check_enabled)
VALUES (true, 'ECS Lojistik', 'TRY', 1000, 14, 30, true)
ON CONFLICT (singleton) DO NOTHING;

INSERT INTO warehouses (code, name, is_active)
VALUES ('MAIN', 'Ana Depo', true)
ON CONFLICT (code) DO NOTHING;
