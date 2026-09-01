using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollPaymentsPayslipsAndSettlementClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PayrollAuditEventLinks_OneSubject",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentAcknowledgementId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayrollPaymentId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayrollPaymentReversalId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayrollSettlementClosureId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayrollSettlementReopenId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PayrollWorkerLines_Id_PayrollCalculationId_WorkerProfileId_~",
                schema: "payroll",
                table: "PayrollWorkerLines",
                columns: new[] { "Id", "PayrollCalculationId", "WorkerProfileId", "TenantId", "FarmId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_PayrollCalculations_Id_PayrollRunId_CalculationVersion_Tena~",
                schema: "payroll",
                table: "PayrollCalculations",
                columns: new[] { "Id", "PayrollRunId", "CalculationVersion", "TenantId", "FarmId" });

            migrationBuilder.CreateTable(
                name: "PayrollPayments",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollCalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationVersion = table.Column<int>(type: "integer", nullable: false),
                    PayrollWorkerLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExternalStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    RecipientCiphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    RecipientNonce = table.Column<byte[]>(type: "bytea", nullable: true),
                    RecipientTag = table.Column<byte[]>(type: "bytea", nullable: true),
                    RecipientKeyId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    MaskedRecipientNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TransactionReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    RecordedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RecordedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPayments", x => x.Id);
                    table.UniqueConstraint("AK_PayrollPayments_Id_PayrollRunId_PayrollCalculationId_Calcul~", x => new { x.Id, x.PayrollRunId, x.PayrollCalculationId, x.CalculationVersion, x.PayrollWorkerLineId, x.TenantId, x.FarmId });
                    table.UniqueConstraint("AK_PayrollPayments_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PayrollPayments_Amount", "\"AmountUsd\" > 0");
                    table.CheckConstraint("CK_PayrollPayments_Method", "(\"Method\" = 'Cash' AND \"ExternalStatus\" = 'Posted' AND \"Provider\" IS NULL AND \"RecipientCiphertext\" IS NULL AND \"TransactionReference\" IS NULL) OR (\"Method\" = 'MobileMoney' AND \"Provider\" IS NOT NULL AND \"RecipientCiphertext\" IS NOT NULL AND \"RecipientNonce\" IS NOT NULL AND \"RecipientTag\" IS NOT NULL AND \"RecipientKeyId\" IS NOT NULL AND \"MaskedRecipientNumber\" IS NOT NULL AND \"TransactionReference\" IS NOT NULL AND \"ExternalStatus\" IN ('Posted','Successful','Pending','Failed'))");
                    table.CheckConstraint("CK_PayrollPayments_ProtectedRecipient", "\"RecipientCiphertext\" IS NULL OR (octet_length(\"RecipientCiphertext\") > 0 AND octet_length(\"RecipientNonce\") = 12 AND octet_length(\"RecipientTag\") = 16)");
                    table.CheckConstraint("CK_PayrollPayments_Version", "\"CalculationVersion\" > 0");
                    table.ForeignKey(
                        name: "FK_PayrollPayments_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPayments_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPayments_PayrollCalculations_PayrollCalculationId_Pa~",
                        columns: x => new { x.PayrollCalculationId, x.PayrollRunId, x.CalculationVersion, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollCalculations",
                        principalColumns: new[] { "Id", "PayrollRunId", "CalculationVersion", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPayments_PayrollRuns_PayrollRunId_TenantId_FarmId",
                        columns: x => new { x.PayrollRunId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPayments_PayrollWorkerLines_PayrollWorkerLineId_Payr~",
                        columns: x => new { x.PayrollWorkerLineId, x.PayrollCalculationId, x.WorkerProfileId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollWorkerLines",
                        principalColumns: new[] { "Id", "PayrollCalculationId", "WorkerProfileId", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPayments_Persons_RecordedByPersonId_FarmId",
                        columns: x => new { x.RecordedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPayments_WorkerProfiles_WorkerProfileId_TenantId_Far~",
                        columns: x => new { x.WorkerProfileId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkerProfiles",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollSettlementClosures",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollCalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationVersion = table.Column<int>(type: "integer", nullable: false),
                    CloseSequence = table.Column<int>(type: "integer", nullable: false),
                    GrossAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DeductionAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActivePaymentAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WorkerCount = table.Column<int>(type: "integer", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ClosedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollSettlementClosures", x => x.Id);
                    table.UniqueConstraint("AK_PayrollSettlementClosures_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PayrollSettlementClosures_Totals", "\"CalculationVersion\" > 0 AND \"CloseSequence\" > 0 AND \"GrossAmountUsd\" >= 0 AND \"DeductionAmountUsd\" >= 0 AND \"NetAmountUsd\" = \"GrossAmountUsd\" - \"DeductionAmountUsd\" AND \"ActivePaymentAmountUsd\" = \"NetAmountUsd\" AND \"WorkerCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_PayrollSettlementClosures_AspNetUsers_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSettlementClosures_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSettlementClosures_PayrollCalculations_PayrollCalcul~",
                        columns: x => new { x.PayrollCalculationId, x.PayrollRunId, x.CalculationVersion, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollCalculations",
                        principalColumns: new[] { "Id", "PayrollRunId", "CalculationVersion", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSettlementClosures_PayrollRuns_PayrollRunId_TenantId~",
                        columns: x => new { x.PayrollRunId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSettlementClosures_Persons_ClosedByPersonId_FarmId",
                        columns: x => new { x.ClosedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAcknowledgements",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    AcknowledgedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CapturedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CapturedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EvidenceReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAcknowledgements", x => x.Id);
                    table.UniqueConstraint("AK_PaymentAcknowledgements_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PaymentAcknowledgements_Status", "\"Status\" IN ('Acknowledged','Declined')");
                    table.ForeignKey(
                        name: "FK_PaymentAcknowledgements_AspNetUsers_CapturedByUserId",
                        column: x => x.CapturedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAcknowledgements_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAcknowledgements_PayrollPayments_PayrollPaymentId_Te~",
                        columns: x => new { x.PayrollPaymentId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollPayments",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAcknowledgements_Persons_AcknowledgedByPersonId_Farm~",
                        columns: x => new { x.AcknowledgedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAcknowledgements_Persons_CapturedByPersonId_FarmId",
                        columns: x => new { x.CapturedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollPaymentReversals",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollCalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationVersion = table.Column<int>(type: "integer", nullable: false),
                    PayrollWorkerLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReversedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ReversedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPaymentReversals", x => x.Id);
                    table.UniqueConstraint("AK_PayrollPaymentReversals_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PayrollPaymentReversals_Amount", "\"AmountUsd\" > 0 AND length(trim(\"Reason\")) > 0");
                    table.ForeignKey(
                        name: "FK_PayrollPaymentReversals_AspNetUsers_ReversedByUserId",
                        column: x => x.ReversedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentReversals_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentReversals_PayrollCalculations_PayrollCalculat~",
                        columns: x => new { x.PayrollCalculationId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollCalculations",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentReversals_PayrollPayments_PayrollPaymentId_Pa~",
                        columns: x => new { x.PayrollPaymentId, x.PayrollRunId, x.PayrollCalculationId, x.CalculationVersion, x.PayrollWorkerLineId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollPayments",
                        principalColumns: new[] { "Id", "PayrollRunId", "PayrollCalculationId", "CalculationVersion", "PayrollWorkerLineId", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentReversals_PayrollRuns_PayrollRunId_TenantId_F~",
                        columns: x => new { x.PayrollRunId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentReversals_PayrollWorkerLines_PayrollWorkerLin~",
                        columns: x => new { x.PayrollWorkerLineId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollWorkerLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPaymentReversals_Persons_ReversedByPersonId_FarmId",
                        columns: x => new { x.ReversedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollSettlementReopens",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollSettlementClosureId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollCalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationVersion = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReopenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReopenedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ReopenedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollSettlementReopens", x => x.Id);
                    table.UniqueConstraint("AK_PayrollSettlementReopens_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PayrollSettlementReopens_Reason", "length(trim(\"Reason\")) > 0");
                    table.ForeignKey(
                        name: "FK_PayrollSettlementReopens_AspNetUsers_ReopenedByUserId",
                        column: x => x.ReopenedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSettlementReopens_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSettlementReopens_PayrollCalculations_PayrollCalcula~",
                        columns: x => new { x.PayrollCalculationId, x.PayrollRunId, x.CalculationVersion, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollCalculations",
                        principalColumns: new[] { "Id", "PayrollRunId", "CalculationVersion", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSettlementReopens_PayrollRuns_PayrollRunId_TenantId_~",
                        columns: x => new { x.PayrollRunId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSettlementReopens_PayrollSettlementClosures_PayrollS~",
                        columns: x => new { x.PayrollSettlementClosureId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollSettlementClosures",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSettlementReopens_Persons_ReopenedByPersonId_FarmId",
                        columns: x => new { x.ReopenedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_PaymentAcknowledgementId_TenantId_Fa~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PaymentAcknowledgementId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_PayrollPaymentId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollPaymentId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_PayrollPaymentReversalId_TenantId_Fa~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollPaymentReversalId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_PayrollSettlementClosureId_TenantId_~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollSettlementClosureId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_PayrollSettlementReopenId_TenantId_F~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollSettlementReopenId", "TenantId", "FarmId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayrollAuditEventLinks_OneSubject",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                sql: "num_nonnulls(\"PayrollPeriodId\", \"WorkerAdvanceId\", \"AdvanceApprovalId\", \"AdvanceIssueId\", \"PayrollRunId\", \"PayrollCalculationId\", \"PayrollApprovalId\", \"PayrollPaymentId\", \"PaymentAcknowledgementId\", \"PayrollPaymentReversalId\", \"PayrollSettlementClosureId\", \"PayrollSettlementReopenId\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAcknowledgements_AcknowledgedByPersonId_FarmId",
                schema: "payroll",
                table: "PaymentAcknowledgements",
                columns: new[] { "AcknowledgedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAcknowledgements_CapturedByPersonId_FarmId",
                schema: "payroll",
                table: "PaymentAcknowledgements",
                columns: new[] { "CapturedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAcknowledgements_CapturedByUserId",
                schema: "payroll",
                table: "PaymentAcknowledgements",
                column: "CapturedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAcknowledgements_FarmId_TenantId",
                schema: "payroll",
                table: "PaymentAcknowledgements",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAcknowledgements_PayrollPaymentId_TenantId_FarmId",
                schema: "payroll",
                table: "PaymentAcknowledgements",
                columns: new[] { "PayrollPaymentId", "TenantId", "FarmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAcknowledgements_TenantId_FarmId_IdempotencyKey",
                schema: "payroll",
                table: "PaymentAcknowledgements",
                columns: new[] { "TenantId", "FarmId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAcknowledgements_TenantId_FarmId_PayrollPaymentId",
                schema: "payroll",
                table: "PaymentAcknowledgements",
                columns: new[] { "TenantId", "FarmId", "PayrollPaymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentReversals_FarmId_TenantId",
                schema: "payroll",
                table: "PayrollPaymentReversals",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentReversals_PayrollCalculationId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollPaymentReversals",
                columns: new[] { "PayrollCalculationId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentReversals_PayrollPaymentId",
                schema: "payroll",
                table: "PayrollPaymentReversals",
                column: "PayrollPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentReversals_PayrollPaymentId_PayrollRunId_Payro~",
                schema: "payroll",
                table: "PayrollPaymentReversals",
                columns: new[] { "PayrollPaymentId", "PayrollRunId", "PayrollCalculationId", "CalculationVersion", "PayrollWorkerLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentReversals_PayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollPaymentReversals",
                columns: new[] { "PayrollRunId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentReversals_PayrollWorkerLineId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollPaymentReversals",
                columns: new[] { "PayrollWorkerLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentReversals_ReversedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollPaymentReversals",
                columns: new[] { "ReversedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentReversals_ReversedByUserId",
                schema: "payroll",
                table: "PayrollPaymentReversals",
                column: "ReversedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPaymentReversals_TenantId_FarmId_IdempotencyKey",
                schema: "payroll",
                table: "PayrollPaymentReversals",
                columns: new[] { "TenantId", "FarmId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_FarmId_TenantId",
                schema: "payroll",
                table: "PayrollPayments",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_PayrollCalculationId_PayrollRunId_Calculati~",
                schema: "payroll",
                table: "PayrollPayments",
                columns: new[] { "PayrollCalculationId", "PayrollRunId", "CalculationVersion", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_PayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollPayments",
                columns: new[] { "PayrollRunId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_PayrollWorkerLineId_PayrollCalculationId_Wo~",
                schema: "payroll",
                table: "PayrollPayments",
                columns: new[] { "PayrollWorkerLineId", "PayrollCalculationId", "WorkerProfileId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_RecordedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollPayments",
                columns: new[] { "RecordedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_RecordedByUserId",
                schema: "payroll",
                table: "PayrollPayments",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_TenantId_FarmId_IdempotencyKey",
                schema: "payroll",
                table: "PayrollPayments",
                columns: new[] { "TenantId", "FarmId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_TenantId_FarmId_PayrollRunId_PayrollCalcula~",
                schema: "payroll",
                table: "PayrollPayments",
                columns: new[] { "TenantId", "FarmId", "PayrollRunId", "PayrollCalculationId", "CalculationVersion", "PayrollWorkerLineId" });

            migrationBuilder.CreateIndex(
                name: "UX_PayrollPayments_MobileReference",
                schema: "payroll",
                table: "PayrollPayments",
                columns: new[] { "TenantId", "FarmId", "Provider", "TransactionReference" },
                unique: true,
                filter: "\"TransactionReference\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayments_WorkerProfileId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollPayments",
                columns: new[] { "WorkerProfileId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementClosures_ClosedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollSettlementClosures",
                columns: new[] { "ClosedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementClosures_ClosedByUserId",
                schema: "payroll",
                table: "PayrollSettlementClosures",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementClosures_FarmId_TenantId",
                schema: "payroll",
                table: "PayrollSettlementClosures",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementClosures_PayrollCalculationId_CalculationV~",
                schema: "payroll",
                table: "PayrollSettlementClosures",
                columns: new[] { "PayrollCalculationId", "CalculationVersion", "CloseSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementClosures_PayrollCalculationId_PayrollRunId~",
                schema: "payroll",
                table: "PayrollSettlementClosures",
                columns: new[] { "PayrollCalculationId", "PayrollRunId", "CalculationVersion", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementClosures_PayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollSettlementClosures",
                columns: new[] { "PayrollRunId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementClosures_TenantId_FarmId_IdempotencyKey",
                schema: "payroll",
                table: "PayrollSettlementClosures",
                columns: new[] { "TenantId", "FarmId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementReopens_FarmId_TenantId",
                schema: "payroll",
                table: "PayrollSettlementReopens",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementReopens_PayrollCalculationId_PayrollRunId_~",
                schema: "payroll",
                table: "PayrollSettlementReopens",
                columns: new[] { "PayrollCalculationId", "PayrollRunId", "CalculationVersion", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementReopens_PayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollSettlementReopens",
                columns: new[] { "PayrollRunId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementReopens_PayrollSettlementClosureId_TenantI~",
                schema: "payroll",
                table: "PayrollSettlementReopens",
                columns: new[] { "PayrollSettlementClosureId", "TenantId", "FarmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementReopens_ReopenedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollSettlementReopens",
                columns: new[] { "ReopenedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementReopens_ReopenedByUserId",
                schema: "payroll",
                table: "PayrollSettlementReopens",
                column: "ReopenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementReopens_TenantId_FarmId_IdempotencyKey",
                schema: "payroll",
                table: "PayrollSettlementReopens",
                columns: new[] { "TenantId", "FarmId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettlementReopens_TenantId_FarmId_PayrollSettlementC~",
                schema: "payroll",
                table: "PayrollSettlementReopens",
                columns: new[] { "TenantId", "FarmId", "PayrollSettlementClosureId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAuditEventLinks_PaymentAcknowledgements_PaymentAckno~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PaymentAcknowledgementId", "TenantId", "FarmId" },
                principalSchema: "payroll",
                principalTable: "PaymentAcknowledgements",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollPaymentReversals_PayrollPayme~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollPaymentReversalId", "TenantId", "FarmId" },
                principalSchema: "payroll",
                principalTable: "PayrollPaymentReversals",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollPayments_PayrollPaymentId_Ten~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollPaymentId", "TenantId", "FarmId" },
                principalSchema: "payroll",
                principalTable: "PayrollPayments",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollSettlementClosures_PayrollSet~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollSettlementClosureId", "TenantId", "FarmId" },
                principalSchema: "payroll",
                principalTable: "PayrollSettlementClosures",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollSettlementReopens_PayrollSett~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollSettlementReopenId", "TenantId", "FarmId" },
                principalSchema: "payroll",
                principalTable: "PayrollSettlementReopens",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION payroll."ValidatePayrollSettlementMutation"()
                RETURNS trigger LANGUAGE plpgsql AS $BODY$
                DECLARE
                    active_paid numeric(18,2);
                    approved_net numeric(18,2);
                    original_amount numeric(18,2);
                    original_status text;
                    original_method text;
                    lock_line_id uuid;
                BEGIN
                    IF TG_TABLE_NAME IN ('PayrollPayments', 'PayrollPaymentReversals') THEN
                        lock_line_id := NEW."PayrollWorkerLineId";
                    ELSE
                        SELECT payment."PayrollWorkerLineId" INTO lock_line_id FROM payroll."PayrollPayments" payment WHERE payment."Id" = NEW."PayrollPaymentId" AND payment."TenantId" = NEW."TenantId" AND payment."FarmId" = NEW."FarmId";
                    END IF;
                    PERFORM pg_advisory_xact_lock(hashtextextended(lock_line_id::text, 0));

                    IF TG_TABLE_NAME = 'PayrollPayments' THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM payroll."PayrollApprovals" approval
                            JOIN payroll."PayrollRuns" run ON run."Id" = approval."PayrollRunId" AND run."TenantId" = approval."TenantId" AND run."FarmId" = approval."FarmId"
                            WHERE approval."Approved" = true AND run."Status" = 'Approved'
                              AND approval."PayrollRunId" = NEW."PayrollRunId" AND approval."PayrollCalculationId" = NEW."PayrollCalculationId"
                              AND approval."CalculationVersion" = NEW."CalculationVersion" AND approval."TenantId" = NEW."TenantId" AND approval."FarmId" = NEW."FarmId") THEN
                            RAISE EXCEPTION 'Payroll payment must bind to the exact Grower-approved calculation.';
                        END IF;
                        IF EXISTS (SELECT 1 FROM payroll."PayrollSettlementClosures" closure WHERE closure."PayrollRunId" = NEW."PayrollRunId" AND closure."TenantId" = NEW."TenantId" AND closure."FarmId" = NEW."FarmId" AND NOT EXISTS (SELECT 1 FROM payroll."PayrollSettlementReopens" reopen WHERE reopen."PayrollSettlementClosureId" = closure."Id" AND reopen."TenantId" = closure."TenantId" AND reopen."FarmId" = closure."FarmId")) THEN
                            RAISE EXCEPTION 'Payroll settlement is closed.';
                        END IF;
                        IF NEW."Method" = 'Cash' OR NEW."ExternalStatus" IN ('Posted','Successful') THEN
                            SELECT line."NetAmountUsd" INTO approved_net FROM payroll."PayrollWorkerLines" line WHERE line."Id" = NEW."PayrollWorkerLineId" AND line."PayrollCalculationId" = NEW."PayrollCalculationId" AND line."WorkerProfileId" = NEW."WorkerProfileId" AND line."TenantId" = NEW."TenantId" AND line."FarmId" = NEW."FarmId";
                            SELECT COALESCE(sum(CASE WHEN payment."Method" = 'Cash' OR payment."ExternalStatus" IN ('Posted','Successful') THEN payment."AmountUsd" ELSE 0 END), 0) - COALESCE((SELECT sum(reversal."AmountUsd") FROM payroll."PayrollPaymentReversals" reversal WHERE reversal."PayrollWorkerLineId" = NEW."PayrollWorkerLineId" AND reversal."TenantId" = NEW."TenantId" AND reversal."FarmId" = NEW."FarmId"), 0) INTO active_paid FROM payroll."PayrollPayments" payment WHERE payment."PayrollWorkerLineId" = NEW."PayrollWorkerLineId" AND payment."TenantId" = NEW."TenantId" AND payment."FarmId" = NEW."FarmId";
                            IF active_paid + NEW."AmountUsd" > approved_net THEN RAISE EXCEPTION 'Payroll payment would exceed approved worker net pay.'; END IF;
                        END IF;
                    ELSIF TG_TABLE_NAME = 'PayrollPaymentReversals' THEN
                        IF EXISTS (SELECT 1 FROM payroll."PayrollSettlementClosures" closure WHERE closure."PayrollRunId" = NEW."PayrollRunId" AND closure."TenantId" = NEW."TenantId" AND closure."FarmId" = NEW."FarmId" AND NOT EXISTS (SELECT 1 FROM payroll."PayrollSettlementReopens" reopen WHERE reopen."PayrollSettlementClosureId" = closure."Id" AND reopen."TenantId" = closure."TenantId" AND reopen."FarmId" = closure."FarmId")) THEN RAISE EXCEPTION 'Payroll settlement is closed.'; END IF;
                        SELECT payment."AmountUsd", payment."ExternalStatus", payment."Method" INTO original_amount, original_status, original_method FROM payroll."PayrollPayments" payment WHERE payment."Id" = NEW."PayrollPaymentId" AND payment."TenantId" = NEW."TenantId" AND payment."FarmId" = NEW."FarmId" FOR SHARE;
                        IF original_method <> 'Cash' AND original_status NOT IN ('Posted','Successful') THEN RAISE EXCEPTION 'Only an active posted payment can be reversed.'; END IF;
                        IF COALESCE((SELECT sum(reversal."AmountUsd") FROM payroll."PayrollPaymentReversals" reversal WHERE reversal."PayrollPaymentId" = NEW."PayrollPaymentId" AND reversal."TenantId" = NEW."TenantId" AND reversal."FarmId" = NEW."FarmId"), 0) + NEW."AmountUsd" > original_amount THEN RAISE EXCEPTION 'Payment reversal exceeds remaining unreversed amount.'; END IF;
                    ELSIF TG_TABLE_NAME = 'PaymentAcknowledgements' THEN
                        IF EXISTS (SELECT 1 FROM payroll."PayrollPayments" payment JOIN payroll."PayrollSettlementClosures" closure ON closure."PayrollRunId" = payment."PayrollRunId" AND closure."TenantId" = payment."TenantId" AND closure."FarmId" = payment."FarmId" WHERE payment."Id" = NEW."PayrollPaymentId" AND payment."TenantId" = NEW."TenantId" AND payment."FarmId" = NEW."FarmId" AND NOT EXISTS (SELECT 1 FROM payroll."PayrollSettlementReopens" reopen WHERE reopen."PayrollSettlementClosureId" = closure."Id" AND reopen."TenantId" = closure."TenantId" AND reopen."FarmId" = closure."FarmId")) THEN RAISE EXCEPTION 'Payroll settlement is closed.'; END IF;
                    END IF;
                    RETURN NEW;
                END;
                $BODY$;

                CREATE OR REPLACE FUNCTION payroll."ValidatePayrollSettlementClosure"()
                RETURNS trigger LANGUAGE plpgsql AS $BODY$
                DECLARE active_paid numeric(18,2);
                BEGIN
                    PERFORM pg_advisory_xact_lock(hashtextextended(NEW."PayrollRunId"::text, 0));
                    IF NOT EXISTS (SELECT 1 FROM payroll."PayrollApprovals" approval JOIN payroll."PayrollRuns" run ON run."Id" = approval."PayrollRunId" AND run."TenantId" = approval."TenantId" AND run."FarmId" = approval."FarmId" WHERE approval."Approved" = true AND run."Status" = 'Approved' AND approval."PayrollRunId" = NEW."PayrollRunId" AND approval."PayrollCalculationId" = NEW."PayrollCalculationId" AND approval."CalculationVersion" = NEW."CalculationVersion" AND approval."TenantId" = NEW."TenantId" AND approval."FarmId" = NEW."FarmId") THEN RAISE EXCEPTION 'Settlement closure requires the exact Grower-approved calculation.'; END IF;
                    IF EXISTS (SELECT 1 FROM payroll."PayrollSettlementClosures" closure WHERE closure."PayrollRunId" = NEW."PayrollRunId" AND closure."TenantId" = NEW."TenantId" AND closure."FarmId" = NEW."FarmId" AND NOT EXISTS (SELECT 1 FROM payroll."PayrollSettlementReopens" reopen WHERE reopen."PayrollSettlementClosureId" = closure."Id" AND reopen."TenantId" = closure."TenantId" AND reopen."FarmId" = closure."FarmId")) THEN RAISE EXCEPTION 'Payroll settlement is already closed.'; END IF;
                    SELECT COALESCE(sum(CASE WHEN payment."Method" = 'Cash' OR payment."ExternalStatus" IN ('Posted','Successful') THEN payment."AmountUsd" ELSE 0 END), 0) - COALESCE((SELECT sum(reversal."AmountUsd") FROM payroll."PayrollPaymentReversals" reversal WHERE reversal."PayrollRunId" = NEW."PayrollRunId" AND reversal."TenantId" = NEW."TenantId" AND reversal."FarmId" = NEW."FarmId"), 0) INTO active_paid FROM payroll."PayrollPayments" payment WHERE payment."PayrollRunId" = NEW."PayrollRunId" AND payment."TenantId" = NEW."TenantId" AND payment."FarmId" = NEW."FarmId";
                    IF active_paid <> NEW."ActivePaymentAmountUsd" OR EXISTS (SELECT 1 FROM payroll."PayrollWorkerLines" line WHERE line."PayrollCalculationId" = NEW."PayrollCalculationId" AND line."TenantId" = NEW."TenantId" AND line."FarmId" = NEW."FarmId" AND ((SELECT COALESCE(sum(CASE WHEN payment."Method" = 'Cash' OR payment."ExternalStatus" IN ('Posted','Successful') THEN payment."AmountUsd" ELSE 0 END), 0) - COALESCE((SELECT sum(reversal."AmountUsd") FROM payroll."PayrollPaymentReversals" reversal WHERE reversal."PayrollWorkerLineId" = line."Id" AND reversal."TenantId" = line."TenantId" AND reversal."FarmId" = line."FarmId"), 0) FROM payroll."PayrollPayments" payment WHERE payment."PayrollWorkerLineId" = line."Id" AND payment."TenantId" = line."TenantId" AND payment."FarmId" = line."FarmId") <> line."NetAmountUsd" OR EXISTS (SELECT 1 FROM payroll."PayrollPayments" cash WHERE cash."PayrollWorkerLineId" = line."Id" AND cash."Method" = 'Cash' AND cash."TenantId" = line."TenantId" AND cash."FarmId" = line."FarmId" AND cash."AmountUsd" > COALESCE((SELECT sum(reversal."AmountUsd") FROM payroll."PayrollPaymentReversals" reversal WHERE reversal."PayrollPaymentId" = cash."Id"), 0) AND NOT EXISTS (SELECT 1 FROM payroll."PaymentAcknowledgements" acknowledgement WHERE acknowledgement."PayrollPaymentId" = cash."Id" AND acknowledgement."TenantId" = cash."TenantId" AND acknowledgement."FarmId" = cash."FarmId" AND acknowledgement."Status" = 'Acknowledged')))) THEN RAISE EXCEPTION 'Payroll settlement must be fully settled and acknowledged.'; END IF;
                    IF NOT EXISTS (SELECT 1 FROM payroll."PayrollCalculations" calculation WHERE calculation."Id" = NEW."PayrollCalculationId" AND calculation."PayrollRunId" = NEW."PayrollRunId" AND calculation."CalculationVersion" = NEW."CalculationVersion" AND calculation."TenantId" = NEW."TenantId" AND calculation."FarmId" = NEW."FarmId" AND calculation."GrossAmountUsd" = NEW."GrossAmountUsd" AND calculation."DeductionAmountUsd" = NEW."DeductionAmountUsd" AND calculation."NetAmountUsd" = NEW."NetAmountUsd" AND (SELECT count(*) FROM payroll."PayrollWorkerLines" line WHERE line."PayrollCalculationId" = calculation."Id") = NEW."WorkerCount") THEN RAISE EXCEPTION 'Settlement closure snapshot does not match the immutable calculation.'; END IF;
                    RETURN NEW;
                END;
                $BODY$;

                CREATE TRIGGER "TR_PayrollPayments_Validate" BEFORE INSERT ON payroll."PayrollPayments" FOR EACH ROW EXECUTE FUNCTION payroll."ValidatePayrollSettlementMutation"();
                CREATE TRIGGER "TR_PaymentAcknowledgements_Validate" BEFORE INSERT ON payroll."PaymentAcknowledgements" FOR EACH ROW EXECUTE FUNCTION payroll."ValidatePayrollSettlementMutation"();
                CREATE TRIGGER "TR_PayrollPaymentReversals_Validate" BEFORE INSERT ON payroll."PayrollPaymentReversals" FOR EACH ROW EXECUTE FUNCTION payroll."ValidatePayrollSettlementMutation"();
                CREATE TRIGGER "TR_PayrollSettlementClosures_Validate" BEFORE INSERT ON payroll."PayrollSettlementClosures" FOR EACH ROW EXECUTE FUNCTION payroll."ValidatePayrollSettlementClosure"();

                CREATE TRIGGER "TR_PayrollPayments_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PayrollPayments" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE TRIGGER "TR_PaymentAcknowledgements_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PaymentAcknowledgements" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE TRIGGER "TR_PayrollPaymentReversals_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PayrollPaymentReversals" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE TRIGGER "TR_PayrollSettlementClosures_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PayrollSettlementClosures" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE TRIGGER "TR_PayrollSettlementReopens_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PayrollSettlementReopens" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_PayrollSettlementReopens_AppendOnly" ON payroll."PayrollSettlementReopens";
                DROP TRIGGER IF EXISTS "TR_PayrollSettlementClosures_AppendOnly" ON payroll."PayrollSettlementClosures";
                DROP TRIGGER IF EXISTS "TR_PayrollPaymentReversals_AppendOnly" ON payroll."PayrollPaymentReversals";
                DROP TRIGGER IF EXISTS "TR_PaymentAcknowledgements_AppendOnly" ON payroll."PaymentAcknowledgements";
                DROP TRIGGER IF EXISTS "TR_PayrollPayments_AppendOnly" ON payroll."PayrollPayments";
                DROP TRIGGER IF EXISTS "TR_PayrollSettlementClosures_Validate" ON payroll."PayrollSettlementClosures";
                DROP TRIGGER IF EXISTS "TR_PayrollPaymentReversals_Validate" ON payroll."PayrollPaymentReversals";
                DROP TRIGGER IF EXISTS "TR_PaymentAcknowledgements_Validate" ON payroll."PaymentAcknowledgements";
                DROP TRIGGER IF EXISTS "TR_PayrollPayments_Validate" ON payroll."PayrollPayments";
                DROP FUNCTION IF EXISTS payroll."ValidatePayrollSettlementClosure"();
                DROP FUNCTION IF EXISTS payroll."ValidatePayrollSettlementMutation"();
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAuditEventLinks_PaymentAcknowledgements_PaymentAckno~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollPaymentReversals_PayrollPayme~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollPayments_PayrollPaymentId_Ten~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollSettlementClosures_PayrollSet~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollSettlementReopens_PayrollSett~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropTable(
                name: "PaymentAcknowledgements",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollPaymentReversals",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollSettlementReopens",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollPayments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollSettlementClosures",
                schema: "payroll");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PayrollWorkerLines_Id_PayrollCalculationId_WorkerProfileId_~",
                schema: "payroll",
                table: "PayrollWorkerLines");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PayrollCalculations_Id_PayrollRunId_CalculationVersion_Tena~",
                schema: "payroll",
                table: "PayrollCalculations");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAuditEventLinks_PaymentAcknowledgementId_TenantId_Fa~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAuditEventLinks_PayrollPaymentId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAuditEventLinks_PayrollPaymentReversalId_TenantId_Fa~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAuditEventLinks_PayrollSettlementClosureId_TenantId_~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAuditEventLinks_PayrollSettlementReopenId_TenantId_F~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PayrollAuditEventLinks_OneSubject",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "PaymentAcknowledgementId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "PayrollPaymentId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "PayrollPaymentReversalId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "PayrollSettlementClosureId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "PayrollSettlementReopenId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayrollAuditEventLinks_OneSubject",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                sql: "num_nonnulls(\"PayrollPeriodId\", \"WorkerAdvanceId\", \"AdvanceApprovalId\", \"AdvanceIssueId\", \"PayrollRunId\", \"PayrollCalculationId\", \"PayrollApprovalId\") = 1");
        }
    }
}
