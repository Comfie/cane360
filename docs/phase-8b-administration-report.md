# Phase 8B Administration review

## Scope

The Administration workspace now composes tenant users, fixed role capabilities, activity types, units, inventory application rules, one supported effective-dated farm setting, document categories, and tenant audit. Domain ownership remains with the existing modules. The server is authoritative for membership status, role checks, tenant scope, effective ranges, and audit access.

The only supported farm setting is `ActivityLateEntryReasonDays` (integer 0–30). It controls the existing actual-work late-entry reason check by event date. Worker pay rates remain in Labour & Payroll.

Existing identity supports a single-use FarmManager invitation for the active primary FarmManager person and invitation revocation. It does not provide email delivery, resend, account recovery, password reset, or a general invitation flow. Those operations were not added. Application memberships and operational people remain distinct, with the existing `PersonId` relationship for a FarmManager account.

FarmManager cannot list tenant memberships or invitation details through Administration. Grower can. FarmManager can view the fixed capability matrix and configure the existing permitted reference data. Audit query and export are Grower only. Platform Administrator has no routine tenant configuration path through Administration.

## Migration gate

`20260916193145_AddAdministrationConfiguration` adds `FarmSettings`, `DocumentCategories`, and optional category references and code snapshots on evidence. It does not rewrite existing memberships, audit, inventory, payroll, or mill records. The forward SQL is at `/private/tmp/cane360-p8b-administration-forward.sql` for review.

Railway Development status at the gate: **17 applied / 1 pending**. The migration has not been applied. `Phase8BPostMigration` tests are explicit and must run only after approved application to Railway Development.

SHA-256:

- Migration: `ff3df6094474afc273652f67b19eb19add0f165972bf5ac0144e6440fa5cb2ec`
- Designer: `e994a1929f60ead838fef0b1de18f4ee7b3880d32ecf1491b5a832838e508a16`
- Forward SQL: `c6789a894486c8008dde2b91cef0be0026d2eba2757ce3c4cea042e779e1da26`

## Verification before migration

- Solution build: passed, zero warnings.
- Application tests: 346 passed.
- Web tests: 91 passed.
- Integration project build: passed.
- Frontend tests: 87 passed.
- ESLint, TypeScript, and production build: passed.
- EF model parity: clean.
- `git diff --check`: clean.
- Forward SQL review: additive schema operations and constraints only; no historical data rewrite.

Responsive CSS has compact, scrollable section navigation, stacked mobile forms and cards, and 44px actions. An authenticated browser check at 1440, 1024, 768, 390, and 360 px remains pending after migration approval; the in-app browser tool was unavailable in this session. PostgreSQL acceptance tests also remain pending until migration approval. Do not treat Phase 8B as released before those checks pass.
