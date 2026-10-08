# CR-01 — Post-System-Review Enhancements

## Requirements lock (CR-01.0)

CR-01.0, CR-01.1, CR-01.2, CR-01.3 and CR-01.4 are complete. Git closure and validation records are below. CR-01.5 remains unstarted.

| Slice | Scope | State |
| --- | --- | --- |
| CR-01.1 | Farm Owner and Farm Profile | Complete; implemented and migrated to Railway Development |
| CR-01.2 | Field/Crop UX | Complete; implemented, validated and merged into main |
| CR-01.3 | Employee Master | Complete; implemented, validated, migrated to Railway Development and merged into main |
| CR-01.4 | Inventory Category Administration | Complete; implemented, validated, migrated to Railway Development and merged into main |
| CR-01.5 | Regression/Integration | Deferred |
| CR-01.6 | Release Closure | Deferred |

Locked decisions: Farm Owner is the business term; Grower/Farmer internal names remain. Farm Model is an optional category only, with no seeded options or category-driven behavior. Fields, mapping and GPS remain unchanged. Configurable 14-month crop maturity, crop age/Expected Maturity calculations, manual Actual Yield UX, simplified Add Field and extended field Edit/View belong to CR-01.2. Employee details/photos/next of kin belong to CR-01.3. Editable Inventory Categories belong to CR-01.4. NSSA, NEC, PAYE and statutory payroll remain unchanged. Field maps/GIS, lease contracts, investor relationships, ownership percentages, lease dates/payments, mobile/PWA/offline/AI/IoT and Phase 8 work are out of scope.

## Measured technical baseline

- Source branch main; SHA `2d0a76321c0064c62ece9cb095958653f09ac666`.
- Untracked `docs/ ui-reference/` and role-based-access invitation plan preserved.
- The checkout already contains Phase 8A/8B implementation and reports, despite the supplied Phase 7C description. This CR does not extend that work.
- .NET solution build: passed, 0 warnings, 0 errors.
- Application tests: 393 passed, 0 failed, 0 skipped.
- Web/API tests: 115 passed, 0 failed, 0 skipped.
- Frontend: 106 tests, 98 passed, 8 failed (existing source/stylesheet contract assertions); lint and typecheck passed; production build passed with the existing bundle-size advisory.
- Railway Development (existing local secret mechanism): 20 applied migrations, 0 pending. Latest `20260917105247_FilterActiveTenantMembershipUniqueness`.
- Baseline EF pending-model check reports drift; this must be understood and resolved before migration application.
- API regeneration during baseline build exposes existing generated-client drift. Review generated output against current OpenAPI rather than accepting unrelated changes blindly.

## Existing design and CR-01.1 design

GrowerProfile currently has DisplayName and Phone; these are retained. New structured identity, association, registered address, email and photo-reference fields are optional for existing records. Existing names are not parsed or fabricated. Active/inactive profile status is descriptive and does not disable tenant membership or farm access.

National ID currently exists on WorkerProfile, not GrowerProfile. Reuse the existing AES-GCM protector, context binding, mask and keyed fingerprint; never put plaintext/ciphertext in ordinary response DTOs or audit summaries. Reveal remains Grower-only, with requested/succeeded audit facts. Manager can edit profile but cannot reveal ID. Supervisor keeps existing farm read access and cannot edit. Owner uniqueness is tenant-scoped; the system permits only one owner profile per tenant. Worker uniqueness stays unchanged and separate from owner identity.

Farm Model follows the existing tenant reference-data pattern with stable GUID, unique normalized tenant code, name, active flag and optimistic version. No hard-delete endpoint; inactive categories remain visible on already assigned farms but cannot be newly assigned. Composite tenant foreign key prevents cross-tenant assignment. No generic category registry currently fits Farm Model without mixing unrelated semantics.

No general photo storage/upload service exists. Store an optional photograph reference (metadata only), with no blob column, upload service, cloud dependency or automatic external-image fetching. UI makes this limitation explicit.

Profile/master-data edits and category assignment/administration use existing AuditEvent facts with safe summaries. Existing API routes/required fields stay compatible; optional enhancements are additive and omitted enhancements preserve existing profile/model values.

## Verification and migration record

Migration: `20261005202118_AddFarmOwnerProfileEnhancements`, applied successfully to Railway Development after the pre-migration gate. Final state: 21 applied, 0 pending; EF has-pending-model-changes reports no differences.

Forward SQL is additive and transactional: 17 profile columns (optional descriptive/protected fields plus Active default true), one optional FarmModelId column, FarmModels table, reference-data uniqueness, protected-ID shape check, and composite tenant-bound foreign key with restrict deletion. No established columns/tables are dropped or renamed, no existing values are fabricated, no categories are seeded, and no migration rollback or data cleanup was run. The conventional generated Down path removes these new additions; it must not be used against shared Railway Development.

Eight pre-existing read-only properties were explicitly mapped to prevent EF from proposing destructive drops: WorkVerification.SupervisorVerifiedAt; StatementTicketMatch.CompletesMatching; Person.ActiveFrom; PersonRoleAssignment.EffectiveFrom; ManagerInvitation.ExpiresAt; FieldLineProfile.EffectiveFrom; ActivityType.SupportsPlanned and SupportsUnplanned. Their existing database representation and behavior are preserved; they introduce no forward migration operations.

The eight initial frontend failures were source contract assertions affected by earlier formatting. Only the affected assertions were changed to tolerate whitespace while retaining their expected markup, copy, versioned mutation calls and responsive CSS checks. No payroll or inventory production logic changed.

## Final implementation

- Domain: GrowerProfile retains DisplayName/Phone, gains optional structured owner identity/contact/association/photo-reference fields and Active status. National ID uses nullable protected components. Farm optionally references a tenant FarmModel; stable normalized codes, active flag and optimistic version support category administration without deletion or seeds.
- API: existing POST/PUT `/api/FarmSetup/farm` accept optional `ownerProfile`. PUT additionally accepts `farmModelId` and explicit `updateFarmModel`; omitted enhancement data preserves existing values. Blank national ID preserves the current protected identity. Existing response fields remain and new masked/profile/model fields are additive.
- New endpoints: GET `/api/FarmSetup/farm-models`, POST `/api/FarmSetup/farm-models` (create/update with stable code and expected version), POST `/api/FarmSetup/owner/national-id/reveal` (profile ID and plaintext only in the authorized no-store response).
- Frontend: grouped owner creation/edit forms, read-only owner summary, masked ID with authorized reveal/hide, Farm Model selection and add/activate/deactivate controls. Farm summary/personnel and navigation description use Farm Owner where appropriate. Model options come exclusively from the API.
- Authorization: existing Identity cookies and tenant membership resolution remain. Grower and FarmManager retain edit/reference administration access. Supervisor retains farm read access; editor is hidden and backend denies writes. Reveal is Grower-only. No permission framework or expanded Grower edit privilege was introduced.
- Audit: existing AuditEvent records profile creation/update, national ID change, requested/succeeded reveal, model assignment and model administration. Summaries omit identity/contact values. Category facts use FarmModel subject IDs; assignment facts use Farm IDs.
- Generated TypeScript client was regenerated; formatting for unchanged declarations/members was preserved after syntax comparison to avoid unrelated generated-client churn.

## Exact verification results

| Check | Result |
| --- | --- |
| Solution build (including integration project) | Pass; 0 warnings, 0 errors |
| All Application unit tests | 414 passed, 0 failed, 0 skipped |
| All Web/API unit tests | 119 passed, 0 failed, 0 skipped |
| Focused new owner/model/migration tests (subset of Application) | 21 passed, 0 failed, 0 skipped |
| Frontend tests | 110 passed, 0 failed |
| Frontend ESLint | Pass |
| Frontend TypeScript | Pass |
| Production Vite build | Pass; existing bundle-size advisory remains |
| Generated-client method-name check | Pass |
| EF model parity | No pending model changes |
| Railway pre-migration legacy fixture | 1 passed, 0 failed, 0 skipped |
| Railway post-migration acceptance | 5 passed, 0 failed, 0 skipped |
| git diff --check | Pass |

Pre-migration full checks passed before generation, followed by additive-migration safety, scoped SQL review, model parity and full backend regressions before application. Profile creation was also enhanced within CR-01.1 and all affected checks rerun; this introduced no further model/schema changes.

The first post-migration run was 4 passed / 1 failed because the test did not load the application's local National ID key configuration. The fixture was corrected to read the existing NationalId section without overriding the database target. The final five-test run passed; no security keys were invented for the shared database.

Railway acceptance covers a labelled legacy profile surviving migration with null new identity fields; persisted new profile fields; ciphertext/masking and audited reveal; Farm Model assignment; rejected duplicate tenant category code; rejected composite cross-tenant foreign key; tenant-scoped reference/workspace queries; and existing field/draft crop-cycle loading. All queries and writes use synthetic labels/IDs. Constraint tests roll back their own uncommitted transactions. No real tenant was sampled or modified. Synthetic committed fixtures are retained, including legacy label `AUTOTEST-CR01-LEGACY-20261005-7d725ce2f8994c449741de9b05e386a4`.

Browser checks used intercepted synthetic API responses: profile creation and edit/save, invalid email/farm code validation, and no page/dialog horizontal overflow at 1440, 768, 390 and 360 CSS px. These are frontend flow checks, not a claim of authenticated browser testing against Railway. Database-backed application handlers and repository/constraint behavior were tested separately. Local screenshots: `artifacts/cr01-owner-profile-mobile.png` and `artifacts/cr01-owner-editor-mobile.png`.

## Limitations and closure boundary

Photographs are metadata references only: no upload, storage service or external-image fetching. Structured names remain optional and existing display names are not split. Inactive owner status is descriptive and does not revoke access. Only one owner profile exists per tenant, so duplicate owners/IDs within a tenant are already prevented by profile uniqueness; worker national-ID uniqueness remains separate and unchanged. Category rename/update is supported by API; the compact UI exposes creation and activation/deactivation. Existing bundle-size advisory remains. Synthetic acceptance records are intentionally retained.

Implementation branch: `feature/cr-01-1-farm-owner-profile`. Baseline commit: `2d0a76321c0064c62ece9cb095958653f09ac666`. Original untracked documentation and pre-existing Phase 8 work are preserved.

## Git closure

The closure pre-commit gate passed: solution build with 0 warnings/errors, Application 414/414, Web/API 119/119, frontend 110/110, ESLint and TypeScript checks. EF reports no pending model changes. The read-only Railway Development status check confirms 21 applied migrations and 0 pending; `20261005202118_AddFarmOwnerProfileEnhancements` is already applied and is not reapplied during Git closure.

Feature commit: `d70cb6b3daae0038fde76472a3347af14e8e206f` (`feat(cr-01): add farm owner profile enhancements`), pushed to `origin/feature/cr-01-1-farm-owner-profile`; the feature branch is retained.

Merge commit: `e49a4a6be26c8630f59627dec38e41e5f6b3006c`, a regular two-parent merge into main with no squash, history rewrite or conflicts. This is also the main SHA at final executable verification. The final main tip includes the subsequent documentation-only closure commit; resolve its full SHA with `git log -1 --format=%H --grep="^docs(cr-01): record git closure$" main`. Its own SHA cannot be embedded in its contents; the final user closure report records it explicitly.

Post-merge gate on main: solution build passed with 0 warnings/errors; Application 414 passed, Web/API 119 passed, frontend 110 passed, all with 0 failures and 0 skips. ESLint, TypeScript, generated-client name check and production Vite build passed. EF reports no pending model changes. Railway Development reports 21 applied migrations and 0 pending. Existing CR-01 post-migration operational acceptance passed 5/5 with 0 failures/skips, covering protected identity/reveal/audit, legacy profiles, Farm Model constraints and tenant isolation, plus existing fields/crop cycles. Migration `20261005202118_AddFarmOwnerProfileEnhancements` was not reapplied.

All 89 original unrelated untracked files were checked against their pre-closure SHA-256 fingerprints and preserved. Pre-existing Phase 8 code remains unchanged. Known limitations above remain unchanged.

Main is ready for a normal push after this documentation-only commit; successful push is confirmed in the final user closure report. No application deployment was performed during Git closure. Railway database migration is complete; Railway application deployment status is not verified, and a Git push alone does not establish deployment success.

CR-01.2, CR-01.3, CR-01.4 and later slices have NOT started. No new Phase 8 work was implemented.


## Files changed

