using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMillRecordsAndStatementReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mill");

            migrationBuilder.CreateTable(
                name: "MillRecordExports",
                schema: "mill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Filters = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MillRecordExports", x => x.Id);
                    table.UniqueConstraint("AK_MillRecordExports_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.ForeignKey(
                        name: "FK_MillRecordExports_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MillRecordExports_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mills",
                schema: "mill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mills", x => x.Id);
                    table.UniqueConstraint("AK_Mills_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.ForeignKey(
                        name: "FK_Mills_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mills_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GrowerStatements",
                schema: "mill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    MillId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalTonnes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    TotalAmountUsd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RecordingIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CorrectsStatementId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowerStatements", x => x.Id);
                    table.UniqueConstraint("AK_GrowerStatements_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_GrowerStatements_Correction", "(\"CorrectsStatementId\" IS NULL AND \"CorrectionReason\" IS NULL) OR (\"CorrectsStatementId\" IS NOT NULL AND \"CorrectionReason\" IS NOT NULL)");
                    table.CheckConstraint("CK_GrowerStatements_Lifecycle", "(\"Status\" = 'Draft' AND \"RecordedByUserId\" IS NULL AND \"RecordedAt\" IS NULL AND \"RecordingIdempotencyKey\" IS NULL) OR (\"Status\" = 'Recorded' AND \"RecordedByUserId\" IS NOT NULL AND \"RecordedAt\" IS NOT NULL AND \"RecordingIdempotencyKey\" IS NOT NULL)");
                    table.CheckConstraint("CK_GrowerStatements_Period", "\"PeriodEnd\" >= \"PeriodStart\"");
                    table.CheckConstraint("CK_GrowerStatements_Totals", "\"TotalTonnes\" >= 0 AND \"TotalAmountUsd\" >= 0");
                    table.ForeignKey(
                        name: "FK_GrowerStatements_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GrowerStatements_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GrowerStatements_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GrowerStatements_GrowerStatements_CorrectsStatementId_Tenan~",
                        columns: x => new { x.CorrectsStatementId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "GrowerStatements",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GrowerStatements_Mills_MillId_TenantId_FarmId",
                        columns: x => new { x.MillId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "Mills",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeighbridgeTickets",
                schema: "mill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    MillId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TicketDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GrossTonnes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    TareTonnes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    NetTonnes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RecordingIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CorrectsTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeighbridgeTickets", x => x.Id);
                    table.UniqueConstraint("AK_WeighbridgeTickets_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_WeighbridgeTickets_Correction", "(\"CorrectsTicketId\" IS NULL AND \"CorrectionReason\" IS NULL) OR (\"CorrectsTicketId\" IS NOT NULL AND \"CorrectionReason\" IS NOT NULL)");
                    table.CheckConstraint("CK_WeighbridgeTickets_Lifecycle", "(\"Status\" = 'Draft' AND \"RecordedByUserId\" IS NULL AND \"RecordedAt\" IS NULL AND \"RecordingIdempotencyKey\" IS NULL) OR (\"Status\" = 'Recorded' AND \"RecordedByUserId\" IS NOT NULL AND \"RecordedAt\" IS NOT NULL AND \"RecordingIdempotencyKey\" IS NOT NULL)");
                    table.CheckConstraint("CK_WeighbridgeTickets_Weights", "\"GrossTonnes\" >= 0 AND \"NetTonnes\" > 0 AND (\"TareTonnes\" IS NULL OR (\"TareTonnes\" >= 0 AND \"GrossTonnes\" >= \"TareTonnes\" AND \"NetTonnes\" = \"GrossTonnes\" - \"TareTonnes\"))");
                    table.ForeignKey(
                        name: "FK_WeighbridgeTickets_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeighbridgeTickets_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeighbridgeTickets_CropCycles_CropCycleId_FieldId",
                        columns: x => new { x.CropCycleId, x.FieldId },
                        principalSchema: "farm",
                        principalTable: "CropCycles",
                        principalColumns: new[] { "Id", "FieldId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeighbridgeTickets_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeighbridgeTickets_Fields_FieldId_FarmId",
                        columns: x => new { x.FieldId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Fields",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeighbridgeTickets_Mills_MillId_TenantId_FarmId",
                        columns: x => new { x.MillId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "Mills",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeighbridgeTickets_WeighbridgeTickets_CorrectsTicketId_Tena~",
                        columns: x => new { x.CorrectsTicketId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "WeighbridgeTickets",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceDocuments",
                schema: "mill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeighbridgeTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrowerStatementId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    UploadedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceDocuments", x => x.Id);
                    table.UniqueConstraint("AK_EvidenceDocuments_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_EvidenceDocuments_OneSubject", "num_nonnulls(\"WeighbridgeTicketId\", \"GrowerStatementId\") = 1");
                    table.CheckConstraint("CK_EvidenceDocuments_Size", "\"SizeBytes\" > 0");
                    table.ForeignKey(
                        name: "FK_EvidenceDocuments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceDocuments_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceDocuments_GrowerStatements_GrowerStatementId_Tenant~",
                        columns: x => new { x.GrowerStatementId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "GrowerStatements",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvidenceDocuments_WeighbridgeTickets_WeighbridgeTicketId_Te~",
                        columns: x => new { x.WeighbridgeTicketId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "WeighbridgeTickets",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StatementTicketMatches",
                schema: "mill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrowerStatementId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeighbridgeTicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchedTonnes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    MatchedAmountUsd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    CompletesMatching = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ReversesMatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementTicketMatches", x => x.Id);
                    table.UniqueConstraint("AK_StatementTicketMatches_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_StatementTicketMatches_Action", "(\"Action\" = 'Added' AND \"ReversesMatchId\" IS NULL) OR (\"Action\" = 'Reversed' AND \"ReversesMatchId\" IS NOT NULL AND \"Reason\" IS NOT NULL)");
                    table.CheckConstraint("CK_StatementTicketMatches_Quantities", "\"MatchedTonnes\" > 0 AND (\"MatchedAmountUsd\" IS NULL OR \"MatchedAmountUsd\" >= 0)");
                    table.ForeignKey(
                        name: "FK_StatementTicketMatches_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatementTicketMatches_GrowerStatements_GrowerStatementId_T~",
                        columns: x => new { x.GrowerStatementId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "GrowerStatements",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatementTicketMatches_StatementTicketMatches_ReversesMatch~",
                        columns: x => new { x.ReversesMatchId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "StatementTicketMatches",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatementTicketMatches_WeighbridgeTickets_WeighbridgeTicket~",
                        columns: x => new { x.WeighbridgeTicketId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "WeighbridgeTickets",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MillRecordAuditEventLinks",
                schema: "mill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    MillId = table.Column<Guid>(type: "uuid", nullable: true),
                    WeighbridgeTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrowerStatementId = table.Column<Guid>(type: "uuid", nullable: true),
                    StatementTicketMatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvidenceDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    MillRecordExportId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MillRecordAuditEventLinks", x => x.Id);
                    table.CheckConstraint("CK_MillRecordAuditEventLinks_OneSubject", "num_nonnulls(\"MillId\", \"WeighbridgeTicketId\", \"GrowerStatementId\", \"StatementTicketMatchId\", \"EvidenceDocumentId\", \"MillRecordExportId\") = 1");
                    table.ForeignKey(
                        name: "FK_MillRecordAuditEventLinks_AuditEvents_AuditEventId_TenantId~",
                        columns: x => new { x.AuditEventId, x.TenantId, x.FarmId },
                        principalSchema: "audit",
                        principalTable: "AuditEvents",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MillRecordAuditEventLinks_EvidenceDocuments_EvidenceDocumen~",
                        columns: x => new { x.EvidenceDocumentId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "EvidenceDocuments",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MillRecordAuditEventLinks_GrowerStatements_GrowerStatementI~",
                        columns: x => new { x.GrowerStatementId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "GrowerStatements",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MillRecordAuditEventLinks_MillRecordExports_MillRecordExpor~",
                        columns: x => new { x.MillRecordExportId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "MillRecordExports",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MillRecordAuditEventLinks_Mills_MillId_TenantId_FarmId",
                        columns: x => new { x.MillId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "Mills",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MillRecordAuditEventLinks_StatementTicketMatches_StatementT~",
                        columns: x => new { x.StatementTicketMatchId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "StatementTicketMatches",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MillRecordAuditEventLinks_WeighbridgeTickets_WeighbridgeTic~",
                        columns: x => new { x.WeighbridgeTicketId, x.TenantId, x.FarmId },
                        principalSchema: "mill",
                        principalTable: "WeighbridgeTickets",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_FarmId_TenantId",
                schema: "mill",
                table: "EvidenceDocuments",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_GrowerStatementId_TenantId_FarmId",
                schema: "mill",
                table: "EvidenceDocuments",
                columns: new[] { "GrowerStatementId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_StorageKey",
                schema: "mill",
                table: "EvidenceDocuments",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_UploadedByUserId",
                schema: "mill",
                table: "EvidenceDocuments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_WeighbridgeTicketId_TenantId_FarmId",
                schema: "mill",
                table: "EvidenceDocuments",
                columns: new[] { "WeighbridgeTicketId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_GrowerStatements_CorrectsStatementId_TenantId_FarmId",
                schema: "mill",
                table: "GrowerStatements",
                columns: new[] { "CorrectsStatementId", "TenantId", "FarmId" },
                unique: true,
                filter: "\"CorrectsStatementId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GrowerStatements_CreatedByUserId",
                schema: "mill",
                table: "GrowerStatements",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GrowerStatements_FarmId_TenantId",
                schema: "mill",
                table: "GrowerStatements",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_GrowerStatements_MillId_TenantId_FarmId",
                schema: "mill",
                table: "GrowerStatements",
                columns: new[] { "MillId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_GrowerStatements_RecordedByUserId",
                schema: "mill",
                table: "GrowerStatements",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GrowerStatements_TenantId_FarmId_MillId_StatementReference",
                schema: "mill",
                table: "GrowerStatements",
                columns: new[] { "TenantId", "FarmId", "MillId", "StatementReference" },
                unique: true,
                filter: "\"CorrectsStatementId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GrowerStatements_TenantId_FarmId_PeriodStart_PeriodEnd",
                schema: "mill",
                table: "GrowerStatements",
                columns: new[] { "TenantId", "FarmId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_GrowerStatements_TenantId_FarmId_RecordingIdempotencyKey",
                schema: "mill",
                table: "GrowerStatements",
                columns: new[] { "TenantId", "FarmId", "RecordingIdempotencyKey" },
                unique: true,
                filter: "\"RecordingIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MillRecordAuditEventLinks_AuditEventId_TenantId_FarmId",
                schema: "mill",
                table: "MillRecordAuditEventLinks",
                columns: new[] { "AuditEventId", "TenantId", "FarmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MillRecordAuditEventLinks_EvidenceDocumentId_TenantId_FarmId",
                schema: "mill",
                table: "MillRecordAuditEventLinks",
                columns: new[] { "EvidenceDocumentId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_MillRecordAuditEventLinks_GrowerStatementId_TenantId_FarmId",
                schema: "mill",
                table: "MillRecordAuditEventLinks",
                columns: new[] { "GrowerStatementId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_MillRecordAuditEventLinks_MillId_TenantId_FarmId",
                schema: "mill",
                table: "MillRecordAuditEventLinks",
                columns: new[] { "MillId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_MillRecordAuditEventLinks_MillRecordExportId_TenantId_FarmId",
                schema: "mill",
                table: "MillRecordAuditEventLinks",
                columns: new[] { "MillRecordExportId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_MillRecordAuditEventLinks_StatementTicketMatchId_TenantId_F~",
                schema: "mill",
                table: "MillRecordAuditEventLinks",
                columns: new[] { "StatementTicketMatchId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_MillRecordAuditEventLinks_WeighbridgeTicketId_TenantId_Farm~",
                schema: "mill",
                table: "MillRecordAuditEventLinks",
                columns: new[] { "WeighbridgeTicketId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_MillRecordExports_CreatedByUserId",
                schema: "mill",
                table: "MillRecordExports",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MillRecordExports_FarmId_TenantId",
                schema: "mill",
                table: "MillRecordExports",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_MillRecordExports_TenantId_FarmId_CreatedAt",
                schema: "mill",
                table: "MillRecordExports",
                columns: new[] { "TenantId", "FarmId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Mills_CreatedByUserId",
                schema: "mill",
                table: "Mills",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Mills_FarmId_TenantId",
                schema: "mill",
                table: "Mills",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Mills_TenantId_FarmId_Code",
                schema: "mill",
                table: "Mills",
                columns: new[] { "TenantId", "FarmId", "Code" },
                unique: true,
                filter: "\"Active\"");

            migrationBuilder.CreateIndex(
                name: "IX_StatementTicketMatches_CreatedByUserId",
                schema: "mill",
                table: "StatementTicketMatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StatementTicketMatches_GrowerStatementId_TenantId_FarmId",
                schema: "mill",
                table: "StatementTicketMatches",
                columns: new[] { "GrowerStatementId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_StatementTicketMatches_ReversesMatchId_TenantId_FarmId",
                schema: "mill",
                table: "StatementTicketMatches",
                columns: new[] { "ReversesMatchId", "TenantId", "FarmId" },
                unique: true,
                filter: "\"ReversesMatchId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StatementTicketMatches_TenantId_FarmId_GrowerStatementId_We~",
                schema: "mill",
                table: "StatementTicketMatches",
                columns: new[] { "TenantId", "FarmId", "GrowerStatementId", "WeighbridgeTicketId" });

            migrationBuilder.CreateIndex(
                name: "IX_StatementTicketMatches_TenantId_FarmId_IdempotencyKey",
                schema: "mill",
                table: "StatementTicketMatches",
                columns: new[] { "TenantId", "FarmId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatementTicketMatches_WeighbridgeTicketId_TenantId_FarmId",
                schema: "mill",
                table: "StatementTicketMatches",
                columns: new[] { "WeighbridgeTicketId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WeighbridgeTickets_CorrectsTicketId_TenantId_FarmId",
                schema: "mill",
                table: "WeighbridgeTickets",
                columns: new[] { "CorrectsTicketId", "TenantId", "FarmId" },
                unique: true,
                filter: "\"CorrectsTicketId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WeighbridgeTickets_CreatedByUserId",
                schema: "mill",
                table: "WeighbridgeTickets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeighbridgeTickets_CropCycleId_FieldId",
                schema: "mill",
                table: "WeighbridgeTickets",
                columns: new[] { "CropCycleId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_WeighbridgeTickets_FarmId_TenantId",
                schema: "mill",
                table: "WeighbridgeTickets",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_WeighbridgeTickets_FieldId_FarmId",
                schema: "mill",
                table: "WeighbridgeTickets",
                columns: new[] { "FieldId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WeighbridgeTickets_MillId_TenantId_FarmId",
                schema: "mill",
                table: "WeighbridgeTickets",
                columns: new[] { "MillId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WeighbridgeTickets_RecordedByUserId",
                schema: "mill",
                table: "WeighbridgeTickets",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeighbridgeTickets_TenantId_FarmId_MillId_TicketReference",
                schema: "mill",
                table: "WeighbridgeTickets",
                columns: new[] { "TenantId", "FarmId", "MillId", "TicketReference" },
                unique: true,
                filter: "\"CorrectsTicketId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WeighbridgeTickets_TenantId_FarmId_RecordingIdempotencyKey",
                schema: "mill",
                table: "WeighbridgeTickets",
                columns: new[] { "TenantId", "FarmId", "RecordingIdempotencyKey" },
                unique: true,
                filter: "\"RecordingIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WeighbridgeTickets_TenantId_FarmId_TicketDate",
                schema: "mill",
                table: "WeighbridgeTickets",
                columns: new[] { "TenantId", "FarmId", "TicketDate" });

            migrationBuilder.Sql("""
                ALTER TABLE mill."Mills"
                    ADD CONSTRAINT "CK_Mills_NormalizedCode"
                    CHECK ("Code" = upper(btrim("Code")));
                ALTER TABLE mill."WeighbridgeTickets"
                    ADD CONSTRAINT "CK_WeighbridgeTickets_NormalizedReference"
                    CHECK ("TicketReference" = upper(btrim("TicketReference")));
                ALTER TABLE mill."GrowerStatements"
                    ADD CONSTRAINT "CK_GrowerStatements_NormalizedReference"
                    CHECK ("StatementReference" = upper(btrim("StatementReference")));

                CREATE FUNCTION mill."reject_authoritative_mutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    RAISE EXCEPTION '% records are append-only', TG_TABLE_NAME USING ERRCODE = '23514';
                END;
                $function$;

                CREATE FUNCTION mill."protect_mill_reference"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Mill references cannot be hard deleted' USING ERRCODE = '23514';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE FUNCTION mill."protect_weighbridge_ticket"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    original mill."WeighbridgeTickets"%ROWTYPE;
                BEGIN
                    IF TG_OP IN ('UPDATE', 'DELETE') AND OLD."Status" = 'Recorded' THEN
                        RAISE EXCEPTION 'Recorded weighbridge tickets are immutable' USING ERRCODE = '23514';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(
                        NEW."TenantId"::text || ':' || NEW."FarmId"::text || ':' ||
                        NEW."MillId"::text || ':' || NEW."TicketReference", 0));
                    IF NEW."CorrectsTicketId" IS NOT NULL THEN
                        SELECT * INTO original FROM mill."WeighbridgeTickets"
                        WHERE "Id" = NEW."CorrectsTicketId" AND "TenantId" = NEW."TenantId"
                          AND "FarmId" = NEW."FarmId" FOR KEY SHARE;
                        IF NOT FOUND OR original."Status" <> 'Recorded' OR original."MillId" <> NEW."MillId" THEN
                            RAISE EXCEPTION 'Ticket correction lineage must target a recorded ticket for the same mill and scope' USING ERRCODE = '23514';
                        END IF;
                    END IF;
                    IF TG_OP IN ('INSERT', 'UPDATE') AND EXISTS (
                        SELECT 1 FROM mill."WeighbridgeTickets" candidate
                        WHERE candidate."TenantId" = NEW."TenantId"
                          AND candidate."FarmId" = NEW."FarmId"
                          AND candidate."MillId" = NEW."MillId"
                          AND candidate."TicketReference" = NEW."TicketReference"
                          AND candidate."Id" <> NEW."Id"
                          AND candidate."Id" <> COALESCE(NEW."CorrectsTicketId", '00000000-0000-0000-0000-000000000000'::uuid)
                          AND NOT EXISTS (
                              SELECT 1 FROM mill."WeighbridgeTickets" replacement
                              WHERE replacement."CorrectsTicketId" = candidate."Id"
                                AND replacement."Status" = 'Recorded')) THEN
                        RAISE EXCEPTION 'An active ticket reference already exists for this mill' USING ERRCODE = '23505';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE FUNCTION mill."protect_grower_statement"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    original mill."GrowerStatements"%ROWTYPE;
                BEGIN
                    IF TG_OP IN ('UPDATE', 'DELETE') AND OLD."Status" = 'Recorded' THEN
                        RAISE EXCEPTION 'Recorded grower statements are immutable' USING ERRCODE = '23514';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    IF TG_OP = 'INSERT' AND NEW."Status" <> 'Draft' THEN
                        RAISE EXCEPTION 'Grower statements must be inserted as drafts so original evidence can be linked before recording' USING ERRCODE = '23514';
                    END IF;
                    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(
                        NEW."TenantId"::text || ':' || NEW."FarmId"::text || ':' ||
                        NEW."MillId"::text || ':' || NEW."StatementReference", 0));
                    IF NEW."CorrectsStatementId" IS NOT NULL THEN
                        SELECT * INTO original FROM mill."GrowerStatements"
                        WHERE "Id" = NEW."CorrectsStatementId" AND "TenantId" = NEW."TenantId"
                          AND "FarmId" = NEW."FarmId" FOR KEY SHARE;
                        IF NOT FOUND OR original."Status" <> 'Recorded' OR original."MillId" <> NEW."MillId" THEN
                            RAISE EXCEPTION 'Statement correction lineage must target a recorded statement for the same mill and scope' USING ERRCODE = '23514';
                        END IF;
                    END IF;
                    IF TG_OP = 'UPDATE' AND OLD."Status" = 'Draft' AND NEW."Status" = 'Recorded'
                       AND NOT EXISTS (
                           SELECT 1 FROM mill."EvidenceDocuments" evidence
                           WHERE evidence."GrowerStatementId" = NEW."Id"
                             AND evidence."TenantId" = NEW."TenantId"
                             AND evidence."FarmId" = NEW."FarmId") THEN
                        RAISE EXCEPTION 'Original statement evidence is required before recording' USING ERRCODE = '23514';
                    END IF;
                    IF TG_OP IN ('INSERT', 'UPDATE') AND EXISTS (
                        SELECT 1 FROM mill."GrowerStatements" candidate
                        WHERE candidate."TenantId" = NEW."TenantId"
                          AND candidate."FarmId" = NEW."FarmId"
                          AND candidate."MillId" = NEW."MillId"
                          AND candidate."StatementReference" = NEW."StatementReference"
                          AND candidate."Id" <> NEW."Id"
                          AND candidate."Id" <> COALESCE(NEW."CorrectsStatementId", '00000000-0000-0000-0000-000000000000'::uuid)
                          AND NOT EXISTS (
                              SELECT 1 FROM mill."GrowerStatements" replacement
                              WHERE replacement."CorrectsStatementId" = candidate."Id"
                                AND replacement."Status" = 'Recorded')) THEN
                        RAISE EXCEPTION 'An active statement reference already exists for this mill' USING ERRCODE = '23505';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE FUNCTION mill."validate_statement_ticket_match"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    statement_record mill."GrowerStatements"%ROWTYPE;
                    ticket_record mill."WeighbridgeTickets"%ROWTYPE;
                    original_match mill."StatementTicketMatches"%ROWTYPE;
                    active_tonnes numeric(14,3);
                BEGIN
                    PERFORM pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(
                        NEW."TenantId"::text || ':' || NEW."FarmId"::text || ':' ||
                        NEW."WeighbridgeTicketId"::text, 0));
                    SELECT * INTO statement_record FROM mill."GrowerStatements"
                    WHERE "Id" = NEW."GrowerStatementId" AND "TenantId" = NEW."TenantId"
                      AND "FarmId" = NEW."FarmId" FOR KEY SHARE;
                    SELECT * INTO ticket_record FROM mill."WeighbridgeTickets"
                    WHERE "Id" = NEW."WeighbridgeTicketId" AND "TenantId" = NEW."TenantId"
                      AND "FarmId" = NEW."FarmId" FOR KEY SHARE;
                    IF statement_record."Status" <> 'Recorded' OR ticket_record."Status" <> 'Recorded'
                       OR statement_record."MillId" <> ticket_record."MillId" THEN
                        RAISE EXCEPTION 'Matches require recorded statement and ticket evidence for the same mill and scope' USING ERRCODE = '23514';
                    END IF;
                    IF NEW."Action" = 'Reversed' THEN
                        SELECT * INTO original_match FROM mill."StatementTicketMatches"
                        WHERE "Id" = NEW."ReversesMatchId" AND "TenantId" = NEW."TenantId"
                          AND "FarmId" = NEW."FarmId" FOR KEY SHARE;
                        IF NOT FOUND OR original_match."Action" <> 'Added'
                           OR original_match."GrowerStatementId" <> NEW."GrowerStatementId"
                           OR original_match."WeighbridgeTicketId" <> NEW."WeighbridgeTicketId"
                           OR original_match."MatchedTonnes" <> NEW."MatchedTonnes"
                           OR original_match."MatchedAmountUsd" IS DISTINCT FROM NEW."MatchedAmountUsd" THEN
                            RAISE EXCEPTION 'Match reversal must exactly identify its original added fact' USING ERRCODE = '23514';
                        END IF;
                        RETURN NEW;
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM mill."StatementTicketMatches" active
                        WHERE active."GrowerStatementId" = NEW."GrowerStatementId"
                          AND active."WeighbridgeTicketId" = NEW."WeighbridgeTicketId"
                          AND active."TenantId" = NEW."TenantId" AND active."FarmId" = NEW."FarmId"
                          AND active."Action" = 'Added'
                          AND NOT EXISTS (SELECT 1 FROM mill."StatementTicketMatches" reversal
                              WHERE reversal."ReversesMatchId" = active."Id")) THEN
                        RAISE EXCEPTION 'The ticket is already active in this statement' USING ERRCODE = '23505';
                    END IF;
                    SELECT COALESCE(sum(active."MatchedTonnes"), 0) INTO active_tonnes
                    FROM mill."StatementTicketMatches" active
                    WHERE active."WeighbridgeTicketId" = NEW."WeighbridgeTicketId"
                      AND active."TenantId" = NEW."TenantId" AND active."FarmId" = NEW."FarmId"
                      AND active."Action" = 'Added'
                      AND NOT EXISTS (SELECT 1 FROM mill."StatementTicketMatches" reversal
                          WHERE reversal."ReversesMatchId" = active."Id");
                    IF active_tonnes + NEW."MatchedTonnes" > ticket_record."NetTonnes" THEN
                        RAISE EXCEPTION 'Active matched tonnes exceed the ticket net-tonnage capacity' USING ERRCODE = '23514';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER "TR_Mills_NoDelete"
                    BEFORE DELETE ON mill."Mills"
                    FOR EACH ROW EXECUTE FUNCTION mill."protect_mill_reference"();
                CREATE TRIGGER "TR_WeighbridgeTickets_ProtectAuthoritative"
                    BEFORE INSERT OR UPDATE OR DELETE ON mill."WeighbridgeTickets"
                    FOR EACH ROW EXECUTE FUNCTION mill."protect_weighbridge_ticket"();
                CREATE TRIGGER "TR_GrowerStatements_ProtectAuthoritative"
                    BEFORE INSERT OR UPDATE OR DELETE ON mill."GrowerStatements"
                    FOR EACH ROW EXECUTE FUNCTION mill."protect_grower_statement"();
                CREATE TRIGGER "TR_StatementTicketMatches_ValidateInsert"
                    BEFORE INSERT ON mill."StatementTicketMatches"
                    FOR EACH ROW EXECUTE FUNCTION mill."validate_statement_ticket_match"();
                CREATE TRIGGER "TR_StatementTicketMatches_AppendOnly"
                    BEFORE UPDATE OR DELETE ON mill."StatementTicketMatches"
                    FOR EACH ROW EXECUTE FUNCTION mill."reject_authoritative_mutation"();
                CREATE TRIGGER "TR_EvidenceDocuments_AppendOnly"
                    BEFORE UPDATE OR DELETE ON mill."EvidenceDocuments"
                    FOR EACH ROW EXECUTE FUNCTION mill."reject_authoritative_mutation"();
                CREATE TRIGGER "TR_MillRecordExports_AppendOnly"
                    BEFORE UPDATE OR DELETE ON mill."MillRecordExports"
                    FOR EACH ROW EXECUTE FUNCTION mill."reject_authoritative_mutation"();
                CREATE TRIGGER "TR_MillRecordAuditEventLinks_AppendOnly"
                    BEFORE UPDATE OR DELETE ON mill."MillRecordAuditEventLinks"
                    FOR EACH ROW EXECUTE FUNCTION mill."reject_authoritative_mutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MillRecordAuditEventLinks",
                schema: "mill");

            migrationBuilder.DropTable(
                name: "EvidenceDocuments",
                schema: "mill");

            migrationBuilder.DropTable(
                name: "MillRecordExports",
                schema: "mill");

            migrationBuilder.DropTable(
                name: "StatementTicketMatches",
                schema: "mill");

            migrationBuilder.DropTable(
                name: "GrowerStatements",
                schema: "mill");

            migrationBuilder.DropTable(
                name: "WeighbridgeTickets",
                schema: "mill");

            migrationBuilder.DropTable(
                name: "Mills",
                schema: "mill");

            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS mill."validate_statement_ticket_match"();
                DROP FUNCTION IF EXISTS mill."protect_grower_statement"();
                DROP FUNCTION IF EXISTS mill."protect_weighbridge_ticket"();
                DROP FUNCTION IF EXISTS mill."protect_mill_reference"();
                DROP FUNCTION IF EXISTS mill."reject_authoritative_mutation"();
                """);
        }
    }
}
