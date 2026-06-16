#!/usr/bin/env bash
# Restore a PostgreSQL dump created by scripts/backup.sh (custom -Fc format).
# WARNING: --clean drops existing objects in the target database first.
#   ./scripts/restore.sh ./backups/ecs_fleet-20260616-020000.dump
set -euo pipefail

DUMP_FILE="${1:?usage: restore.sh <dump-file>}"
PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-ecs}"
PGDATABASE="${PGDATABASE:-ecs_fleet}"

if [ ! -f "$DUMP_FILE" ]; then
    echo "Dump file not found: $DUMP_FILE" >&2
    exit 1
fi

echo "Restoring ${DUMP_FILE} into ${PGDATABASE}@${PGHOST}:${PGPORT}"
echo "This will DROP and recreate existing objects. Ctrl-C within 5s to abort."
sleep 5

pg_restore -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" \
    --clean --if-exists --no-owner --single-transaction "$DUMP_FILE"

echo "Restore complete."