- `docs/CR-01-Post-System-Review-Enhancements.md`
- `src/Application/Common/Interfaces/IFarmSetupRepository.cs`
- `src/Application/FarmSetup/CreateGrowerFarmCommand.cs`
- `src/Application/FarmSetup/CreateGrowerFarmCommandHandler.cs`
- `src/Application/FarmSetup/CreateGrowerFarmCommandValidator.cs`
- `src/Application/FarmSetup/FarmDto.cs`
- `src/Application/FarmSetup/FarmModelDto.cs`
- `src/Application/FarmSetup/FarmOwnerProfileInput.cs`
- `src/Application/FarmSetup/FarmOwnerProfileInputValidator.cs`
- `src/Application/FarmSetup/FarmOwnerProfileUpdater.cs`
- `src/Application/FarmSetup/FarmProfileAudit.cs`
- `src/Application/FarmSetup/FarmSetupMapper.cs`
- `src/Application/FarmSetup/GetFarmModelsQuery.cs`
- `src/Application/FarmSetup/GetFarmModelsQueryHandler.cs`
- `src/Application/FarmSetup/GrowerDto.cs`
- `src/Application/FarmSetup/RevealFarmOwnerNationalIdCommand.cs`
- `src/Application/FarmSetup/RevealFarmOwnerNationalIdCommandHandler.cs`
- `src/Application/FarmSetup/RevealedFarmOwnerNationalIdDto.cs`
- `src/Application/FarmSetup/SaveFarmModelCommand.cs`
- `src/Application/FarmSetup/SaveFarmModelCommandHandler.cs`
- `src/Application/FarmSetup/SaveFarmModelCommandValidator.cs`
- `src/Application/FarmSetup/UpdateFarmInformationCommand.cs`
- `src/Application/FarmSetup/UpdateFarmInformationCommandHandler.cs`
- `src/Application/FarmSetup/UpdateFarmInformationCommandValidator.cs`
- `src/Domain/Farms/Farm.cs`
- `src/Domain/Farms/FarmModel.cs`
- `src/Domain/Farms/GrowerProfile.cs`
- `src/Infrastructure/Data/ApplicationDbContext.cs`
- `src/Infrastructure/Data/Configurations/ActivityTypeConfiguration.cs`
- `src/Infrastructure/Data/Configurations/FarmConfiguration.cs`
- `src/Infrastructure/Data/Configurations/FarmModelConfiguration.cs`
- `src/Infrastructure/Data/Configurations/FieldLineProfileConfiguration.cs`
- `src/Infrastructure/Data/Configurations/GrowerProfileConfiguration.cs`
- `src/Infrastructure/Data/Configurations/ManagerInvitationConfiguration.cs`
- `src/Infrastructure/Data/Configurations/PersonConfiguration.cs`
- `src/Infrastructure/Data/Configurations/PersonRoleAssignmentConfiguration.cs`
- `src/Infrastructure/Data/Configurations/StatementTicketMatchConfiguration.cs`
- `src/Infrastructure/Data/Configurations/WorkVerificationConfiguration.cs`
- `src/Infrastructure/Data/FarmSetupRepository.cs`
- `src/Infrastructure/Data/Migrations/20261005202118_AddFarmOwnerProfileEnhancements.Designer.cs`
- `src/Infrastructure/Data/Migrations/20261005202118_AddFarmOwnerProfileEnhancements.cs`
- `src/Infrastructure/Data/Migrations/ApplicationDbContextModelSnapshot.cs`
- `src/Web/ClientApp/package.json`
- `src/Web/ClientApp/src/components/farm-setup/FarmModelEditor.tsx`
- `src/Web/ClientApp/src/components/farm-setup/FarmOwnerFields.tsx`
- `src/Web/ClientApp/src/components/farm-setup/FarmOwnerSummary.tsx`
- `src/Web/ClientApp/src/components/farm-setup/FarmProfileEditor.tsx`
- `src/Web/ClientApp/src/components/farm-setup/FarmSummary.tsx`
- `src/Web/ClientApp/src/components/farm-setup/PersonnelRegister.tsx`
- `src/Web/ClientApp/src/components/farm-setup/farmOwnerForm.test.ts`
- `src/Web/ClientApp/src/components/farm-setup/farmOwnerForm.ts`
- `src/Web/ClientApp/src/components/finance/millRecordsView.test.ts`
- `src/Web/ClientApp/src/components/inventory/inputControlView.test.ts`
- `src/Web/ClientApp/src/components/pages/FarmPage.tsx`
- `src/Web/ClientApp/src/components/payroll/payrollView.test.ts`
- `src/Web/ClientApp/src/navigation.ts`
- `src/Web/ClientApp/src/styles.scss`
- `src/Web/ClientApp/src/styles.test.js`
- `src/Web/ClientApp/src/web-api-client.ts`
- `src/Web/Controllers/FarmSetupController.cs`
- `src/Web/Models/FarmSetup/CreateGrowerFarmRequest.cs`
- `src/Web/Models/FarmSetup/UpdateFarmInformationRequest.cs`
- `tests/Application.UnitTests/FarmSetup/FarmOwnerProfileTests.cs`
- `tests/Application.UnitTests/FarmSetup/FarmProfileModelTests.cs`
- `tests/Application.UnitTests/FarmSetup/FarmSetupCommandTests.cs`
- `tests/Application.UnitTests/Farms/SupervisorAuthorizationBoundaryTests.cs`
- `tests/Infrastructure.IntegrationTests/PostgreSqlFarmOwnerProfileAcceptanceTests.cs`
- `tests/Infrastructure.IntegrationTests/PostgreSqlFarmOwnerProfilePrerequisiteTests.cs`
- `tests/Web.UnitTests/Controllers/FarmProfileContractTests.cs`

## CR-01.2 minimal implementation approach

Inspection: Field owns physical identifiers, declared/mapped/reporting area, irrigation and soil notes. CropCycle owns mandatory StartDate (planting/cycle start), persisted expected harvest window, expected yield, variety/ratoon and lifecycle. HarvestResult owns manual actual tonnes and actual harvest date. There is no separate maturity column or manually entered crop age. Mill records/reconciliation are separate. Add Field already excludes crop properties.

Use application options `CropCycles:DefaultCropMaturityMonths` (initial 14), overridable through `CropCycles__DefaultCropMaturityMonths`. Existing FarmSettings is constrained in the database to activity late-entry days; broadening it would require a migration and administrative changes. No runtime maturity administration is required. Reuse the persisted expected harvest window as the maturity expectation rather than create a duplicate date: when no explicit window is supplied, snapshot StartDate.AddMonths(configured months) into both bounds. Explicit windows remain overrides. Existing cycles never recalculate on read or configuration change. Draft plan edits can explicitly request recalculation; active/completed start dates remain immutable to protect operational history.

Age is derived from StartDate to the clock date or recorded harvest date in completed calendar months, with future starts displayed as zero. No age is persisted. Actual yield remains positive manually entered tonnes (three decimal places), recorded at harvest and correctable while Harvested; Closed/Cancelled cycles remain read-only. Reuse version concurrency and lifecycle history for plan/yield edits. Physical field edits remain separate from crop commands. No migration is anticipated.

## CR-01.2 implementation and validation — 2026-10-05

Implementation handoff baseline/HEAD: `af30f3281525d8581b2ceed1e9031f8fa5efd19a`; feature branch `feature/cr-01-2-field-crop-ux`. At that handoff, no commit, staging, push, merge or deployment had been performed. The earlier CR-01.1 closure statements describe that slice's historical state; CR-01.2 Git closure is recorded below.

### Physical Field creation and post-creation access

Before and after, Add Field has exactly these seven physical inputs: field code, field name, declared hectares, optional mapped hectares (required when reporting from mapped area), reporting area source, irrigation method, and optional soil notes. Code, name, declared area and irrigation retain their required validation; reporting source defaults to Declared and record status remains Active. Field has no independent GPS input in this model; Farm location/mapping and GPS architecture are unchanged. Crop controls were already absent, so none were artificially removed from other workflows.

The core form is now a separate reusable/testable component, its guidance separates physical creation from crop setup, soil notes use a smaller input, and saving a field no longer automatically opens the next crop form. A field without a cycle shows an honest empty state. Existing current-cycle links, draft links and the chronological cycle register remain the routes to operational data. Physical field editing exposes name, irrigation and soil notes separately; code and immutable area/reporting properties retain their existing domain behavior. Soil notes are also visible in Field View.

Field still owns physical production area. CropCycle still owns StartDate, variety/type/ratoon, expected harvest window and expected yield. HarvestResult owns actual harvest date and manually entered tonnes. No crop properties, maturity date, yield or age were added to Field. No activity, labour, inventory, costing or Phase 7 relationships changed.

### Maturity configuration and historical truth

The authoritative setting is `CropMaturityOptions.DefaultCropMaturityMonths`, initially **14**, bound from `CropCycles:DefaultCropMaturityMonths`. Operators may override it in application configuration or with `CropCycles__DefaultCropMaturityMonths`; restart the application to use a changed value. Startup validation permits 1–120 months. No production dependencies or runtime settings screen were added.

The existing effective-dated FarmSettings mechanism is intentionally limited by a database constraint to `ActivityLateEntryReasonDays`. Reusing it for maturity would require a database migration and expanding an unrelated settings administration flow. Application options provide the approved configurability with no schema change.

`StartDate` is the established planting/cycle-start concept, including ratoon starts; it is not a new planting column. The existing persisted expected harvest window is the maturity expectation snapshot. For a new draft with both window dates omitted, the application calculates `StartDate.AddMonths(DefaultCropMaturityMonths)` and stores it as both window bounds. Calendar month-end and leap-year behavior follows DateOnly.AddMonths. If both dates are supplied explicitly, that existing window overrides the default and remains unchanged. Partial windows, absent/default start dates, inverted windows and out-of-range dates are rejected.

The tenant-scoped maturity preview returns no date for an absent planting date, otherwise the configured calendar calculation. The frontend asks the server for this preview and contains no maturity-month calculation or default literal. Its generated date-only query serialization preserves the selected local calendar date instead of shifting it through UTC.

Draft editing initially preserves the stored window. Selecting “Use calculated maturity” explicitly recalculates from the edited start date and current configured months. Existing explicit windows remain supported. No read dynamically recalculates stored dates, and a configuration change affects only new or explicitly recalculated draft plans. Active, ReadyForHarvest, Harvested, Closed and Cancelled plans cannot be rewritten. Historical snapshots are protected, including when the default changes later. There is no duplicate persisted or calculated maturity property.

### Crop age and manual actual yield

Crop age is a derived whole-calendar-month display from StartDate to the existing TimeProvider UTC date (the same date convention as the existing harvest command). After a harvest result exists, its HarvestDate is the fixed end date, including after closure. A future start displays zero months. The API provides CropAgeMonths in crop-cycle collection/details responses; age is never stored or manually entered. The overview consistently labels “Crop age” or “Crop age at harvest”. Fields without crop cycles have no crop-age, planting or yield values.

Actual Yield remains manually recorded in the existing harvest lifecycle operation. The new correction flow updates HarvestResult.ActualTonnes only while the cycle is Harvested, before closure. It accepts positive tonnes up to the existing limit, with at most three decimal places; negative, zero and excessive-precision inputs are rejected. Actual harvest date and lifecycle safeguards remain unchanged. Closed and Cancelled cycles remain read-only.

Manual yield has no dependency on the mill repository or reconciliation service. Mill tickets and statement matches remain independent; the focused test keeps recorded ticket net tonnes and reconciliation match tonnes unchanged during a manual yield correction. Railway persistence tests also verify that the same field/cycle ticket retains its original net tonnes after correction. No Phase 7 calculation or reconciliation code was modified.

### API, authorization and history

Existing create-crop-cycle requests now accept an omitted/null expected window for calculated maturity; explicitly supplied windows retain their previous behavior. Added authenticated endpoints are GET crop-cycle maturity preview, PUT draft crop plan, PUT crop-cycle actual yield, and PUT physical field details. Route IDs supply the field/cycle context, and response DTOs include derived age. The TypeScript client was regenerated; its normalizer preserves existing checked-in formatting to avoid unrelated generation churn.

Write operations use the existing tenant repository scope for Grower/FarmManager membership. Supervisor restrictions and Farm Owner access remain unchanged. Crop edits use existing version concurrency and the existing CropCycleStatusChange history collection: same-status edits increment Version, retain actor/timestamp, and record before/after plan or actual-yield values in Reason. The timeline labels these records “Crop cycle edited”. Physical field edits use existing tenant AuditEvents. No second history/audit system was introduced.

### Verification and database state

**CR-01.2 requires no database migration.** No migration was generated, applied, rolled back or reapplied. Railway Development's last verified schema baseline remains 21 applied / 0 pending, including `20261005202118_AddFarmOwnerProfileEnhancements`; schema state was not queried merely to prove that no new migration exists.

| Gate | Result |
| --- | --- |
| dotnet build Cane360.slnx --no-restore | Passed; 0 warnings/errors |
| Full Application suite | 436/436 passed |
| Full Web/API suite | 122/122 passed |
| Focused FieldCropUxTests | 22/22 passed |
| New API route/preview contract cases | 3/3 passed within Web suite |
| Frontend suite | 118/118 passed; includes 8 new rendering/client cases |
| Frontend lint | Passed |
| Frontend typecheck | Passed |
| Frontend production build | Passed; existing large-bundle advisory remains |
| EF has-pending-model-changes | Clean; no model changes since last migration |
| Railway Development CR012Acceptance | 5/5 passed |
| Isolated browser UI smoke | Passed at 1440px and 390px; no horizontal overflow |
| git diff --check | Passed |

Railway acceptance tests each create a uniquely labelled synthetic tenant and run identifier inside their own uncommitted transaction, assert only their own records, and roll back that transaction. They cover physical field create/edit, maturity persistence and draft recalculation/history, manual actual yield and independent mill-ticket persistence, historical harvest age/snapshot reads, and rejected foreign field identity. They neither clean committed data nor query global business-table counts. No application/test startup migration was used.

Browser smoke used isolated synthetic API fixtures, not a production or pilot account. It verified Add Field's exact input set, separate crop setup after saving, Field editing, maturity preview, draft editing, crop-age presentation, manual yield saving, selected-cycle request context and responsive widths. Real persistence and repository scoping were separately verified by Railway acceptance tests. Application deployment and a live authenticated end-to-end deployment smoke were not performed.

### Limitations and final design check

- Maturity is an application-wide configurable default, requiring restart for an operator change; no tenant maturity administration was added.
- Expected maturity reuses the persisted expected harvest-window start. Historical explicit windows remain historical expectations; they are not rewritten to an assumed 14-month value. Draft recalculation is an explicit choice.
- Planting/cycle-start edits are draft-only. Actual yield corrections are Harvested-only; completed/closed history and actual harvest dates retain their established immutability.
- Physical Field code, areas and reporting source retain existing immutability. Field detail edits use existing audit metadata and last-save behavior rather than inventing a new concurrency model.
- Crop age is whole calendar months on the existing UTC harvest-date convention; it is a display, not a persisted agronomic measurement.
- Existing production bundle-size advisory remains. No deployment was performed or verified.

Final design checks: no crop-cycle properties moved to Field; Add Field asks only for physical data; Actual Yield is manual; mill/reconciliation cannot overwrite it; maturity months have one configured default; historical snapshots do not recalculate on reads/configuration changes. All 89 original unrelated untracked paths remain untracked, nothing is staged, and the pre-existing Phase 8 files are unchanged. CR-01.3 Employee Master, CR-01.4 Inventory Category Administration and new Phase 8 work have NOT started.

### CR-01.2 changed-file manifest

55 files: 31 tracked modifications and 24 new CR-01.2 files. Original untracked files are excluded.

