#!/bin/sh
# Applies ECS SQL migrations in order. If the base schema is already present,
# only idempotent post-base migrations (0011+) are refreshed.
#
# Connection comes from libpq env vars (PGHOST/PGPORT/PGUSER/PGPASSWORD/PGDATABASE).
set -eu

MIGRATIONS_DIR="${MIGRATIONS_DIR:-/database/migrations}"
SEED_FILE="${SEED_FILE:-/database/seed/demo_seed.sql}"

echo "ECS migrator: target ${PGDATABASE:-?}@${PGHOST:-?}:${PGPORT:-5432}"

ALREADY="$(psql -tAc "SELECT to_regclass('public.users') IS NOT NULL;" 2>/dev/null || echo f)"
if [ "$ALREADY" = "t" ]; then
    echo "Schema already present - refreshing idempotent post-base migrations."
    for f in "$MIGRATIONS_DIR"/001[1-9]*.sql; do
        [ -e "$f" ] || continue
        echo "  applying $(basename "$f")"
        psql -v ON_ERROR_STOP=1 -f "$f"
    done
else
    for f in "$MIGRATIONS_DIR"/0*.sql; do
        echo "  applying $(basename "$f")"
        psql -v ON_ERROR_STOP=1 -f "$f"
    done
    echo "Migrations applied."
fi

if [ "${RUN_DEMO_SEED:-false}" = "true" ]; then
    echo "Loading demo seed (RUN_DEMO_SEED=true)..."
    psql -v ON_ERROR_STOP=1 -f "$SEED_FILE"
fi

echo "ECS migrator: done."
