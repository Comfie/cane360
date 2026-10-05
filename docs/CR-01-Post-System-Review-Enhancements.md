# CR-01 — Post-System-Review Enhancements

## Requirements lock (CR-01.0)

Only CR-01.0 and CR-01.1 are authorized for this change.

| Slice | Scope | State |
| --- | --- | --- |
| CR-01.1 | Farm Owner and Farm Profile | Complete; implemented and migrated to Railway Development |
| CR-01.2 | Field/Crop UX | Not started |
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

Feature and merge commit references, post-merge results and push state are recorded after Git closure. No application deployment is performed by the Git closure procedure; Railway application deployment status is not yet verified.

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
