# Cane360 backup and recovery runbook

## Scope and safety

This runbook covers Cane360 PostgreSQL recovery for the pilot. It does not
authorize changes to pilot or production data. A restore must first create or
use an isolated target labelled `AUTOTEST-P8B-*`; never restore over the source
database and never point the application at a restored database until the
incident commander approves a separately reviewed cutover.

Never place connection strings, database passwords, backup contents, or signed
storage URLs in tickets, chat, terminal transcripts, or committed evidence.

## Required protection layers

Production must have all of the following before onboarding:

1. Railway scheduled volume backups: daily, weekly, and monthly schedules.
2. Railway PostgreSQL point-in-time recovery (PITR), with archiving healthy and
   a visible restore window.
3. An encrypted logical backup stored outside the Railway project, with access
   restricted to named recovery operators and retention agreed with the Grower.

Railway documents that volume restores are limited to the same project and
environment, while PITR creates a new sibling PostgreSQL service without
touching the source. Logical backups provide the independent recovery layer.
See [Railway's PostgreSQL backup and restore guide](https://docs.railway.com/guides/postgres-backups-restores)
and [PITR reference](https://docs.railway.com/volumes/point-in-time-recovery).

Do not treat a configured schedule as proof of recovery. The backup is verified
only after a restore rehearsal completes and its evidence is reviewed.

## Recovery objectives

- Daily snapshot coverage implies a worst-case snapshot recovery point of less
  than 24 hours once a successful daily backup exists.
- PITR is the preferred recovery path for a narrower recovery point within the
  provider's displayed archive window.
- The measured recovery time is established by the rehearsal; do not publish an
  unmeasured RTO.
- Backup failures, an unhealthy PITR archive, or a missed daily recovery point
  are Severity 2 until protection is restored.

These are operating objectives, not contractual guarantees. Record the actual
backup timestamps and rehearsal duration.

## Daily verification

The duty operator checks the PostgreSQL service's Backups tab and records only:

- environment and service label;
- newest successful snapshot timestamp;
- PITR archive status and earliest/latest restorable timestamps;
- failure state, if any;
- operator and verification timestamp.

Do not copy provider credentials or connection details into the record. Escalate
an absent/failed daily backup or unhealthy PITR archive using the incident
runbook.

## Logical backup

Install compatible PostgreSQL client tools on the controlled operator machine.
Create an already-encrypted destination directory outside the repository, then
provide the source URL only through the shell environment:

```bash
export CANE360_BACKUP_SOURCE_URL='<secret supplied outside logs>'
export CANE360_BACKUP_OUTPUT_DIRECTORY='/approved/encrypted/location'
export CANE360_BACKUP_ENVIRONMENT='production'
scripts/operations/create-logical-backup.sh
unset CANE360_BACKUP_SOURCE_URL
```

The script creates a custom-format dump and SHA-256 sidecar with owner-only
permissions. Move both files to the approved independent store, verify the
checksum after transfer, and apply the agreed retention policy. The repository
must never contain either artifact.

## Quarterly and pre-pilot restore rehearsal

The recovery lead provisions a new, empty PostgreSQL target with a label
starting `AUTOTEST-P8B-`. It must not share pilot data and must not be configured
as an application connection target.

1. Record source backup timestamp, target label, drill owner, and start time.
2. Verify the dump checksum.
3. Supply the isolated target connection only through the environment.
4. Run the guarded restore:

   ```bash
   export CANE360_RESTORE_TARGET_URL='<isolated empty target secret>'
   export CANE360_RESTORE_TARGET_LABEL='AUTOTEST-P8B-YYYYMMDD'
   export CANE360_RESTORE_CONFIRM='RESTORE-EMPTY-AUTOTEST-P8B'
   scripts/operations/rehearse-logical-restore.sh /approved/path/backup.dump
   unset CANE360_RESTORE_TARGET_URL
   ```

   The script refuses a non-test label, missing explicit confirmation, or a
   target containing any user tables.

5. Against the restored target, run `dotnet run --project src/Web --
   --database-status` using an ephemeral environment variable. Expect 17
   applied migrations and zero pending for the Phase 8B baseline.
6. Validate tenant count and per-tenant aggregate counts without exporting row
   contents. Exercise read-only login, farm, field diary, inventory, payroll,
   finance, and mill report checks using only synthetic test tenants.
7. Record restore duration, backup age, validation results, and deviations.
8. Revoke temporary access. Decommissioning the isolated target is a separate,
   explicitly approved provider action; do not automate it from this repository.

## PITR incident recovery

1. Declare an incident, freeze migrations and authoritative mutations, and
   identify the last known-good UTC timestamp from audit evidence.
2. Use Railway PITR to create a new sibling service at that timestamp. Do not
   modify the source.
3. Validate migration state and business aggregates on the fork.
4. Choose either row-level recovery into the source or a connection cutover.
   Require incident commander and data-owner approval.
5. If cutting over, preserve the original read-only, rotate affected secrets,
   deploy, and verify `/api/Health/ready` plus golden-path reads.
6. Enable backup/PITR coverage on the new primary and record a post-incident
   recovery point.

## Evidence record

Use this non-sensitive structure:

```text
Drill ID: P8B-RECOVERY-YYYYMMDD-NN
Environment class: isolated synthetic
Source backup UTC:
Restore started/completed UTC:
Restore duration seconds:
Backup age at restore:
Migration count / pending count:
Synthetic tenant validation:
Operator / reviewer:
Result and follow-up:
```

No automatic deletion of pilot data is permitted. Retention or deletion changes
require Grower agreement and support review, consistent with NFR-013.