- `docs/CR-01-Post-System-Review-Enhancements.md`
- `src/Application/CropCycles/ActivateCropCycleCommandHandler.cs`
- `src/Application/CropCycles/CalculateCropMaturityQuery.cs`
- `src/Application/CropCycles/CalculateCropMaturityQueryHandler.cs`
- `src/Application/CropCycles/CalculateCropMaturityQueryValidator.cs`
- `src/Application/CropCycles/CancelCropCycleCommandHandler.cs`
- `src/Application/CropCycles/CloseCropCycleCommandHandler.cs`
- `src/Application/CropCycles/CreateCropCycleCommand.cs`
- `src/Application/CropCycles/CreateCropCycleCommandHandler.cs`
- `src/Application/CropCycles/CreateCropCycleCommandValidator.cs`
- `src/Application/CropCycles/CropCycleListItemDto.cs`
- `src/Application/CropCycles/CropCycleMapper.cs`
- `src/Application/CropCycles/CropMaturityDto.cs`
- `src/Application/CropCycles/CropMaturityOptions.cs`
- `src/Application/CropCycles/GetCropCycleDetailsQueryHandler.cs`
- `src/Application/CropCycles/GetCropCyclesQueryHandler.cs`
- `src/Application/CropCycles/HarvestCropCycleCommandHandler.cs`
- `src/Application/CropCycles/MarkCropCycleReadyForHarvestCommandHandler.cs`
- `src/Application/CropCycles/UpdateActualYieldCommand.cs`
- `src/Application/CropCycles/UpdateActualYieldCommandHandler.cs`
- `src/Application/CropCycles/UpdateActualYieldCommandValidator.cs`
- `src/Application/CropCycles/UpdateCropCyclePlanCommand.cs`
- `src/Application/CropCycles/UpdateCropCyclePlanCommandHandler.cs`
- `src/Application/CropCycles/UpdateCropCyclePlanCommandValidator.cs`
- `src/Application/DependencyInjection.cs`
- `src/Application/FarmSetup/UpdateFieldDetailsCommand.cs`
- `src/Application/FarmSetup/UpdateFieldDetailsCommandHandler.cs`
- `src/Application/FarmSetup/UpdateFieldDetailsCommandValidator.cs`
- `src/Domain/Farms/CropCycle.cs`
- `src/Domain/Farms/Field.cs`
- `src/Domain/Farms/HarvestResult.cs`
- `src/Web/ClientApp/package.json`
- `src/Web/ClientApp/scripts/normalize-generated-client.cjs`
- `src/Web/ClientApp/src/components/crop-cycles/CropCycleEditForm.tsx`
- `src/Web/ClientApp/src/components/crop-cycles/CropCycleForm.tsx`
- `src/Web/ClientApp/src/components/crop-cycles/CropPlanFields.tsx`
- `src/Web/ClientApp/src/components/crop-cycles/fieldCropUx.test.js`
- `src/Web/ClientApp/src/components/farm-setup/FieldDetailsForm.tsx`
- `src/Web/ClientApp/src/components/farm-setup/FieldForm.tsx`
- `src/Web/ClientApp/src/components/farm-setup/FieldRecord.tsx`
- `src/Web/ClientApp/src/components/pages/CropCycleOverviewPage.tsx`
- `src/Web/ClientApp/src/components/pages/FieldsPage.tsx`
- `src/Web/ClientApp/src/web-api-client.ts`
- `src/Web/Controllers/CropCyclesController.cs`
- `src/Web/Controllers/FarmSetupController.cs`
- `src/Web/Models/CropCycles/CreateCropCycleRequest.cs`
- `src/Web/Models/CropCycles/UpdateActualYieldRequest.cs`
- `src/Web/Models/CropCycles/UpdateCropCyclePlanRequest.cs`
- `src/Web/Models/FarmSetup/UpdateFieldDetailsRequest.cs`
- `tests/Application.UnitTests/CropCycles/CropCycleCommandTests.cs`
- `tests/Application.UnitTests/CropCycles/CropCycleLabourHistoryTests.cs`
- `tests/Application.UnitTests/CropCycles/FieldCropUxTests.cs`
- `tests/Application.UnitTests/Farms/SupervisorAuthorizationBoundaryTests.cs`
- `tests/Infrastructure.IntegrationTests/PostgreSqlFieldCropUxAcceptanceTests.cs`
- `tests/Web.UnitTests/Controllers/CropCyclesControllerTests.cs`

## CR-01.2 Git closure — 2026-10-05

Feature commit: `9be3968ea24d47fda158922844411e3eff43c64c` (`feat(cr-01): improve field and crop cycle UX`), pushed to `origin/feature/cr-01-2-field-crop-ux`. The feature branch is retained locally and remotely.

Merge commit: `1362a4d6faa099f12dc7c08c7cb3111618dd4e7c` (`Merge CR-01.2 field and crop cycle UX enhancements`). Main and origin/main both matched baseline `af30f3281525d8581b2ceed1e9031f8fa5efd19a` before the merge. The merge is a regular two-parent merge, matching CR-01.1, with no squash, conflicts, force push or history rewrite. Its tree exactly matches the verified feature tree.

Main SHA at final executable verification: `1362a4d6faa099f12dc7c08c7cb3111618dd4e7c`. The final main tip is the subsequent documentation-only commit `docs(cr-01): record CR-01.2 git closure`; resolve its full SHA with `git log -1 --format=%H --grep="^docs(cr-01): record CR-01.2 git closure$" main`. As in CR-01.1, that commit cannot embed its own SHA in its contents; the final user report records the final main SHA and push outcome explicitly.

The pre-commit and post-merge gates both passed:

| Closure gate | Final result on merged main |
| --- | --- |
| .NET solution build | Passed; 0 warnings/errors |
| Application tests | 436/436 passed; 0 failures/skips |
| Web/API tests | 122/122 passed; 0 failures/skips |
| Focused CR-01.2 backend tests | 22/22 passed; 0 failures/skips |
| Additional focused API cases | 3 passed within Web/API suite |
| Frontend tests | 118/118 passed; includes 8 focused CR-01.2 cases |
| Frontend lint and typecheck | Passed |
| Frontend production build and generated-client checks | Passed; existing bundle-size advisory remains |
| EF pending-model/model parity | Clean; no changes since last migration |
| Railway Development CR012Acceptance rerun | 5/5 passed; 0 failures/skips |
| git diff --check | Passed |

**No CR-01.2 database migration exists or is required.** No migration file or model snapshot changed, and no migration was created, applied or reapplied during closure. Railway Development's last verified schema state remains 21 applied / 0 pending, including `20261005202118_AddFarmOwnerProfileEnhancements`. Migration counts were not queried during closure merely to prove that no migration exists. The acceptance rerun used uniquely labelled synthetic tenants/run identifiers and rolled back only each test's own uncommitted transaction; it did not clean committed data or modify non-test tenants.

The 55-file staged set exactly matched the reviewed CR-01.2 manifest. All 89 unrelated untracked paths and their content hashes were preserved. All tracked files outside the CR-01.2 set, including pre-existing Phase 8 work, retained their content hashes. Local environment files, secrets, temporary outputs and unrelated documentation were not staged. The post-merge executable checks left the tracked tree clean; only this closure documentation was changed afterward.

Known limitations are unchanged: the application-wide maturity setting requires restart; expected maturity reuses the persisted harvest-window expectation, with explicit draft recalculation and stable historical values; planting/cycle-plan edits are draft-only; manual yield corrections are Harvested-only before closure; physical Field code/areas/reporting source retain existing immutability; crop age uses whole calendar months and the existing UTC date convention. The existing bundle-size advisory remains. No application deployment or authenticated deployment smoke was performed or verified.

CR-01.3 Employee Master, CR-01.4 Inventory Category Administration and new Phase 8 work have NOT started. Git closure is the stopping boundary for this task.


## CR-01.3 Employee Master — architecture and implementation

Authorized baseline: `bdd11e94c1f1791b9160f7c14903bd3701564c43`; branch `feature/cr-01-3-employee-master`. CR-01.4 and new Phase 8 work remain out of scope.

The existing `WorkerProfile` extends a farm `Person`. Worker IDs, `labour.WorkerProfiles`, namespaces and operational routes remain. Employee is the business term for master screens; no second Employee aggregate is introduced. Attendance, WorkerRate, WorkRecord and WorkerAdvance/PayrollWorkerLine retain composite Worker/tenant/farm foreign keys. WorkScope/verification/activity links, evidence consumption, payroll calculations and payslip/payment links remain downstream of that unchanged graph.

Reuse Person.DisplayName/Phone, Worker.EmploymentType (all five approved types), ActiveFrom as Employment Date, Active/Archived status, protected National ID, audit metadata and Version. ActiveFrom stays immutable because changing it retroactively would alter labour eligibility; no fabricated employment date is introduced. Profile edits never replace Person roles. A separate expected Person version protects shared name/contact edits.

Add nullable EmployeeNumber, Title, FirstName, Surname, Sex, DateOfBirth, Address, PhotoReference and four next-of-kin fields directly on WorkerProfile. One current next-of-kin contact needs no dependent-contact subsystem. EmployeeNumber is optional, trimmed/uppercased, unique per tenant when present, never a primary key; legacy rows remain null with no backfill. Structured names are optional for legacy/API compatibility and must be supplied together; when supplied their combined value becomes Person.DisplayName. Existing names are never automatically split. Clearing structured fields preserves the supplied legacy display name.

Create remains compact: First Name, Surname, Employee Type, Employment Date and protected National ID; Employee Number and other enrichment are optional through Edit. Existing API clients may still create using DisplayName or PersonId. Full Edit groups identity, employment, contact, next of kin and photo reference. Archived status uses the existing archive operation/date and remains irreversible under existing rules.

National ID AES-GCM storage, farm-scoped HMAC uniqueness, masking, key management and exact Grower-only audited reveal remain. A dedicated Grower-only correction endpoint uses the existing CorrectNationalId domain operation, protects new values, excludes the current worker in duplicate checks, and records no secret values. New profile access uses existing Grower/FarmManager membership scope; Supervisor gains no access and Farm Owner gains no new privilege.

Photograph follows CR-01.1: bounded opaque metadata reference only, no binary, upload service or external-image fetching. An additive migration is required for nullable profile columns and the optional tenant number index. Gates and acceptance results are recorded below.

### CR-01.3 implementation and validation results

Employee business terminology is used in the Labour master tab, register, empty state, Create and Edit/profile controls. Labour navigation and attendance/evidence operational Worker references remain. Create contains exactly First Name, Surname, Employee Type, Employment Date and National ID. Employee Number, contact data and all other enrichment remain optional through Edit. Existing DisplayName/PersonId API creation remains supported. Combined structured names fit the existing 120-character Person limit (FirstName up to 60, Surname up to 59); no legacy values are split or replaced during migration. Operational reads use the current Person name; payroll's existing name/rate/evidence snapshots remain historical.

Full Edit supports Employee Number, Title, paired First Name/Surname, legacy DisplayName, Sex, optional Date of Birth, Employee Type, primary phone, contact address, photograph reference and one next-of-kin name/relationship/phone/address. Employment Date reuses immutable ActiveFrom; status reuses Active/Archived and the existing dated archive command. No reactivation, employment date rewriting, age policy or statutory payroll rule was added.

Existing `/api/workers` routes are retained. Create accepts an optional `profile`. Detail responses add `profile` and `personVersion`; list responses add only optional EmployeeNumber. New PUT `/api/workers/{id}/profile` checks both Worker and shared Person versions. New PUT `/api/workers/{id}/national-id` is Grower-only and calls the existing protected correction operation. The existing reveal route now has a named, typed OpenAPI contract for the generated client, with the same route/payload, exact authorization and requested/succeeded audit behavior. No ciphertext, fingerprint, full National ID or extended personal/kin information was added to list responses.

Profile updates produce `WorkerProfileUpdated`; registrations and archives retain their existing events. Corrections produce `NationalIdCorrected`. Audit messages record the action/actor/subject/correlation and never include ID values or submitted free-text correction reasons. The existing shared repository context saves Person, Worker and audit changes together. FarmManager can create/edit/read under existing membership rules but cannot reveal/correct National ID. Grower permissions are retained, not broadened. Supervisor has no new employee-master access.

Migration: `20261005221527_AddEmployeeMasterEnhancements`. Its 13 forward operations add 12 nullable bounded profile columns to `labour.WorkerProfiles` and one filtered unique `(TenantId, EmployeeNumber)` index. Manual migration, snapshot and forward SQL review found no table rename/drop, foreign-key/primary-key changes, data rewrite, backfill, binary photograph column or other-schema changes. Forward SQL was inspected at `/tmp/cr013-forward.sql`. No rollback was run.

Railway Development was verified before generation at 21 applied / 0 pending, before application at 21 applied / 1 pending (only this migration), and after application at **22 applied / 0 pending**. No prior migration was reapplied, and startup/tests never migrated automatically.

| Gate | Final result |
| --- | --- |
| .NET solution build | Passed; 0 warnings/errors |
| Application tests | 458/458 passed; 0 failures/skips |
| Web/API tests | 128/128 passed; 0 failures/skips |
| Focused EmployeeMasterTests | 22/22 passed |
| Focused Employee API/model/migration tests | 6/6 passed within Web suite |
| Frontend tests | 125/125 passed, including 7 focused Employee cases |
| Frontend lint | Passed |
| Frontend typecheck | Passed |
| Frontend production build/generated-client name check | Passed; existing bundle-size advisory remains |
| EF model parity after generation/application | Clean; no pending model changes |
| Railway labelled pre-migration fixture | 1/1 passed |
| Railway CR013Acceptance | 8/8 passed; 0 failures/skips |
| Isolated browser smoke | Passed at 1440, 768, 390 and 360 CSS px |
| git diff --check | Passed |

The required pre-generation gate passed before migration creation (Application 457, Web 127, frontend 125). After generation the migration safety case increased Web to 128 and all relevant gates passed before application. A final explicit attendance/evidence/payroll name-snapshot regression increased Application to 458 after application; its focused/full runs also passed. An older CR-01.1 scope test was corrected to inspect its immutable released migration rather than classify every future model change as CR-01.1; no test was suppressed.

The pre-migration fixture uses label `AUTOTEST-CR013-LEGACY-5d2acc4407a04ccb91efcf6e1bd04be2`. It inserted only existing Worker columns and established Person, attendance, confirmed activity-linked evidence, rates and payroll-advance relationships before the new columns existed. Post-migration acceptance reads only that uniquely labelled synthetic graph and verifies null enrichment and unchanged links. The fixture is intentionally retained. Every other CR013Acceptance test creates its own uniquely labelled synthetic tenant/run inside an uncommitted transaction and rolls back only that work. Assertions are tenant/farm/ID/run scoped; no real records or global business-table counts are inspected. Constraint checks use transaction savepoints and restore only their own uncommitted writes.

Acceptance verifies legacy Worker identity/eligibility, existing attendance/evidence/activity/payroll-advance links, new core create, structured/profile/DOB/contact/photo/kin persistence, masked reads, audited authorized reveal/correction, duplicate National ID/number rejection, cross-tenant reads and composite foreign-key rejection, same number allowed across tenants, database uniqueness and optimistic concurrency. Application regressions also explicitly retain payroll-line/earning/evidence/attendance IDs and historical payroll name snapshots through employee name edits. All existing attendance, Labour evidence, activity-worker, payroll, audit and monthly-proration suites remain green; payroll calculations were not changed.

The browser smoke used intercepted synthetic API responses, not a real account or deployed Railway app. It verified progressive Create, full Edit/save, Employee Number/kin/photo metadata, masking, authorized reveal/hide and zero page/dialog horizontal overflow at all four widths. Mobile screenshot: `artifacts/cr013-employee-edit-mobile.png` (ignored generated artifact). Actual database persistence/security were tested separately by Railway acceptance.

Known limitations: photograph references only, with no upload/storage service/image fetching; optional Employee Numbers have no automatic generation or legacy backfill; Employment Date remains immutable; Active/Archived status retains existing archive semantics without reactivation; paired structured names must fit Person.DisplayName's existing limit; one current next-of-kin contact; existing bundle-size advisory; no application deployment or live authenticated deployment smoke.

Final design checks: no second Employee aggregate; no changed Worker primary keys; no rewritten attendance/labour/payroll foreign keys; no weaker National ID protection/authorization; legacy null enrichment remains valid with no fabricated data; compact five-field Create and full Edit available; Employee business terminology with Worker internals retained; NSSA/NEC/PAYE untouched. All 89 original unrelated untracked paths were checked against their original SHA-256 hashes and remain unchanged/untracked. Pre-existing Phase 8/administration and unrelated local work are preserved. CR-01.4 has NOT started.

