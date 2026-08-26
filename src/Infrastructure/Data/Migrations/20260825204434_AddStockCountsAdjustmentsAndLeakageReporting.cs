using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockCountsAdjustmentsAndLeakageReporting : Migration
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

            migrationBuilder.AddColumn<Guid>(
                name: "StockAdjustmentId",
                schema: "inventory",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryLeakageExportId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StockAdjustmentId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StockCountId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StockAdjustmentId",
                schema: "inventory",
                table: "ApprovalDecisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryLeakageExports",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FilterSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    ExportedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ExportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLeakageExports", x => x.Id);
                    table.UniqueConstraint("AK_InventoryLeakageExports_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.ForeignKey(
                        name: "FK_InventoryLeakageExports_AspNetUsers_ExportedByUserId",
                        column: x => x.ExportedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCounts",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CountingPersons = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CutoffPostingSequence = table.Column<long>(type: "bigint", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCounts", x => x.Id);
                    table.UniqueConstraint("AK_StockCounts_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.ForeignKey(
                        name: "FK_StockCounts_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCounts_Stores_StoreId_FarmId",
                        columns: x => new { x.StoreId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Stores",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAdjustments",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockPositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockCountLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdjustmentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SignedQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    ExplicitUnitValueUsd = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    SourceCountLineVersion = table.Column<long>(type: "bigint", nullable: true),
                    SourceCountVersion = table.Column<long>(type: "bigint", nullable: true),
                    UnitCostUsdSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    SignedValueUsdSnapshot = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StockMovementId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalOfStockAdjustmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalStockAdjustmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                    table.UniqueConstraint("AK_StockAdjustments_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_StockAdjustments_CountType", "(\"StockCountLineId\" IS NULL) OR \"AdjustmentType\" = 'CountVariance'");
                    table.CheckConstraint("CK_StockAdjustments_ExplicitValue", "\"ExplicitUnitValueUsd\" IS NULL OR \"ExplicitUnitValueUsd\" >= 0");
                    table.CheckConstraint("CK_StockAdjustments_Nonzero", "\"SignedQuantity\" <> 0");
                    table.ForeignKey(
                        name: "FK_StockAdjustments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_InventoryItems_InventoryItemId_TenantId_Fa~",
                        columns: x => new { x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_InventoryLots_InventoryLotId_InventoryItem~",
                        columns: x => new { x.InventoryLotId, x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryLots",
                        principalColumns: new[] { "Id", "InventoryItemId", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_StockAdjustments_ReversalOfStockAdjustment~",
                        columns: x => new { x.ReversalOfStockAdjustmentId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockAdjustments",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_StockPositions_StockPositionId_TenantId_Fa~",
                        columns: x => new { x.StockPositionId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockPositions",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_Stores_StoreId_FarmId",
                        columns: x => new { x.StoreId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Stores",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAdjustments_UnitOfMeasures_UnitOfMeasureId_TenantId",
                        columns: x => new { x.UnitOfMeasureId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "UnitOfMeasures",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCountLines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockCountId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockPositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    ExpectedValueUsd = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EnteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EnteredByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    PostedStockAdjustmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountLines", x => x.Id);
                    table.UniqueConstraint("AK_StockCountLines_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_StockCountLines_CountedNonnegative", "\"CountedQuantity\" IS NULL OR \"CountedQuantity\" >= 0");
                    table.CheckConstraint("CK_StockCountLines_ExpectedNonnegative", "\"ExpectedQuantity\" >= 0 AND \"ExpectedValueUsd\" >= 0");
                    table.ForeignKey(
                        name: "FK_StockCountLines_AspNetUsers_EnteredByUserId",
                        column: x => x.EnteredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLines_InventoryItems_InventoryItemId_TenantId_Far~",
                        columns: x => new { x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLines_InventoryLots_InventoryLotId_InventoryItemI~",
                        columns: x => new { x.InventoryLotId, x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryLots",
                        principalColumns: new[] { "Id", "InventoryItemId", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockAdjustments_PostedStockAdjustmentId_Te~",
                        columns: x => new { x.PostedStockAdjustmentId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockAdjustments",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockCounts_StockCountId_TenantId_FarmId",
                        columns: x => new { x.StockCountId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockCounts",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockPositions_StockPositionId_TenantId_Far~",
                        columns: x => new { x.StockPositionId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockPositions",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLines_UnitOfMeasures_UnitOfMeasureId_TenantId",
                        columns: x => new { x.UnitOfMeasureId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "UnitOfMeasures",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockAdjustmentId",
                schema: "inventory",
                table: "StockMovements",
                column: "StockAdjustmentId",
                unique: true,
                filter: "\"StockAdjustmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockAdjustmentId_TenantId_FarmId",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "StockAdjustmentId", "TenantId", "FarmId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_OneSource",
                schema: "inventory",
                table: "StockMovements",
                sql: "num_nonnulls(\"StockReceiptLineId\", \"StockIssueLineId\", \"StockReturnLineId\", \"StockAdjustmentId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Reversal",
                schema: "inventory",
                table: "StockMovements",
                sql: "(\"MovementType\" IN ('ReceiptReversal', 'IssueReversal', 'ReturnReversal', 'AdjustmentReversal')) = (\"ReversalOfStockMovementId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_InventoryLeakageExportId_TenantId_~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InventoryLeakageExportId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_StockAdjustmentId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "StockAdjustmentId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_StockCountId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "StockCountId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_TenantId_FarmId_InventoryLeakageEx~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "TenantId", "FarmId", "InventoryLeakageExportId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_TenantId_FarmId_StockAdjustmentId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "TenantId", "FarmId", "StockAdjustmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_TenantId_FarmId_StockCountId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "TenantId", "FarmId", "StockCountId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryAuditEventLinks_OneSubject",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                sql: "num_nonnulls(\"UnitOfMeasureId\", \"InventoryItemId\", \"SupplierId\", \"InventoryLotId\", \"StockReceiptId\", \"InventoryApplicationRuleId\", \"InputRequestId\", \"StockIssueId\", \"ManagerInvitationId\", \"FieldReceiptId\", \"InputApplicationId\", \"StockReturnId\", \"InventoryLossId\", \"OperationalCostPostingId\", \"ControlExceptionId\", \"CorrectionRecordId\", \"FieldAccountabilityCorrectionId\", \"StockCountId\", \"StockAdjustmentId\", \"InventoryLeakageExportId\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_StockAdjustmentId_SubjectVersion",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "StockAdjustmentId", "SubjectVersion" },
                unique: true,
                filter: "\"StockAdjustmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_StockAdjustmentId_TenantId_FarmId",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "StockAdjustmentId", "TenantId", "FarmId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalDecisions_OneSubject",
                schema: "inventory",
                table: "ApprovalDecisions",
                sql: "num_nonnulls(\"StockReceiptId\", \"InputRequestId\", \"InventoryLossId\", \"FieldAccountabilityCorrectionId\", \"StockAdjustmentId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalDecisions_Role",
                schema: "inventory",
                table: "ApprovalDecisions",
                sql: "(\"StockReceiptId\" IS NULL OR \"ApproverRole\" = 'Grower') AND (\"InputRequestId\" IS NULL OR \"ApproverRole\" IN ('Grower', 'FarmManager')) AND (\"InventoryLossId\" IS NULL OR \"ApproverRole\" = 'Grower') AND (\"FieldAccountabilityCorrectionId\" IS NULL OR \"ApproverRole\" = 'Grower') AND (\"StockAdjustmentId\" IS NULL OR \"ApproverRole\" = 'Grower')");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLeakageExports_ExportedByUserId",
                schema: "inventory",
                table: "InventoryLeakageExports",
                column: "ExportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLeakageExports_TenantId_FarmId_ExportedAt",
                schema: "inventory",
                table: "InventoryLeakageExports",
                columns: new[] { "TenantId", "FarmId", "ExportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_CreatedByUserId",
                schema: "inventory",
                table: "StockAdjustments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_InventoryItemId_TenantId_FarmId",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_InventoryLotId_InventoryItemId_TenantId_Fa~",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "InventoryLotId", "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ReversalOfStockAdjustmentId",
                schema: "inventory",
                table: "StockAdjustments",
                column: "ReversalOfStockAdjustmentId",
                unique: true,
                filter: "\"ReversalOfStockAdjustmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_ReversalOfStockAdjustmentId_TenantId_FarmId",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "ReversalOfStockAdjustmentId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_StockCountLineId",
                schema: "inventory",
                table: "StockAdjustments",
                column: "StockCountLineId",
                filter: "\"StockCountLineId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_StockCountLineId_TenantId_FarmId",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "StockCountLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_StockPositionId_TenantId_FarmId",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "StockPositionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_StoreId_FarmId",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "StoreId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_TenantId_FarmId_StoreId_Status",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "TenantId", "FarmId", "StoreId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustments_UnitOfMeasureId_TenantId",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "UnitOfMeasureId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_EnteredByUserId",
                schema: "inventory",
                table: "StockCountLines",
                column: "EnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_InventoryItemId_TenantId_FarmId",
                schema: "inventory",
                table: "StockCountLines",
                columns: new[] { "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_InventoryLotId_InventoryItemId_TenantId_Far~",
                schema: "inventory",
                table: "StockCountLines",
                columns: new[] { "InventoryLotId", "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_PostedStockAdjustmentId_TenantId_FarmId",
                schema: "inventory",
                table: "StockCountLines",
                columns: new[] { "PostedStockAdjustmentId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockCountId_StockPositionId",
                schema: "inventory",
                table: "StockCountLines",
                columns: new[] { "StockCountId", "StockPositionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockCountId_TenantId_FarmId",
                schema: "inventory",
                table: "StockCountLines",
                columns: new[] { "StockCountId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockPositionId_TenantId_FarmId",
                schema: "inventory",
                table: "StockCountLines",
                columns: new[] { "StockPositionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_TenantId_FarmId_InventoryItemId_InventoryLo~",
                schema: "inventory",
                table: "StockCountLines",
                columns: new[] { "TenantId", "FarmId", "InventoryItemId", "InventoryLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_UnitOfMeasureId_TenantId",
                schema: "inventory",
                table: "StockCountLines",
                columns: new[] { "UnitOfMeasureId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_CreatedByUserId",
                schema: "inventory",
                table: "StockCounts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_StoreId_FarmId",
                schema: "inventory",
                table: "StockCounts",
                columns: new[] { "StoreId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_StoreId_Status",
                schema: "inventory",
                table: "StockCounts",
                columns: new[] { "StoreId", "Status" },
                unique: true,
                filter: "\"Status\" = 'InProgress'");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_TenantId_FarmId_StoreId_Status",
                schema: "inventory",
                table: "StockCounts",
                columns: new[] { "TenantId", "FarmId", "StoreId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalDecisions_StockAdjustments_StockAdjustmentId_Tenant~",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "StockAdjustmentId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "StockAdjustments",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_InventoryLeakageExports_InventoryL~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InventoryLeakageExportId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "InventoryLeakageExports",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_StockAdjustments_StockAdjustmentId~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "StockAdjustmentId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "StockAdjustments",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAuditEventLinks_StockCounts_StockCountId_TenantId_~",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "StockCountId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "StockCounts",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StockAdjustments_StockAdjustmentId_TenantId_~",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "StockAdjustmentId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "StockAdjustments",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustments_StockCountLines_StockCountLineId_TenantId_~",
                schema: "inventory",
                table: "StockAdjustments",
                columns: new[] { "StockCountLineId", "TenantId", "FarmId" },
                principalSchema: "inventory",
                principalTable: "StockCountLines",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_InventoryLeakageExports_AppendOnly"
                BEFORE UPDATE OR DELETE ON inventory."InventoryLeakageExports"
                FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();

                CREATE FUNCTION inventory."RejectPostedStockAdjustmentMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Posted stock adjustment records are immutable; create a reversal instead';
                    END IF;
                    IF OLD."Status" = 'Posted'
                       AND NEW."Status" = 'Reversed'
                       AND NEW."ReversalStockAdjustmentId" IS NOT NULL
                       AND (NEW."TenantId", NEW."FarmId", NEW."StoreId", NEW."StockPositionId", NEW."StockCountLineId", NEW."InventoryItemId", NEW."InventoryLotId", NEW."UnitOfMeasureId", NEW."ItemCodeSnapshot", NEW."ItemNameSnapshot", NEW."LotCodeSnapshot", NEW."UnitCodeSnapshot", NEW."AdjustmentType", NEW."SignedQuantity", NEW."ExplicitUnitValueUsd", NEW."SourceCountLineVersion", NEW."SourceCountVersion", NEW."UnitCostUsdSnapshot", NEW."SignedValueUsdSnapshot", NEW."Reason", NEW."EventDate", NEW."CreatedByUserId", NEW."SubmittedAt", NEW."PostedAt", NEW."StockMovementId", NEW."ReversalOfStockAdjustmentId", NEW."CancellationReason")
                           IS NOT DISTINCT FROM
                           (OLD."TenantId", OLD."FarmId", OLD."StoreId", OLD."StockPositionId", OLD."StockCountLineId", OLD."InventoryItemId", OLD."InventoryLotId", OLD."UnitOfMeasureId", OLD."ItemCodeSnapshot", OLD."ItemNameSnapshot", OLD."LotCodeSnapshot", OLD."UnitCodeSnapshot", OLD."AdjustmentType", OLD."SignedQuantity", OLD."ExplicitUnitValueUsd", OLD."SourceCountLineVersion", OLD."SourceCountVersion", OLD."UnitCostUsdSnapshot", OLD."SignedValueUsdSnapshot", OLD."Reason", OLD."EventDate", OLD."CreatedByUserId", OLD."SubmittedAt", OLD."PostedAt", OLD."StockMovementId", OLD."ReversalOfStockAdjustmentId", OLD."CancellationReason") THEN
                        RETURN NEW;
                    END IF;
                    RAISE EXCEPTION 'Posted stock adjustment records are immutable; create a reversal instead';
                END;
                $$;

                CREATE TRIGGER "TR_StockAdjustments_PostedImmutable"
                BEFORE UPDATE OR DELETE ON inventory."StockAdjustments"
                FOR EACH ROW WHEN (OLD."Status" IN ('Posted', 'Reversed'))
                EXECUTE FUNCTION inventory."RejectPostedStockAdjustmentMutation"();

                CREATE FUNCTION inventory."RejectLockedStockCountMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Stock count records are immutable evidence';
                    END IF;
                    IF OLD."Status" IN ('ClosedNoVariance', 'Cancelled') THEN
                        RAISE EXCEPTION 'Closed or cancelled stock count records are immutable';
                    END IF;
                    IF OLD."Status" = 'Closed' AND (NEW."Status" <> 'PendingAdjustment' OR
                       (NEW."TenantId", NEW."FarmId", NEW."StoreId", NEW."EventDate", NEW."Notes", NEW."CountingPersons", NEW."CutoffPostingSequence", NEW."StartedAt", NEW."ReviewedAt", NEW."CancellationReason")
                       IS DISTINCT FROM
                       (OLD."TenantId", OLD."FarmId", OLD."StoreId", OLD."EventDate", OLD."Notes", OLD."CountingPersons", OLD."CutoffPostingSequence", OLD."StartedAt", OLD."ReviewedAt", OLD."CancellationReason")) THEN
                        RAISE EXCEPTION 'Closed stock counts can only be reopened by an authorised adjustment reversal';
                    END IF;
                    IF OLD."Status" IN ('Review', 'PendingAdjustment') AND
                       (NEW."TenantId", NEW."FarmId", NEW."StoreId", NEW."EventDate", NEW."Notes", NEW."CountingPersons", NEW."CutoffPostingSequence", NEW."StartedAt", NEW."ReviewedAt", NEW."CancellationReason")
                       IS DISTINCT FROM
                       (OLD."TenantId", OLD."FarmId", OLD."StoreId", OLD."EventDate", OLD."Notes", OLD."CountingPersons", OLD."CutoffPostingSequence", OLD."StartedAt", OLD."ReviewedAt", OLD."CancellationReason") THEN
                        RAISE EXCEPTION 'Stock count review snapshots are immutable';
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER "TR_StockCounts_LockedImmutable"
                BEFORE UPDATE OR DELETE ON inventory."StockCounts"
                FOR EACH ROW EXECUTE FUNCTION inventory."RejectLockedStockCountMutation"();

                CREATE FUNCTION inventory."RejectLockedStockCountLineMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Stock count line snapshot is immutable evidence';
                    END IF;
                    IF EXISTS (SELECT 1 FROM inventory."StockCounts" AS count
                        WHERE count."Id" = OLD."StockCountId"
                          AND count."Status" IN ('Review', 'PendingAdjustment', 'ClosedNoVariance', 'Closed', 'Cancelled')) THEN
                        IF (NEW."TenantId", NEW."FarmId", NEW."StockCountId", NEW."StockPositionId", NEW."InventoryItemId", NEW."InventoryLotId", NEW."UnitOfMeasureId", NEW."ItemCodeSnapshot", NEW."ItemNameSnapshot", NEW."LotCodeSnapshot", NEW."UnitCodeSnapshot", NEW."ExpectedQuantity", NEW."ExpectedValueUsd", NEW."CountedQuantity", NEW."Notes", NEW."EnteredAt", NEW."EnteredByUserId")
                           IS DISTINCT FROM
                           (OLD."TenantId", OLD."FarmId", OLD."StockCountId", OLD."StockPositionId", OLD."InventoryItemId", OLD."InventoryLotId", OLD."UnitOfMeasureId", OLD."ItemCodeSnapshot", OLD."ItemNameSnapshot", OLD."LotCodeSnapshot", OLD."UnitCodeSnapshot", OLD."ExpectedQuantity", OLD."ExpectedValueUsd", OLD."CountedQuantity", OLD."Notes", OLD."EnteredAt", OLD."EnteredByUserId") THEN
                            RAISE EXCEPTION 'Stock count line snapshot is immutable after review';
                        END IF;
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER "TR_StockCountLines_LockedImmutable"
                BEFORE UPDATE OR DELETE ON inventory."StockCountLines"
                FOR EACH ROW EXECUTE FUNCTION inventory."RejectLockedStockCountLineMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_StockCountLines_LockedImmutable" ON inventory."StockCountLines";
                DROP FUNCTION IF EXISTS inventory."RejectLockedStockCountLineMutation"();
                DROP TRIGGER IF EXISTS "TR_StockCounts_LockedImmutable" ON inventory."StockCounts";
                DROP FUNCTION IF EXISTS inventory."RejectLockedStockCountMutation"();
                DROP TRIGGER IF EXISTS "TR_StockAdjustments_PostedImmutable" ON inventory."StockAdjustments";
                DROP FUNCTION IF EXISTS inventory."RejectPostedStockAdjustmentMutation"();
                DROP TRIGGER IF EXISTS "TR_InventoryLeakageExports_AppendOnly" ON inventory."InventoryLeakageExports";
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalDecisions_StockAdjustments_StockAdjustmentId_Tenant~",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_InventoryLeakageExports_InventoryL~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_StockAdjustments_StockAdjustmentId~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAuditEventLinks_StockCounts_StockCountId_TenantId_~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StockAdjustments_StockAdjustmentId_TenantId_~",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustments_StockCountLines_StockCountLineId_TenantId_~",
                schema: "inventory",
                table: "StockAdjustments");

            migrationBuilder.DropTable(
                name: "InventoryLeakageExports",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockCountLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockAdjustments",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockCounts",
                schema: "inventory");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_StockAdjustmentId",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_StockAdjustmentId_TenantId_FarmId",
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
                name: "IX_InventoryAuditEventLinks_InventoryLeakageExportId_TenantId_~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_StockAdjustmentId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_StockCountId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_TenantId_FarmId_InventoryLeakageEx~",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_TenantId_FarmId_StockAdjustmentId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditEventLinks_TenantId_FarmId_StockCountId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryAuditEventLinks_OneSubject",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecisions_StockAdjustmentId_SubjectVersion",
                schema: "inventory",
                table: "ApprovalDecisions");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecisions_StockAdjustmentId_TenantId_FarmId",
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
                name: "StockAdjustmentId",
                schema: "inventory",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "InventoryLeakageExportId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "StockAdjustmentId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "StockCountId",
                schema: "inventory",
                table: "InventoryAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "StockAdjustmentId",
                schema: "inventory",
                table: "ApprovalDecisions");

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryAuditEventLinks_OneSubject",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                sql: "num_nonnulls(\"UnitOfMeasureId\", \"InventoryItemId\", \"SupplierId\", \"InventoryLotId\", \"StockReceiptId\", \"InventoryApplicationRuleId\", \"InputRequestId\", \"StockIssueId\", \"ManagerInvitationId\", \"FieldReceiptId\", \"InputApplicationId\", \"StockReturnId\", \"InventoryLossId\", \"OperationalCostPostingId\", \"ControlExceptionId\", \"CorrectionRecordId\", \"FieldAccountabilityCorrectionId\") = 1");

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
        }
    }
}
