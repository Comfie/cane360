#!/usr/bin/env bash
set -euo pipefail

required_variables=(
  CANE360_RESTORE_TARGET_URL
  CANE360_RESTORE_TARGET_LABEL
  CANE360_RESTORE_CONFIRM
)

for variable_name in "${required_variables[@]}"; do
  if [[ -z "${!variable_name:-}" ]]; then
    echo "Required variable ${variable_name} is not set." >&2
    exit 2
  fi
done

if [[ "${CANE360_RESTORE_TARGET_LABEL}" != AUTOTEST-P8B-* ]]; then
  echo "Restore target label must begin with AUTOTEST-P8B-." >&2
  exit 3
fi

if [[ "${CANE360_RESTORE_CONFIRM}" != "RESTORE-EMPTY-AUTOTEST-P8B" ]]; then
  echo "Set CANE360_RESTORE_CONFIRM=RESTORE-EMPTY-AUTOTEST-P8B after verifying the isolated target." >&2
  exit 4
fi

if [[ $# -ne 1 || ! -f "$1" ]]; then
  echo "Usage: rehearse-logical-restore.sh /absolute/path/to/backup.dump" >&2
  exit 5
fi

for command_name in pg_restore psql; do
  if ! command -v "${command_name}" >/dev/null 2>&1; then
    echo "${command_name} is required. Install the PostgreSQL client tools before running this script." >&2
    exit 6
  fi
done

user_table_count="$(psql "${CANE360_RESTORE_TARGET_URL}" --no-psqlrc --tuples-only --no-align \
  --command="select count(*) from pg_catalog.pg_tables where schemaname not in ('pg_catalog', 'information_schema');")"

if [[ "${user_table_count}" != "0" ]]; then
  echo "Restore refused: the target contains ${user_table_count} user tables." >&2
  exit 7
fi

started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
start_seconds="$(date +%s)"

echo "Restoring into isolated target ${CANE360_RESTORE_TARGET_LABEL}. Connection details will not be printed."
pg_restore \
  --dbname="${CANE360_RESTORE_TARGET_URL}" \
  --no-owner \
  --no-acl \
  --exit-on-error \
  "$1"

end_seconds="$(date +%s)"
duration_seconds="$((end_seconds - start_seconds))"
applied_migrations="$(psql "${CANE360_RESTORE_TARGET_URL}" --no-psqlrc --tuples-only --no-align \
  --command='select count(*) from public."__EFMigrationsHistory";')"

echo "Restore completed."
echo "Target label: ${CANE360_RESTORE_TARGET_LABEL}"
echo "Started UTC: ${started_at}"
echo "Duration seconds: ${duration_seconds}"
echo "Applied migration rows: ${applied_migrations}"
echo "Record these values in the recovery rehearsal evidence; do not record the connection string."
