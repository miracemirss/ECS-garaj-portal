#!/usr/bin/env bash
# PostgreSQL logical backup with rotation. Designed for cron, e.g. nightly:
#   0 2 * * *  /opt/ecs/scripts/backup.sh >> /var/log/ecs-backup.log 2>&1
#
# Config via env (with sensible defaults):
#   PGHOST PGPORT PGUSER PGPASSWORD PGDATABASE  - libpq connection
#   BACKUP_DIR        - destination directory (default ./backups)
#   RETENTION_DAYS    - delete dumps older than N days (default 14)
set -euo pipefail

PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-ecs}"
PGDATABASE="${PGDATABASE:-ecs_fleet}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"

mkdir -p "$BACKUP_DIR"
STAMP="$(date +%Y%m%d-%H%M%S)"
OUT="${BACKUP_DIR}/${PGDATABASE}-${STAMP}.dump"

echo "[$(date -Is)] backing up ${PGDATABASE} -> ${OUT}"
# Custom format (-Fc): compressed and restorable with pg_restore (parallel-capable).
pg_dump -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -Fc -f "$OUT"

# Integrity smoke check: the dump must list its table of contents.
pg_restore -l "$OUT" >/dev/null
echo "[$(date -Is)] backup OK ($(du -h "$OUT" | cut -f1))"

echo "[$(date -Is)] pruning dumps older than ${RETENTION_DAYS} days"
find "$BACKUP_DIR" -name "${PGDATABASE}-*.dump" -type f -mtime "+${RETENTION_DAYS}" -print -delete
echo "[$(date -Is)] done"
