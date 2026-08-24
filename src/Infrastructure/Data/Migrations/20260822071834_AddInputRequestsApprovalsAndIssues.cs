using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInputRequestsApprovalsAndIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Reversal",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryAuditEventLinks_OneSubject",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecisions_StockReceiptId_SubjectVersion",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalDecisions_GrowerOpening",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.AddColumn<Guid>(
                name: "FarmId",
                schema: "identity",
                table: "TenantMemberships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PersonId",
                schema: "identity",
                table: "TenantMemberships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StockReceiptLineId",
                schema: "inventory",
                table: "StockMovements",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "StockIssueLineId",
                schema: "inventory",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InputRequestId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryApplicationRuleId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerInvitationId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StockIssueId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OriginalStockReceiptId",
                schema: "inventory",
                table: "CorrectionRecords",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalStockIssueId",
                schema: "inventory",
                table: "CorrectionRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StockReceiptId",
                schema: "inventory",
                table: "ApprovalDecisions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "InputRequestId",
                schema: "inventory",
                table: "ApprovalDecisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InputRequests",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubmissionIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputRequests", x => x.Id);
                    table.UniqueConstraint("AK_InputRequests_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.ForeignKey(
                        name: "FK_InputRequests_Activities_ActivityId_TenantId_FarmId",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputRequests_CropCycles_CropCycleId_FieldId",
                        columns: x => new { x.CropCycleId, x.FieldId },
                        principalSchema: "farm",
                        principalTable: "CropCycles",
                        principalColumns: new[] { "Id", "FieldId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputRequests_Fields_FieldId_FarmId",
                        columns: x => new { x.FieldId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Fields",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryApplicationRules",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CoverageBasis = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RatePerCoverageUnit = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    LowerTolerancePercent = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    UpperTolerancePercent = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryApplicationRules", x => x.Id);
                    table.UniqueConstraint("AK_InventoryApplicationRules_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_InventoryApplicationRules_Dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_InventoryApplicationRules_Rate", "\"RatePerCoverageUnit\" > 0");
                    table.CheckConstraint("CK_InventoryApplicationRules_Tolerances", "\"LowerTolerancePercent\" >= 0 AND \"UpperTolerancePercent\" >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryApplicationRules_ActivityTypes_ActivityTypeId_Tena~",
                        columns: x => new { x.ActivityTypeId, x.TenantId },
                        principalSchema: "activities",
                        principalTable: "ActivityTypes",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryApplicationRules_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryApplicationRules_InventoryItems_InventoryItemId_Te~",
                        columns: x => new { x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryApplicationRules_UnitOfMeasures_UnitOfMeasureId_Te~",
                        columns: x => new { x.UnitOfMeasureId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "UnitOfMeasures",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ManagerInvitations",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RedeemedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagerInvitations", x => x.Id);
                    table.UniqueConstraint("AK_ManagerInvitations_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.ForeignKey(
                        name: "FK_ManagerInvitations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerInvitations_AspNetUsers_RedeemedByUserId",
                        column: x => x.RedeemedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerInvitations_AspNetUsers_RevokedByUserId",
                        column: x => x.RevokedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerInvitations_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerInvitations_Persons_PersonId_FarmId",
                        columns: x => new { x.PersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagerInvitations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockIssues",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    InputRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IssuerPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    LateEntryReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EntryDelayDays = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PostingIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CorrectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CorrectionRequestedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReversedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReversalIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockIssues", x => x.Id);
                    table.UniqueConstraint("AK_StockIssues_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_StockIssues_EntryDelay", "\"EntryDelayDays\" >= 0");
                    table.CheckConstraint("CK_StockIssues_LateReason", "\"EntryDelayDays\" <= 2 OR length(trim(\"LateEntryReason\")) > 0");
                    table.ForeignKey(
                        name: "FK_StockIssues_AspNetUsers_CorrectionRequestedByUserId",
                        column: x => x.CorrectionRequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockIssues_AspNetUsers_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockIssues_InputRequests_InputRequestId_TenantId_FarmId",
                        columns: x => new { x.InputRequestId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InputRequests",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockIssues_Persons_IssuerPersonId_FarmId",
                        columns: x => new { x.IssuerPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockIssues_Persons_RecipientPersonId_FarmId",
                        columns: x => new { x.RecipientPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockIssues_Stores_StoreId_FarmId",
                        columns: x => new { x.StoreId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Stores",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InputRequestLines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    InputRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InventoryApplicationRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleVersionSnapshot = table.Column<long>(type: "bigint", nullable: false),
                    RuleEffectiveFromSnapshot = table.Column<DateOnly>(type: "date", nullable: false),
                    RuleEffectiveToSnapshot = table.Column<DateOnly>(type: "date", nullable: true),
                    CoverageBasisSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PlannedCoverage = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    PlannedRate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    LowerTolerancePercent = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    UpperTolerancePercent = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    ApprovalRequirement = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AvailableQuantitySnapshot = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    EstimatedUnitCostUsdSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    EstimatedValueUsdSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputRequestLines", x => x.Id);
                    table.UniqueConstraint("AK_InputRequestLines_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_InputRequestLines_Estimate", "(\"EstimatedUnitCostUsdSnapshot\" IS NULL) = (\"EstimatedValueUsdSnapshot\" IS NULL)");
                    table.CheckConstraint("CK_InputRequestLines_Quantities", "\"PlannedCoverage\" > 0 AND \"PlannedRate\" > 0 AND \"PlannedQuantity\" > 0 AND \"RequestedQuantity\" > 0");
                    table.CheckConstraint("CK_InputRequestLines_Tolerances", "\"LowerTolerancePercent\" >= 0 AND \"UpperTolerancePercent\" >= 0");
                    table.ForeignKey(
                        name: "FK_InputRequestLines_InputRequests_InputRequestId_TenantId_Far~",
                        columns: x => new { x.InputRequestId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InputRequests",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputRequestLines_InventoryApplicationRules_InventoryApplic~",
                        columns: x => new { x.InventoryApplicationRuleId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryApplicationRules",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputRequestLines_InventoryItems_InventoryItemId_TenantId_F~",
                        columns: x => new { x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputRequestLines_UnitOfMeasures_UnitOfMeasureId_TenantId",
                        columns: x => new { x.UnitOfMeasureId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "UnitOfMeasures",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockIssueLines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockIssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    InputRequestLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    StockPositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    IssueUnitCostUsd = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    IssueValueUsd = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockIssueLines", x => x.Id);
                    table.UniqueConstraint("AK_StockIssueLines_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_StockIssueLines_Cost", "(\"IssueUnitCostUsd\" IS NULL AND \"IssueValueUsd\" IS NULL) OR (\"IssueUnitCostUsd\" >= 0 AND \"IssueValueUsd\" >= 0)");
                    table.CheckConstraint("CK_StockIssueLines_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_StockIssueLines_InputRequestLines_InputRequestLineId_Tenant~",
                        columns: x => new { x.InputRequestLineId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InputRequestLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockIssueLines_InventoryItems_InventoryItemId_TenantId_Far~",
                        columns: x => new { x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockIssueLines_InventoryLots_InventoryLotId_TenantId_FarmId",
                        columns: x => new { x.InventoryLotId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryLots",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockIssueLines_StockIssues_StockIssueId_TenantId_FarmId",
                        columns: x => new { x.StockIssueId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockIssues",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockIssueLines_StockPositions_StockPositionId_TenantId_Far~",
                        columns: x => new { x.StockPositionId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockPositions",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockIssueLines_UnitOfMeasures_UnitOfMeasureId_TenantId",
                        columns: x => new { x.UnitOfMeasureId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "UnitOfMeasures",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_FarmId_TenantId",
                schema: "identity",
                table: "TenantMemberships",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_PersonId_FarmId",
                schema: "identity",
                table: "TenantMemberships",
                columns: new[] { "PersonId", "FarmId" },
                unique: true,
                filter: "\"PersonId\" IS NOT NULL AND \"Status\" = 'Active'");

            migrationBuilder.Sql(
                """
                ALTER TABLE identity."TenantMemberships"
                ADD CONSTRAINT "CK_TenantMemberships_RolePerson"
                CHECK (
                    ("SecurityRole" = 'Grower' AND "FarmId" IS NULL AND "PersonId" IS NULL)
                    OR
                    ("SecurityRole" = 'FarmManager' AND "FarmId" IS NOT NULL AND "PersonId" IS NOT NULL)
                ) NOT VALID;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockIssueLineId",
                schema: "inventory",
                table: "StockMovements",
                column: "StockIssueLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockIssueLineId_TenantId_FarmId",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "StockIssueLineId", "TenantId", "FarmId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_OneSource",
                schema: "inventory",
                table: "StockMovements",
                sql: "num_nonnulls(\"StockReceiptLineId\", \"StockIssueLineId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Reversal",
                schema: "inventory",
                table: "StockMovements",
                sql: "(\"MovementType\" IN ('ReceiptReversal', 'IssueReversal')) = (\"ReversalOfStockMovementId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_InputRequestId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InputRequestId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_InventoryApplicationRuleId_TenantI~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InventoryApplicationRuleId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_ManagerInvitationId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "ManagerInvitationId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_StockIssueId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "StockIssueId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_TenantId_FarmId_InputRequestId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "TenantId", "FarmId", "InputRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_TenantId_FarmId_StockIssueId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "TenantId", "FarmId", "StockIssueId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryAuditEventLinks_OneSubject",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                sql: "num_nonnulls(\"UnitOfMeasureId\", \"InventoryItemId\", \"SupplierId\", \"InventoryLotId\", \"StockReceiptId\", \"InventoryApplicationRuleId\", \"InputRequestId\", \"StockIssueId\", \"ManagerInvitationId\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRecords_OriginalStockIssueId",
                schema: "inventory",
                table: "CorrectionRecords",
                column: "OriginalStockIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRecords_OriginalStockIssueId_TenantId_FarmId",
                schema: "inventory",
                table: "CorrectionRecords",
                columns: new[] { "OriginalStockIssueId", "TenantId", "FarmId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CorrectionRecords_OneSource",
                schema: "inventory",
                table: "CorrectionRecords",
                sql: "num_nonnulls(\"OriginalStockReceiptId\", \"OriginalStockIssueId\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_InputRequestId_SubjectVersion",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "InputRequestId", "SubjectVersion" },
                unique: true,
                filter: "\"InputRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_InputRequestId_TenantId_FarmId",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "InputRequestId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_StockReceiptId_SubjectVersion",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "StockReceiptId", "SubjectVersion" },
                unique: true,
                filter: "\"StockReceiptId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalDecisions_OneSubject",
                schema: "inventory",
                table: "ApprovalDecisions",
                sql: "num_nonnulls(\"StockReceiptId\", \"InputRequestId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalDecisions_Role",
                schema: "inventory",
                table: "ApprovalDecisions",
                sql: "(\"StockReceiptId\" IS NULL OR \"ApproverRole\" = 'Grower') AND (\"InputRequestId\" IS NULL OR \"ApproverRole\" IN ('Grower', 'FarmManager'))");

            migrationBuilder.CreateIndex(
                name: "IX_InputRequestLines_InputRequestId_InventoryItemId",
                schema: "inventory",
                table: "InputRequestLines",
                columns: new[] { "InputRequestId", "InventoryItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InputRequestLines_InputRequestId_LineNumber",
                schema: "inventory",
                table: "InputRequestLines",
                columns: new[] { "InputRequestId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InputRequestLines_InputRequestId_TenantId_FarmId",
                schema: "inventory",
                table: "InputRequestLines",
                columns: new[] { "InputRequestId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputRequestLines_InventoryApplicationRuleId_TenantId_FarmId",
                schema: "inventory",
                table: "InputRequestLines",
                columns: new[] { "InventoryApplicationRuleId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputRequestLines_InventoryItemId_TenantId_FarmId",
                schema: "inventory",
                table: "InputRequestLines",
                columns: new[] { "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputRequestLines_UnitOfMeasureId_TenantId",
                schema: "inventory",
                table: "InputRequestLines",
                columns: new[] { "UnitOfMeasureId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputRequests_ActivityId_TenantId_FarmId",
                schema: "inventory",
                table: "InputRequests",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputRequests_CropCycleId_FieldId",
                schema: "inventory",
                table: "InputRequests",
                columns: new[] { "CropCycleId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputRequests_FieldId_FarmId",
                schema: "inventory",
                table: "InputRequests",
                columns: new[] { "FieldId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputRequests_RequestedByUserId",
                schema: "inventory",
                table: "InputRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InputRequests_SubmissionIdempotencyKey",
                schema: "inventory",
                table: "InputRequests",
                column: "SubmissionIdempotencyKey",
                unique: true,
                filter: "\"SubmissionIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InputRequests_TenantId_FarmId_ActivityId_Status",
                schema: "inventory",
                table: "InputRequests",
                columns: new[] { "TenantId", "FarmId", "ActivityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryApplicationRules_ActivityTypeId_TenantId",
                schema: "inventory",
                table: "InventoryApplicationRules",
                columns: new[] { "ActivityTypeId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryApplicationRules_FarmId_TenantId",
                schema: "inventory",
                table: "InventoryApplicationRules",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryApplicationRules_InventoryItemId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryApplicationRules",
                columns: new[] { "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryApplicationRules_TenantId_FarmId_InventoryItemId_A~",
                schema: "inventory",
                table: "InventoryApplicationRules",
                columns: new[] { "TenantId", "FarmId", "InventoryItemId", "ActivityTypeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryApplicationRules_UnitOfMeasureId_TenantId",
                schema: "inventory",
                table: "InventoryApplicationRules",
                columns: new[] { "UnitOfMeasureId", "TenantId" });

            migrationBuilder.Sql(
                """
                ALTER TABLE inventory."InventoryApplicationRules"
                ADD CONSTRAINT "EX_InventoryApplicationRules_NoOverlap"
                EXCLUDE USING gist
                (
                    "TenantId" WITH =,
                    "FarmId" WITH =,
                    "InventoryItemId" WITH =,
                    "ActivityTypeId" WITH =,
                    daterange("EffectiveFrom", COALESCE("EffectiveTo", 'infinity'::date), '[]') WITH &&
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ManagerInvitations_CreatedByUserId",
                schema: "identity",
                table: "ManagerInvitations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerInvitations_FarmId_TenantId",
                schema: "identity",
                table: "ManagerInvitations",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerInvitations_PersonId_FarmId",
                schema: "identity",
                table: "ManagerInvitations",
                columns: new[] { "PersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagerInvitations_RedeemedByUserId",
                schema: "identity",
                table: "ManagerInvitations",
                column: "RedeemedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerInvitations_RevokedByUserId",
                schema: "identity",
                table: "ManagerInvitations",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerInvitations_TenantId_PersonId",
                schema: "identity",
                table: "ManagerInvitations",
                columns: new[] { "TenantId", "PersonId" },
                unique: true,
                filter: "\"RevokedAt\" IS NULL AND \"RedeemedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ManagerInvitations_TokenHash",
                schema: "identity",
                table: "ManagerInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockIssueLines_InputRequestLineId_TenantId_FarmId",
                schema: "inventory",
                table: "StockIssueLines",
                columns: new[] { "InputRequestLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockIssueLines_InventoryItemId_TenantId_FarmId",
                schema: "inventory",
                table: "StockIssueLines",
                columns: new[] { "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockIssueLines_InventoryLotId_TenantId_FarmId",
                schema: "inventory",
                table: "StockIssueLines",
                columns: new[] { "InventoryLotId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockIssueLines_StockIssueId_InputRequestLineId_InventoryLo~",
                schema: "inventory",
                table: "StockIssueLines",
                columns: new[] { "StockIssueId", "InputRequestLineId", "InventoryLotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockIssueLines_StockIssueId_LineNumber",
                schema: "inventory",
                table: "StockIssueLines",
                columns: new[] { "StockIssueId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockIssueLines_StockIssueId_TenantId_FarmId",
                schema: "inventory",
                table: "StockIssueLines",
                columns: new[] { "StockIssueId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockIssueLines_StockPositionId_TenantId_FarmId",
                schema: "inventory",
                table: "StockIssueLines",
                columns: new[] { "StockPositionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockIssueLines_UnitOfMeasureId_TenantId",
                schema: "inventory",
                table: "StockIssueLines",
                columns: new[] { "UnitOfMeasureId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_CorrectionRequestedByUserId",
                schema: "inventory",
                table: "StockIssues",
                column: "CorrectionRequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_InputRequestId_TenantId_FarmId",
                schema: "inventory",
                table: "StockIssues",
                columns: new[] { "InputRequestId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_IssuerPersonId_FarmId",
                schema: "inventory",
                table: "StockIssues",
                columns: new[] { "IssuerPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_PostedByUserId",
                schema: "inventory",
                table: "StockIssues",
                column: "PostedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_PostingIdempotencyKey",
                schema: "inventory",
                table: "StockIssues",
                column: "PostingIdempotencyKey",
                unique: true,
                filter: "\"PostingIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_RecipientPersonId_FarmId",
                schema: "inventory",
                table: "StockIssues",
                columns: new[] { "RecipientPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_ReversalIdempotencyKey",
                schema: "inventory",
                table: "StockIssues",
                column: "ReversalIdempotencyKey",
                unique: true,
                filter: "\"ReversalIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_StoreId_FarmId",
                schema: "inventory",
                table: "StockIssues",
                columns: new[] { "StoreId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_TenantId_FarmId_InputRequestId_Status",
                schema: "inventory",
                table: "StockIssues",
                columns: new[] { "TenantId", "FarmId", "InputRequestId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalDecisions_InputRequests_InputRequestId_TenantId_Far~",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "InputRequestId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "InputRequests",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CorrectionRecords_StockIssues_OriginalStockIssueId_TenantId~",
                schema: "inventory",
                table: "CorrectionRecords",
                columns: new[] { "OriginalStockIssueId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "StockIssues",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_InputRequests_InputRequestId_Tenan~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InputRequestId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "InputRequests",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_InventoryApplicationRules_Inventor~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InventoryApplicationRuleId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "InventoryApplicationRules",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_ManagerInvitations_ManagerInvitati~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "ManagerInvitationId", "TenantId", "FarmId" },
                principalSchema: "identity",
                principalTable: "ManagerInvitations",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_StockIssues_StockIssueId_TenantId_~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "StockIssueId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "StockIssues",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StockIssueLines_StockIssueLineId_TenantId_Fa~",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "StockIssueLineId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "StockIssueLines",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMemberships_Farms_FarmId_TenantId",
                schema: "identity",
                table: "TenantMemberships",
                columns: new[] { "FarmId", "TenantId" },
                principalSchema: "farm",
                principalTable: "Farms",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMemberships_Persons_PersonId_FarmId",
                schema: "identity",
                table: "TenantMemberships",
                columns: new[] { "PersonId", "FarmId" },
                principalSchema: "farm",
                principalTable: "Persons",
                principalColumns: new[] { "Id", "FarmId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalDecisions_InputRequests_InputRequestId_TenantId_Far~",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_CorrectionRecords_StockIssues_OriginalStockIssueId_TenantId~",
                schema: "inventory",
                table: "CorrectionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_InputRequests_InputRequestId_Tenan~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_InventoryApplicationRules_Inventor~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_ManagerInvitations_ManagerInvitati~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_StockIssues_StockIssueId_TenantId_~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StockIssueLines_StockIssueLineId_TenantId_Fa~",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantMemberships_Farms_FarmId_TenantId",
                schema: "identity",
                table: "TenantMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantMemberships_Persons_PersonId_FarmId",
                schema: "identity",
                table: "TenantMemberships");

            migrationBuilder.DropTable(
                name: "ManagerInvitations",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "StockIssueLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InputRequestLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockIssues",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryApplicationRules",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InputRequests",
                schema: "inventory");

            migrationBuilder.DropIndex(
                name: "IX_TenantMemberships_FarmId_TenantId",
                schema: "identity",
                table: "TenantMemberships");

            migrationBuilder.DropIndex(
                name: "IX_TenantMemberships_PersonId_FarmId",
                schema: "identity",
                table: "TenantMemberships");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TenantMemberships_RolePerson",
                schema: "identity",
                table: "TenantMemberships");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_StockIssueLineId",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_StockIssueLineId_TenantId_FarmId",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_OneSource",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Reversal",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_InputRequestId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_InventoryApplicationRuleId_TenantI~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_ManagerInvitationId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_StockIssueId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_TenantId_FarmId_InputRequestId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_TenantId_FarmId_StockIssueId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryAuditEventLinks_OneSubject",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_CorrectionRecords_OriginalStockIssueId",
                schema: "inventory",
                table: "CorrectionRecords");

            migrationBuilder.DropIndex(
                name: "IX_CorrectionRecords_OriginalStockIssueId_TenantId_FarmId",
                schema: "inventory",
                table: "CorrectionRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CorrectionRecords_OneSource",
                schema: "inventory",
                table: "CorrectionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecisions_InputRequestId_SubjectVersion",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecisions_InputRequestId_TenantId_FarmId",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecisions_StockReceiptId_SubjectVersion",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalDecisions_OneSubject",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalDecisions_Role",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropColumn(
                name: "FarmId",
                schema: "identity",
                table: "TenantMemberships");

            migrationBuilder.DropColumn(
                name: "PersonId",
                schema: "identity",
                table: "TenantMemberships");

            migrationBuilder.DropColumn(
                name: "StockIssueLineId",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "InputRequestId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "InventoryApplicationRuleId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "ManagerInvitationId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "StockIssueId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "OriginalStockIssueId",
                schema: "inventory",
                table: "CorrectionRecords");

            migrationBuilder.DropColumn(
                name: "InputRequestId",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.AlterColumn<Guid>(
                name: "StockReceiptLineId",
                schema: "inventory",
                table: "StockMovements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OriginalStockReceiptId",
                schema: "inventory",
                table: "CorrectionRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StockReceiptId",
                schema: "inventory",
                table: "ApprovalDecisions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Reversal",
                schema: "inventory",
                table: "StockMovements",
                sql: "(\"MovementType\" = 'ReceiptReversal') = (\"ReversalOfStockMovementId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryAuditEventLinks_OneSubject",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                sql: "num_nonnulls(\"UnitOfMeasureId\", \"InventoryItemId\", \"SupplierId\", \"InventoryLotId\", \"StockReceiptId\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_StockReceiptId_SubjectVersion",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "StockReceiptId", "SubjectVersion" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalDecisions_GrowerOpening",
                schema: "inventory",
                table: "ApprovalDecisions",
                sql: "\"ApproverRole\" = 'Grower'");
        }
    }
}
