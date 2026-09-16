#!/usr/bin/env bash
set -euo pipefail

required_variables=(
  CANE360_BACKUP_SOURCE_URL
  CANE360_BACKUP_OUTPUT_DIRECTORY
  CANE360_BACKUP_ENVIRONMENT
)

for variable_name in "${required_variables[@]}"; do
  if [[ -z "${!variable_name:-}" ]]; then
    echo "Required variable ${variable_name} is not set." >&2
    exit 2
  fi
done

if ! command -v pg_dump >/dev/null 2>&1; then
  echo "pg_dump is required. Install the PostgreSQL client tools before running this script." >&2
  exit 3
fi

if [[ ! -d "${CANE360_BACKUP_OUTPUT_DIRECTORY}" ]]; then
  echo "The backup output directory must already exist." >&2
  exit 4
fi

umask 077
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_name="cane360-${CANE360_BACKUP_ENVIRONMENT}-${timestamp}.dump"
backup_path="${CANE360_BACKUP_OUTPUT_DIRECTORY%/}/${backup_name}"
checksum_path="${backup_path}.sha256"

echo "Creating a logical backup for ${CANE360_BACKUP_ENVIRONMENT}. Connection details will not be printed."
pg_dump "${CANE360_BACKUP_SOURCE_URL}" \
  --format=custom \
  --no-owner \
  --no-acl \
  --file="${backup_path}"

if command -v shasum >/dev/null 2>&1; then
  (cd "${CANE360_BACKUP_OUTPUT_DIRECTORY}" && shasum -a 256 "${backup_name}" > "${backup_name}.sha256")
elif command -v sha256sum >/dev/null 2>&1; then
  (cd "${CANE360_BACKUP_OUTPUT_DIRECTORY}" && sha256sum "${backup_name}" > "${backup_name}.sha256")
else
  echo "A SHA-256 tool is required to verify the backup artifact." >&2
  exit 5
fi

chmod 600 "${backup_path}" "${checksum_path}"
echo "Logical backup and checksum created in the requested output directory."
