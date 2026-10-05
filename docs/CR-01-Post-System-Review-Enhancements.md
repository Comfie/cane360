# CR-01 — Post-System-Review Enhancements

## Requirements lock (CR-01.0)

CR-01.0 and CR-01.1 are complete. CR-01.2 is authorized for implementation and validation; Git closure and later slices are not authorized.

| Slice | Scope | State |
| --- | --- | --- |
| CR-01.1 | Farm Owner and Farm Profile | Complete; implemented and migrated to Railway Development |
| CR-01.2 | Field/Crop UX | Implemented and validated; awaiting Git closure |
| CR-01.3 | Employee Master | Not started |
| CR-01.4 | Inventory Category Administration | Not started |
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

Baseline/HEAD: `af30f3281525d8581b2ceed1e9031f8fa5efd19a`; feature branch `feature/cr-01-2-field-crop-ux`. No commit, staging, push, merge or deployment was performed. The earlier CR-01.1 closure statements above describe that slice's historical state; the table and this section describe CR-01.2's current state.

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
