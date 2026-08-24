using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLedgerFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Stores_Id_FarmId",
                schema: "farm",
                table: "Stores",
                columns: new[] { "Id", "FarmId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AuditEvents_Id_TenantId_FarmId",
                schema: "audit",
                table: "AuditEvents",
                columns: new[] { "Id", "TenantId", "FarmId" });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Contact = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.UniqueConstraint("AK_Suppliers_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_Suppliers_Status", "\"Status\" IN ('Active', 'Archived')");
                    table.ForeignKey(
                        name: "FK_Suppliers_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitOfMeasures",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Dimension = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOfMeasures", x => x.Id);
                    table.UniqueConstraint("AK_UnitOfMeasures_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.CheckConstraint("CK_UnitOfMeasures_DecimalPlaces", "\"DecimalPlaces\" BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_UnitOfMeasures_Status", "\"Status\" IN ('Active', 'Archived')");
                });

            migrationBuilder.CreateTable(
                name: "StockReceipts",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReceivedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LateEntryReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PostingIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ReversedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReversedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReversalIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CorrectsStockReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReceipts", x => x.Id);
                    table.UniqueConstraint("AK_StockReceipts_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_StockReceipts_OpeningReason", "\"ReceiptType\" <> 'OpeningBalance' OR length(trim(\"Reason\")) > 0");
                    table.CheckConstraint("CK_StockReceipts_PostingMetadata", "(\"Status\" NOT IN ('Posted', 'Reversed')) OR (\"PostedAt\" IS NOT NULL AND length(trim(\"PostedByUserId\")) > 0 AND length(trim(\"PostingIdempotencyKey\")) > 0)");
                    table.CheckConstraint("CK_StockReceipts_ReversalMetadata", "\"Status\" <> 'Reversed' OR (\"ReversedAt\" IS NOT NULL AND length(trim(\"ReversedByUserId\")) > 0 AND length(trim(\"ReversalIdempotencyKey\")) > 0)");
                    table.CheckConstraint("CK_StockReceipts_Supplier", "(\"ReceiptType\" = 'Purchase' AND \"SupplierId\" IS NOT NULL) OR (\"ReceiptType\" = 'OpeningBalance' AND \"SupplierId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_StockReceipts_AspNetUsers_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReceipts_AspNetUsers_ReversedByUserId",
                        column: x => x.ReversedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReceipts_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReceipts_Persons_ReceivedByPersonId_FarmId",
                        columns: x => new { x.ReceivedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReceipts_StockReceipts_CorrectsStockReceiptId",
                        column: x => x.CorrectsStockReceiptId,
                        principalSchema: "inventory",
                        principalTable: "StockReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReceipts_Stores_StoreId_FarmId",
                        columns: x => new { x.StoreId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Stores",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReceipts_Suppliers_SupplierId_TenantId_FarmId",
                        columns: x => new { x.SupplierId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "Suppliers",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StockUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockUnitCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StockUnitName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReorderLevel = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    LotTrackingPolicy = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ExpiryPolicy = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CostingMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.UniqueConstraint("AK_InventoryItems_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_InventoryItems_CostingMethod", "\"CostingMethod\" = 'MovingWeightedAverage'");
                    table.CheckConstraint("CK_InventoryItems_ExpiryRequiresLots", "\"LotTrackingPolicy\" <> 'None' OR \"ExpiryPolicy\" = 'None'");
                    table.CheckConstraint("CK_InventoryItems_ReorderLevel", "\"ReorderLevel\" IS NULL OR \"ReorderLevel\" >= 0");
                    table.CheckConstraint("CK_InventoryItems_Status", "\"Status\" IN ('Active', 'Archived')");
                    table.ForeignKey(
                        name: "FK_InventoryItems_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_UnitOfMeasures_StockUnitId_TenantId",
                        columns: x => new { x.StockUnitId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "UnitOfMeasures",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalDecisions",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectVersion = table.Column<long>(type: "bigint", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ApproverUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ApproverRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDecisions", x => x.Id);
                    table.CheckConstraint("CK_ApprovalDecisions_GrowerOpening", "\"ApproverRole\" = 'Grower'");
                    table.ForeignKey(
                        name: "FK_ApprovalDecisions_AspNetUsers_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalDecisions_StockReceipts_StockReceiptId_TenantId_Far~",
                        columns: x => new { x.StockReceiptId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockReceipts",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLots",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLots", x => x.Id);
                    table.UniqueConstraint("AK_InventoryLots_Id_InventoryItemId_TenantId_FarmId", x => new { x.Id, x.InventoryItemId, x.TenantId, x.FarmId });
                    table.UniqueConstraint("AK_InventoryLots_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_InventoryLots_Status", "\"Status\" IN ('Active', 'Archived')");
                    table.ForeignKey(
                        name: "FK_InventoryLots_InventoryItems_InventoryItemId_TenantId_FarmId",
                        columns: x => new { x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryAuditEventLinks",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    StockReceiptId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAuditEventLinks", x => x.Id);
                    table.CheckConstraint("CK_InventoryAuditEventLinks_OneSubject", "num_nonnulls(\"UnitOfMeasureId\", \"InventoryItemId\", \"SupplierId\", \"InventoryLotId\", \"StockReceiptId\") = 1");
                    table.ForeignKey(
                        name: "FK_InventoryAuditEventLinks_AuditEvents_AuditEventId_TenantId_~",
                        columns: x => new { x.AuditEventId, x.TenantId, x.FarmId },
                        principalSchema: "audit",
                        principalTable: "AuditEvents",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAuditEventLinks_InventoryItems_InventoryItemId_Ten~",
                        columns: x => new { x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAuditEventLinks_InventoryLots_InventoryLotId_Tenan~",
                        columns: x => new { x.InventoryLotId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryLots",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAuditEventLinks_StockReceipts_StockReceiptId_Tenan~",
                        columns: x => new { x.StockReceiptId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockReceipts",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAuditEventLinks_Suppliers_SupplierId_TenantId_Farm~",
                        columns: x => new { x.SupplierId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "Suppliers",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAuditEventLinks_UnitOfMeasures_UnitOfMeasureId_Ten~",
                        columns: x => new { x.UnitOfMeasureId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "UnitOfMeasures",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockPositions",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    PositionKey = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPositions", x => x.Id);
                    table.UniqueConstraint("AK_StockPositions_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.ForeignKey(
                        name: "FK_StockPositions_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockPositions_InventoryItems_InventoryItemId_TenantId_Farm~",
                        columns: x => new { x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockPositions_InventoryLots_InventoryLotId_InventoryItemId~",
                        columns: x => new { x.InventoryLotId, x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryLots",
                        principalColumns: new[] { "Id", "InventoryItemId", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockPositions_Stores_StoreId_FarmId",
                        columns: x => new { x.StoreId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Stores",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockReceiptLines",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ExpiryDateSnapshot = table.Column<DateOnly>(type: "date", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    UnitCostUsd = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    LineValueUsd = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReceiptLines", x => x.Id);
                    table.UniqueConstraint("AK_StockReceiptLines_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_StockReceiptLines_NonnegativeCost", "\"UnitCostUsd\" >= 0 AND \"LineValueUsd\" >= 0");
                    table.CheckConstraint("CK_StockReceiptLines_PositiveQuantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_StockReceiptLines_InventoryItems_InventoryItemId_TenantId_F~",
                        columns: x => new { x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryItems",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReceiptLines_InventoryLots_InventoryLotId_InventoryIte~",
                        columns: x => new { x.InventoryLotId, x.InventoryItemId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "InventoryLots",
                        principalColumns: new[] { "Id", "InventoryItemId", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReceiptLines_StockReceipts_StockReceiptId_TenantId_Far~",
                        columns: x => new { x.StockReceiptId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockReceipts",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReceiptLines_UnitOfMeasures_UnitOfMeasureId_TenantId",
                        columns: x => new { x.UnitOfMeasureId, x.TenantId },
                        principalSchema: "inventory",
                        principalTable: "UnitOfMeasures",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockPositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LotCodeSnapshot = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    UnitCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MovementType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SignedQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    SignedValueUsd = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    OperationalPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostingSequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    PostingIdentity = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StockReceiptLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReversalOfStockMovementId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.UniqueConstraint("AK_StockMovements_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_StockMovements_NonzeroQuantity", "\"SignedQuantity\" <> 0");
                    table.CheckConstraint("CK_StockMovements_Reversal", "(\"MovementType\" = 'ReceiptReversal') = (\"ReversalOfStockMovementId\" IS NOT NULL)");
                    table.CheckConstraint("CK_StockMovements_Signs", "sign(\"SignedQuantity\") = sign(\"SignedValueUsd\") OR \"SignedValueUsd\" = 0");
                    table.ForeignKey(
                        name: "FK_StockMovements_AspNetUsers_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Persons_OperationalPersonId_FarmId",
                        columns: x => new { x.OperationalPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_StockMovements_ReversalOfStockMovementId",
                        column: x => x.ReversalOfStockMovementId,
                        principalSchema: "inventory",
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_StockPositions_StockPositionId_TenantId_Farm~",
                        columns: x => new { x.StockPositionId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockPositions",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_StockReceiptLines_StockReceiptLineId_TenantI~",
                        columns: x => new { x.StockReceiptLineId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockReceiptLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CorrectionRecords",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalStockReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalStockMovementId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrectingStockMovementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AuthorisedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AuthorisedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrectionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorrectionRecords_AspNetUsers_AuthorisedByUserId",
                        column: x => x.AuthorisedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorrectionRecords_StockMovements_CorrectingStockMovementId_~",
                        columns: x => new { x.CorrectingStockMovementId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockMovements",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorrectionRecords_StockMovements_OriginalStockMovementId_Te~",
                        columns: x => new { x.OriginalStockMovementId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockMovements",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CorrectionRecords_StockReceipts_OriginalStockReceiptId_Tena~",
                        columns: x => new { x.OriginalStockReceiptId, x.TenantId, x.FarmId },
                        principalSchema: "inventory",
                        principalTable: "StockReceipts",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_ApproverUserId",
                schema: "inventory",
                table: "ApprovalDecisions",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_IdempotencyKey",
                schema: "inventory",
                table: "ApprovalDecisions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_StockReceiptId_SubjectVersion",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "StockReceiptId", "SubjectVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisions_StockReceiptId_TenantId_FarmId",
                schema: "inventory",
                table: "ApprovalDecisions",
                columns: new[] { "StockReceiptId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRecords_AuthorisedByUserId",
                schema: "inventory",
                table: "CorrectionRecords",
                column: "AuthorisedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRecords_CorrectingStockMovementId",
                schema: "inventory",
                table: "CorrectionRecords",
                column: "CorrectingStockMovementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRecords_CorrectingStockMovementId_TenantId_FarmId",
                schema: "inventory",
                table: "CorrectionRecords",
                columns: new[] { "CorrectingStockMovementId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRecords_OriginalStockMovementId",
                schema: "inventory",
                table: "CorrectionRecords",
                column: "OriginalStockMovementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRecords_OriginalStockMovementId_TenantId_FarmId",
                schema: "inventory",
                table: "CorrectionRecords",
                columns: new[] { "OriginalStockMovementId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRecords_OriginalStockReceiptId",
                schema: "inventory",
                table: "CorrectionRecords",
                column: "OriginalStockReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectionRecords_OriginalStockReceiptId_TenantId_FarmId",
                schema: "inventory",
                table: "CorrectionRecords",
                columns: new[] { "OriginalStockReceiptId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_AuditEventId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                column: "AuditEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_AuditEventId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "AuditEventId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_InventoryItemId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_InventoryLotId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "InventoryLotId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_StockReceiptId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "StockReceiptId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_SupplierId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "SupplierId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_TenantId_FarmId_StockReceiptId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "TenantId", "FarmId", "StockReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditEventLinks_UnitOfMeasureId_TenantId",
                schema: "inventory",
                table: "InventoryAuditEventLinks",
                columns: new[] { "UnitOfMeasureId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_FarmId_Code",
                schema: "inventory",
                table: "InventoryItems",
                columns: new[] { "FarmId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_FarmId_TenantId",
                schema: "inventory",
                table: "InventoryItems",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_StockUnitId_TenantId",
                schema: "inventory",
                table: "InventoryItems",
                columns: new[] { "StockUnitId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_TenantId_FarmId_Status",
                schema: "inventory",
                table: "InventoryItems",
                columns: new[] { "TenantId", "FarmId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_InventoryItemId_Code",
                schema: "inventory",
                table: "InventoryLots",
                columns: new[] { "InventoryItemId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLots_InventoryItemId_TenantId_FarmId",
                schema: "inventory",
                table: "InventoryLots",
                columns: new[] { "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_OperationalPersonId_FarmId",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "OperationalPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_PostedByUserId",
                schema: "inventory",
                table: "StockMovements",
                column: "PostedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_PostingIdentity",
                schema: "inventory",
                table: "StockMovements",
                column: "PostingIdentity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ReversalOfStockMovementId",
                schema: "inventory",
                table: "StockMovements",
                column: "ReversalOfStockMovementId",
                unique: true,
                filter: "\"ReversalOfStockMovementId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockPositionId_PostingSequence",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "StockPositionId", "PostingSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockPositionId_TenantId_FarmId",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "StockPositionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockReceiptLineId",
                schema: "inventory",
                table: "StockMovements",
                column: "StockReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockReceiptLineId_TenantId_FarmId",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "StockReceiptLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_FarmId_StoreId_InventoryItemId_Inve~",
                schema: "inventory",
                table: "StockMovements",
                columns: new[] { "TenantId", "FarmId", "StoreId", "InventoryItemId", "InventoryLotId", "PostingSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPositions_FarmId_TenantId",
                schema: "inventory",
                table: "StockPositions",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPositions_InventoryItemId_TenantId_FarmId",
                schema: "inventory",
                table: "StockPositions",
                columns: new[] { "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPositions_InventoryLotId_InventoryItemId_TenantId_Farm~",
                schema: "inventory",
                table: "StockPositions",
                columns: new[] { "InventoryLotId", "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPositions_StoreId_FarmId",
                schema: "inventory",
                table: "StockPositions",
                columns: new[] { "StoreId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockPositions_StoreId_InventoryItemId_PositionKey",
                schema: "inventory",
                table: "StockPositions",
                columns: new[] { "StoreId", "InventoryItemId", "PositionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptLines_InventoryItemId_TenantId_FarmId",
                schema: "inventory",
                table: "StockReceiptLines",
                columns: new[] { "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptLines_InventoryLotId_InventoryItemId_TenantId_F~",
                schema: "inventory",
                table: "StockReceiptLines",
                columns: new[] { "InventoryLotId", "InventoryItemId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptLines_StockReceiptId_LineNumber",
                schema: "inventory",
                table: "StockReceiptLines",
                columns: new[] { "StockReceiptId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptLines_StockReceiptId_TenantId_FarmId",
                schema: "inventory",
                table: "StockReceiptLines",
                columns: new[] { "StockReceiptId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptLines_UnitOfMeasureId_TenantId",
                schema: "inventory",
                table: "StockReceiptLines",
                columns: new[] { "UnitOfMeasureId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_CorrectsStockReceiptId",
                schema: "inventory",
                table: "StockReceipts",
                column: "CorrectsStockReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_FarmId_Status_ReceiptDate",
                schema: "inventory",
                table: "StockReceipts",
                columns: new[] { "FarmId", "Status", "ReceiptDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_FarmId_SupplierId_SourceReference",
                schema: "inventory",
                table: "StockReceipts",
                columns: new[] { "FarmId", "SupplierId", "SourceReference" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_FarmId_TenantId",
                schema: "inventory",
                table: "StockReceipts",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_PostedByUserId",
                schema: "inventory",
                table: "StockReceipts",
                column: "PostedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_PostingIdempotencyKey",
                schema: "inventory",
                table: "StockReceipts",
                column: "PostingIdempotencyKey",
                unique: true,
                filter: "\"PostingIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_ReceivedByPersonId_FarmId",
                schema: "inventory",
                table: "StockReceipts",
                columns: new[] { "ReceivedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_ReversalIdempotencyKey",
                schema: "inventory",
                table: "StockReceipts",
                column: "ReversalIdempotencyKey",
                unique: true,
                filter: "\"ReversalIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_ReversedByUserId",
                schema: "inventory",
                table: "StockReceipts",
                column: "ReversedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_StoreId_FarmId",
                schema: "inventory",
                table: "StockReceipts",
                columns: new[] { "StoreId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_SupplierId_TenantId_FarmId",
                schema: "inventory",
                table: "StockReceipts",
                columns: new[] { "SupplierId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_FarmId_Code",
                schema: "inventory",
                table: "Suppliers",
                columns: new[] { "FarmId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_FarmId_TenantId",
                schema: "inventory",
                table: "Suppliers",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitOfMeasures_TenantId_Code",
                schema: "inventory",
                table: "UnitOfMeasures",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION inventory."RejectAppendOnlyMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    RAISE EXCEPTION 'Inventory ledger, approval, correction, and audit-link records are append-only.';
                END;
                $function$;

                CREATE TRIGGER "TR_StockMovements_AppendOnly"
                BEFORE UPDATE OR DELETE ON inventory."StockMovements"
                FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();

                CREATE TRIGGER "TR_ApprovalDecisions_AppendOnly"
                BEFORE UPDATE OR DELETE ON inventory."ApprovalDecisions"
                FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();

                CREATE TRIGGER "TR_CorrectionRecords_AppendOnly"
                BEFORE UPDATE OR DELETE ON inventory."CorrectionRecords"
                FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();

                CREATE TRIGGER "TR_InventoryAuditEventLinks_AppendOnly"
                BEFORE UPDATE OR DELETE ON inventory."InventoryAuditEventLinks"
                FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_StockMovements_AppendOnly" ON inventory."StockMovements";
                DROP TRIGGER IF EXISTS "TR_ApprovalDecisions_AppendOnly" ON inventory."ApprovalDecisions";
                DROP TRIGGER IF EXISTS "TR_CorrectionRecords_AppendOnly" ON inventory."CorrectionRecords";
                DROP TRIGGER IF EXISTS "TR_InventoryAuditEventLinks_AppendOnly" ON inventory."InventoryAuditEventLinks";
                DROP FUNCTION IF EXISTS inventory."RejectAppendOnlyMutation"();
                """);

            migrationBuilder.DropTable(
                name: "ApprovalDecisions",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "CorrectionRecords",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryAuditEventLinks",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockMovements",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockPositions",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockReceiptLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryLots",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "StockReceipts",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "InventoryItems",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "UnitOfMeasures",
                schema: "inventory");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Stores_Id_FarmId",
                schema: "farm",
                table: "Stores");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AuditEvents_Id_TenantId_FarmId",
                schema: "audit",
                table: "AuditEvents");
        }
    }
}
