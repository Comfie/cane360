using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldApplicationAccountability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_OneSource",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Reversal",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryAuditEventLinks_OneSubject",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalDecisions_OneSubject",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalDecisions_Role",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.AddColumn<Guid>(
                name: "StockReturnLineId",
                schema: "inventory",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ControlExceptionId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrectionRecordId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FieldAccountabilityCorrectionId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FieldReceiptId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InputApplicationId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryLossId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperationalCostPostingId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StockReturnId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FieldAccountabilityCorrectionId",
                schema: "inventory",
                table: "ApprovalDecisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryLossId",
                schema: "inventory",
                table: "ApprovalDecisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CorrectionRecords_Id_TenantId_FarmId",
                schema: "inventory",
                table: "CorrectionRecords",
                columns: new[] { "Id", "TenantId", "FarmId" });

            migrationBuilder.CreateTable(
                name: "ControlExceptions",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssuedQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    AppliedQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    ApprovedLossQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    UnaccountedQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlExceptions", x => x.Id);
                    table.UniqueConstraint("AK_ControlExceptions_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_ControlExceptions_Nonnegative", "\"UnaccountedQuantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_ControlExceptions_Activities_ActivityId_TenantId_FarmId",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ControlExceptions_StockIssueLines_StockIssueLineId_TenantId~",
                        columns: x => new { x.StockIssueLineId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockIssueLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldReceipts",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockIssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EnteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EnteredByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    LateEntryReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EntryDelayDays = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldReceipts", x => x.Id);
                    table.UniqueConstraint("AK_FieldReceipts_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_FieldReceipts_Delay", "\"EntryDelayDays\" >= 0");
                    table.CheckConstraint("CK_FieldReceipts_LateReason", "\"EntryDelayDays\" <= 2 OR length(trim(\"LateEntryReason\")) > 0");
                    table.ForeignKey(
                        name: "FK_FieldReceipts_Activities_ActivityId_TenantId_FarmId",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldReceipts_AspNetUsers_EnteredByUserId",
                        column: x => x.EnteredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldReceipts_CropCycles_CropCycleId_FieldId",
                        columns: x => new { x.CropCycleId, x.FieldId },
                        principalSchema: "farm",
                        principalTable: "CropCycles",
                        principalColumns: new[] { "Id", "FieldId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldReceipts_Fields_FieldId_FarmId",
                        columns: x => new { x.FieldId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Fields",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldReceipts_Persons_RecipientPersonId_FarmId",
                        columns: x => new { x.RecipientPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldReceipts_StockIssues_StockIssueId_TenantId_FarmId",
                        columns: x => new { x.StockIssueId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockIssues",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InputApplications",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CoverageBasis = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    VerifiedCoverage = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    EnteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EnteredByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SupervisorPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupervisorAttestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SupervisorAttestationEnteredByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    SupervisorAttestationNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ManagerConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ManagerConfirmedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ConfirmationIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LateConfirmationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsLateConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputApplications", x => x.Id);
                    table.UniqueConstraint("AK_InputApplications_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_InputApplications_Coverage", "\"VerifiedCoverage\" > 0");
                    table.ForeignKey(
                        name: "FK_InputApplications_Activities_ActivityId_TenantId_FarmId",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputApplications_AspNetUsers_EnteredByUserId",
                        column: x => x.EnteredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputApplications_AspNetUsers_ManagerConfirmedByUserId",
                        column: x => x.ManagerConfirmedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputApplications_AspNetUsers_SupervisorAttestationEnteredB~",
                        column: x => x.SupervisorAttestationEnteredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputApplications_Persons_SupervisorPersonId_FarmId",
                        columns: x => new { x.SupervisorPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLosses",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssueUnitCostUsdSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    LossType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLosses", x => x.Id);
                    table.UniqueConstraint("AK_InventoryLosses_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_InventoryLosses_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_InventoryLosses_Activities_ActivityId_TenantId_FarmId",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLosses_AspNetUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLosses_StockIssueLines_StockIssueLineId_TenantId_F~",
                        columns: x => new { x.StockIssueLineId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockIssueLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockReturns",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SenderPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PostingIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ReversedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReversalIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReturns", x => x.Id);
                    table.UniqueConstraint("AK_StockReturns_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.ForeignKey(
                        name: "FK_StockReturns_Activities_ActivityId_TenantId_FarmId",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReturns_AspNetUsers_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReturns_Persons_ReceiverPersonId_FarmId",
                        columns: x => new { x.ReceiverPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReturns_Persons_SenderPersonId_FarmId",
                        columns: x => new { x.SenderPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReturns_Stores_StoreId_FarmId",
                        columns: x => new { x.StoreId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Stores",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldReceiptLines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssueUnitCostUsdSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldReceiptLines", x => x.Id);
                    table.UniqueConstraint("AK_FieldReceiptLines_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_FieldReceiptLines_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_FieldReceiptLines_FieldReceipts_FieldReceiptId_TenantId_Far~",
                        columns: x => new { x.FieldReceiptId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "FieldReceipts",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldReceiptLines_InventoryItems_InventoryItemId_TenantId_F~",
                        columns: x => new { x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldReceiptLines_InventoryLots_InventoryLotId_TenantId_Far~",
                        columns: x => new { x.InventoryLotId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryLots",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldReceiptLines_StockIssueLines_StockIssueLineId_TenantId~",
                        columns: x => new { x.StockIssueLineId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockIssueLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldReceiptLines_UnitOfMeasures_UnitOfMeasureId_TenantId",
                        columns: x => new { x.UnitOfMeasureId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "UnitOfMeasures",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldAccountabilityCorrections",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    InputApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StockReturnId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryLossId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceVersion = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RequestIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldAccountabilityCorrections", x => x.Id);
                    table.UniqueConstraint("AK_FieldAccountabilityCorrections_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_FieldAccountabilityCorrections_OneOriginal", "num_nonnulls(\"FieldReceiptId\", \"InputApplicationId\", \"StockReturnId\", \"InventoryLossId\") = 1");
                    table.ForeignKey(
                        name: "FK_FieldAccountabilityCorrections_Activities_ActivityId_Tenant~",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldAccountabilityCorrections_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldAccountabilityCorrections_FieldReceipts_FieldReceiptId~",
                        columns: x => new { x.FieldReceiptId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "FieldReceipts",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldAccountabilityCorrections_InputApplications_InputAppli~",
                        columns: x => new { x.InputApplicationId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InputApplications",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldAccountabilityCorrections_InventoryLosses_InventoryLos~",
                        columns: x => new { x.InventoryLossId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryLosses",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldAccountabilityCorrections_StockReturns_StockReturnId_T~",
                        columns: x => new { x.StockReturnId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockReturns",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockReturnLines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockPositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssueUnitCostUsdSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReturnLines", x => x.Id);
                    table.UniqueConstraint("AK_StockReturnLines_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_StockReturnLines_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_StockReturnLines_StockIssueLines_StockIssueLineId_TenantId_~",
                        columns: x => new { x.StockIssueLineId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockIssueLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReturnLines_StockPositions_StockPositionId_TenantId_Fa~",
                        columns: x => new { x.StockPositionId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockPositions",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReturnLines_StockReturns_StockReturnId_TenantId_FarmId",
                        columns: x => new { x.StockReturnId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockReturns",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InputApplicationLines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    InputApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldReceiptLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssueUnitCostUsdSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    AppliedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CoverageSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ActualRate = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    RuleIdSnapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleVersionSnapshot = table.Column<long>(type: "bigint", nullable: false),
                    RuleRateSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    LowerTolerancePercentSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    UpperTolerancePercentSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    RateVariance = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputApplicationLines", x => x.Id);
                    table.UniqueConstraint("AK_InputApplicationLines_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_InputApplicationLines_Quantity", "\"AppliedQuantity\" > 0 AND \"CoverageSnapshot\" > 0");
                    table.ForeignKey(
                        name: "FK_InputApplicationLines_FieldReceiptLines_FieldReceiptLineId_~",
                        columns: x => new { x.FieldReceiptLineId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "FieldReceiptLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputApplicationLines_InputApplications_InputApplicationId_~",
                        columns: x => new { x.InputApplicationId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InputApplications",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputApplicationLines_InventoryItems_InventoryItemId_Tenant~",
                        columns: x => new { x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputApplicationLines_InventoryLots_InventoryLotId_TenantId~",
                        columns: x => new { x.InventoryLotId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryLots",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputApplicationLines_StockIssueLines_StockIssueLineId_Tena~",
                        columns: x => new { x.StockIssueLineId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockIssueLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputApplicationLines_UnitOfMeasures_UnitOfMeasureId_Tenant~",
                        columns: x => new { x.UnitOfMeasureId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "UnitOfMeasures",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperationalCostPostings",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InputApplicationLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryLossId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceQuantitySnapshot = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitCostUsdSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(20,2)", precision: 20, scale: 2, nullable: false),
                    PostingIdentity = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ReversalOfOperationalCostPostingId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalCostPostings", x => x.Id);
                    table.UniqueConstraint("AK_OperationalCostPostings_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_OperationalCostPostings_ActiveSource", "\"ReversalOfOperationalCostPostingId\" IS NOT NULL OR ((\"Category\" = 'AppliedInput') = (\"InputApplicationLineId\" IS NOT NULL))");
                    table.CheckConstraint("CK_OperationalCostPostings_OneSource", "num_nonnulls(\"InputApplicationLineId\", \"InventoryLossId\") = 1");
                    table.ForeignKey(
                        name: "FK_OperationalCostPostings_Activities_ActivityId_TenantId_Farm~",
                        columns: x => new { x.ActivityId, x.TenantId, x.FarmId },
                        principalSchema: "activities",
                        principalTable: "Activities",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationalCostPostings_CropCycles_CropCycleId_FieldId",
                        columns: x => new { x.CropCycleId, x.FieldId },
                        principalSchema: "farm",
                        principalTable: "CropCycles",
                        principalColumns: new[] { "Id", "FieldId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationalCostPostings_InputApplicationLines_InputApplicat~",
                        columns: x => new { x.InputApplicationLineId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InputApplicationLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationalCostPostings_InventoryLosses_InventoryLossId_Ten~",
                        columns: x => new { x.InventoryLossId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryLosses",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationalCostPostings_OperationalCostPostings_ReversalOfO~",
                        column: x => x.ReversalOfOperationalCostPostingId,
                        principalSchema: "finance",
                        principalTable: "OperationalCostPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockReturnLineId",
                schema: "inventory",
                table: "StockMovements",
                column: "StockReturnLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockReturnLineId_TenantId_FarmId",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "StockReturnLineId", "TenantId", "FarmId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_OneSource",
                schema: "inventory",
                table: "StockMovements",
                sql: "num_nonnulls(\"StockReceiptLineId\", \"StockIssueLineId\", \"StockReturnLineId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Reversal",
                schema: "inventory",
                table: "StockMovements",
                sql: "(\"MovementType\" IN ('ReceiptReversal', 'IssueReversal', 'ReturnReversal')) = (\"ReversalOfStockMovementId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_ControlExceptionId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "ControlExceptionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_CorrectionRecordId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "CorrectionRecordId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_FieldAccountabilityCorrectionId_Te~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "FieldAccountabilityCorrectionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_FieldReceiptId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "FieldReceiptId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_InputApplicationId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InputApplicationId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_InventoryLossId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InventoryLossId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_OperationalCostPostingId_TenantId_~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "OperationalCostPostingId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_StockReturnId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "StockReturnId", "TenantId", "FarmId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryAuditEventLinks_OneSubject",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                sql: "num_nonnulls(\"UnitOfMeasureId\", \"InventoryItemId\", \"SupplierId\", \"InventoryLotId\", \"StockReceiptId\", \"InventoryApplicationRuleId\", \"InputRequestId\", \"StockIssueId\", \"ManagerInvitationId\", \"FieldReceiptId\", \"InputApplicationId\", \"StockReturnId\", \"InventoryLossId\", \"OperationalCostPostingId\", \"ControlExceptionId\", \"CorrectionRecordId\", \"FieldAccountabilityCorrectionId\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_FieldAccountabilityCorrectionId_SubjectVe~",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "FieldAccountabilityCorrectionId", "SubjectVersion" },
                unique: true,
                filter: "\"FieldAccountabilityCorrectionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_FieldAccountabilityCorrectionId_TenantId_~",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "FieldAccountabilityCorrectionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_InventoryLossId_SubjectVersion",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "InventoryLossId", "SubjectVersion" },
                unique: true,
                filter: "\"InventoryLossId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_InventoryLossId_TenantId_FarmId",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "InventoryLossId", "TenantId", "FarmId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalDecisions_OneSubject",
                schema: "inventory",
                table: "ApprovalDecisions",
                sql: "num_nonnulls(\"StockReceiptId\", \"InputRequestId\", \"InventoryLossId\", \"FieldAccountabilityCorrectionId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalDecisions_Role",
                schema: "inventory",
                table: "ApprovalDecisions",
                sql: "(\"StockReceiptId\" IS NULL OR \"ApproverRole\" = 'Grower') AND (\"InputRequestId\" IS NULL OR \"ApproverRole\" IN ('Grower', 'FarmManager')) AND (\"InventoryLossId\" IS NULL OR \"ApproverRole\" = 'Grower') AND (\"FieldAccountabilityCorrectionId\" IS NULL OR \"ApproverRole\" = 'Grower')");

            migrationBuilder.CreateIndex(
                name: "IX_ControlExceptions_ActivityId_TenantId_FarmId",
                schema: "audit",
                table: "ControlExceptions",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_ControlExceptions_StockIssueLineId_TenantId_FarmId",
                schema: "audit",
                table: "ControlExceptions",
                columns: new[] { "StockIssueLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_ControlExceptions_TenantId_FarmId_ActivityId_Status",
                schema: "audit",
                table: "ControlExceptions",
                columns: new[] { "TenantId", "FarmId", "ActivityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ControlExceptions_TenantId_FarmId_StockIssueLineId_Code",
                schema: "audit",
                table: "ControlExceptions",
                columns: new[] { "TenantId", "FarmId", "StockIssueLineId", "Code" },
                unique: true,
                filter: "\"Status\" = 'Open'");

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccountabilityCorrections_ActivityId_TenantId_FarmId",
                schema: "inventory",
                table: "FieldAccountabilityCorrections",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccountabilityCorrections_FieldReceiptId_TenantId_Farm~",
                schema: "inventory",
                table: "FieldAccountabilityCorrections",
                columns: new[] { "FieldReceiptId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccountabilityCorrections_InputApplicationId_TenantId_~",
                schema: "inventory",
                table: "FieldAccountabilityCorrections",
                columns: new[] { "InputApplicationId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccountabilityCorrections_InventoryLossId_TenantId_Far~",
                schema: "inventory",
                table: "FieldAccountabilityCorrections",
                columns: new[] { "InventoryLossId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccountabilityCorrections_RequestedByUserId",
                schema: "inventory",
                table: "FieldAccountabilityCorrections",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccountabilityCorrections_StockReturnId_TenantId_FarmId",
                schema: "inventory",
                table: "FieldAccountabilityCorrections",
                columns: new[] { "StockReturnId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccountabilityCorrections_TenantId_FarmId_ActivityId_S~",
                schema: "inventory",
                table: "FieldAccountabilityCorrections",
                columns: new[] { "TenantId", "FarmId", "ActivityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccountabilityCorrections_TenantId_FarmId_RequestIdemp~",
                schema: "inventory",
                table: "FieldAccountabilityCorrections",
                columns: new[] { "TenantId", "FarmId", "RequestIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceiptLines_FieldReceiptId_StockIssueLineId",
                schema: "inventory",
                table: "FieldReceiptLines",
                columns: new[] { "FieldReceiptId", "StockIssueLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceiptLines_FieldReceiptId_TenantId_FarmId",
                schema: "inventory",
                table: "FieldReceiptLines",
                columns: new[] { "FieldReceiptId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceiptLines_InventoryItemId_TenantId_FarmId",
                schema: "inventory",
                table: "FieldReceiptLines",
                columns: new[] { "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceiptLines_InventoryLotId_TenantId_FarmId",
                schema: "inventory",
                table: "FieldReceiptLines",
                columns: new[] { "InventoryLotId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceiptLines_StockIssueLineId",
                schema: "inventory",
                table: "FieldReceiptLines",
                column: "StockIssueLineId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceiptLines_StockIssueLineId_TenantId_FarmId",
                schema: "inventory",
                table: "FieldReceiptLines",
                columns: new[] { "StockIssueLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceiptLines_UnitOfMeasureId_TenantId",
                schema: "inventory",
                table: "FieldReceiptLines",
                columns: new[] { "UnitOfMeasureId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceipts_ActivityId_TenantId_FarmId",
                schema: "inventory",
                table: "FieldReceipts",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceipts_CropCycleId_FieldId",
                schema: "inventory",
                table: "FieldReceipts",
                columns: new[] { "CropCycleId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceipts_EnteredByUserId",
                schema: "inventory",
                table: "FieldReceipts",
                column: "EnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceipts_FieldId_FarmId",
                schema: "inventory",
                table: "FieldReceipts",
                columns: new[] { "FieldId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceipts_RecipientPersonId_FarmId",
                schema: "inventory",
                table: "FieldReceipts",
                columns: new[] { "RecipientPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceipts_StockIssueId_TenantId_FarmId",
                schema: "inventory",
                table: "FieldReceipts",
                columns: new[] { "StockIssueId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldReceipts_TenantId_FarmId_StockIssueId_Status",
                schema: "inventory",
                table: "FieldReceipts",
                columns: new[] { "TenantId", "FarmId", "StockIssueId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InputApplicationLines_FieldReceiptLineId_TenantId_FarmId",
                schema: "inventory",
                table: "InputApplicationLines",
                columns: new[] { "FieldReceiptLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputApplicationLines_InputApplicationId_FieldReceiptLineId",
                schema: "inventory",
                table: "InputApplicationLines",
                columns: new[] { "InputApplicationId", "FieldReceiptLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InputApplicationLines_InputApplicationId_TenantId_FarmId",
                schema: "inventory",
                table: "InputApplicationLines",
                columns: new[] { "InputApplicationId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputApplicationLines_InventoryItemId_TenantId_FarmId",
                schema: "inventory",
                table: "InputApplicationLines",
                columns: new[] { "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputApplicationLines_InventoryLotId_TenantId_FarmId",
                schema: "inventory",
                table: "InputApplicationLines",
                columns: new[] { "InventoryLotId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputApplicationLines_StockIssueLineId",
                schema: "inventory",
                table: "InputApplicationLines",
                column: "StockIssueLineId");

            migrationBuilder.CreateIndex(
                name: "IX_InputApplicationLines_StockIssueLineId_TenantId_FarmId",
                schema: "inventory",
                table: "InputApplicationLines",
                columns: new[] { "StockIssueLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputApplicationLines_UnitOfMeasureId_TenantId",
                schema: "inventory",
                table: "InputApplicationLines",
                columns: new[] { "UnitOfMeasureId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputApplications_ActivityId_TenantId_FarmId",
                schema: "inventory",
                table: "InputApplications",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputApplications_EnteredByUserId",
                schema: "inventory",
                table: "InputApplications",
                column: "EnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InputApplications_ManagerConfirmedByUserId",
                schema: "inventory",
                table: "InputApplications",
                column: "ManagerConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InputApplications_SupervisorAttestationEnteredByUserId",
                schema: "inventory",
                table: "InputApplications",
                column: "SupervisorAttestationEnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InputApplications_SupervisorPersonId_FarmId",
                schema: "inventory",
                table: "InputApplications",
                columns: new[] { "SupervisorPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InputApplications_TenantId_FarmId_ActivityId_Status",
                schema: "inventory",
                table: "InputApplications",
                columns: new[] { "TenantId", "FarmId", "ActivityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLosses_ActivityId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryLosses",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLosses_StockIssueLineId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryLosses",
                columns: new[] { "StockIssueLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLosses_SubmittedByUserId",
                schema: "inventory",
                table: "InventoryLosses",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLosses_TenantId_FarmId_StockIssueLineId_Status",
                schema: "inventory",
                table: "InventoryLosses",
                columns: new[] { "TenantId", "FarmId", "StockIssueLineId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_ActivityId_TenantId_FarmId",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_CropCycleId_FieldId",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "CropCycleId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_InputApplicationLineId_Category",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "InputApplicationLineId", "Category" },
                unique: true,
                filter: "\"InputApplicationLineId\" IS NOT NULL AND \"ReversalOfOperationalCostPostingId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_InputApplicationLineId_TenantId_Far~",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "InputApplicationLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_InventoryLossId_Category",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "InventoryLossId", "Category" },
                unique: true,
                filter: "\"InventoryLossId\" IS NOT NULL AND \"ReversalOfOperationalCostPostingId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_InventoryLossId_TenantId_FarmId",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "InventoryLossId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_PostingIdentity",
                schema: "finance",
                table: "OperationalCostPostings",
                column: "PostingIdentity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_ReversalOfOperationalCostPostingId",
                schema: "finance",
                table: "OperationalCostPostings",
                column: "ReversalOfOperationalCostPostingId",
                unique: true,
                filter: "\"ReversalOfOperationalCostPostingId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockReturnLines_StockIssueLineId",
                schema: "inventory",
                table: "StockReturnLines",
                column: "StockIssueLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReturnLines_StockIssueLineId_TenantId_FarmId",
                schema: "inventory",
                table: "StockReturnLines",
                columns: new[] { "StockIssueLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReturnLines_StockPositionId_TenantId_FarmId",
                schema: "inventory",
                table: "StockReturnLines",
                columns: new[] { "StockPositionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReturnLines_StockReturnId_StockIssueLineId",
                schema: "inventory",
                table: "StockReturnLines",
                columns: new[] { "StockReturnId", "StockIssueLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReturnLines_StockReturnId_TenantId_FarmId",
                schema: "inventory",
                table: "StockReturnLines",
                columns: new[] { "StockReturnId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReturns_ActivityId_TenantId_FarmId",
                schema: "inventory",
                table: "StockReturns",
                columns: new[] { "ActivityId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReturns_PostedByUserId",
                schema: "inventory",
                table: "StockReturns",
                column: "PostedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReturns_PostingIdempotencyKey",
                schema: "inventory",
                table: "StockReturns",
                column: "PostingIdempotencyKey",
                unique: true,
                filter: "\"PostingIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockReturns_ReceiverPersonId_FarmId",
                schema: "inventory",
                table: "StockReturns",
                columns: new[] { "ReceiverPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReturns_ReversalIdempotencyKey",
                schema: "inventory",
                table: "StockReturns",
                column: "ReversalIdempotencyKey",
                unique: true,
                filter: "\"ReversalIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockReturns_SenderPersonId_FarmId",
                schema: "inventory",
                table: "StockReturns",
                columns: new[] { "SenderPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReturns_StoreId_FarmId",
                schema: "inventory",
                table: "StockReturns",
                columns: new[] { "StoreId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReturns_TenantId_FarmId_ActivityId_Status",
                schema: "inventory",
                table: "StockReturns",
                columns: new[] { "TenantId", "FarmId", "ActivityId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalDecisions_FieldAccountabilityCorrections_FieldAccou~",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "FieldAccountabilityCorrectionId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "FieldAccountabilityCorrections",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalDecisions_InventoryLosses_InventoryLossId_TenantId_~",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "InventoryLossId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "InventoryLosses",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_ControlExceptions_ControlException~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "ControlExceptionId", "TenantId", "FarmId" },
                principalSchema: "audit",
                principalTable: "ControlExceptions",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_CorrectionRecords_CorrectionRecord~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "CorrectionRecordId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "CorrectionRecords",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_FieldAccountabilityCorrections_Fie~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "FieldAccountabilityCorrectionId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "FieldAccountabilityCorrections",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_FieldReceipts_FieldReceiptId_Tenan~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "FieldReceiptId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "FieldReceipts",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_InputApplications_InputApplication~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InputApplicationId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "InputApplications",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_InventoryLosses_InventoryLossId_Te~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InventoryLossId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "InventoryLosses",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_OperationalCostPostings_Operationa~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "OperationalCostPostingId", "TenantId", "FarmId" },
                principalSchema: "finance",
                principalTable: "OperationalCostPostings",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_StockReturns_StockReturnId_TenantI~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "StockReturnId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "StockReturns",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StockReturnLines_StockReturnLineId_TenantId_~",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "StockReturnLineId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "StockReturnLines",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER "TR_OperationalCostPostings_AppendOnly"
                BEFORE UPDATE OR DELETE ON finance."OperationalCostPostings"
                FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS \"TR_OperationalCostPostings_AppendOnly\" ON finance.\"OperationalCostPostings\";");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalDecisions_FieldAccountabilityCorrections_FieldAccou~",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalDecisions_InventoryLosses_InventoryLossId_TenantId_~",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_ControlExceptions_ControlException~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_CorrectionRecords_CorrectionRecord~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_FieldAccountabilityCorrections_Fie~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_FieldReceipts_FieldReceiptId_Tenan~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_InputApplications_InputApplication~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_InventoryLosses_InventoryLossId_Te~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_OperationalCostPostings_Operationa~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_StockReturns_StockReturnId_TenantI~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StockReturnLines_StockReturnLineId_TenantId_~",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "ControlExceptions",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "FieldAccountabilityCorrections",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "OperationalCostPostings",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "StockReturnLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InputApplicationLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryLosses",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockReturns",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "FieldReceiptLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InputApplications",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "FieldReceipts",
                schema: "inventory");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_StockReturnLineId",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_StockReturnLineId_TenantId_FarmId",
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
                name: "IX_InventoryAuditEventLinks_ControlExceptionId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_CorrectionRecordId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_FieldAccountabilityCorrectionId_Te~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_FieldReceiptId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_InputApplicationId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_InventoryLossId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_OperationalCostPostingId_TenantId_~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_StockReturnId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryAuditEventLinks_OneSubject",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CorrectionRecords_Id_TenantId_FarmId",
                schema: "inventory",
                table: "CorrectionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecisions_FieldAccountabilityCorrectionId_SubjectVe~",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecisions_FieldAccountabilityCorrectionId_TenantId_~",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecisions_InventoryLossId_SubjectVersion",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecisions_InventoryLossId_TenantId_FarmId",
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
                name: "StockReturnLineId",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ControlExceptionId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "CorrectionRecordId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "FieldAccountabilityCorrectionId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "FieldReceiptId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "InputApplicationId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "InventoryLossId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "OperationalCostPostingId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "StockReturnId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "FieldAccountabilityCorrectionId",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropColumn(
                name: "InventoryLossId",
                schema: "inventory",
                table: "ApprovalDecisions");

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryAuditEventLinks_OneSubject",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                sql: "num_nonnulls(\"UnitOfMeasureId\", \"InventoryItemId\", \"SupplierId\", \"InventoryLotId\", \"StockReceiptId\", \"InventoryApplicationRuleId\", \"InputRequestId\", \"StockIssueId\", \"ManagerInvitationId\") = 1");

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
        }
    }
}
