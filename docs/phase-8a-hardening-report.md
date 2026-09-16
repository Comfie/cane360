# Phase 8A hardening report

Status: implementation and available automated validation complete; release review required. Phase 8B and Phase 8C were not started.

## Baseline

- Main and origin/main were synchronized at `43592ff0eb18b891340cf419d1e294e32faea774` before the branch was created.
- Branch: `feat/pilot-hardening-release-quality`.
- Railway Development started at 17 applied migrations, 0 pending. Latest migration: `20260914052225_AddMillRecordsAndStatementReconciliation`.
- Initial EF model parity was clean.
- Initial regressions: Application 305, Web 76, non-gated .NET 381, Phase 7C focused 40, frontend 82, Phase7CPostMigration 18/18. Solution/integration builds, ESLint, TypeScript and production build were green.
- Initial production bundle: application JS 916.38 kB minified / 173.68 kB gzip; CSS 200.58 / 29.99 kB.
- The pre-existing untracked `docs/ ui-reference/` directory was preserved.

## UX

### Responsive results

The protected-route Chromium runner exercised Dashboard, Farm, Fields, Activities, Labour, Payroll, Inventory, Finance, Reports and Administration at 1440, 1024, 768, 390 and 360 CSS pixels. All 50 route/viewport combinations passed the page-overflow check. Twelve keyboard/form assertions covered navigation and representative Farm, Personnel and Finance dialogs.

Populated, intercepted UI checks separately exercised:

- Mill ticket and statement registers: 50-row first page and five-row second page, filters, authoritative totals, table/card switch and 44 px actions at all five widths.
- Finance transactions: 50-row first page and five-row second page, filter preservation, authoritative totals, table/card switch and 44 px actions at all five widths.

Fixes include fluid filter grids, shrink-safe workspace tracks, compact intact mobile tabs, mobile card conversions and 44 px coarse-pointer targets. There was no page-level horizontal overflow at 360 px in the exercised routes. Screenshots are retained under `artifacts/p8a-*.png`.

### Accessibility results

- A shared dialog focus hook traps Tab/Shift+Tab, focuses the dialog, closes on Escape and restores prior focus. It is applied to the custom confirmation, Farm, Personnel, line-profile and input-workflow dialogs.
- A global `:focus-visible` outline remains enabled. No production style suppresses focus outlines.
- Mobile/coarse-pointer actions use a 44 px minimum.
- Icon-only dismiss/actions have accessible names; decorative icons are hidden from assistive technology in reviewed components.
- Errors use an assertive live region and safe actionable copy. Loading and success regions use polite live announcements where implemented.
- Status text is present in addition to colour. Responsive card conversions retain explicit labels.
- Print tables repeat headings and avoid row breaks where supported.

This is not a formal WCAG conformance certification. Remaining manual review areas are listed under Known issues.

### Browser results

- Chromium 154.0.8037.17: protected-route responsive and keyboard suites passed.
- Google Chrome 154.0.8037.17: current-browser 390 px login-shell render passed.
- Microsoft Edge 153.0.4234.32: current-browser 390 px login-shell render passed.
- Firefox 150.0.3 was launched, but the local headless runtime failed in its plugin-container/render sandbox; no pass is claimed.
- Safari and previous-major Chrome/Edge runtimes were unavailable for automation; no pass is claimed.

The branded Chrome/Edge checks validate the unauthenticated shell only. Authenticated golden paths were exercised in Chromium, not in every branded engine.

## Performance

### Dataset profile

Railway fixture `AUTOTEST-P8A-PERF-20260915051901-120db0f451294f33b38eaf3787a4cc86` is an isolated retained synthetic tenant. It contains 1 farm, 50 fields, 100 cycles, 2,000 activities, 250 workers, 5,000 attendance records, 3,000 inventory movements, 1,000 confirmed work/payroll earning lines, 2,000 finance transactions, 500 direct-cost postings, 2,000 tickets, 100 statements and 10,000 audit events. No fixture cleanup ran.

The reproducible Railway diagnostic is `Phase8APerformanceTests`. The HTTP harness is `src/Web/ClientApp/scripts/p8a-performance.mjs`; it requires an environment-only authorization value and a fixture manifest, rejects non-local API targets and excludes file transfer.