Implementation stopping state before Git closure: branch `feature/cr-01-3-employee-master`, HEAD unchanged at `bdd11e94c1f1791b9160f7c14903bd3701564c43`; 25 tracked files modified, 23 new CR-01.3 files untracked, plus the 89 original untracked paths. Index remains empty. No commit, push or merge was performed.

### CR-01.3 changed-file manifest

48 code/test/documentation files, excluding the 89 unrelated files and ignored build/screenshot artifacts.

- `docs/CR-01-Post-System-Review-Enhancements.md`
- `src/Application/Common/Interfaces/ILabourRepository.cs`
- `src/Application/Labour/ArchiveWorkerCommandHandler.cs`
- `src/Application/Labour/CorrectWorkerNationalIdCommand.cs`
- `src/Application/Labour/CorrectWorkerNationalIdCommandHandler.cs`
- `src/Application/Labour/CorrectWorkerNationalIdCommandValidator.cs`
- `src/Application/Labour/CreateWorkerCommand.cs`
- `src/Application/Labour/CreateWorkerCommandHandler.cs`
- `src/Application/Labour/CreateWorkerCommandValidator.cs`
- `src/Application/Labour/CreateWorkerRateCommandHandler.cs`
- `src/Application/Labour/EndWorkerRateCommandHandler.cs`
- `src/Application/Labour/GetWorkerDetailsQueryHandler.cs`
- `src/Application/Labour/LabourMapper.cs`
- `src/Application/Labour/UpdateWorkerProfileCommand.cs`
- `src/Application/Labour/UpdateWorkerProfileCommandHandler.cs`
- `src/Application/Labour/UpdateWorkerProfileCommandValidator.cs`
- `src/Application/Labour/WorkerDetailsDto.cs`
- `src/Application/Labour/WorkerListItemDto.cs`
- `src/Application/Labour/WorkerNumberRules.cs`
- `src/Application/Labour/WorkerProfileInput.cs`
- `src/Application/Labour/WorkerProfileInputValidator.cs`
- `src/Application/Labour/WorkerProfileUpdater.cs`
- `src/Domain/Activities/Person.cs`
- `src/Domain/Labour/WorkerProfile.cs`
- `src/Infrastructure/Data/Configurations/WorkerProfileConfiguration.cs`
- `src/Infrastructure/Data/LabourRepository.cs`
- `src/Infrastructure/Data/Migrations/20261005221527_AddEmployeeMasterEnhancements.Designer.cs`
- `src/Infrastructure/Data/Migrations/20261005221527_AddEmployeeMasterEnhancements.cs`
- `src/Infrastructure/Data/Migrations/ApplicationDbContextModelSnapshot.cs`
- `src/Web/ClientApp/package.json`
- `src/Web/ClientApp/scripts/normalize-generated-client.cjs`
- `src/Web/ClientApp/src/components/labour/EmployeeProfile.tsx`
- `src/Web/ClientApp/src/components/labour/employeeForm.ts`
- `src/Web/ClientApp/src/components/labour/employeeMaster.test.js`
- `src/Web/ClientApp/src/components/labour/labourApi.ts`
- `src/Web/ClientApp/src/components/pages/LabourPage.tsx`
- `src/Web/ClientApp/src/web-api-client.ts`
- `src/Web/Controllers/WorkersController.cs`
- `src/Web/Models/Labour/CorrectWorkerNationalIdRequest.cs`
- `src/Web/Models/Labour/CreateWorkerRequest.cs`
- `src/Web/Models/Labour/UpdateWorkerProfileRequest.cs`
- `tests/Application.UnitTests/FarmSetup/FarmProfileModelTests.cs`
- `tests/Application.UnitTests/Labour/EmployeeMasterTests.cs`
- `tests/Infrastructure.IntegrationTests/EmployeeMasterAcceptanceFixture.cs`
- `tests/Infrastructure.IntegrationTests/PostgreSqlEmployeeMasterAcceptanceTests.cs`
- `tests/Infrastructure.IntegrationTests/PostgreSqlEmployeeMasterPrerequisiteTests.cs`
- `tests/Web.UnitTests/Controllers/EmployeeMasterControllerTests.cs`
- `tests/Web.UnitTests/Infrastructure/EmployeeMasterModelTests.cs`


## CR-01.3 Git closure — 2026-10-06

Feature commit: `3976490fadeaf16cac460d21801a3eb46648e5d9` (`feat(cr-01): add employee master enhancements`), pushed to `origin/feature/cr-01-3-employee-master`. The feature branch is retained locally and remotely.

Merge commit: `f5262def45646f9491c2589d1546db88ec09946a` (`Merge CR-01.3 employee master enhancements`). Main and refreshed origin/main both matched the approved baseline `bdd11e94c1f1791b9160f7c14903bd3701564c43` before merging. The merge is a regular two-parent merge, matching CR-01.1/CR-01.2, without squash, conflicts, history rewrite or force push. Its tree exactly matches the verified feature tree.

Main SHA at final executable verification: `f5262def45646f9491c2589d1546db88ec09946a`. The final main tip includes the subsequent documentation-only commit `docs(cr-01): record CR-01.3 git closure`; resolve its full SHA with `git log -1 --format=%H --grep="^docs(cr-01): record CR-01.3 git closure$" main`. As in prior CR slices, the closure commit cannot embed its own SHA in its contents. The final user report records that full final main SHA and confirms push status.

The pre-commit and post-merge gates both passed:

| Closure gate | Final result on merged main |
| --- | --- |
| .NET solution build | Passed; 0 warnings/errors |
| Full Application suite | 458/458 passed; 0 failures/skips |
| Full Web/API suite | 128/128 passed; 0 failures/skips |
| Focused CR-01.3 Application tests | 22/22 passed |
| Focused CR-01.3 Web/API/model/migration tests | 6/6 passed |
| Frontend tests | 125/125 passed; 0 failures/skips |
| Frontend lint | Passed |
| Frontend typecheck | Passed |
| Frontend production build/generated-client name checks | Passed; existing bundle-size advisory remains |
| EF model parity | Clean; no pending model changes |
| Railway Development migration state | 22 applied / 0 pending |
| Railway CR013Acceptance rerun | 8/8 passed; 0 failures/skips |
| git diff --check | Passed |

Migration `20261005221527_AddEmployeeMasterEnhancements` remains present exactly once, with its original designer and model snapshot. The forward migration remains 12 nullable WorkerProfile columns and one filtered tenant-scoped unique EmployeeNumber index; no primary-key/foreign-key rewrite, table rename/drop, fabricated number or personal-data backfill. It was already applied to Railway Development before Git closure. No migration was regenerated, applied, reapplied or rolled back during closure.

Post-merge acceptance reused the existing uniquely labelled pre-migration legacy fixture read-only. Each acceptance test created its own uniquely labelled synthetic tenant/run inside an uncommitted transaction and rolled back only its own work. Tests verified legacy Worker reads; unchanged attendance, confirmed evidence/activity and payroll-advance links; Employee profile create/read/edit; masked National ID; authorized audited reveal/correction; duplicate rejection; tenant isolation; optional tenant number uniqueness and concurrency. No real tenant was sampled or modified, no global business-table counts were asserted, and no destructive cleanup or startup migration was used. The committed pre-migration fixture was retained.

The staged feature set exactly matched the reviewed 48-file CR-01.3 manifest. Original SHA-256 fingerprints verified all 89 unrelated untracked files and all tracked files outside the CR-01.3 manifest throughout closure. Pre-existing Phase 8 work was preserved. Local environment/configuration files, secrets, temporary artifacts and unrelated documentation were not staged. WorkerProfile/Worker IDs and historical relationships remain intact; no second Employee aggregate or plaintext ID storage/audit payload was introduced. National ID protector, keys, uniqueness, masking and exact reveal authorization remain intact. Payroll calculations, NSSA, NEC and PAYE were not modified.

Known limitations remain unchanged: reference-only photographs with no uploads/storage service; optional Employee Numbers without automatic generation or legacy backfill; immutable ActiveFrom employment date; existing archive semantics without reactivation; paired structured-name length bounded by the existing Person display name; one current next-of-kin contact; existing bundle-size advisory. Earlier browser validation at 1440/768/390/360 px used synthetic intercepted responses. No additional product changes, browser redesign or deployment smoke were part of Git closure. No application deployment was performed or verified; a Git push alone does not establish deployment success.

Main is ready for a normal push after this documentation-only commit; successful push and synchronized final main/origin/main are confirmed in the final user closure report. The feature branch is retained. CR-01.4 and new Phase 8 functionality have NOT started. CR-01.3 Git closure is the stopping boundary.


## CR-01.4 — Inventory Category Administration: design before implementation

Authorized baseline: `87148403f5f997bef276f8db0d8e78a84ce8d6cc`; branch `feature/cr-01-4-inventory-categories`. No commit/push/merge is authorized. CR-01.5 remains unstarted.

Existing architecture: InventoryItemCategory is an enum (Fertiliser, Chemical, SeedAndPlantingMaterial, Other), persisted as a 40-character string on InventoryItems. Item creation accepts that string; no item update/recategorization capability exists. Categories are only catalogue classification, not transaction or costing rules. Receipts, issues, field receipts, applications, returns, losses, counts, adjustments, corrections and reversals reference stable item/line IDs and snapshot item/unit/lot details; they do not snapshot categories. Moving weighted average derives quantity/value from StockMovements; issue cost locks at posting; application/loss cost postings use locked issue cost. OperationalCostCategory is an independent enum and remains untouched. Suppliers and units are existing tenant/farm master data. Reports derive ledger/accountability/cost information independently of item category names.

Chosen smallest additive model: InventoryCategory (GUID primary key, tenant ownership, immutable Code, editable Name/Description/DisplayOrder, Active, optimistic Version, existing auditable metadata). Retain InventoryItems.Category column and exact legacy strings as stable codes, with a restrictive composite (Category,TenantId) foreign key to (Code,TenantId). This uses the established tenant reference-data key pattern without an item backfill or ledger rewrite. Domain item creation resolves an active category in the same tenant; no item recategorization is introduced. New codes normalize to uppercase, while the four existing mixed-case enum identifiers are explicitly preserved. Codes are immutable from creation, including before first use. Names are trimmed and tenant-unique by uppercase normalized name (including inactive categories); description max 500, name max 120, display order 0–10000. No hard-delete operation.

Migration is required for the category table and restrictive tenant foreign key. Seed exactly the four existing category options per existing tenant, using exact codes and existing humanized labels (Fertiliser, Chemical, Seed And Planting Material, Other); also preserve any distinct persisted category string verbatim if one exists outside the enum. Each (tenant,code) receives one deterministic GUID derived from tenant/code. No item row changes; no arbitrary reclassification. New tenants receive the same four legacy defaults at explicit tenant creation, never on application startup or reads. No examples such as Fuel/PPE are seeded. Category audit facts use the existing AuditEvent infrastructure and stable category subject ID; no separate audit subsystem.

Category management belongs in Inventory Catalogue using compact existing cards/dialogs. Only FarmManager may administer; Farm Owner retains read access and does not gain administration; supervisors may consume tenant category data. Item creation offers only active categories. Existing inactive item categories remain readable with an inactive label; category deactivation does not archive items or block existing stock operations. Current catalogue names follow current category metadata; existing transaction snapshots and facts remain unchanged.

Integrity strategy: category commands load/save only category master data and append safe AuditEvent facts. No posting handlers, quantity/value algorithms, inventory ledger facts, prior audit facts, or statutory payroll are changed. Focused tests and labelled synthetic Railway before/after assertions must verify quantities, movement facts, locked costs, accountability and OperationalCostPostings. Pre-migration build/test/lint/typecheck/production-build gate must pass before applying the single migration.

Pre-implementation Railway Development status (2026-10-06): 22 applied, 0 pending; latest `20261005221527_AddEmployeeMasterEnhancements`. Validation and acceptance are pending. The Down path explicitly raises an exception instead of dropping configurable category data; rollback is unsupported and requires a forward remediation.


### CR-01.4 implementation and validation record

Tenant-owned `inventory.InventoryCategories` is implemented. The GUID primary key and Code are stable from creation. Editable fields are Name (required, trimmed, max 120), Description (optional, max 500), DisplayOrder (0–10000), and Active through an explicit versioned status command. Version is an EF concurrency token. Created/LastModified and actor metadata use existing auditable infrastructure. NormalizedName enforces uppercase/trimmed tenant uniqueness even for inactive categories; a database-generated NormalizedCode enforces case-insensitive tenant code uniqueness. New codes normalize to uppercase (max 40; letters/digits/underscore/hyphen). Legacy mixed-case codes remain exact. No delete operation exists; the restrictive category/item FK also prevents deleting a referenced category at the database boundary.

Items retain the original `Category` column as a stable code. The FK `(Category,TenantId) -> (Code,TenantId)` resolves the category identity in the item's own tenant. This deliberately avoids changing any item row or item/ledger primary key. The legacy enum is retained for default initialization and existing trusted domain fixtures; public item creation resolves a configured, active, same-tenant category. Existing inactive categories remain readable; existing items remain active and their stock workflows remain usable. No item edit or recategorization workflow is introduced. Transaction item/unit/lot snapshots are preserved; current Catalogue category labels follow current category metadata. Existing reports and OperationalCostCategory are untouched.

Authorization reuses MediatR tenant-role attributes and the existing membership infrastructure. Only FarmManager may create/update/activate/deactivate categories. Handlers additionally check the manager membership. Category listing permits active operational tenant members, including supervisors; Inventory workspace/navigation retain their existing visibility restrictions. Farm Owner retains visibility without acquiring category management. Workspace exposes Categories and CanManageCategories for the compact Inventory > Catalogue UI. AuditEvent facts use InventoryCategory as subject type and its stable GUID as SubjectId; Created, Updated, Activated and Deactivated are appended with code/name/status/order summaries. No old audit facts are rewritten and no second audit system is introduced.

API additions (existing Inventory Item contract retained):

| Method | Route | Behavior |
| --- | --- | --- |
| GET | `/api/inventory/categories` | Tenant category list, including inactive references |
| POST | `/api/inventory/categories` | Manager creates category |
| PUT | `/api/inventory/categories/{categoryId}` | Manager edits name/description/order with ExpectedVersion |
| POST | `/api/inventory/categories/{categoryId}/active` | Manager sets Active with ExpectedVersion |

Generated TypeScript client was refreshed. Catalogue uses existing compact cards, forms, dialog focus behavior, status pills, edit actions and explicit deactivation confirmation. Code is read-only on edit. Internal IDs, tenant IDs, audit fields and version tokens are not editable controls. New Inventory Item selectors use active configured categories, ordered by DisplayOrder/name. Existing items resolve renamed/inactive labels. Shared SimpleForm awaits save/refresh completion so category errors use the existing error surface. Desktop and 390px mobile screenshots were inspected; no horizontal overflow. Browser API fixtures are synthetic and separate from Railway acceptance.

