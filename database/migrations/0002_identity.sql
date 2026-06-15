-- ============================================================================
-- Migration 0002 - Identity & access (users, roles, user_roles, refresh_tokens)
-- ============================================================================
-- users.created_by is a self-referencing nullable FK so the very first (seed)
-- user can be created with a NULL author.
-- ============================================================================

CREATE TABLE users (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email           citext NOT NULL UNIQUE,
    full_name       text   NOT NULL,
    password_hash   text   NOT NULL,
    phone           text,
    is_active       boolean NOT NULL DEFAULT true,
    last_login_at   timestamptz,

    created_at      timestamptz NOT NULL DEFAULT now(),
    created_by      uuid REFERENCES users(id) ON DELETE SET NULL,
    updated_at      timestamptz,
    updated_by      uuid REFERENCES users(id) ON DELETE SET NULL,
    is_deleted      boolean NOT NULL DEFAULT false,
    deleted_at      timestamptz,
    deleted_by      uuid REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE roles (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name         text NOT NULL UNIQUE,            -- 'Admin', 'FleetManager', ...
    description  text,
    is_system    boolean NOT NULL DEFAULT false,  -- protect built-in roles from deletion (enforced in app)
    created_at   timestamptz NOT NULL DEFAULT now(),
    updated_at   timestamptz
);

CREATE TABLE user_roles (
    user_id      uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id      uuid NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    assigned_at  timestamptz NOT NULL DEFAULT now(),
    assigned_by  uuid REFERENCES users(id) ON DELETE SET NULL,
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE refresh_tokens (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id                 uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash              text NOT NULL UNIQUE,   -- store a hash, never the raw token
    expires_at              timestamptz NOT NULL,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by_ip           inet,
    revoked_at              timestamptz,
    revoked_by_ip           inet,
    replaced_by_token_hash  text,
    reason_revoked          text,

    CONSTRAINT ck_refresh_tokens_expiry CHECK (expires_at > created_at)
);

-- Performance indexes
CREATE INDEX ix_users_is_active           ON users (is_active) WHERE is_deleted = false;
CREATE INDEX ix_user_roles_role_id        ON user_roles (role_id);
CREATE INDEX ix_refresh_tokens_user_id    ON refresh_tokens (user_id);
CREATE INDEX ix_refresh_tokens_expires_at ON refresh_tokens (expires_at) WHERE revoked_at IS NULL;