### Stable measurements

Each row used one warmup and 20 measured runs against the labelled Railway fixture.

| Workload | Sample | p50 ms | p95 ms | Max ms | Gate |
| --- | ---: | ---: | ---: | ---: | --- |
| Dashboard farm setup | 50 fields / 100 cycles | 559.71 | 583.88 | 619.12 | Pass `<2s` |
| Worker register | 250 workers | 986.63 | 1,004.98 | 1,006.56 | Pass `<2s` |
| Farm reference context | reference graph | 372.02 | 392.22 | 553.89 | Pass `<2s` |
| Ticket page service | 50 / 2,000 | 1,095.81 | 1,256.91 | 1,269.87 | Pass `<2s` |
| Statement page service | 50 / 100 | 1,085.57 | 1,146.66 | 1,255.56 | Pass `<2s` |
| Statement reconciliation report | all 100 | 1,100.16 | 1,377.31 | 1,457.83 | Pass `<4s` |
| Finance transaction page | 50 / 2,000 | 736.90 | 906.82 | 958.58 | Pass `<2s` |
| Payroll run summary | 1 run | 1,275.38 | 1,432.52 | 1,445.23 | Pass `<2s` |
| Selected payroll detail | 1,000 earning lines | 2,043.28 | 2,238.82 | 2,373.39 | Pass `<4s` |
| Raw ticket repository control | all 2,000 | 554.30 | 605.49 | 1,129.67 | Pass `<2s` |

An adverse 20-run sample was retained rather than discarded: unrelated service reads simultaneously measured p95 6.8–12.5 seconds while the raw 2,000-ticket query remained p95 564 ms. This demonstrates substantial Railway public-TCP/runtime variance. It does not replace the stable passing results and must be considered during release review. Authenticated 50-run HTTP acceptance was not executed because the retained performance identity intentionally has no reusable credential.

### Query and pagination changes

- Dashboard/farm setup, worker register, Finance, Mill and read-only payroll views use purpose-specific reference contexts rather than hydrating activity/evidence histories.
- Payroll list rows no longer eagerly load calculation and approval graphs. Selected detail is lazy-loaded; the 1,000-line calculation uses one measured query shape.
- Ticket, statement and Finance lists use server-side 50-row pages, deterministic ID tie-breakers, filter-before-page semantics and authoritative filtered totals.
- Ticket and statement mappings removed per-row query patterns. Finance page data and scalar summaries are returned from a consolidated query shape.
- Inventory movements are repository-capped at 500 and the workspace exposes the most recent 100. Labour attendance/work browser calls are date-scoped. Ticket/statement/transaction histories are server-paged.
- No index was added: measured query evidence did not justify a schema change.

Payroll periods/runs/advances, workers and receipt histories remain bounded by realistic pilot usage rather than a new paging contract; their long-horizon strategy is a release-review item.

### Bundle result

Final measured production output:

- Main JS: 807.68 kB / 151.78 kB gzip.
- Payroll route: 58.36 / 13.46 kB gzip.
- Finance route: 59.11 / 13.70 kB gzip.
- CSS: 202.02 / 30.31 kB gzip.

The startup gzip payload improved by 21.90 kB (12.6%) from baseline. Payroll and Finance are lazy route chunks. The minified main chunk remains above Vite's 500 kB advisory; the warning threshold was not raised and broader routing/auth refactoring was rejected as disproportionate Phase 8A risk.

## Exports

- Weighbridge register, statement reconciliation and leakage-exception CSV paths were reviewed and hardened.
- Mill screen pages and exports share the same current-record filtering/mapping rules and authoritative summaries. A transaction-scoped correction test proved superseded originals are excluded globally and screen/export rows and totals reconcile.
- Export metadata includes farm, report name, exact filters, generated timestamp and source context where applicable.
- CSV columns and ordering are deterministic; numbers/dates are invariant formatted; output is UTF-8.
- Shared `CsvCell` protection neutralizes user text beginning with `=`, `+`, `-` or `@`, including leading whitespace/control characters, without changing genuine numeric values. Explicit Unicode, delimiter, quote and formula tests pass.
- National IDs and mobile-money recipients remain masked in ordinary/exported views. Evidence storage keys are absent from ordinary DTOs.
- Mill and leakage exports create existing-domain audit facts containing actor, tenant/farm, report type, filter/reference and generation time without copying sensitive report contents.
- Payroll payslip/cash-register and budget-variance print views show farm, period/version or report context and generated/as-of time. Print CSS repeats table headings, avoids split rows and retains high-contrast output.