Migration required: **Yes**, `20261006170131_AddInventoryCategoryAdministration`. One additive migration creates the category table, normalized uniqueness indexes, item category index and restrictive tenant/code FK. Seed maps each exact legacy code to its existing display label:

| Existing item category / immutable code | Initial category name | Order |
| --- | --- | --- |
| Fertiliser | Fertiliser | 0 |
| Chemical | Chemical | 1 |
| SeedAndPlantingMaterial | Seed And Planting Material | 2 |
| Other | Other | 3 |

Each existing tenant receives exactly these previously available defaults. Any additional distinct persisted code is preserved verbatim as its own code/name (order 4), never collapsed or arbitrarily remapped. Seed GUID is `md5(tenant UUID text + ':inventory-category:' + exact code)::uuid`, deterministic per tenant/code. Case/name conflicts would abort transactionally rather than merge categories. Newly created tenants initialize the same four defaults only during explicit tenant creation. No Fuel/PPE/etc. business examples are seeded. Existing item rows are never backfilled or rewritten. Manual migration/forward-SQL inspection confirmed no ledger drops, item updates, movement rewrites, quantity/cost changes, historical FK removal, or cross-tenant mapping. SQL is transactional and was applied once after the green gate. Down refuses potentially lossy rollback and performs no drop.

Railway Development: baseline 22 applied/0 pending; immediately before apply 22 applied/1 pending (only this migration); after apply **23 applied/0 pending**. Latest applied is `20261006170131_AddInventoryCategoryAdministration`. No reset, recreation, rollback, cleanup or automatic migration execution was performed.

| Validation | Result |
| --- | --- |
| .NET solution build | Passed, 0 warnings / 0 errors |
| Application tests | 481/481 passed |
| Web/API tests | 138/138 passed |
| Focused CR-01.4 Application | 23/23 passed |
| Focused CR-01.4 API/schema/migration | 10/10 passed |
| Frontend tests | 132/132 passed, including 7 category checks |
| Frontend lint / typecheck | Passed / passed |
| Frontend production build | Passed; existing bundle-size advisory remains |
| EF model parity | Clean after migration generation |
| Synthetic legacy fixture before migration | 1/1 passed |
| CR-01.4 Railway acceptance | 5/5 passed |
| Browser category checks | 9 passed; desktop and 390px responsive layout inspected |
| Existing targeted Railway inventory regressions | 15/15 passed |

Focused Railway acceptance covers category creation and audit, rename/description/order updates, deactivation/reactivation, inactive assignment rejection, historical item readability, owner denial, tenant-scoped listing/edit/status, composite FK rejection, normalized-name uniqueness and genuine two-context optimistic concurrency. Legacy fixture `AUTOTEST-CR014-LEGACY-20261006190611-50c0442372314603a7e1d1fd7d6ad78c` (tenant `6ff0d7c6-8d3c-4bb9-a59d-4b28f38e3b3d`) was retained before migration. Exact JSON facts for InventoryItems, StockPositions, StockReceipts, StockReceiptLines and StockMovements matched afterwards; all four legacy categories/labels survived. Stock remained **17 units / USD 46.75 / USD 2.75 average unit cost**.

Strong category-operation acceptance creates a uniquely labelled synthetic tenant with posted receipt and issue, field receipt, confirmed application, posted/reversed return, approved loss, approved accountability correction and cost reversals. It captures entire rows across **21 tables** plus stock quantity/value/moving-average cost, renames/deactivates/reactivates a used category, creates another category, and compares before/after facts exactly. Results: unchanged item identity, balances, receipt relationships, movement quantities/values/counts, locked issue costs, application costs/accountability, returns, losses, correction/reversal references, approvals and OperationalCostPostings. Category metadata/audit additions are the only changes. Constraint tests roll back only their own uncommitted invalid attempts; committed labelled synthetic fixtures remain. No non-test tenant was queried by acceptance assertions or modified by tests.

Known limitations: no category deletion; no item editing/recategorization (existing behavior); immutable codes including unused categories; metadata follows current category display rather than adding category snapshots; no new valuation, reporting, warehouse, lot, expiry, procurement or other inventory capabilities; browser checks use synthetic API responses while persisted backend behavior is separately verified against Railway. Migration rollback is intentionally unsupported. Existing frontend bundle-size advisory remains.

Scope/preservation: no statutory payroll changes, new Phase 8 functionality, general inventory redesign, or CR-01.5 work. All 89 original unrelated untracked files were hash-checked and remain unchanged/untracked. Pre-existing Phase 8 tracked files remain at baseline; the index is empty. No commit/push/merge was performed and HEAD remains the authorized baseline.

Changed/new files:

- `docs/CR-01-Post-System-Review-Enhancements.md`
- `src/Application/Common/Interfaces/IInventoryRepository.cs`
- `src/Application/Inventory/CreateInventoryCategoryCommand.cs`
- `src/Application/Inventory/CreateInventoryCategoryCommandHandler.cs`
- `src/Application/Inventory/CreateInventoryCategoryCommandValidator.cs`
- `src/Application/Inventory/CreateInventoryItemCommandHandler.cs`
- `src/Application/Inventory/CreateInventoryItemCommandValidator.cs`
- `src/Application/Inventory/GetInventoryCategoriesQuery.cs`
- `src/Application/Inventory/GetInventoryCategoriesQueryHandler.cs`
- `src/Application/Inventory/GetInventoryWorkspaceQueryHandler.cs`
- `src/Application/Inventory/InventoryAudit.cs`
- `src/Application/Inventory/InventoryCategoryDto.cs`
- `src/Application/Inventory/InventoryMapper.cs`
- `src/Application/Inventory/InventoryWorkspaceDto.cs`
- `src/Application/Inventory/SetInventoryCategoryActiveCommand.cs`
- `src/Application/Inventory/SetInventoryCategoryActiveCommandHandler.cs`
- `src/Application/Inventory/SetInventoryCategoryActiveCommandValidator.cs`
- `src/Application/Inventory/UpdateInventoryCategoryCommand.cs`
- `src/Application/Inventory/UpdateInventoryCategoryCommandHandler.cs`
- `src/Application/Inventory/UpdateInventoryCategoryCommandValidator.cs`
- `src/Domain/Farms/Tenant.cs`
- `src/Domain/Inventory/InventoryCategory.cs`
- `src/Domain/Inventory/InventoryItem.cs`
- `src/Infrastructure/Data/ApplicationDbContext.cs`
- `src/Infrastructure/Data/Configurations/InventoryCategoryConfiguration.cs`
- `src/Infrastructure/Data/Configurations/InventoryItemConfiguration.cs`
- `src/Infrastructure/Data/Configurations/TenantConfiguration.cs`
- `src/Infrastructure/Data/InventoryRepository.cs`
- `src/Infrastructure/Data/Migrations/20261006170131_AddInventoryCategoryAdministration.Designer.cs`
- `src/Infrastructure/Data/Migrations/20261006170131_AddInventoryCategoryAdministration.cs`
- `src/Infrastructure/Data/Migrations/ApplicationDbContextModelSnapshot.cs`
- `src/Web/ClientApp/package.json`
- `src/Web/ClientApp/src/components/inventory/inventoryCategoryApi.ts`
- `src/Web/ClientApp/src/components/inventory/inventoryCategoryView.test.ts`
- `src/Web/ClientApp/src/components/inventory/inventoryCategoryView.ts`
- `src/Web/ClientApp/src/components/pages/InventoryPage.tsx`
- `src/Web/ClientApp/src/styles.scss`
- `src/Web/ClientApp/src/web-api-client.ts`
- `src/Web/Controllers/InventoryCategoriesController.cs`
- `src/Web/Models/Inventory/CreateInventoryCategoryRequest.cs`
- `src/Web/Models/Inventory/SetInventoryCategoryActiveRequest.cs`
- `src/Web/Models/Inventory/UpdateInventoryCategoryRequest.cs`
- `tests/Application.UnitTests/Inventory/InventoryCategoryTests.cs`
- `tests/Infrastructure.IntegrationTests/PostgreSqlFieldApplicationAccountabilityAcceptanceTests.cs`
- `tests/Infrastructure.IntegrationTests/PostgreSqlInventoryCategoryLegacyAcceptanceTests.cs`
- `tests/Web.UnitTests/Controllers/InventoryCategoriesControllerTests.cs`
- `tests/Web.UnitTests/Controllers/OpenApiCoverageTests.cs`
- `tests/Web.UnitTests/Infrastructure/InventoryCategoryModelTests.cs`


Final CR-01.4 design checks:

1. Category GUIDs are stable; immutable tenant codes preserve item references.
2. Names can be edited without changing ledger facts (verified persisted before/after).
3. Used categories cannot be hard deleted: no delete operation and restrictive FK.
4. Inactive categories remain readable historically.
5. Inactive categories cannot be newly assigned through item creation.
6. Existing stock quantities are unchanged.
7. Moving-average costs are unchanged.
8. Historical locked issue costs are unchanged.
9. Application costs are unchanged.
10. Reversals/losses/returns and correction relationships are unchanged.
11. OperationalCostPostings are unchanged by category administration.
12. Tenant isolation is preserved at query/command/domain/FK boundaries.
13. No general inventory redesign was introduced.
14. All 89 unrelated untracked files and pre-existing Phase 8 work were preserved.
15. CR-01.5 remains unstarted.

Final targeted Railway regression result: **15/15 passed** (receipt idempotency/concurrency, posting-order moving average, reversal guards and links, tenant isolation, issue concurrency/idempotency/draft behavior, effective-rule exclusion, field receipt limits, cost posting retry/correction, locked return/reversal valuation, adjustment reversal and historic cutoff). Tests attempting destructive ledger mutations were excluded. Together with focused CR-01.4 Railway acceptance (**5/5**) and pre-migration legacy preparation (**1/1**), all selected persisted checks are green.

Working state at implementation handoff, before the separately authorized Git closure below: branch `feature/cr-01-4-inventory-categories`; HEAD `87148403f5f997bef276f8db0d8e78a84ce8d6cc`. 48 CR-01.4 files changed/new (21 tracked modifications and 27 new files), all unstaged; existing unrelated untracked paths preserved. Index empty, diff whitespace check clean. No commit, push, merge or CR-01.5/release closure had been started.

## CR-01.4 Git closure — 2026-10-06

Git closure was separately authorized after implementation and validation. Feature commit: `5ba0854454fd619e7c66afde292123bb3cf6449b` (`feat(cr-01): add inventory category administration`), pushed to `origin/feature/cr-01-4-inventory-categories`. The feature branch is retained locally and remotely.

Merge commit: `35871c9455dc7b44ae33971aa2c9ca81381d4b10` (`Merge CR-01.4 inventory category administration`). Main and refreshed origin/main both matched baseline `87148403f5f997bef276f8db0d8e78a84ce8d6cc` before merging. This is a regular two-parent merge matching prior CR slices, without squash, conflicts, history rewrite or force push. Its tree exactly matches the verified feature tree.

Main SHA at final executable verification: `35871c9455dc7b44ae33971aa2c9ca81381d4b10`. The final main tip includes the subsequent documentation-only commit `docs(cr-01): record CR-01.4 git closure`; resolve its full SHA with `git log -1 --format=%H --grep="^docs(cr-01): record CR-01.4 git closure$" main`. As in previous slices, the closure commit cannot embed its own SHA. The final user report records the full final main SHA and confirmed push status.

Both pre-commit and post-merge gates passed:

| Closure gate | Final result on merged main |
| --- | --- |
| .NET solution build | Passed; 0 warnings/errors |
| Full Application suite | 481/481 passed; 0 failures/skips |
| Full Web/API suite | 138/138 passed; 0 failures/skips |
| Focused CR-01.4 Application | 23/23 passed |
| Focused CR-01.4 API/schema/migration | 10/10 passed |
| Frontend tests | 132/132 passed; 0 failures/skips |
| Frontend lint / typecheck | Passed / passed |
| Frontend production build/generated-client checks | Passed; existing bundle-size advisory remains |
| EF model parity | Clean; no pending model changes |
| Railway Development migration state | 23 applied / 0 pending |
| Railway CR014PostMigration acceptance rerun | 5/5 passed; 0 failures/skips |
| Diff whitespace checks | Passed |

Migration `20261006170131_AddInventoryCategoryAdministration` remains present exactly once with its original content, designer and snapshot. The designer model exactly matches the snapshot. Review confirmed additive category infrastructure, deterministic tenant/code GUIDs, verbatim legacy codes, restrictive tenant/category references, and an intentionally protective Down path. No item IDs/category strings, ledger rows, quantities or costs are rewritten. The migration was already applied once before closure; no migration was generated, applied, reapplied or rolled back during Git closure.

Post-merge acceptance reused the labelled pre-migration legacy fixture read-only. Its exact InventoryItems, StockPositions, StockReceipts, StockReceiptLines and StockMovements facts remain unchanged; all four legacy categories survive, with stock still **17 units / USD 46.75 / USD 2.75 average unit cost**. The strong category-operation test compared complete rows across 21 inventory/accountability/cost tables and stock valuation before and after rename, deactivate, reactivate and category creation. Item identity, receipt/issue relationships, movement count/quantity/value, moving-average cost, locked issue/application costs, field applications, returns, losses, reversals/corrections, approval history and OperationalCostPostings were identical. Only category metadata and new category audit facts changed.

Acceptance also verified manager category creation/update/status, inactive historical readability and new-assignment rejection, owner denial, normalized uniqueness, optimistic concurrency and cross-tenant protections. Tests scoped assertions to uniquely labelled synthetic tenants/runs; invalid constraint attempts rolled back only their own uncommitted work. Committed synthetic fixtures remain. No non-test tenant was sampled or modified, no global business-table counts were asserted, and no destructive cleanup or automatic migration execution occurred. The earlier 15/15 targeted Railway regression, 1/1 pre-migration preparation and nine browser checks remain recorded as implementation evidence; they were not rerun during Git closure.

Stable category GUIDs and immutable codes remain intact. Used categories have no hard-delete operation and retain restrictive references. Current metadata labels may change while transaction snapshots remain unchanged; no destructive historical recategorization occurred. Tenant isolation remains enforced at query, command, domain and database boundaries. No general inventory redesign, statutory payroll change, new Phase 8 functionality or CR-01.5 work was introduced.

The feature staging set exactly matched the reviewed 48-file CR-01.4 manifest. SHA-256 fingerprints verified all 89 original unrelated untracked files and all tracked files outside that manifest. Pre-existing Phase 8 work was preserved. Local configuration, secrets, temporary outputs and unrelated documentation were not staged. This closure commit changes only this document.

Known limitations remain unchanged: no category deletion or item editing/recategorization; codes immutable even when unused; current category display metadata without a new snapshot system; intentionally unsupported migration rollback; synthetic browser fixtures; existing frontend bundle-size advisory. No application deployment was performed or verified; a Git push alone does not establish deployment success.

