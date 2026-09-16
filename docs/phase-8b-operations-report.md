# Phase 8B backup, recovery, observability, and support report

Status: code and runbooks implemented on the Phase 8B feature branch;
provider-side operational gates remain open. Phase 8C has not started.

## Baseline

- Starting main: `1633c6e6b41c889545cbde15a51a00ab379a9582`
- Railway Development at Phase 8A closure: 17 applied / 0 pending
- Phase 8A: accepted and merged with no schema change
- Phase 8B schema changes: none expected

## Backup and recovery

- A three-layer production policy now covers scheduled Railway volume backups,
  PITR, and independently stored encrypted logical dumps.
- Logical backup automation creates custom-format dumps, SHA-256 evidence, and
  owner-only artifacts without printing connection details.
- Restore automation refuses non-`AUTOTEST-P8B-*` labels, missing explicit
  confirmation, non-empty targets, and restore errors.
- Recovery guard self-test: passed locally. PostgreSQL client tools are not
  available in this runtime, so no logical dump or restore was performed.
- Actual Railway backup/PITR configuration and an isolated restore remain an
  operator/provider action requiring approved platform access. They must be
  evidenced before pilot onboarding; this report does not claim they ran.

## Observability

- Production console logs use JSON formatting for Railway Log Explorer.
- Request logs use route templates rather than raw paths/query strings and carry
  status, duration, and correlation ID.
- User IDs and usernames were removed from request/performance logs.
- Database status logs no longer expose database server or database name.
- A validated `X-Correlation-ID` is propagated in each API response and remains
  available in safe Problem Details responses and AuditEvent creation.
- `/api/Health/live` and `/api/Health/ready` separate process health from
  dependency readiness; Railway now gates deployments on readiness.
- The current MVP has no background jobs, so job monitoring is not applicable.

## Support operations

- Runbook defines severity, initial-response targets, incident roles,
  correlation-first troubleshooting, escalation, evidence handling, and
  post-incident review.
- Support access is named, least-privilege, purpose-bound, approved, and revoked
  at closure. Shared access, support impersonation, and direct production SQL
  mutation are prohibited.
- Retention changes and pilot-data deletion require Grower agreement and support
  review; there is no automatic pilot deletion.

## Verification

- Solution build: green, zero warnings/errors before final formatter test change.
- Application: 329/329; Web: 91/91; non-gated .NET: 420 passed.
- Frontend: 87/87; ESLint, TypeScript, production build: green. Existing Vite
  807.68 kB main-chunk advisory remains from Phase 8A.
- Integration build: green. MvpGoldenPathAcceptance: 6/6 against isolated
  Railway Development synthetic data.
- EF model parity: clean. Railway Development: 17 applied / 0 pending.
- Recovery guard self-test and shell syntax checks: green.
- Loopback HTTP smoke was unavailable in the local runtime sandbox; endpoint
  controller and correlation middleware tests passed. No production deployment
  or external alert test is claimed.

## Outstanding release evidence

| Item | Severity | Owner/category | Required closure |
| --- | --- | --- | --- |
| Confirm Production scheduled backup configuration | Sev 2 until enabled | Operations/platform | Capture non-sensitive schedule and latest-success evidence |
| Confirm Production PITR archive health | Sev 2 until enabled | Operations/platform | Capture restore window and archiver-health evidence |
| Run isolated logical restore rehearsal | Sev 2 until passed | Recovery lead | Record duration, backup age, migrations, and synthetic validation |
| Configure external continuous health checks and notification recipients | Sev 2 until enabled | Operations/platform | Exercise alert delivery and record result |
| Confirm Railway plan log retention and resource-monitor availability | Sev 3 | Operations/platform | Record plan-specific retention and monitor configuration |

These are explicit operational gates, not silently downgraded code defects.