No new PDF engine was introduced.

## Security

- Existing cross-tenant database suites and the six golden paths use isolated synthetic tenants. Page tests explicitly verify foreign-tenant ticket, statement and Finance identifiers yield empty tenant-safe results/counts.
- Tenant-safe NotFound responses no longer echo record identifiers. Validation/authorization/conflict responses are normalized; unexpected failures return safe text plus `traceId` and do not expose stack/SQL/storage detail.
- EF concurrency failures map to HTTP 409. Frontend conflict copy instructs refresh/review/retry, and stale responses are reconciled from the server.
- Full National ID reveal is Grower-only. A focused test proves Farm Manager rejection occurs before lookup, audit persistence or decryption.
- Mutation guards suppress synchronous duplicate UI submissions while retaining database idempotency as authoritative protection.
- Source/artifact filename-safe scans found no PostgreSQL URL, private-key header, common OpenAI/AWS key form or Railway password pattern. Reviewed DTOs do not expose evidence storage keys. Synthetic protected data is random/fake and labelled.
- Request timing logs contain method, route template, status, duration and correlation reference; no tenant/business/sensitive identifier was added.

## Golden paths

`MvpGoldenPathAcceptance` passed 6/6 on Railway Development after the final query changes:

| Scenario | Result |
| --- | --- |
| A. Field diary/history | Pass |
| B. Input accountability closure | Pass |
| C. Leakage exception uniqueness | Pass |
| D. Piece-rate payroll calculation | Pass |
| E. Advance recovery | Pass |
| F. Correction/replacement integrity | Pass |

Phase7CPostMigration also passed 18/18 after the hardening changes.

## Regression

Final results: Application 329/329, Web 80/80, total non-gated .NET 409, frontend 87/87, Phase7CPostMigration 18/18 and MvpGoldenPathAcceptance 6/6. The focused dashboard/worker and payroll performance acceptance tests also passed.

- Solution and integration-project builds: green, zero warnings/errors.
- ESLint: green.
- TypeScript: green.
- Production build: green; Vite advisory documented above.
- EF parity: clean in the latest Railway acceptance setup.
- `git diff --check`: clean at the recorded checkpoint.
- No migration was applied, no unrelated AUTOTEST data was cleaned, and no commit or push was performed.

## Database

No Phase 8A migration is required. There is no model or schema change. All 17 applied migrations remain unaltered; Railway Development remains 17 applied / 0 pending with clean EF model parity.

## Known issues

| Severity | Issue | Workaround / owner-category |
| --- | --- | --- |
| Release QA | Authenticated 50-run HTTP harness lacks an environment-provided reusable synthetic authorization value | Supply an ephemeral token for the retained AUTOTEST-P8A identity and run the local-only harness; release QA |
| Release QA | Previous-major Chrome/Edge and Safari runtimes were unavailable; Firefox headless failed locally | Exercise in the release browser farm; release QA |
| Moderate / accessibility | Formal contrast tooling, background inertness and every field-level error association were not exhaustively certified | Manual WCAG 2.2 AA audit with assistive technology; frontend QA |
| Moderate / pagination | Payroll/worker/receipt long-horizon lists do not yet expose explicit page navigation; current pilot shapes are bounded/date-scoped or low-growth | Add paging before tenant volumes exceed the documented pilot profile; backend/frontend |
| Advisory | Main minified Vite chunk remains above 500 kB despite a 12.6% gzip startup reduction | Revisit route boundaries only with measured pilot telemetry; frontend |
| Product baseline | Reports and Administration routes remain intentionally non-operational placeholders; reports live in their authoritative workspaces | Do not represent placeholders as completed report/audit workspaces; product |

No Sev-1 defect was found. The release reviewer must decide whether the unavailable browser/HTTP environments and the long-horizon pagination item block pilot sign-off. Phase 8B/8C remain untouched.