Main is ready for a normal push after this documentation-only commit; successful push and synchronized final main/origin/main are confirmed in the final user report. The feature branch is retained. **CR-01.5 has NOT started.** CR-01.4 Git closure is the stopping boundary.

## CR-01.5 — Consolidated Regression & UAT Gate

Validation baseline: `54206cede20baff89a22cf96c644a7653eaa354e`, synchronized main with a clean tracked tree/index. Validation branch: `feature/cr-01-5-consolidated-regression`. Initially this phase changed only this document and added one CR015Acceptance test to the existing PostgreSqlFieldApplicationAccountabilityAcceptanceTests fixture. After the initial HIGH finding, the user explicitly approved only CR015-D01: a sequencing correction in PostStockReturnCommandHandler, with focused tests and complete revalidation. Configuration, dependencies and database model changes remain outside scope. Git closure remains a separate prompt; no commit, push or merge is performed here.

### Requirements traceability

Each row identifies the existing component and executable evidence; related requirements are grouped without extending their scope. Browser evidence uses intercepted synthetic responses on the local application, separately from persisted Railway acceptance.

| Approved requirement | Slice / component | Validation evidence | Status |
| --- | --- | --- | --- |
| Farm Owner business terminology; Grower internals retained | CR-01.1 / FarmPage, FarmOwnerFields, GrowerProfile | farmOwnerForm/navigation frontend tests; owner browser workflow; FarmOwnerProfileTests | Covered |
| Structured owner identity, association/membership, contact/address/email and status | CR-01.1 / FarmOwnerProfileInput, profile handlers, FarmProfileEditor | FarmOwnerProfileTests; ProfilePersistenceProtectsIdentityAndLoadsExistingFieldsAndCycles; integrated CR015Acceptance | Covered |
| Tenant-owned Farm Model create/assign/change/clear and inactive semantics | CR-01.1 / FarmModel, FarmModelEditor, composite FK | ModelCanBeAssignedChangedAndCleared; inactive/cross-tenant domain tests; CR01PostMigration uniqueness/FK checks | Covered |
| Owner ID protection, masking, exact authorized reveal, audit and normalized repeated-ID behavior | CR-01.1 / existing sensitive-data protector, reveal handler/controller | FarmOwnerProfileTests normalization/context/reveal denial tests; FarmProfileContractTests; persisted encrypted/masked/audited reveal | Covered |
| One owner per tenant; tenant-scoped protected-ID uniqueness | CR-01.1 / GrowerProfile EF mapping | Existing unique TenantId and filtered tenant/fingerprint model mapping; normalized repeated ID preserves fingerprint; foreign-context decryption rejection | Covered |
| Owner photograph reference | CR-01.1 / profile metadata and editor | profile create/update tests; persisted asset reference; owner browser edit | Covered; reference only |
| Compact Add Field with physical data only; no fabricated crop/Actual Yield | CR-01.2 / FieldForm, Field, CreateField handler | PhysicalFieldCreationAndEditingPersistWithoutCropValues; fieldCropUx frontend tests; browser Add Field; integrated empty-cycle assertion | Covered |
| Independent CropCycle creation and retained Field/CropCycle boundary | CR-01.2 / CropCycleForm, CreateCropCycle handler | FieldCropUxTests; CR012Acceptance; integrated CR015Acceptance | Covered |
| Configurable 14-month default and calendar month-end/leap-year calculation | CR-01.2 / CropMaturityOptions and options binding | DefaultMaturityIsFourteenMonthsAndMissingDateHasNoMaturity; configured binding test; three leap/month-end cases | Covered |
| Maturity snapshot; no configuration-driven recalculation of historical cycles | CR-01.2 / stored harvest window, draft editing | CreatingCycleSnapshotsConfiguredMaturityAndReadsDoNotRecalculate; ActiveAndHistoricalPlansCannotBeRecalculated; persisted CR012Acceptance | Covered |
| Derived crop age using injected clock; harvested age stops at harvest | CR-01.2 / CropCycle.AgeInMonths, query mapper | ActiveAgeUsesInjectedClockAndFutureStartIsZero; HarvestedAndClosedAgeStopsAtActualHarvestDate; historical persisted read | Covered |
| Manual Actual Yield, existing tonnes precision, lifecycle and correction history | CR-01.2 / HarvestResult, UpdateActualYield handler, CropCycleEditForm | FieldCropUxTests; CR012Acceptance; browser manual save; integrated persisted 55.125 t and timeline | Covered |
| Mill tickets/reconciliation independent from Actual Yield | CR-01.2 / unchanged mill domain, separate crop handler | existing unit ticket/match immutability assertions; persisted CR012Acceptance; integrated ticket/statement/match before/after comparison | Covered |
| Employee business terminology and compact Create/extended Edit | CR-01.3 / LabourPage, WorkerDetails | employeeMaster frontend tests; EmployeeMasterControllerTests; Employee browser create/edit | Covered |
| Existing WorkerProfile aggregate/IDs; legacy names not split; shared Person identity | CR-01.3 / WorkerProfile, Person, profile handlers | LegacyWorkerRemainsReadableWithSameIdentityAndNullEnrichment; NullStructuredNamesDoNotSplitOrDestroyLegacyDisplayName; CR013 legacy fixture | Covered |
| Employee Number normalization, optionality and tenant uniqueness | CR-01.3 / WorkerProfile and unique filtered index | EmployeeMasterTests; duplicate/concurrency/FK CR013Acceptance checks | Covered |
| Structured names, Title, Sex, address and DOB validation | CR-01.3 / WorkerProfileInput, validators/domain | profile update/clear tests; future DOB validator/domain rejection; persisted profile test | Covered |
| Next of Kin and photograph reference | CR-01.3 / profile fields/editor | EnrichmentCanBeClearedAndKinUpdated; persisted profile test; Employee browser edit | Covered; reference only |
| Employment Date uses immutable ActiveFrom; archive semantics unchanged | CR-01.3 / WorkerProfile eligibility/status | EmployeeMasterTests immutable-date/profile/status checks; archived enrichment test; legacy persisted ActiveFrom assertion | Covered |
| Employee ID encrypted/masked, authorized reveal/correction/audit, duplicate rejection | CR-01.3 / retained sensitive-data architecture | EmployeeMasterTests; CR013Acceptance ciphertext/mask/reveal/correction/duplicate checks; API no-cache/contract tests | Covered |
| Worker attendance, labour evidence, activity and payroll references/name snapshots/totals retained | CR-01.3 / existing operational relationships | CR013 legacy and profile persistence tests; IdentityEnrichmentKeepsAttendanceEvidenceAndPayrollSnapshotReferences; integrated persisted payroll/evidence comparison | Covered |
| Tenant-owned configurable categories; stable GUID and immutable code | CR-01.4 / InventoryCategory, composite item/category FK | InventoryCategoryTests; InventoryCategoryModelTests; CR014PostMigration; integrated identity/code checks | Covered |
| Category create/edit name/description/order; normalized uniqueness/version | CR-01.4 / commands, validators, controller, Catalogue | unit/API tests; persisted normalized uniqueness and two-context concurrency; browser create/edit/read-only code | Covered |
| Activate/deactivate; historical inactive visibility; active-only assignment | CR-01.4 / status command and item creation selector | unit tests; metadata integrity acceptance and rejected inactive assignment; browser selectors/status/labels | Covered |
| No hard delete of used category | CR-01.4 / no delete route, restrictive references | CategoryControllerRequiresAuthenticationAndHasNoDeleteRoute; model Restrict assertions; migration inspection | Covered |
| Category/item tenant isolation at query/command/domain/API/FK boundaries | CR-01.4 / existing tenant access plus composite FK | unit/API/model tests; CategoryRepositoryAndCommandsRejectOtherSyntheticTenant; database tenant-reference check | Covered |
| Preserve legacy item category codes/IDs and historical records | CR-01.4 / additive deterministic seed, unchanged item strings | MigratedLegacyCategoriesAndInventoryFactsRemainIdentical; five-table legacy snapshot; original migration hash | Covered |
| Stock, moving average, receipt/issue/application costs, returns/losses/reversals/corrections and OperationalCostPostings unchanged | CR-01.4 / category metadata operations only | CategoryMetadataLeavesCompleteLedgerAndAccountabilityHistoryUnchanged (21 tables + stock); safe Inventory regressions; integrated payroll/ledger comparison | Covered |
| Farm Owner → Farm/Field/Crop → Employee/Labour → Inventory/Application → Harvest → Mill/reconciliation coexist | CR-01.1–4 / existing operational chain | new EnrichedOwnerEmployeeAndCategoriesCoexistThroughLabourApplicationHarvestAndReconciliation in existing labelled Railway fixture | Covered; enhanced return → reversal → replacement-return workflow and full revalidation passed |

### Consolidated execution record

Initial full regression passed: Application 481/481, Web/API 138/138, frontend 132/132, lint/typecheck/production build passed, .NET build zero warnings/errors, EF model parity clean. Initial selected Railway CR suites passed **23/23** (CR-01.1 5; CR-01.2 5; CR-01.3 8; CR-01.4 5). Focused CR/authorization/protected-ID Application filter passed 120/120 and focused API/schema/labour filter passed 25/25. All infrastructure fixtures are Explicit; an unfiltered infrastructure invocation selects zero executable tests, so it is not accepted as persisted validation evidence. Explicit, safe selected acceptance runs provide that evidence instead.

The integrated test adds a meaningful single-tenant persisted cross-slice path using the existing synthetic operational graph. It compares original payroll calculation and employee name snapshots, attendance/evidence/activity links, ledger facts and stock valuation before/after metadata enrichment; manual yield is separately compared with unchanged recorded mill ticket/statement/match quantities and amounts. It verifies category and profile audit actors and crop correction history. The fixture supplies the existing required reason for confirmation of historical August work in October. It permits at most three attempts for the existing InventorySerializationFailureException response, reusing the same idempotency key in a fresh context; all other failures and retry exhaustion fail the test. No business rule or production retry behavior was changed.

Validation development findings: new-test compile/API mismatches were corrected in test code only. The first integrated run correctly rejected a missing late-confirmation reason. A concurrent acceptance run surfaced the existing retryable PostgreSQL 40001 response; subsequent consolidated acceptance ran sequentially and the bounded test retry handles only that existing response. The first harvest attempt correctly rejected an activity that the fixture had not closed. These test-harness corrections do not waive failing release checks. Completing the actual application/return/accountability chain then exposed the HIGH defect recorded below; validation stopped without a functional fix.

### Authorization, audit, isolation and historical semantics

| Role | Existing CR access preserved |
| --- | --- |
| Farm Manager | Farm/profile and Field/Crop management, Employee profile administration, category create/edit/status; protected owner/worker ID reveal and worker ID correction remain denied |
| Farm Owner (internal Grower) | Existing profile, farm, Field/Crop, Employee and operational rights retained; exact protected-ID reveal/correction authorization retained; no category administration rights added |
| Supervisor | Existing operational tenant/field/activity reads and assigned work remain; owner/employee/category administration and payroll access remain blocked |

This is a regression of existing policies, not a new role model. In particular, Farm Owner already had Employee creation/profile access before CR-01; retaining those rights does not grant a new privilege. MediatR role checks, handler defenses, controller authentication, masked contracts/no-cache reveal endpoints, scoped repository reads and composite database references are covered by the complete and targeted suites. Tenant isolation is checked across owner/model, physical Field/CropCycle, Worker/protected ID, categories/items and operational inventory; no cross-tenant association is enabled.

AuditEvent checks cover owner ProfileUpdated/FarmModelAssigned and reveal, worker profile/ID changes, category Created/Updated/Activated/Deactivated and inventory postings. Crop lifecycle, plan and Actual Yield changes intentionally use the existing append-only CropCycleStatusChange timeline; no second audit system is introduced. Tenant, actor and stable subject checks use synthetic identifiers; plaintext protected IDs are excluded from ordinary DTO/audit summaries.

Historical evidence uses the retained labelled pre-CR owner, worker/attendance/evidence/payroll-advance and inventory fixtures. Other operational regression data is uniquely labelled synthetic data created for each run, not sampled real tenants. No destructive update/delete tests, global business-row counts or cleanup are used. Constraint failures roll back only their own invalid uncommitted attempts; committed labelled fixtures remain.

### Responsive and deployment evidence

Synthetic local browser UAT passed at **1440, 1024, 768, 390 and 360 px**. Owner/Field/Crop checks covered 30 screen/viewport combinations, associated labels, no document/dialog horizontal overflow, owner dialog Tab/Escape, masked owner reveal/hide, Supervisor read-only visibility and manual yield save. Employee create/edit/mask/reveal/hide and list/profile/edit overflow checks passed at all five widths. Inventory list/create/edit/deactivate/reactivate, immutable code, inactive historical label, active-only selector and owner read-only checks passed, including list/edit dialogs at all five widths. Mobile screenshots were visually inspected for owner/Employee dialogs, crop yield and category administration. This is targeted keyboard/layout UAT, not a full assistive-technology certification.

Deployment smoke status: **B — not performed; local/API/browser/database validation only**. No deployed Development application was verified and no application deployment was performed. Synthetic interception demonstrates rendered frontend behavior; Railway acceptance separately demonstrates persisted backend behavior.

### Preservation and limitations

Original SHA-256 hashes cover all 89 unrelated untracked paths and every baseline tracked file. Final preservation checks permit only this document, the existing acceptance-test file and the approved PostStockReturnCommandHandler / ReverseStockReturnCommandHandler corrections to differ. All existing CR migrations, designer files/model snapshot, secrets, unrelated documents and pre-existing Phase 8 work remain unchanged. No CR-01.5 migration is created; CR-01.2 still has none. Railway remains at 23 applied/0 pending.

Existing limitations remain: photograph references only, no storage/upload service; immutable Employee Employment Date and existing archive semantics; category codes immutable and no category deletion/item recategorization; current category metadata labels without new transaction snapshots; intentionally unsupported CR-01.4 migration rollback; existing bundle-size advisory and untrusted-proxy rate-limit startup warning. Production dependencies are unchanged; the only approved behavior corrections are CR015-D01 and the revised CR015-D02 correction, documented below. This phase does not perform CR-01 Git/documentation closure or resume Phase 8.

### Initial stopped gate — HIGH defect CR015-D01

**Defect:** posting the final stock return can leave a stale open InventoryUnaccounted ControlException even when the issued quantity is fully accounted for. The new integrated test issued 10 units, confirmed application of 4 units, and posted a return of the remaining 6 units through existing handlers. HasBlockingInventoryExceptionAsync still returned true, so the explicit inputsAccounted assertion failed before activity closure/harvest. The failing synthetic fixture is retained; it was not cleaned up or altered to bypass the failure.

**Affected scope:** existing Inventory field accountability/stock return workflow (Phase 5C), exercised as CR-01.4 and consolidated CR-01.5 regression. This is a **HIGH** workflow regression finding because a valid fully accounted activity cannot close, and crop harvest requires activities to be closed or cancelled. It blocks CR-01 closure. No stock/cost corruption, protected-ID leak or tenant-isolation failure was observed; those successful checks do not waive this failure.

