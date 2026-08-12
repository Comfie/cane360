using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivitiesAndFieldDiary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "activities");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Fields_Id_FarmId",
                schema: "farm",
                table: "Fields",
                columns: new[] { "Id", "FarmId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Farms_Id_TenantId",
                schema: "farm",
                table: "Farms",
                columns: new[] { "Id", "TenantId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CropCycles_Id_FieldId",
                schema: "farm",
                table: "CropCycles",
                columns: new[] { "Id", "FieldId" });

            migrationBuilder.CreateTable(
                name: "ActivityTypes",
                schema: "activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SupportsPlanned = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsUnplanned = table.Column<bool>(type: "boolean", nullable: false),
                    QuantityBasis = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTypes", x => x.Id);
                    table.UniqueConstraint("AK_ActivityTypes_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_ActivityTypes_PlanningMode", "\"SupportsPlanned\" OR \"SupportsUnplanned\"");
                    table.CheckConstraint("CK_ActivityTypes_QuantityBasis", "\"QuantityBasis\" IN ('None', 'Hectares', 'StandardLines')");
                    table.CheckConstraint("CK_ActivityTypes_Status", "\"Status\" IN ('Active', 'Archived')");
                    table.ForeignKey(
                        name: "FK_ActivityTypes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldLineProfiles",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardLineLengthMetres = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EstimatedLineCount = table.Column<int>(type: "integer", nullable: false),
                    NumberingScheme = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
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
                    table.PrimaryKey("PK_FieldLineProfiles", x => x.Id);
                    table.UniqueConstraint("AK_FieldLineProfiles_Id_FieldId", x => new { x.Id, x.FieldId });
                    table.CheckConstraint("CK_FieldLineProfiles_EffectiveDates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_FieldLineProfiles_PositiveValues", "\"StandardLineLengthMetres\" > 0 AND \"EstimatedLineCount\" > 0");
                    table.ForeignKey(
                        name: "FK_FieldLineProfiles_Fields_FieldId",
                        column: x => x.FieldId,
                        principalSchema: "farm",
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ActiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ActiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                    table.UniqueConstraint("AK_Persons_Id_FarmId", x => new { x.Id, x.FarmId });
                    table.CheckConstraint("CK_Persons_ActiveDates", "\"ActiveTo\" IS NULL OR \"ActiveTo\" >= \"ActiveFrom\"");
                    table.CheckConstraint("CK_Persons_Status", "\"Status\" IN ('Active', 'Archived')");
                    table.ForeignKey(
                        name: "FK_Persons_Farms_FarmId",
                        column: x => x.FarmId,
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                schema: "activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityTypeCode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ActivityTypeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SupervisorPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityBasis = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ActualAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActualQuantity = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    FieldLineProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    LineContextUnavailable = table.Column<bool>(type: "boolean", nullable: false),
                    ActualEnteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActualEnteredByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LateEntryReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EntryDelayDays = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.UniqueConstraint("AK_Activities_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_Activities_EntryDelayDays", "\"EntryDelayDays\" >= 0");
                    table.CheckConstraint("CK_Activities_EntryTime", "\"ActualEnteredAt\" IS NULL OR \"ActualAt\" IS NULL OR \"ActualEnteredAt\" >= \"ActualAt\"");
                    table.CheckConstraint("CK_Activities_Kind", "\"Kind\" IN ('Planned', 'Unplanned')");
                    table.CheckConstraint("CK_Activities_LateReason", "\"EntryDelayDays\" <= 2 OR length(trim(\"LateEntryReason\")) > 0");
                    table.CheckConstraint("CK_Activities_PlannedDate", "\"Kind\" <> 'Planned' OR \"PlannedDate\" IS NOT NULL");
                    table.CheckConstraint("CK_Activities_Quantity", "(\"QuantityBasis\" = 'None' AND \"ActualQuantity\" IS NULL) OR (\"QuantityBasis\" <> 'None' AND (\"ActualQuantity\" IS NULL OR \"ActualQuantity\" > 0))");
                    table.CheckConstraint("CK_Activities_QuantityBasis", "\"QuantityBasis\" IN ('None', 'Hectares', 'StandardLines')");
                    table.CheckConstraint("CK_Activities_RequiredActual", "\"Status\" NOT IN ('AwaitingVerification', 'ManagerConfirmation', 'Completed', 'Closed') OR (\"ActualAt\" IS NOT NULL AND (\"QuantityBasis\" = 'None' OR \"ActualQuantity\" IS NOT NULL))");
                    table.CheckConstraint("CK_Activities_Status", "\"Status\" IN ('Draft', 'Planned', 'InProgress', 'AwaitingVerification', 'ManagerConfirmation', 'Completed', 'Closed', 'Cancelled')");
                    table.CheckConstraint("CK_Activities_WholeLines", "\"QuantityBasis\" <> 'StandardLines' OR \"ActualQuantity\" IS NULL OR \"ActualQuantity\" = trunc(\"ActualQuantity\")");
                    table.ForeignKey(
                        name: "FK_Activities_ActivityTypes_ActivityTypeId_TenantId",
                        columns: x => new { x.ActivityTypeId, x.TenantId },
                        principalSchema: "activities",
                        principalTable: "ActivityTypes",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activities_AspNetUsers_ActualEnteredByUserId",
                        column: x => x.ActualEnteredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activities_CropCycles_CropCycleId_FieldId",
                        columns: x => new { x.CropCycleId, x.FieldId },
                        principalSchema: "farm",
                        principalTable: "CropCycles",
                        principalColumns: new[] { "Id", "FieldId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activities_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activities_FieldLineProfiles_FieldLineProfileId_FieldId",
                        columns: x => new { x.FieldLineProfileId, x.FieldId },
                        principalSchema: "farm",
                        principalTable: "FieldLineProfiles",
                        principalColumns: new[] { "Id", "FieldId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activities_Fields_FieldId_FarmId",
                        columns: x => new { x.FieldId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Fields",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activities_Persons_SupervisorPersonId_FarmId",
                        columns: x => new { x.SupervisorPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonRoleAssignments",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonRoleAssignments", x => x.Id);
                    table.CheckConstraint("CK_PersonRoleAssignments_EffectiveDates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_PersonRoleAssignments_PrimaryRole", "NOT \"IsPrimary\" OR \"Role\" = 'FarmManager'");
                    table.CheckConstraint("CK_PersonRoleAssignments_Role", "\"Role\" IN ('FarmManager', 'Supervisor', 'Storekeeper')");
                    table.ForeignKey(
                        name: "FK_PersonRoleAssignments_Persons_PersonId_FarmId",
                        columns: x => new { x.PersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityStatusChanges",
                schema: "activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    OperationalPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityStatusChanges", x => x.Id);
                    table.CheckConstraint("CK_ActivityStatusChanges_CancellationReason", "\"ToStatus\" <> 'Cancelled' OR length(trim(\"Reason\")) > 0");
                    table.CheckConstraint("CK_ActivityStatusChanges_Status", "\"FromStatus\" IN ('Draft', 'Planned', 'InProgress', 'AwaitingVerification', 'ManagerConfirmation', 'Completed') AND \"ToStatus\" IN ('Planned', 'InProgress', 'AwaitingVerification', 'ManagerConfirmation', 'Completed', 'Closed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_ActivityStatusChanges_Activities_ActivityId_TenantId_FarmId",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityStatusChanges_AspNetUsers_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityStatusChanges_Persons_OperationalPersonId_FarmId",
                        columns: x => new { x.OperationalPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceLinks",
                schema: "activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    SourceSheetReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CapturedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceLinks", x => x.Id);
                    table.CheckConstraint("CK_EvidenceLinks_Role", "\"Role\" = 'SourceSheet'");
                    table.ForeignKey(
                        name: "FK_EvidenceLinks_Activities_ActivityId_TenantId_FarmId",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceLinks_AspNetUsers_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ActivityTypeId_TenantId",
                schema: "activities",
                table: "Activities",
                columns: new[] { "ActivityTypeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ActualEnteredByUserId",
                schema: "activities",
                table: "Activities",
                column: "ActualEnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CropCycleId_FieldId",
                schema: "activities",
                table: "Activities",
                columns: new[] { "CropCycleId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CropCycleId_Status",
                schema: "activities",
                table: "Activities",
                columns: new[] { "CropCycleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_FarmId_TenantId",
                schema: "activities",
                table: "Activities",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_FieldId_FarmId",
                schema: "activities",
                table: "Activities",
                columns: new[] { "FieldId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_FieldLineProfileId_FieldId",
                schema: "activities",
                table: "Activities",
                columns: new[] { "FieldLineProfileId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_SupervisorPersonId_FarmId",
                schema: "activities",
                table: "Activities",
                columns: new[] { "SupervisorPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TenantId_PlannedDate",
                schema: "activities",
                table: "Activities",
                columns: new[] { "TenantId", "PlannedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityStatusChanges_ActivityId_RecordedAt",
                schema: "activities",
                table: "ActivityStatusChanges",
                columns: new[] { "ActivityId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityStatusChanges_ActivityId_TenantId_FarmId",
                schema: "activities",
                table: "ActivityStatusChanges",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityStatusChanges_OperationalPersonId_FarmId",
                schema: "activities",
                table: "ActivityStatusChanges",
                columns: new[] { "OperationalPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityStatusChanges_RecordedBy",
                schema: "activities",
                table: "ActivityStatusChanges",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTypes_TenantId_Code",
                schema: "activities",
                table: "ActivityTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceLinks_ActivityId_RecordedAt",
                schema: "activities",
                table: "EvidenceLinks",
                columns: new[] { "ActivityId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceLinks_ActivityId_TenantId_FarmId",
                schema: "activities",
                table: "EvidenceLinks",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceLinks_RecordedBy",
                schema: "activities",
                table: "EvidenceLinks",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FieldLineProfiles_FieldId",
                schema: "farm",
                table: "FieldLineProfiles",
                column: "FieldId",
                unique: true,
                filter: "\"EffectiveTo\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoleAssignments_FarmId",
                schema: "farm",
                table: "PersonRoleAssignments",
                column: "FarmId",
                unique: true,
                filter: "\"Role\" = 'FarmManager' AND \"IsPrimary\" AND \"EffectiveTo\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoleAssignments_PersonId_FarmId",
                schema: "farm",
                table: "PersonRoleAssignments",
                columns: new[] { "PersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoleAssignments_PersonId_Role",
                schema: "farm",
                table: "PersonRoleAssignments",
                columns: new[] { "PersonId", "Role" },
                unique: true,
                filter: "\"EffectiveTo\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_FarmId_Status",
                schema: "farm",
                table: "Persons",
                columns: new[] { "FarmId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityStatusChanges",
                schema: "activities");

            migrationBuilder.DropTable(
                name: "EvidenceLinks",
                schema: "activities");

            migrationBuilder.DropTable(
                name: "PersonRoleAssignments",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "Activities",
                schema: "activities");

            migrationBuilder.DropTable(
                name: "ActivityTypes",
                schema: "activities");

            migrationBuilder.DropTable(
                name: "FieldLineProfiles",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "Persons",
                schema: "farm");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Fields_Id_FarmId",
                schema: "farm",
                table: "Fields");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Farms_Id_TenantId",
                schema: "farm",
                table: "Farms");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CropCycles_Id_FieldId",
                schema: "farm",
                table: "CropCycles");
        }
    }
}
