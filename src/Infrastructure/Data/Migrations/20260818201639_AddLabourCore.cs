using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLabourCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "labour");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AuthenticatedUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SecurityRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OperationalPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SafeSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_AspNetUsers_AuthenticatedUserId",
                        column: x => x.AuthenticatedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Persons_OperationalPersonId_FarmId",
                        columns: x => new { x.OperationalPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkerProfiles",
                schema: "labour",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ActiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ActiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    NationalIdCiphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    NationalIdNonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    NationalIdTag = table.Column<byte[]>(type: "bytea", nullable: false),
                    NationalIdKeyId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NationalIdFingerprint = table.Column<byte[]>(type: "bytea", nullable: false),
                    NationalIdMask = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerProfiles", x => x.Id);
                    table.UniqueConstraint("AK_WorkerProfiles_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_WorkerProfiles_ActiveDates", "\"ActiveTo\" IS NULL OR \"ActiveTo\" >= \"ActiveFrom\"");
                    table.CheckConstraint("CK_WorkerProfiles_EmploymentType", "\"EmploymentType\" IN ('Permanent', 'Seasonal', 'Casual', 'Contract', 'TaskBased')");
                    table.CheckConstraint("CK_WorkerProfiles_ProtectedNationalId", "octet_length(\"NationalIdCiphertext\") > 0 AND octet_length(\"NationalIdNonce\") = 12 AND octet_length(\"NationalIdTag\") = 16 AND octet_length(\"NationalIdFingerprint\") = 32");
                    table.CheckConstraint("CK_WorkerProfiles_Status", "\"Status\" IN ('Active', 'Archived')");
                    table.ForeignKey(
                        name: "FK_WorkerProfiles_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerProfiles_Persons_PersonId_FarmId",
                        columns: x => new { x.PersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                schema: "labour",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EnteredByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    LateEntryReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EntryDelayDays = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.UniqueConstraint("AK_Attendances_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_Attendances_EntryDelay", "\"EntryDelayDays\" >= 0 AND (\"EntryDelayDays\" <= 2 OR length(trim(\"LateEntryReason\")) > 0)");
                    table.CheckConstraint("CK_Attendances_FieldAllocation", "(\"Status\" = 'Present' AND \"FieldId\" IS NOT NULL) OR (\"Status\" = 'Absent' AND \"FieldId\" IS NULL)");
                    table.CheckConstraint("CK_Attendances_Status", "\"Status\" IN ('Present', 'Absent')");
                    table.ForeignKey(
                        name: "FK_Attendances_AspNetUsers_EnteredByUserId",
                        column: x => x.EnteredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_Fields_FieldId_FarmId",
                        columns: x => new { x.FieldId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Fields",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_WorkerProfiles_WorkerProfileId_TenantId_FarmId",
                        columns: x => new { x.WorkerProfileId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkerProfiles",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkerRates",
                schema: "labour",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Basis = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ActivityTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RateUsd = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerRates", x => x.Id);
                    table.CheckConstraint("CK_WorkerRates_ActivityScope", "((\"Basis\" IN ('Hectare', 'StandardLine')) = (\"ActivityTypeId\" IS NOT NULL))");
                    table.CheckConstraint("CK_WorkerRates_Basis", "\"Basis\" IN ('Daily', 'Monthly', 'Hectare', 'StandardLine')");
                    table.CheckConstraint("CK_WorkerRates_EffectiveDates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_WorkerRates_PositiveRate", "\"RateUsd\" > 0");
                    table.ForeignKey(
                        name: "FK_WorkerRates_ActivityTypes_ActivityTypeId_TenantId",
                        columns: x => new { x.ActivityTypeId, x.TenantId },
                        principalSchema: "activities",
                        principalTable: "ActivityTypes",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerRates_WorkerProfiles_WorkerProfileId_TenantId_FarmId",
                        columns: x => new { x.WorkerProfileId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkerProfiles",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkRecords",
                schema: "labour",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PayBasis = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    WorkerRateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedRateUsd = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    RateEffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    RateEffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    RateActivityTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    CalculatedAmountUsd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    EnteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EnteredByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    LateEntryReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EntryDelayDays = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CorrectsWorkRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupersededAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SupersededByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CorrectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkRecords", x => x.Id);
                    table.UniqueConstraint("AK_WorkRecords_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_WorkRecords_Basis", "\"PayBasis\" IN ('Daily', 'Monthly', 'Hectare', 'StandardLine')");
                    table.CheckConstraint("CK_WorkRecords_EntryDelay", "\"EntryDelayDays\" >= 0 AND (\"EntryDelayDays\" <= 2 OR length(trim(\"LateEntryReason\")) > 0)");
                    table.CheckConstraint("CK_WorkRecords_MonthlyDeferred", "\"PayBasis\" <> 'Monthly' OR \"CalculatedAmountUsd\" IS NULL");
                    table.CheckConstraint("CK_WorkRecords_Quantity", "((\"PayBasis\" IN ('Hectare', 'StandardLine')) AND \"Quantity\" > 0) OR ((\"PayBasis\" IN ('Daily', 'Monthly')) AND \"Quantity\" IS NULL)");
                    table.CheckConstraint("CK_WorkRecords_Status", "\"Status\" IN ('Draft', 'SupervisorVerified', 'Confirmed', 'Cancelled', 'Superseded')");
                    table.CheckConstraint("CK_WorkRecords_WholeLines", "\"PayBasis\" <> 'StandardLine' OR \"Quantity\" = trunc(\"Quantity\")");
                    table.ForeignKey(
                        name: "FK_WorkRecords_AspNetUsers_EnteredByUserId",
                        column: x => x.EnteredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkRecords_AspNetUsers_SupersededByUserId",
                        column: x => x.SupersededByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkRecords_Attendances_AttendanceId_TenantId_FarmId",
                        columns: x => new { x.AttendanceId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "Attendances",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkRecords_Fields_FieldId_FarmId",
                        columns: x => new { x.FieldId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Fields",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkRecords_WorkerProfiles_WorkerProfileId_TenantId_FarmId",
                        columns: x => new { x.WorkerProfileId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkerProfiles",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkRecords_WorkerRates_WorkerRateId",
                        column: x => x.WorkerRateId,
                        principalSchema: "labour",
                        principalTable: "WorkerRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkRecordActivities",
                schema: "labour",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkRecordActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkRecordActivities_Activities_ActivityId_TenantId_FarmId",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkRecordActivities_WorkRecords_WorkRecordId_TenantId_Farm~",
                        columns: x => new { x.WorkRecordId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkRecords",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkScopes",
                schema: "labour",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    FieldLineProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartLine = table.Column<int>(type: "integer", nullable: true),
                    EndLine = table.Column<int>(type: "integer", nullable: true),
                    SectionName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    NormalizedSectionName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SupersededAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkScopes", x => x.Id);
                    table.CheckConstraint("CK_WorkScopes_Shape", "(\"ScopeType\" = 'LineRange' AND \"FieldLineProfileId\" IS NOT NULL AND \"StartLine\" > 0 AND \"EndLine\" >= \"StartLine\" AND \"SectionName\" IS NULL AND \"NormalizedSectionName\" IS NULL) OR (\"ScopeType\" = 'NamedSection' AND \"FieldLineProfileId\" IS NULL AND \"StartLine\" IS NULL AND \"EndLine\" IS NULL AND length(trim(\"NormalizedSectionName\")) > 0)");
                    table.CheckConstraint("CK_WorkScopes_Type", "\"ScopeType\" IN ('LineRange', 'NamedSection')");
                    table.ForeignKey(
                        name: "FK_WorkScopes_Activities_ActivityId_TenantId_FarmId",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScopes_FieldLineProfiles_FieldLineProfileId",
                        column: x => x.FieldLineProfileId,
                        principalSchema: "farm",
                        principalTable: "FieldLineProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkScopes_WorkRecords_WorkRecordId_TenantId_FarmId",
                        columns: x => new { x.WorkRecordId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkRecords",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkVerifications",
                schema: "labour",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupervisorPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupervisorVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SupervisorVerificationEnteredByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ManagerConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ManagerConfirmedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkVerifications", x => x.Id);
                    table.CheckConstraint("CK_WorkVerifications_ConfirmationTime", "\"ManagerConfirmedAt\" IS NULL OR \"ManagerConfirmedAt\" >= \"SupervisorVerifiedAt\"");
                    table.ForeignKey(
                        name: "FK_WorkVerifications_AspNetUsers_ManagerConfirmedByUserId",
                        column: x => x.ManagerConfirmedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkVerifications_AspNetUsers_SupervisorVerificationEntered~",
                        column: x => x.SupervisorVerificationEnteredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkVerifications_Persons_SupervisorPersonId_FarmId",
                        columns: x => new { x.SupervisorPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkVerifications_WorkRecords_WorkRecordId_TenantId_FarmId",
                        columns: x => new { x.WorkRecordId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkRecords",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EnteredByUserId",
                schema: "labour",
                table: "Attendances",
                column: "EnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_FieldId_FarmId",
                schema: "labour",
                table: "Attendances",
                columns: new[] { "FieldId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_WorkerProfileId_TenantId_FarmId",
                schema: "labour",
                table: "Attendances",
                columns: new[] { "WorkerProfileId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "UX_Attendances_Worker_WorkDate",
                schema: "labour",
                table: "Attendances",
                columns: new[] { "WorkerProfileId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_AuthenticatedUserId",
                schema: "audit",
                table: "AuditEvents",
                column: "AuthenticatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_FarmId_TenantId",
                schema: "audit",
                table: "AuditEvents",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OperationalPersonId_FarmId",
                schema: "audit",
                table: "AuditEvents",
                columns: new[] { "OperationalPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_TenantId_FarmId_SubjectType_SubjectId_OccurredAt",
                schema: "audit",
                table: "AuditEvents",
                columns: new[] { "TenantId", "FarmId", "SubjectType", "SubjectId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerProfiles_FarmId_PersonId",
                schema: "labour",
                table: "WorkerProfiles",
                columns: new[] { "FarmId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerProfiles_FarmId_TenantId",
                schema: "labour",
                table: "WorkerProfiles",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerProfiles_PersonId_FarmId",
                schema: "labour",
                table: "WorkerProfiles",
                columns: new[] { "PersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "UX_WorkerProfiles_Farm_NationalIdFingerprint",
                schema: "labour",
                table: "WorkerProfiles",
                columns: new[] { "FarmId", "NationalIdFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerRates_ActivityTypeId_TenantId",
                schema: "labour",
                table: "WorkerRates",
                columns: new[] { "ActivityTypeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerRates_WorkerProfileId_Basis_ActivityTypeId_EffectiveF~",
                schema: "labour",
                table: "WorkerRates",
                columns: new[] { "WorkerProfileId", "Basis", "ActivityTypeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerRates_WorkerProfileId_TenantId_FarmId",
                schema: "labour",
                table: "WorkerRates",
                columns: new[] { "WorkerProfileId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecordActivities_ActivityId_TenantId_FarmId",
                schema: "labour",
                table: "WorkRecordActivities",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecordActivities_ActivityId_WorkRecordId",
                schema: "labour",
                table: "WorkRecordActivities",
                columns: new[] { "ActivityId", "WorkRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecordActivities_WorkRecordId_ActivityId",
                schema: "labour",
                table: "WorkRecordActivities",
                columns: new[] { "WorkRecordId", "ActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecordActivities_WorkRecordId_TenantId_FarmId",
                schema: "labour",
                table: "WorkRecordActivities",
                columns: new[] { "WorkRecordId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecords_AttendanceId_TenantId_FarmId",
                schema: "labour",
                table: "WorkRecords",
                columns: new[] { "AttendanceId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecords_EnteredByUserId",
                schema: "labour",
                table: "WorkRecords",
                column: "EnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecords_FarmId_WorkDate",
                schema: "labour",
                table: "WorkRecords",
                columns: new[] { "FarmId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecords_FieldId_FarmId",
                schema: "labour",
                table: "WorkRecords",
                columns: new[] { "FieldId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecords_SupersededByUserId",
                schema: "labour",
                table: "WorkRecords",
                column: "SupersededByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecords_WorkerProfileId_TenantId_FarmId",
                schema: "labour",
                table: "WorkRecords",
                columns: new[] { "WorkerProfileId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkRecords_WorkerRateId",
                schema: "labour",
                table: "WorkRecords",
                column: "WorkerRateId");

            migrationBuilder.CreateIndex(
                name: "UX_WorkRecords_Attendance_TimeBasis",
                schema: "labour",
                table: "WorkRecords",
                columns: new[] { "AttendanceId", "PayBasis" },
                unique: true,
                filter: "\"Status\" NOT IN ('Cancelled', 'Superseded') AND \"PayBasis\" IN ('Daily', 'Monthly')");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScopes_ActivityId_TenantId_FarmId",
                schema: "labour",
                table: "WorkScopes",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScopes_FieldLineProfileId",
                schema: "labour",
                table: "WorkScopes",
                column: "FieldLineProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkScopes_WorkRecordId_TenantId_FarmId",
                schema: "labour",
                table: "WorkScopes",
                columns: new[] { "WorkRecordId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkScopes_Activity_NamedSection",
                schema: "labour",
                table: "WorkScopes",
                columns: new[] { "ActivityId", "NormalizedSectionName" },
                filter: "\"ScopeType\" = 'NamedSection' AND \"SupersededAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkVerifications_ManagerConfirmedByUserId",
                schema: "labour",
                table: "WorkVerifications",
                column: "ManagerConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkVerifications_SupervisorPersonId_FarmId",
                schema: "labour",
                table: "WorkVerifications",
                columns: new[] { "SupervisorPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkVerifications_SupervisorVerificationEnteredByUserId",
                schema: "labour",
                table: "WorkVerifications",
                column: "SupervisorVerificationEnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkVerifications_WorkRecordId",
                schema: "labour",
                table: "WorkVerifications",
                column: "WorkRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkVerifications_WorkRecordId_TenantId_FarmId",
                schema: "labour",
                table: "WorkVerifications",
                columns: new[] { "WorkRecordId", "TenantId", "FarmId" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE labour."WorkerRates"
                ADD CONSTRAINT "EX_WorkerRates_NoOverlap"
                EXCLUDE USING gist
                (
                    "WorkerProfileId" WITH =,
                    "Basis" WITH =,
                    (COALESCE("ActivityTypeId", '00000000-0000-0000-0000-000000000000'::uuid)) WITH =,
                    daterange("EffectiveFrom", "EffectiveTo", '[]') WITH &&
                );
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE labour."WorkScopes"
                ADD CONSTRAINT "EX_WorkScopes_NoLineOverlap"
                EXCLUDE USING gist
                (
                    "ActivityId" WITH =,
                    int4range("StartLine", "EndLine", '[]') WITH &&
                )
                WHERE ("ScopeType" = 'LineRange' AND "SupersededAt" IS NULL)
                DEFERRABLE INITIALLY DEFERRED;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE labour."WorkScopes"
                ADD CONSTRAINT "EX_WorkScopes_NoNamedSectionDuplicate"
                EXCLUDE USING gist
                (
                    "ActivityId" WITH =,
                    "NormalizedSectionName" WITH =
                )
                WHERE ("ScopeType" = 'NamedSection' AND "SupersededAt" IS NULL)
                DEFERRABLE INITIALLY DEFERRED;
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION audit."RejectAuditEventMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    RAISE EXCEPTION 'Audit events are append-only.';
                END;
                $function$;

                CREATE TRIGGER "TR_AuditEvents_AppendOnly"
                BEFORE UPDATE OR DELETE ON audit."AuditEvents"
                FOR EACH ROW EXECUTE FUNCTION audit."RejectAuditEventMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_AuditEvents_AppendOnly" ON audit."AuditEvents";
                DROP FUNCTION IF EXISTS audit."RejectAuditEventMutation"();
                """);

            migrationBuilder.DropTable(
                name: "AuditEvents",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "WorkRecordActivities",
                schema: "labour");

            migrationBuilder.DropTable(
                name: "WorkScopes",
                schema: "labour");

            migrationBuilder.DropTable(
                name: "WorkVerifications",
                schema: "labour");

            migrationBuilder.DropTable(
                name: "WorkRecords",
                schema: "labour");

            migrationBuilder.DropTable(
                name: "Attendances",
                schema: "labour");

            migrationBuilder.DropTable(
                name: "WorkerRates",
                schema: "labour");

            migrationBuilder.DropTable(
                name: "WorkerProfiles",
                schema: "labour");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:btree_gist", ",,");
        }
    }
}
