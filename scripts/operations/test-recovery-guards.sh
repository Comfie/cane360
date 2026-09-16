#!/usr/bin/env bash
set -euo pipefail

script_directory="$(cd "$(dirname "$0")" && pwd)"
restore_script="${script_directory}/rehearse-logical-restore.sh"
temporary_directory="$(mktemp -d)"
trap 'rm -rf "${temporary_directory}"' EXIT
touch "${temporary_directory}/synthetic.dump"

set +e
CANE360_RESTORE_TARGET_URL="not-used" \
CANE360_RESTORE_TARGET_LABEL="Production" \
CANE360_RESTORE_CONFIRM="RESTORE-EMPTY-AUTOTEST-P8B" \
  "${restore_script}" "${temporary_directory}/synthetic.dump" >/dev/null 2>&1
production_status=$?

CANE360_RESTORE_TARGET_URL="not-used" \
CANE360_RESTORE_TARGET_LABEL="AUTOTEST-P8B-guard" \
CANE360_RESTORE_CONFIRM="incorrect" \
  "${restore_script}" "${temporary_directory}/synthetic.dump" >/dev/null 2>&1
confirmation_status=$?
set -e

if [[ "${production_status}" -ne 3 || "${confirmation_status}" -ne 4 ]]; then
  echo "Recovery guard test failed." >&2
  exit 1
fi

echo "Recovery guard tests passed."