**Evidence and cause:** PostStockReturnCommandHandler marks the return posted and appends its movement in memory, then invokes InventoryAccountability.SynchronizeExceptionAsync before SaveChangesAsync. GetPostedReturnedQuantityAsync queries persisted StockReturns with Posted status, so the just-posted return is excluded during reconciliation. The open exception is retained and the handler subsequently commits the return without reconciling again. HasBlockingInventoryExceptionAsync correctly sees the remaining open exception. ConfirmInputApplicationCommandHandler already uses the safe contrasting sequence: save confirmation inside the serializable transaction before querying authoritative accountability rows.

**Proposed minimal fix, requiring explicit approval:** persist the return status/movements/audit within the existing serializable transaction before accountability reconciliation; reconcile against those persisted rows, save exception/audit changes, and commit once. Preserve existing locks, idempotency, ledger facts and locked costs. Add focused final-return closure coverage and rerun the consolidated gate. Review analogous posting/reversal sequencing only to the extent necessary for the approved fix. No migration, new functionality, valuation change or ledger rewrite is proposed. Production code had not been changed at this initial stop.

| Gate at stop | Observed result |
| --- | --- |
| Latest .NET build | Passed; 0 warnings/errors |
| Full Application / Web/API | 481/481 / 138/138 passed |
| Focused CR Application / API-schema | 86/86 / 26/26 passed |
| Additional CR/authorization/protected-ID and API/labour filters | 120/120 / 25/25 passed |
| Frontend tests / lint / typecheck / production build | 132/132 / passed / passed / passed |
| EF model parity / CR-01.5 migration | Clean / none |
| Railway migration state | 23 applied / 0 pending; no migration applied in this phase |
| Existing CR-01.1–4 consolidated Railway acceptance | Initial 23/23 passed |
| Expanded 24-test consolidated Railway gate | 23 passed / 1 failed; integrated fixture initially omitted required activity closure |
| Corrected integrated accountability workflow | Failed: final return leaves blocking open ControlException |
| Safe existing Inventory regression selection | 15/15 passed; this selection did not cover final-return exception resolution |
| Final complete release gate | Not completed green; stopped on HIGH defect |

Browser results remain synthetic local evidence. The base owner/Field/Crop 30 layout combinations, Employee and category workflows passed. Expanded Employee Create and category Create dialogs passed at all five widths, with labels and keyboard interaction. The optional expanded crop-planning browser script has not passed: its CropVarieties interception used the wrong route casing, producing an invalid synthetic response. That script issue is not classified as a product defect; it is not counted as successful UAT evidence. Existing crop-plan frontend and persisted tests remain green. No deployed application smoke was performed. The new integrated test did not reach its final mill/statement comparisons, so those new persisted financial assertions remain unverified; existing crop/mill unit and CR012 persisted ticket independence evidence remain valid.

Defects: **HIGH 1 (CR015-D01)**; no observed BLOCKER, MEDIUM or LOW product defects. Test-harness corrections and the unresolved optional browser fixture issue are separately recorded above. No defect was silently fixed or failing test waived. CR-01.1–4 remain closed slices; CR-01.5 is incomplete and CR-01 is not marked fully closed.

Initial release recommendation: **NOT READY FOR CR-01 CLOSURE**. The HIGH accountability defect and failing integrated gate require an explicitly approved fix and a fresh complete regression/UAT gate. No CR-01 migration is pending; no CR-01.5 migration exists. Phase 8 remains stopped pending CR-01 Git/documentation closure.


### Approved CR015-D01 correction — revalidation record

The initial failure above is retained as release history. The user subsequently approved the minimal sequencing correction and required a fresh complete CR-01.5 gate. Before editing production code, the integrated Railway test failed twice at the original inputsAccounted assertion. The diagnostic reproduction read persisted issued/applied/returned facts of **10 / 4 / 6**, calculated unaccounted **0**, and a still **Open** exception with its previous snapshot of 10.

The only production change is six added lines in PostStockReturnCommandHandler: finish appending all return movements, call SaveChangesAsync inside the existing serializable transaction, then perform the existing per-line reconciliation. The original final SaveChangesAsync and transaction.CommitAsync remain in place. Locks, tenant/authorization checks, expected-version validation, idempotency check, quantity limits, return cost snapshots, movement construction and audit construction are unchanged. The flush is not a commit; there is no second transaction, background reconciliation, schema/model change or migration.

Five focused PostgreSQL cases are added to the existing fixture: exact return 10/4/6; partial return 10/4/5; partial return 5 followed by final return 1; complete application 10 with no return; and forced failure at the second save. The exact/partial cases also exercise an identical-key retry with the original stale version, compare the full 21-table/stock snapshot and audit snapshot across retry, assert one movement/Posted audit and at most one resolution audit, and compare original receipts, issues, applications, movements and OperationalCostPostings across posting. Expected return cost is the unchanged locked issue cost of USD 3/unit: six returned units restore USD 18, leaving 16 units/USD 48 at USD 3/unit. Partial return restores five units/USD 15, leaving 15 units/USD 45; its persisted aggregate has one unaccounted unit and remains blocking. The existing exception snapshot/lifecycle rules are retained.

The rollback interceptor allows the first flush, verifies the posted return and movement are visible inside that same transaction, verifies reconciliation has resolved the tracked exception, and throws before the second save. A fresh connection must exactly match the prior committed draft, return lines, ledger, stock valuation, costs, exception and audit state. This tests rollback after a real PostgreSQL flush, not an in-memory mock.

The integrated workflow retains the original failure assertion and now uses TransitionActivityCommandHandler for normal status progression and closure. It checks the persisted final-return quantities/exception, normal closure history, harvest prerequisites, manual yield 55.125 t, and unchanged recorded mill ticket/statement/match (90 t and USD 1,800). Original payroll calculations/name snapshots and worker/attendance/evidence/activity links are compared across profile enrichment; category stable GUID/code and historical inventory facts are compared across rename.

Test-only revalidation corrections are recorded separately: persisted mill decimal scales must be compared using newly materialized AsNoTracking records on both sides rather than an EF tracked object whose equal decimal value retains its original CLR scale; parameterized return cases require different synthetic idempotency keys, while each retry deliberately reuses its own original key/version. No financial assertion was removed and no production cost logic was changed. The optional local crop-planning browser fixture route casing was corrected only in its temporary script outside the repository; the expanded run passed all 35 screen/viewport combinations with labels, keyboard/dialog checks, owner reveal/hide, Supervisor read-only state and manual yield save at 1440/1024/768/390/360 px, with no page errors. Previously successful Employee/category browser evidence is retained, not claimed as rerun. Deployment status remains B: no deployed application smoke or deployment performed.

The focused PostgreSQL gate passed **6/6**. The fresh consolidated Railway gate passed **29/29**: CR-01.1 5, CR-01.2 5, CR-01.3 8, CR-01.4 5, integrated workflow 1 and focused CR015-D01 5. The partial-return case additionally attempted real activity closure and correctly received the existing validation rejection. The original 10/4/6 integrated assertion, normal activity closure, downstream harvest, manual yield and mill/statement comparisons all passed. The original failure assertion was neither removed nor weakened.

| Fresh gate after approved correction | Result |
| --- | --- |
| .NET solution build, including the new reversal regression | Passed; 0 warnings/errors |
| Complete Application / Web/API | 481/481 / 138/138 passed |
| Focused CR Application / API-schema | 86/86 / 26/26 passed |
| CR/authorization/protected-ID / API-labour filters | 120/120 / 25/25 passed |
| Focused Inventory/activity Application filter | 94/94 passed |
| Frontend tests / lint / typecheck / production build | 132/132 / passed / passed / passed |
| EF pending-model parity | Clean; no model change since last migration |
| Railway migration status | Development; 23 applied / 0 pending, read-only check |
| Consolidated selected Railway CR acceptance | 29/29 passed; no failures/skips |
| Additional final-return reversal PostgreSQL regression | 0/1 passed: HIGH CR015-D02; actual closure incorrectly succeeded |
| Prior 15 safe Inventory regressions / planned extra accountability selection | Not rerun after the approved fix: stopped on the newly confirmed HIGH defect; their prior 15/15 result is historical evidence only |
| Complete final release gate | Red/incomplete; no failure waived |

TRX records for the fresh full and focused .NET runs are retained locally in /private/tmp/cr015-results. Frontend build retains the existing chunk-size advisory; lint retains the generated-client Babel size note. Generated-client contents remained hash-identical, so these do not represent new frontend changes.

CR015-D01 final status: **STILL OPEN** under the required full-gate verification rule. Its approved correction works and all six targeted PostgreSQL cases, the original reproduction assertion, activity closure and integrated harvest path pass. It is not labelled FIXED — VERIFIED because the additional required reversal regression exposed CR015-D02 and the complete final regression/UAT gate is not green.

### New stopped gate — HIGH defect CR015-D02

**Defect:** reversing a final posted stock return does not restore the required InventoryUnaccounted blocker. The approved CR015-D01 correction resolves the initial exception correctly. However, ReverseStockReturnCommandHandler posts its opposite movement, marks the return Reversed, saves and commits without calling the existing accountability reconciler. The previous resolved exception remains Resolved even though the return is no longer counted as Posted.

**Real PostgreSQL evidence:** ReversingFinalReturnReopensAccountabilityAndBlocksClosure creates only its uniquely labelled synthetic tenant/run, applies 4 of 10 issued units, posts the final 6-unit return and confirms the closure blocker is gone. It then reverses that return through the existing Grower handler. Persisted quantities become **issued 10, applied 4, returned 0, unaccounted 6**; HasBlockingInventoryExceptionAsync returns **false** and the prior exception remains **Resolved**. The test follows normal Activity transitions through Completed, then calls TransitionActivityCommandHandler for Closed. Closure **incorrectly succeeds**, and the test fails because the required validation rejection did not occur. The failing assertions and clearly labelled fixture are retained; no cleanup, closure-rule bypass or assertion waiver is used.

**Financial evidence in that failing test:** before the closure assertion, original receipts/issues/applications/movements and OperationalCostPostings match their pre-reversal snapshot; the normal opposite return movement restores the expected stock balance of 10 units/USD 30 at USD 3/unit. The observed defect concerns exception/closure lifecycle, not an observed valuation, tenant-isolation or protected-ID failure.

**Affected scope and severity:** existing Inventory stock-return reversal / field accountability / activity closure, exercised by the consolidated CR-01.4/CR-01.5 regression. **HIGH**, blocking CR-01 closure: an activity with renewed unresolved inputs can close and therefore satisfy a downstream harvest prerequisite incorrectly. This latent missing-reconciliation path is exposed once final-return posting resolves the exception correctly. No BLOCKER, MEDIUM or LOW product defect was observed.

**Proposed minimal fix — not implemented; separate approval required:** in ReverseStockReturnCommandHandler, retain the existing transaction, locks, authorization, concurrency/idempotency checks and exact opposite valuation logic. After its existing SaveChangesAsync flushes the Reversed return and reversal movements, load the scoped issue lines and call InventoryAccountability.SynchronizeExceptionAsync. Persist the resulting new open exception/audit through a second save, then commit the same transaction once. Use the existing exception lifecycle; do not rewrite the resolved historical exception, redesign ledger/accountability, change costs, add schema or introduce another transaction. Add rollback/idempotency and fully accounted return/reversal closure tests, then restart the complete release gate after explicit approval.

Work stopped immediately after this real PostgreSQL failure. No reversal production code, new functionality, Phase 8, statutory payroll, model or migration changes were made. The only production diff remains the six-line approved PostStockReturnCommandHandler sequencing correction. The 23 migration definitions, designers and model snapshot remain unchanged; the three existing CR migrations each exist exactly once and CR-01.2/CR-01.5 have no migration. No migration was generated, edited or applied.

The requirements traceability matrix and all original stopped-gate history are retained. CR-01.1/2/3 UAT and the CR-01.4 category/legacy/cost tests pass; CR-01.4/consolidated Inventory reversal lifecycle acceptance is blocked by CR015-D02. Authorization/protected-ID and audit/tenant/historical/payroll/mill checks passed on the executed paths; the missing reopening audit/exception is specifically part of the new defect, not waived as a passing audit result.

Preservation: SHA-256 comparison confirms all 89 unrelated untracked paths and all baseline tracked files outside this document, the existing acceptance-test file and the approved posting handler remain unchanged, including pre-existing Phase 8 work, generated client, existing migrations and model snapshot. Index empty; all three changes remain unstaged. Current branch feature/cr-01-5-consolidated-regression; HEAD remains 54206cede20baff89a22cf96c644a7653eaa354e. No commit, push, merge, deployment or Phase 8 resumption occurred. Deployment smoke remains B: local/API/synthetic-browser/database evidence only. Existing photograph-reference, immutable Employment Date/category code, unsupported migration rollback and accessibility/deployment limitations remain.

Release recommendation: **NOT READY FOR CR-01 CLOSURE**. CR015-D02 is a confirmed HIGH defect, its regression is red, and the remaining complete Inventory/final UAT gate is uncompleted. A separately approved minimal reversal reconciliation correction and fresh complete revalidation are required. Phase 8 remains stopped pending CR-01 Git/documentation closure.


### D02 approval follow-up — root-cause gate stopped before implementation

The user approved the minimal D02 correction, while explicitly requiring a stop if inspection found a materially different cause from a reconciliation query executed before the reversal flush. Before editing production code, the existing ReversingFinalReturnReopensAccountabilityAndBlocksClosure test was rerun against Railway Development using its uniquely labelled synthetic tenant/run. It failed again: issued/applied/effective-returned **10 / 4 / 0**, calculated unaccounted **6**, exception **Resolved**, blocking exception **false**, and normal activity closure **succeeded**. The reproduction's historical/valuation assertions passed first: unchanged original receipts/issues/applications/movements/OperationalCostPostings and stock **10 units / USD 30 / USD 3 per unit**. TRX evidence: /private/tmp/cr015-results/cr015-d02-approved-reproduction.trx (1 executed, 1 failed).

Actual path: InputControlsController.ReverseReturn dispatches ReverseStockReturnCommand to ReverseStockReturnCommandHandler. That handler creates exact opposite movements with original reversal links, marks the return Reversed, records the reversal audit, saves inside its existing serializable transaction, then commits. It contains **no accountability reconciliation call at all**; no pre-flush accountability query exists in this reversal path. The existing transaction boundary is suitable and disposal rolls back uncommitted work. The existing reconciler preserves resolved history and creates a new open exception episode only when unaccounted quantity is positive and no open exception exists.

Execution stopped under the user's explicit root-cause condition before modifying the reversal handler. The required correction remains adding the existing reconciliation after the handler's existing flush, followed by exception/audit persistence and the same final transaction commit; the diagnostic distinction is missing reconciliation versus premature reconciliation. No D02 implementation or new test changes were made during this follow-up. The D01 six-line correction and all prior test/documentation work remain intact. Full regression/UAT, new D02 matrix and final migration/model checks were not rerun after this stop; previous results remain historical evidence, not fresh release-gate results. D02 remains STILL OPEN and the recommendation remains NOT READY FOR CR-01 CLOSURE.

