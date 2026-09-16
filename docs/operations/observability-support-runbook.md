# Cane360 observability and support runbook

## Operational signals

Cane360 emits single-line JSON logs to standard output in production. Railway
captures these logs and makes structured fields searchable in Log Explorer.
Request events include the endpoint route template, status, elapsed time, and a
validated correlation ID; they exclude query strings, form values, user names,
user IDs, tenant IDs, and database endpoints.

Railway's deployment health check uses `/api/Health/ready`. It returns `503`
when PostgreSQL is unavailable. `/api/Health/live` confirms only that the API
process can answer and does not query PostgreSQL. The legacy `/api/Health`
remains a readiness alias.

Railway health checks are deployment gates, not continuous monitoring. See the
[Railway health-check reference](https://docs.railway.com/deployments/healthchecks).
Continuous availability checks must poll the public liveness and readiness
endpoints from outside Railway.

## Correlation workflow

- Cane360 accepts `X-Correlation-ID` only when it is one value, at most 64
  characters, containing letters, digits, `.`, `_`, or `-`.
- An absent or unsafe value is replaced with a random support reference.
- Every API response returns the accepted/generated value in the same header.
- Safe API failures also expose it as `traceId` in Problem Details.
- Support searches structured logs by correlation ID and time window, then uses
  AuditEvent records for authorized business-action context.

Do not ask users for cookies, bearer tokens, passwords, full National IDs,
mobile-money recipients, or screenshots that reveal those values.

## Railway dashboard and alerts

Before pilot onboarding, configure the Production environment with:

- external HTTPS uptime checks for `/api/Health/live` and
  `/api/Health/ready`;
- immediate failed-deployment and repeated-restart notification;
- resource monitors for sustained CPU, memory, and disk pressure appropriate to
  the provisioned service limits;
- PostgreSQL volume-capacity monitoring;
- a named primary and secondary notification recipient;
- a monthly alert-delivery test.

Railway documents environment-wide structured-log search and plan-dependent log
retention in [Logs](https://docs.railway.com/observability/logs). Its native
resource monitors and project webhooks are described in the
[alerting guide](https://docs.railway.com/guides/alerts-crashes-failed-deploys).
Record the configured plan and retention window; do not assume indefinite log
retention.

Suggested log queries:

```text
@level:error
@CorrelationId:<support-reference>
@EndpointRoute:/api/Health/ready AND @StatusCode:503
@Elapsed:>2000
```

Field names must be confirmed in Log Explorer after deployment because Railway
normalizes some logger fields. Do not create alerts that include sensitive log
content in notification bodies.

## Severity and response

| Severity | Example | Initial response | Escalation |
| --- | --- | --- | --- |
| Sev 1 | Confirmed tenant disclosure, destructive corruption, or all users unable to perform authoritative work | 15 minutes | Incident commander, security/data owner, engineering lead immediately |
| Sev 2 | Readiness failure, repeated crashes, backup/PITR unhealthy, or a core workflow unavailable | 30 minutes | Engineering lead and operations owner |
| Sev 3 | Degraded performance with workaround, isolated export/report failure | 4 business hours | Product/engineering queue |
| Sev 4 | Cosmetic or documentation defect | 2 business days | Normal backlog |

For Sev 1/2:

1. Assign incident ID, commander, scribe, severity, and UTC start time.
2. Preserve logs and AuditEvent evidence; never paste secrets into the record.
3. Stop further harm using the narrowest reversible action.
4. Communicate impact and workaround without speculating about cause.
5. Recover using the deployment rollback or backup runbook as appropriate.
6. Validate readiness and affected golden-path operations.
7. Record timeline, root cause, corrective action, and owners.

## Troubleshooting sequence

1. Confirm user-visible time, route/workflow, and correlation ID.
2. Check external liveness, then readiness.
3. Inspect deployment/restart events and resource graphs.
4. Search structured logs by correlation ID; expand only the smallest useful
   time window.
5. Inspect authorized audit history in the affected tenant/farm.
6. Reproduce with synthetic data when possible.
7. Escalate rather than modifying business records directly in PostgreSQL.

There are no application background jobs in the current MVP. Job monitoring is
therefore not applicable; database backups are provider operations and are
checked separately under the recovery runbook.

## Support access

- Use least-privilege project membership and named accounts; shared credentials
  are prohibited.
- Grant elevated provider access only for an incident/change ticket, stated
  purpose, environment, approver, and expiry.
- Application support must use existing authorized roles and AuditEvent-backed
  workflows. Cane360 has no support impersonation or direct-record-edit mode.
- Direct production SQL mutation is prohibited as a troubleshooting technique.
- Sensitive-data reveal remains an explicit authorized action and must retain
  its existing audit event.
- Revoke temporary access at incident/change closure and review the provider
  access log.

## Routine checklist

Daily: external checks green, deployments stable, no sustained error spike,
backups/PITR healthy. Weekly: review Sev 1/2 events, slow requests, resource
headroom, failed authorization patterns, and backup age. Monthly: test alert
delivery, review access membership, verify log retention, and review open
corrective actions. Quarterly: run the isolated recovery rehearsal.
