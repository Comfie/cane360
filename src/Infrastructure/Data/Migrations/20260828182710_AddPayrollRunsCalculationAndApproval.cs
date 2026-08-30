using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollRunsCalculationAndApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PayrollPeriods_Status",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PayrollAuditEventLinks_OneSubject",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAt",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByPayrollRunId",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByPersonId",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedByUserId",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayrollApprovalId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayrollCalculationId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayrollRunId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_WorkerRates_Id_TenantId_FarmId",
                schema: "labour",
                table: "WorkerRates",
                columns: new[] { "Id", "TenantId", "FarmId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AdvanceInstallments_Id_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceInstallments",
                columns: new[] { "Id", "TenantId", "FarmId" });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LatestCalculationVersion = table.Column<int>(type: "integer", nullable: false),
                    SubmittedCalculationVersion = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubmittedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.Id);
                    table.UniqueConstraint("AK_PayrollRuns_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PayrollRuns_CalculationVersions", "\"LatestCalculationVersion\" >= 0 AND (\"SubmittedCalculationVersion\" IS NULL OR (\"SubmittedCalculationVersion\" > 0 AND \"SubmittedCalculationVersion\" <= \"LatestCalculationVersion\"))");
                    table.CheckConstraint("CK_PayrollRuns_DecisionState", "(\"Status\" = 'Approved' AND \"ApprovedAt\" IS NOT NULL) OR (\"Status\" = 'Rejected' AND \"RejectedAt\" IS NOT NULL AND length(trim(\"RejectionReason\")) > 0) OR \"Status\" NOT IN ('Approved','Rejected')");
                    table.CheckConstraint("CK_PayrollRuns_Status", "\"Status\" IN ('Draft','Calculated','PendingGrowerApproval','Approved','Rejected','Cancelled')");
                    table.CheckConstraint("CK_PayrollRuns_SubmissionState", "(\"Status\" = 'PendingGrowerApproval' AND \"SubmittedCalculationVersion\" IS NOT NULL AND \"SubmittedAt\" IS NOT NULL AND \"SubmittedByUserId\" IS NOT NULL) OR \"Status\" <> 'PendingGrowerApproval'");
                    table.ForeignKey(
                        name: "FK_PayrollRuns_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_AspNetUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_PayrollPeriods_PayrollPeriodId_TenantId_FarmId",
                        columns: x => new { x.PayrollPeriodId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_Persons_CreatedByPersonId_FarmId",
                        columns: x => new { x.CreatedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollCalculations",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculationVersion = table.Column<int>(type: "integer", nullable: false),
                    GrossAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DeductionAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EvidenceCount = table.Column<int>(type: "integer", nullable: false),
                    BlockerSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CalculatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CalculatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCalculations", x => x.Id);
                    table.UniqueConstraint("AK_PayrollCalculations_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PayrollCalculations_Totals", "\"GrossAmountUsd\" >= 0 AND \"DeductionAmountUsd\" >= 0 AND \"NetAmountUsd\" >= 0 AND \"NetAmountUsd\" = \"GrossAmountUsd\" - \"DeductionAmountUsd\"");
                    table.CheckConstraint("CK_PayrollCalculations_Version", "\"CalculationVersion\" > 0");
                    table.ForeignKey(
                        name: "FK_PayrollCalculations_AspNetUsers_CalculatedByUserId",
                        column: x => x.CalculatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollCalculations_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollCalculations_PayrollPeriods_PayrollPeriodId_TenantId~",
                        columns: x => new { x.PayrollPeriodId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollCalculations_PayrollRuns_PayrollRunId_TenantId_FarmId",
                        columns: x => new { x.PayrollRunId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollCalculations_Persons_CalculatedByPersonId_FarmId",
                        columns: x => new { x.CalculatedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollApprovals",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollCalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunVersion = table.Column<long>(type: "bigint", nullable: false),
                    CalculationVersion = table.Column<int>(type: "integer", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DecidedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DecidedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollApprovals", x => x.Id);
                    table.UniqueConstraint("AK_PayrollApprovals_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PayrollApprovals_Reason", "\"Approved\" OR length(trim(\"Reason\")) > 0");
                    table.ForeignKey(
                        name: "FK_PayrollApprovals_AspNetUsers_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollApprovals_PayrollCalculations_PayrollCalculationId_T~",
                        columns: x => new { x.PayrollCalculationId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollCalculations",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollApprovals_PayrollRuns_PayrollRunId_TenantId_FarmId",
                        columns: x => new { x.PayrollRunId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollApprovals_Persons_DecidedByPersonId_FarmId",
                        columns: x => new { x.DecidedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollEvidenceConsumptions",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollCalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollEvidenceConsumptions", x => x.Id);
                    table.UniqueConstraint("AK_PayrollEvidenceConsumptions_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.ForeignKey(
                        name: "FK_PayrollEvidenceConsumptions_PayrollCalculations_PayrollCalc~",
                        columns: x => new { x.PayrollCalculationId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollCalculations",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollEvidenceConsumptions_PayrollRuns_PayrollRunId_Tenant~",
                        columns: x => new { x.PayrollRunId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollEvidenceConsumptions_WorkRecords_EvidenceId_TenantId~",
                        columns: x => new { x.EvidenceId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkRecords",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollWorkerLines",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollCalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GrossAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DeductionAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollWorkerLines", x => x.Id);
                    table.UniqueConstraint("AK_PayrollWorkerLines_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PayrollWorkerLines_Totals", "\"GrossAmountUsd\" > 0 AND \"DeductionAmountUsd\" >= 0 AND \"NetAmountUsd\" >= 0 AND \"NetAmountUsd\" = \"GrossAmountUsd\" - \"DeductionAmountUsd\"");
                    table.ForeignKey(
                        name: "FK_PayrollWorkerLines_PayrollCalculations_PayrollCalculationId~",
                        columns: x => new { x.PayrollCalculationId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollCalculations",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollWorkerLines_WorkerProfiles_WorkerProfileId_TenantId_~",
                        columns: x => new { x.WorkerProfileId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkerProfiles",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAdvanceDeductions",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollWorkerLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollCalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerAdvanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdvanceInstallmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecoveryPayrollPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentSequence = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingBeforeUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAdvanceDeductions", x => x.Id);
                    table.UniqueConstraint("AK_PayrollAdvanceDeductions_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PayrollAdvanceDeductions_Amounts", "\"AmountUsd\" > 0 AND \"OutstandingBeforeUsd\" >= \"AmountUsd\" AND \"ScheduledAmountUsd\" >= \"OutstandingBeforeUsd\"");
                    table.ForeignKey(
                        name: "FK_PayrollAdvanceDeductions_AdvanceInstallments_AdvanceInstall~",
                        columns: x => new { x.AdvanceInstallmentId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "AdvanceInstallments",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAdvanceDeductions_PayrollPeriods_RecoveryPayrollPeri~",
                        columns: x => new { x.RecoveryPayrollPeriodId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAdvanceDeductions_PayrollWorkerLines_PayrollWorkerLi~",
                        columns: x => new { x.PayrollWorkerLineId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollWorkerLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAdvanceDeductions_WorkerAdvances_WorkerAdvanceId_Ten~",
                        columns: x => new { x.WorkerAdvanceId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "WorkerAdvances",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAdvanceDeductions_WorkerProfiles_WorkerProfileId_Ten~",
                        columns: x => new { x.WorkerProfileId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkerProfiles",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollEarningLines",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollWorkerLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollCalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AttendanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceVersion = table.Column<long>(type: "bigint", nullable: false),
                    SupervisorVerifiedAtSnapshot = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ManagerConfirmedAtSnapshot = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivitySnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RateType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RateAmountUsd = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    RateSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RateVersion = table.Column<long>(type: "bigint", nullable: false),
                    EarningAmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollEarningLines", x => x.Id);
                    table.UniqueConstraint("AK_PayrollEarningLines_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PayrollEarningLines_Positive", "\"Quantity\" > 0 AND \"RateAmountUsd\" > 0 AND \"EarningAmountUsd\" > 0");
                    table.ForeignKey(
                        name: "FK_PayrollEarningLines_Attendances_AttendanceId_TenantId_FarmId",
                        columns: x => new { x.AttendanceId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "Attendances",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollEarningLines_Fields_FieldId_FarmId",
                        columns: x => new { x.FieldId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Fields",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollEarningLines_PayrollWorkerLines_PayrollWorkerLineId_~",
                        columns: x => new { x.PayrollWorkerLineId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollWorkerLines",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollEarningLines_WorkRecords_EvidenceId_TenantId_FarmId",
                        columns: x => new { x.EvidenceId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkRecords",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollEarningLines_WorkerProfiles_WorkerProfileId_TenantId~",
                        columns: x => new { x.WorkerProfileId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkerProfiles",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollEarningLines_WorkerRates_RateSourceId_TenantId_FarmId",
                        columns: x => new { x.RateSourceId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkerRates",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdvanceRecoveries",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollCalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollAdvanceDeductionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerAdvanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdvanceInstallmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RecoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvanceRecoveries", x => x.Id);
                    table.UniqueConstraint("AK_AdvanceRecoveries_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_AdvanceRecoveries_Amount", "\"AmountUsd\" > 0");
                    table.ForeignKey(
                        name: "FK_AdvanceRecoveries_AdvanceInstallments_AdvanceInstallmentId_~",
                        columns: x => new { x.AdvanceInstallmentId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "AdvanceInstallments",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceRecoveries_PayrollAdvanceDeductions_PayrollAdvanceDe~",
                        columns: x => new { x.PayrollAdvanceDeductionId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollAdvanceDeductions",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceRecoveries_PayrollCalculations_PayrollCalculationId_~",
                        columns: x => new { x.PayrollCalculationId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollCalculations",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceRecoveries_PayrollRuns_PayrollRunId_TenantId_FarmId",
                        columns: x => new { x.PayrollRunId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollRuns",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceRecoveries_WorkerAdvances_WorkerAdvanceId_TenantId_F~",
                        columns: x => new { x.WorkerAdvanceId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "WorkerAdvances",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceRecoveries_WorkerProfiles_WorkerProfileId_TenantId_F~",
                        columns: x => new { x.WorkerProfileId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkerProfiles",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_ClosedByPayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "ClosedByPayrollRunId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_ClosedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "ClosedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_ClosedByUserId",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "ClosedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayrollPeriods_ClosedMetadata",
                schema: "payroll",
                table: "PayrollPeriods",
                sql: "(\"Status\" = 'Closed' AND \"ClosedAt\" IS NOT NULL AND \"ClosedByUserId\" IS NOT NULL AND \"ClosedByPayrollRunId\" IS NOT NULL) OR (\"Status\" <> 'Closed' AND \"ClosedAt\" IS NULL AND \"ClosedByUserId\" IS NULL AND \"ClosedByPersonId\" IS NULL AND \"ClosedByPayrollRunId\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayrollPeriods_Status",
                schema: "payroll",
                table: "PayrollPeriods",
                sql: "\"Status\" IN ('Draft', 'Open', 'Closed', 'Cancelled')");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_PayrollApprovalId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollApprovalId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_PayrollCalculationId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollCalculationId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_PayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollRunId", "TenantId", "FarmId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayrollAuditEventLinks_OneSubject",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                sql: "num_nonnulls(\"PayrollPeriodId\", \"WorkerAdvanceId\", \"AdvanceApprovalId\", \"AdvanceIssueId\", \"PayrollRunId\", \"PayrollCalculationId\", \"PayrollApprovalId\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRecoveries_AdvanceInstallmentId_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceRecoveries",
                columns: new[] { "AdvanceInstallmentId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRecoveries_PayrollAdvanceDeductionId",
                schema: "payroll",
                table: "AdvanceRecoveries",
                column: "PayrollAdvanceDeductionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRecoveries_PayrollAdvanceDeductionId_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceRecoveries",
                columns: new[] { "PayrollAdvanceDeductionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRecoveries_PayrollCalculationId_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceRecoveries",
                columns: new[] { "PayrollCalculationId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRecoveries_PayrollRunId_PayrollCalculationId_WorkerA~",
                schema: "payroll",
                table: "AdvanceRecoveries",
                columns: new[] { "PayrollRunId", "PayrollCalculationId", "WorkerAdvanceId", "AdvanceInstallmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRecoveries_PayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceRecoveries",
                columns: new[] { "PayrollRunId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRecoveries_WorkerAdvanceId_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceRecoveries",
                columns: new[] { "WorkerAdvanceId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceRecoveries_WorkerProfileId_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceRecoveries",
                columns: new[] { "WorkerProfileId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvanceDeductions_AdvanceInstallmentId_TenantId_Farm~",
                schema: "payroll",
                table: "PayrollAdvanceDeductions",
                columns: new[] { "AdvanceInstallmentId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvanceDeductions_PayrollCalculationId_WorkerAdvance~",
                schema: "payroll",
                table: "PayrollAdvanceDeductions",
                columns: new[] { "PayrollCalculationId", "WorkerAdvanceId", "AdvanceInstallmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvanceDeductions_PayrollWorkerLineId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAdvanceDeductions",
                columns: new[] { "PayrollWorkerLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvanceDeductions_RecoveryPayrollPeriodId_TenantId_F~",
                schema: "payroll",
                table: "PayrollAdvanceDeductions",
                columns: new[] { "RecoveryPayrollPeriodId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvanceDeductions_WorkerAdvanceId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAdvanceDeductions",
                columns: new[] { "WorkerAdvanceId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvanceDeductions_WorkerProfileId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAdvanceDeductions",
                columns: new[] { "WorkerProfileId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollApprovals_DecidedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollApprovals",
                columns: new[] { "DecidedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollApprovals_DecidedByUserId",
                schema: "payroll",
                table: "PayrollApprovals",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollApprovals_PayrollCalculationId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollApprovals",
                columns: new[] { "PayrollCalculationId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollApprovals_PayrollRunId_CalculationVersion",
                schema: "payroll",
                table: "PayrollApprovals",
                columns: new[] { "PayrollRunId", "CalculationVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollApprovals_PayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollApprovals",
                columns: new[] { "PayrollRunId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollApprovals_TenantId_FarmId_IdempotencyKey",
                schema: "payroll",
                table: "PayrollApprovals",
                columns: new[] { "TenantId", "FarmId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalculations_CalculatedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollCalculations",
                columns: new[] { "CalculatedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalculations_CalculatedByUserId",
                schema: "payroll",
                table: "PayrollCalculations",
                column: "CalculatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalculations_FarmId_TenantId",
                schema: "payroll",
                table: "PayrollCalculations",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalculations_PayrollPeriodId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollCalculations",
                columns: new[] { "PayrollPeriodId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalculations_PayrollRunId_CalculationVersion",
                schema: "payroll",
                table: "PayrollCalculations",
                columns: new[] { "PayrollRunId", "CalculationVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalculations_PayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollCalculations",
                columns: new[] { "PayrollRunId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_AttendanceId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollEarningLines",
                columns: new[] { "AttendanceId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_EvidenceId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollEarningLines",
                columns: new[] { "EvidenceId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_FieldId_FarmId",
                schema: "payroll",
                table: "PayrollEarningLines",
                columns: new[] { "FieldId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_PayrollCalculationId_EvidenceId",
                schema: "payroll",
                table: "PayrollEarningLines",
                columns: new[] { "PayrollCalculationId", "EvidenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_PayrollWorkerLineId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollEarningLines",
                columns: new[] { "PayrollWorkerLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_RateSourceId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollEarningLines",
                columns: new[] { "RateSourceId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_TenantId_FarmId_WorkerProfileId_WorkDate",
                schema: "payroll",
                table: "PayrollEarningLines",
                columns: new[] { "TenantId", "FarmId", "WorkerProfileId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEarningLines_WorkerProfileId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollEarningLines",
                columns: new[] { "WorkerProfileId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEvidenceConsumptions_EvidenceId",
                schema: "payroll",
                table: "PayrollEvidenceConsumptions",
                column: "EvidenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEvidenceConsumptions_EvidenceId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollEvidenceConsumptions",
                columns: new[] { "EvidenceId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEvidenceConsumptions_PayrollCalculationId_TenantId_F~",
                schema: "payroll",
                table: "PayrollEvidenceConsumptions",
                columns: new[] { "PayrollCalculationId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEvidenceConsumptions_PayrollRunId_PayrollCalculation~",
                schema: "payroll",
                table: "PayrollEvidenceConsumptions",
                columns: new[] { "PayrollRunId", "PayrollCalculationId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollEvidenceConsumptions_PayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollEvidenceConsumptions",
                columns: new[] { "PayrollRunId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_CreatedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollRuns",
                columns: new[] { "CreatedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_CreatedByUserId",
                schema: "payroll",
                table: "PayrollRuns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_FarmId_TenantId",
                schema: "payroll",
                table: "PayrollRuns",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PayrollPeriodId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollRuns",
                columns: new[] { "PayrollPeriodId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_SubmittedByUserId",
                schema: "payroll",
                table: "PayrollRuns",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_PayrollRuns_ActivePeriod",
                schema: "payroll",
                table: "PayrollRuns",
                columns: new[] { "FarmId", "PayrollPeriodId" },
                unique: true,
                filter: "\"Status\" <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollWorkerLines_PayrollCalculationId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollWorkerLines",
                columns: new[] { "PayrollCalculationId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollWorkerLines_PayrollCalculationId_WorkerProfileId",
                schema: "payroll",
                table: "PayrollWorkerLines",
                columns: new[] { "PayrollCalculationId", "WorkerProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollWorkerLines_WorkerProfileId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollWorkerLines",
                columns: new[] { "WorkerProfileId", "TenantId", "FarmId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollApprovals_PayrollApprovalId_T~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollApprovalId", "TenantId", "FarmId" },
                principalSchema: "payroll",
                principalTable: "PayrollApprovals",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollCalculations_PayrollCalculati~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollCalculationId", "TenantId", "FarmId" },
                principalSchema: "payroll",
                principalTable: "PayrollCalculations",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollRuns_PayrollRunId_TenantId_Fa~",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollRunId", "TenantId", "FarmId" },
                principalSchema: "payroll",
                principalTable: "PayrollRuns",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_ClosedByUserId",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "ClosedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_PayrollRuns_ClosedByPayrollRunId_TenantId_Fa~",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "ClosedByPayrollRunId", "TenantId", "FarmId" },
                principalSchema: "payroll",
                principalTable: "PayrollRuns",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_Persons_ClosedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "ClosedByPersonId", "FarmId" },
                principalSchema: "farm",
                principalTable: "Persons",
                principalColumns: new[] { "Id", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER "TR_PayrollCalculations_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PayrollCalculations" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE TRIGGER "TR_PayrollWorkerLines_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PayrollWorkerLines" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE TRIGGER "TR_PayrollEarningLines_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PayrollEarningLines" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE TRIGGER "TR_PayrollAdvanceDeductions_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PayrollAdvanceDeductions" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE TRIGGER "TR_PayrollApprovals_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PayrollApprovals" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE TRIGGER "TR_PayrollEvidenceConsumptions_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PayrollEvidenceConsumptions" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE TRIGGER "TR_AdvanceRecoveries_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."AdvanceRecoveries" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE TRIGGER "TR_PayrollAuditEventLinks_AppendOnly" BEFORE UPDATE OR DELETE ON payroll."PayrollAuditEventLinks" FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                CREATE FUNCTION payroll."RejectConsumedLabourEvidenceMutation"() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM payroll."PayrollEvidenceConsumptions" consumption WHERE consumption."EvidenceId" = OLD."Id") THEN
                        RAISE EXCEPTION 'Labour evidence consumed by an approved payroll is immutable.';
                    END IF;
                    RETURN OLD;
                END;
                $$;
                CREATE TRIGGER "TR_WorkRecords_ApprovedPayrollLock" BEFORE UPDATE OR DELETE ON labour."WorkRecords" FOR EACH ROW EXECUTE FUNCTION payroll."RejectConsumedLabourEvidenceMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_WorkRecords_ApprovedPayrollLock" ON labour."WorkRecords";
                DROP FUNCTION IF EXISTS payroll."RejectConsumedLabourEvidenceMutation"();
                DROP TRIGGER IF EXISTS "TR_PayrollAuditEventLinks_AppendOnly" ON payroll."PayrollAuditEventLinks";
                DROP TRIGGER IF EXISTS "TR_AdvanceRecoveries_AppendOnly" ON payroll."AdvanceRecoveries";
                DROP TRIGGER IF EXISTS "TR_PayrollEvidenceConsumptions_AppendOnly" ON payroll."PayrollEvidenceConsumptions";
                DROP TRIGGER IF EXISTS "TR_PayrollApprovals_AppendOnly" ON payroll."PayrollApprovals";
                DROP TRIGGER IF EXISTS "TR_PayrollAdvanceDeductions_AppendOnly" ON payroll."PayrollAdvanceDeductions";
                DROP TRIGGER IF EXISTS "TR_PayrollEarningLines_AppendOnly" ON payroll."PayrollEarningLines";
                DROP TRIGGER IF EXISTS "TR_PayrollWorkerLines_AppendOnly" ON payroll."PayrollWorkerLines";
                DROP TRIGGER IF EXISTS "TR_PayrollCalculations_AppendOnly" ON payroll."PayrollCalculations";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollApprovals_PayrollApprovalId_T~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollCalculations_PayrollCalculati~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAuditEventLinks_PayrollRuns_PayrollRunId_TenantId_Fa~",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_ClosedByUserId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_PayrollRuns_ClosedByPayrollRunId_TenantId_Fa~",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_Persons_ClosedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropTable(
                name: "AdvanceRecoveries",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollApprovals",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollEarningLines",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollEvidenceConsumptions",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollAdvanceDeductions",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollWorkerLines",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollCalculations",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollRuns",
                schema: "payroll");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_WorkerRates_Id_TenantId_FarmId",
                schema: "labour",
                table: "WorkerRates");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_ClosedByPayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_ClosedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_ClosedByUserId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PayrollPeriods_ClosedMetadata",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PayrollPeriods_Status",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAuditEventLinks_PayrollApprovalId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAuditEventLinks_PayrollCalculationId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAuditEventLinks_PayrollRunId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PayrollAuditEventLinks_OneSubject",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AdvanceInstallments_Id_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceInstallments");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "ClosedByPayrollRunId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "ClosedByPersonId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "PayrollApprovalId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "PayrollCalculationId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "PayrollRunId",
                schema: "payroll",
                table: "PayrollAuditEventLinks");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayrollPeriods_Status",
                schema: "payroll",
                table: "PayrollPeriods",
                sql: "\"Status\" IN ('Draft', 'Open', 'Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayrollAuditEventLinks_OneSubject",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                sql: "num_nonnulls(\"PayrollPeriodId\", \"WorkerAdvanceId\", \"AdvanceApprovalId\", \"AdvanceIssueId\") = 1");
        }
    }
}