Preservation checks again confirmed all 89 unrelated untracked paths and every baseline tracked file outside the three existing CR-01.5 files remain hash-identical, including Phase 8 work, migrations and model snapshot. No migration was created or applied; no staging, commit, push, merge or Phase 8 work occurred. Branch remains feature/cr-01-5-consolidated-regression; HEAD remains 54206cede20baff89a22cf96c644a7653eaa354e; index empty, three existing tracked changes unstaged.


### Revised CR015-D02 approval — correction implemented; full revalidation in progress

After the root-cause stop above, the user explicitly confirmed the actual missing-reconciliation cause and approved adding the existing reconciler after the reversal handler's existing flush. ReverseStockReturnCommandHandler now loads each scoped issue line after that flush, invokes InventoryAccountability.SynchronizeExceptionAsync with the return's ActivityId, saves exception/audit changes and commits the same serializable transaction. The production diff is eleven added lines. No locks, validations, authorization, concurrency/idempotency checks, valuation/movement construction, reversal relationships or business rules changed. This is a missing reconciliation invocation, not a pre-flush-query defect. The D01 PostStockReturnCommandHandler correction remains byte-for-byte unchanged.

The existing reconciler intentionally preserves resolved episode history. A new deficit creates exactly one new Open episode only when none is open; existing Open episodes are reused according to existing snapshot semantics. Focused tests now assert original resolved history byte-for-byte, distinct new episode identity/quantity, no duplicate open episode, stale-version idempotent reversal retry, unchanged historical inventory/cost facts and exact opposite movement/link values. Final and partial return reversals, replacement return resolution, a valid complete-application/reversal-retry control, and second-save failure rollback are covered. The rollback test verifies reversal facts are visible inside the transaction after its first flush and the new Open episode is tracked, then interrupts persistence before commit; a fresh connection must match the complete prior ledger/accountability/audit snapshot.

The integrated workflow retains the D01 10/4/6 assertions and extends the chain through reversal, real closure rejection, natural harvest rejection, valid replacement return, second episode resolution, real closure, harvest/manual yield and mill/reconciliation reads. It compares original payroll snapshots and original inventory/cost facts across the complete chain, checks two Posted audits/one Reversed audit and both episode histories/actors/order, three return-related movements and final stock 16 units/USD 48 at USD 3/unit. Synthetic browser evidence is retained unchanged: Owner/Field/Crop 35 checks at 1440/1024/768/390/360 and prior Employee/category evidence. No UI or deployment changes were made.

Build passed with zero warnings/errors; the Inventory/activity Application filter passed 94/94. The combined ten-case D01/D02 PostgreSQL gate is in progress. The initial launch was interrupted by an agent-server restart; process inspection confirmed no surviving test runner and no result directory before restarting that launch. Complete CR-01.5 regression will restart only after the focused gate is green. Defects remain pending full-gate verification; no final release recommendation is upgraded at this intermediate point.

The first combined focused run subsequently encountered Railway transport failures before product assertions in the integrated metadata snapshot (connection reset by peer) and D01 rollback fixture setup (read timeout while creating its receipt). The unchanged D02 rollback, complete-application control and exact/partial D01 cases passed on that run. These failures are retained as a red run; they are not waived or classified as proof of an accountability regression. The complete ten-case focused suite must be repeated unchanged and pass before the full release gate starts. No retry policy, dependency or assertion was changed to accommodate the connection errors.

The unchanged focused rerun passed **10/10** against PostgreSQL (cr015-d01-d02-focused-rerun.trx): five D01 cases, four D02 cases and the enhanced integrated workflow. The earlier transport-failed run remains separately retained as cr015-d01-d02-focused-transport-failures.trx (8 passed, 2 failed). Both the integrated workflow and D01 rollback fixture passed without any code or assertion changes on the rerun. The complete CR-01.5 gate is now restarted from its build/full-suite beginning; focused success alone does not mark either defect FIXED — VERIFIED.

Fresh complete-gate local results after the ten-case focused rerun:

| Restarted gate | Result |
| --- | --- |
| .NET solution build | Passed; 0 warnings/errors |
| Application | 481/481 passed |
| Web/API, including infrastructure/model contract tests | 138/138 passed |
| Focused CR Application / API-schema | 86/86 / 26/26 passed |
| Authorization/protected-ID / API-labour | 120/120 / 25/25 passed |
| Inventory/activity Application | 94/94 passed |
| Frontend tests | 132/132 passed |
| Lint / typecheck | Passed / passed |
| Frontend production build | Passed; existing bundle advisory only |
| EF pending-model check | No model changes since last migration |
| Railway read-only database status | Connection succeeded; 23 applied / 0 pending |

The consolidated PostgreSQL run is executing all existing CR-01.1–4 categories plus the D01/D02 and enhanced integrated cases. The prior 15 safe Inventory regressions and eight additional existing accountability/closure/isolation checks will execute sequentially after it. Current successful counts above do not substitute for those remaining persisted gates. No migration was generated, edited or applied.

The restarted consolidated Railway acceptance completed **33/33 passed**: CR-01.1 5/5, CR-01.2 5/5, CR-01.3 8/8, CR-01.4 5/5, D01 5/5, D02 4/4 and enhanced integrated workflow 1/1. The integrated path again passed real closure rejection and natural harvest rejection after reversal, then replacement return, normal closure, harvest/manual yield and mill/reconciliation independence. TRX: /private/tmp/cr015-d02-revised-results/cr015-final-consolidated.trx. The final 23-case safe Inventory run has started (the original fifteen plus eight additional existing accountability checks); release verification is still pending that run and final preservation/status checks.

### Final restarted CR-01.5 gate — 8 October 2026

The complete gate restarted after the unchanged focused suite passed 10/10. The original fifteen safe Inventory regressions now pass **15/15**, with **8/8** additional existing accountability/closure/isolation checks: **23/23** in cr015-final-safe-inventory.trx. Every selected test executed and passed; none was skipped or waived. This completes the persisted regression work that the previous D01/D02 findings had stopped. The final read-only Railway check again confirms **23 applied / 0 pending**; the final EF check reports no model changes since the last migration.

| Final fresh release gate | Result |
| --- | --- |
| Application tests | 481/481 passed |
| Web/API tests, including model/contract tests | 138/138 passed |
| Focused CR Application / API-schema | 86/86 / 26/26 passed |
| Authorization/protected-ID / API-labour | 120/120 / 25/25 passed |
| Inventory/activity Application | 94/94 passed |
| Frontend tests | 132/132 passed |
| Lint / typecheck | Passed / passed |
| .NET solution build | Passed; 0 warnings/errors |
| Frontend production build | Passed; existing chunk-size advisory only |
| EF parity / pending model changes | Clean / none |
| Railway migrations | 23 applied / 0 pending |
| Consolidated Railway acceptance | 33/33 passed |
| Separate focused D01+D02+integrated PostgreSQL rerun | 10/10 passed; repeated within the 33-case gate |
| Original safe Inventory regressions / additional accountability checks | 15/15 / 8/8 passed |
| New/edited migration or model snapshot | None |

Counts from overlapping focused filters are not added to full-suite totals. Executable .NET evidence is retained in /private/tmp/cr015-d02-revised-results; build/frontend/parity/status logs are retained in /private/tmp/cr015-d02-*.log. The transport-failed run remains retained separately and is superseded by the unchanged successful focused rerun and complete successful gate, not waived.

**CR015-D01: FIXED — VERIFIED.** The six-line posting correction remains byte-for-byte identical to its approved starting state. Persisting a posted return/movement inside the existing transaction before reconciliation restores the exact 10 issued / 4 applied / 6 returned result: zero unaccounted, the original episode Resolved, normal closure and downstream harvest allowed. Partial 10/4/5 stays Open with one unit unaccounted; a later valid return restores accountability without double-counting. Stale-version idempotent retries preserve complete ledger/accountability/audit snapshots. Failure after the first flush rolls the entire transaction back. Locked valuation and audit/cost idempotency pass in the focused and complete PostgreSQL runs.

**CR015-D02: FIXED — VERIFIED.** Before correction, PostgreSQL reproduced effective 10/4/0 facts and six unaccounted units after reversal, while the exception remained Resolved and the real closure handler incorrectly succeeded. The actual root cause was the complete absence of reconciliation in ReverseStockReturnCommandHandler. The revised approved eleven-line correction invokes the existing InventoryAccountability.SynchronizeExceptionAsync after the handler's existing reversal flush, then saves resulting exception/audit state before committing the same transaction. No new algorithm, lifecycle rule, lock, transaction, valuation calculation or schema was introduced.

The primary reversal test now proves: original episode stays byte-for-byte Resolved; exactly one distinct new Open episode records six unaccounted units; the real TransitionActivityCommandHandler rejects Closed; stale-version reversal retry produces no second reversal/movement/link/open episode/audit/cost effect. A partial-return reversal reuses the existing Open episode without duplication; its current persisted accountability aggregate is six unaccounted units. Existing Open-episode snapshot semantics are retained. A valid replacement six-unit return resolves the second episode, preserves the first resolved history and permits normal closure. A valid complete-application control accounts for all ten units after reversal; later idempotent reversal retry does not create an unnecessary episode.

The reversal rollback test proves the first save is a transaction-local flush: reversal status/movement are visible inside the transaction and the new exception is tracked before an injected second-save failure. A fresh connection then matches all prior ledger/accountability/audit snapshots; the original return remains Posted/effective, only the original Resolved episode remains, and no partial reversal, correction link, audit or cost state survives. Idempotency guards still return before reconciliation for a duplicate reversal request.

Valuation remains unchanged: issue ten units at USD 3; return six units at locked USD 18; reversal uses the existing exact opposite USD -18 movement and original reversal link. After reversal stock is **10 units / USD 30 / USD 3 unit value**. After a valid replacement return, stock is **16 units / USD 48 / USD 3 unit value**. These are expected new operational movements; original receipt/issue/application facts, locked costs and OperationalCostPostings remain unchanged. Category metadata operations also preserve the complete 21-table inventory/accountability snapshot and stock valuation.

The enhanced integrated workflow passed twice in the fresh focused/consolidated runs: Farm Owner → Farm/Field/CropCycle → Employee/attendance/labour/payroll → category/item → issue 10 → apply 4 → return 6/resolution → reversal/new Open episode → real closure rejection → normal harvest rejection while activity remains unclosed → replacement return/second resolution → normal closure → harvest/manual Actual Yield → mill/reconciliation read. Stable tenant and entity references remain intact. Actual Yield 55.125 tonnes coexists with unchanged recorded ticket/statement/match facts of 90 tonnes/USD 1800. Original payroll totals/name snapshots and original inventory facts remain identical across the operational chain.

Consolidated UAT results: **CR-01.1 passed 5/5 persisted checks**, with profile/Farm Model, protected identity, masking/reveal/audit, tenant constraints and legacy Grower compatibility covered by the full/focused API/application suites and retained browser evidence. **CR-01.2 passed 5/5 persisted checks** for physical Field/CropCycle separation, configured maturity snapshots, historical crop age, manual yield and mill independence; month-end/leap-year and injected-clock cases pass in the Application suite. **CR-01.3 passed 8/8 persisted checks** for Employee/Worker compatibility, structured data/number, protected IDs, tenant/concurrency constraints and labelled legacy attendance/evidence/payroll links. **CR-01.4 passed 5/5 persisted checks** for category identity/code, configuration/status, active-only assignment, legacy mapping and ledger integrity, supplemented by the now-green return/reversal and safe Inventory suites. The requirements traceability matrix is complete for the approved scope.

Authorization and tenant isolation passed through the existing application/API/model/database boundaries; no permissions changed. National ID plaintext remains excluded from ordinary contracts/audits. Audit assertions verify original return/resolution, owner reversal, distinct new Open episode, manager replacement/resolution and activity closure history, correct tenant/actor/stable subject IDs/order, and no duplicate events. Category/profile/reveal audits and crop lifecycle/yield timeline evidence remain green. Existing restrictive cross-tenant references, optimistic concurrency and one-open-exception database protection remain intact.

Historical/financial validation passed for labelled synthetic pre-CR owner, worker/attendance/evidence/payroll and inventory fixtures and representative current-run operational records. Original stock movements, receipt/issue/application valuations, loss/correction/reversal relationships and OperationalCostPostings are preserved except for the explicitly requested new operational return/reversal facts. Payroll totals and historical worker name snapshots remain unchanged; mill/statement/reconciliation quantities/amounts remain independent of manual Actual Yield. No real tenant's records were sampled or altered, and no destructive cleanup was performed. Committed, uniquely labelled multi-context synthetic fixtures remain under the established acceptance policy; transaction-failure cases roll back their own uncommitted work.

Browser evidence is **retained, not rerun during this backend-only correction**: corrected Owner/Field/Crop fixture 35 checks at 1440/1024/768/390/360, plus the prior Employee/category viewport/create/edit/status evidence. Existing labels, keyboard/dialog/masked-control and overflow checks remain applicable. No UI production change was made. Deployment smoke remains **B — not performed; local/API/synthetic-browser/database validation only**. No deployment or actual deployed application instance is claimed.

No other BLOCKER, HIGH, MEDIUM or LOW product defect was found in the completed gate. The earlier transport failures and daemon-interrupted launch remain validation history, not newly inferred product defects. Known limitations remain reference-only photographs, immutable Employment Date/category code, no category hard-delete or item recategorization, current metadata display semantics without new transaction snapshots, intentionally unsupported CR-01.4 migration rollback, unchanged Babel/bundle advisories and previously observed proxy-startup warning, targeted rather than full assistive-technology UAT, and no deployed application smoke. Existing operational snapshot/lifecycle semantics were intentionally retained.

Final SHA-256 preservation checks confirm the exact set and contents of all **89 unrelated untracked paths**, and every tracked file outside the four authorized CR-01.5 files, including pre-existing Phase 8 work, all migrations/designers/model snapshot and generated API client. D01 is additionally hash-checked against its approved starting contents. All three existing CR migration definitions remain present exactly once; CR-01.2 and CR-01.5 have no migration. Railway data/migration history are preserved. No statutory payroll, new Phase 8, unrelated inventory functionality or production dependency change was introduced.

Git remains on feature/cr-01-5-consolidated-regression at **54206cede20baff89a22cf96c644a7653eaa354e**. The two approved handler changes, existing acceptance-test file and this document are unstaged; index empty. No stage, commit, push, merge or release Git closure occurred.

Release recommendation: **READY FOR CR-01 CLOSURE**. No CR-01 migration is pending; no Blocker/High defect remains; all consolidated regression gates are green; CR-01.1 through CR-01.4 acceptance evidence is complete. This is validation readiness, not completed Git/release closure. **Phase 8 may resume only after the separate CR-01 Git/documentation closure.**
