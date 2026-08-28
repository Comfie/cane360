using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollFoundationsWorkerAdvancesAndPreflight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payroll");

            migrationBuilder.CreateTable(
                name: "PayrollPeriods",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OpenedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    OpenedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CancelledByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPeriods", x => x.Id);
                    table.UniqueConstraint("AK_PayrollPeriods_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_PayrollPeriods_Dates", "\"StartDate\" = make_date(\"Year\", \"Month\", 1) AND \"EndDate\" = (make_date(\"Year\", \"Month\", 1) + interval '1 month - 1 day')::date");
                    table.CheckConstraint("CK_PayrollPeriods_Month", "\"Month\" BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_PayrollPeriods_Status", "\"Status\" IN ('Draft', 'Open', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_PayrollPeriods_AspNetUsers_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPeriods_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPeriods_AspNetUsers_OpenedByUserId",
                        column: x => x.OpenedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPeriods_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPeriods_Persons_CancelledByPersonId_FarmId",
                        columns: x => new { x.CancelledByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPeriods_Persons_CreatedByPersonId_FarmId",
                        columns: x => new { x.CreatedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollPeriods_Persons_OpenedByPersonId_FarmId",
                        columns: x => new { x.OpenedByPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkerAdvances",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAmountUsd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ApprovedAmountUsd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestedEventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecoveryStartPayrollPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentCount = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RequestingPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerAdvances", x => x.Id);
                    table.UniqueConstraint("AK_WorkerAdvances_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_WorkerAdvances_Amounts", "\"RequestedAmountUsd\" > 0 AND (\"ApprovedAmountUsd\" IS NULL OR \"ApprovedAmountUsd\" > 0)");
                    table.CheckConstraint("CK_WorkerAdvances_Installments", "\"InstallmentCount\" > 0");
                    table.CheckConstraint("CK_WorkerAdvances_Status", "\"Status\" IN ('Draft', 'PendingGrowerApproval', 'Approved', 'Rejected', 'Issued', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_WorkerAdvances_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerAdvances_PayrollPeriods_RecoveryStartPayrollPeriodId_~",
                        columns: x => new { x.RecoveryStartPayrollPeriodId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerAdvances_Persons_RequestingPersonId_FarmId",
                        columns: x => new { x.RequestingPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerAdvances_WorkerProfiles_WorkerProfileId_TenantId_Farm~",
                        columns: x => new { x.WorkerProfileId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkerProfiles",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdvanceApprovals",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerAdvanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdvanceVersion = table.Column<long>(type: "bigint", nullable: false),
                    AmountUsdSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InstallmentCountSnapshot = table.Column<int>(type: "integer", nullable: false),
                    InstallmentScheduleSnapshot = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    GrowerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvanceApprovals", x => x.Id);
                    table.UniqueConstraint("AK_AdvanceApprovals_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_AdvanceApprovals_AmountSnapshot", "\"AmountUsdSnapshot\" > 0");
                    table.CheckConstraint("CK_AdvanceApprovals_InstallmentCountSnapshot", "\"InstallmentCountSnapshot\" > 0");
                    table.CheckConstraint("CK_AdvanceApprovals_ScheduleSnapshot", "length(\"InstallmentScheduleSnapshot\") > 0");
                    table.ForeignKey(
                        name: "FK_AdvanceApprovals_AspNetUsers_GrowerUserId",
                        column: x => x.GrowerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceApprovals_WorkerAdvances_WorkerAdvanceId_TenantId_Fa~",
                        columns: x => new { x.WorkerAdvanceId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "WorkerAdvances",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdvanceInstallments",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerAdvanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    PayrollPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvanceInstallments", x => x.Id);
                    table.CheckConstraint("CK_AdvanceInstallments_Amount", "\"AmountUsd\" > 0");
                    table.CheckConstraint("CK_AdvanceInstallments_Sequence", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_AdvanceInstallments_PayrollPeriods_PayrollPeriodId_TenantId~",
                        columns: x => new { x.PayrollPeriodId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceInstallments_WorkerAdvances_WorkerAdvanceId_TenantId~",
                        columns: x => new { x.WorkerAdvanceId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "WorkerAdvances",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdvanceIssues",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerAdvanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PayingPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivingWorkerId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkerAcknowledged = table.Column<bool>(type: "boolean", nullable: true),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MaskedRecipientNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ExternalReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    TransactionStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvanceIssues", x => x.Id);
                    table.UniqueConstraint("AK_AdvanceIssues_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_AdvanceIssues_Amount", "\"AmountUsd\" > 0");
                    table.CheckConstraint("CK_AdvanceIssues_Method", "(\"PaymentMethod\" = 'Cash' AND \"PayingPersonId\" IS NOT NULL AND \"ReceivingWorkerId\" IS NOT NULL AND \"WorkerAcknowledged\" = true) OR (\"PaymentMethod\" = 'MobileMoney' AND \"Provider\" IS NOT NULL AND \"MaskedRecipientNumber\" IS NOT NULL AND \"ExternalReference\" IS NOT NULL AND \"TransactionStatus\" IS NOT NULL AND length(trim(\"Provider\")) > 0 AND length(trim(\"MaskedRecipientNumber\")) > 0 AND length(trim(\"ExternalReference\")) > 0 AND length(trim(\"TransactionStatus\")) > 0)");
                    table.ForeignKey(
                        name: "FK_AdvanceIssues_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceIssues_Persons_PayingPersonId_FarmId",
                        columns: x => new { x.PayingPersonId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Persons",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceIssues_WorkerAdvances_WorkerAdvanceId_TenantId_FarmId",
                        columns: x => new { x.WorkerAdvanceId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "WorkerAdvances",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdvanceIssues_WorkerProfiles_ReceivingWorkerId_TenantId_Far~",
                        columns: x => new { x.ReceivingWorkerId, x.TenantId, x.FarmId },
                        principalSchema: "labour",
                        principalTable: "WorkerProfiles",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAuditEventLinks",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollPeriodId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkerAdvanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdvanceApprovalId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdvanceIssueId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAuditEventLinks", x => x.Id);
                    table.CheckConstraint("CK_PayrollAuditEventLinks_OneSubject", "num_nonnulls(\"PayrollPeriodId\", \"WorkerAdvanceId\", \"AdvanceApprovalId\", \"AdvanceIssueId\") = 1");
                    table.ForeignKey(
                        name: "FK_PayrollAuditEventLinks_AdvanceApprovals_AdvanceApprovalId_T~",
                        columns: x => new { x.AdvanceApprovalId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "AdvanceApprovals",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAuditEventLinks_AdvanceIssues_AdvanceIssueId_TenantI~",
                        columns: x => new { x.AdvanceIssueId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "AdvanceIssues",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAuditEventLinks_AuditEvents_AuditEventId_TenantId_Fa~",
                        columns: x => new { x.AuditEventId, x.TenantId, x.FarmId },
                        principalSchema: "audit",
                        principalTable: "AuditEvents",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAuditEventLinks_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAuditEventLinks_PayrollPeriods_PayrollPeriodId_Tenan~",
                        columns: x => new { x.PayrollPeriodId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAuditEventLinks_WorkerAdvances_WorkerAdvanceId_Tenan~",
                        columns: x => new { x.WorkerAdvanceId, x.TenantId, x.FarmId },
                        principalSchema: "payroll",
                        principalTable: "WorkerAdvances",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceApprovals_GrowerUserId",
                schema: "payroll",
                table: "AdvanceApprovals",
                column: "GrowerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceApprovals_TenantId_FarmId_IdempotencyKey",
                schema: "payroll",
                table: "AdvanceApprovals",
                columns: new[] { "TenantId", "FarmId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceApprovals_WorkerAdvanceId_AdvanceVersion",
                schema: "payroll",
                table: "AdvanceApprovals",
                columns: new[] { "WorkerAdvanceId", "AdvanceVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceApprovals_WorkerAdvanceId_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceApprovals",
                columns: new[] { "WorkerAdvanceId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceInstallments_PayrollPeriodId_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceInstallments",
                columns: new[] { "PayrollPeriodId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceInstallments_WorkerAdvanceId_Sequence",
                schema: "payroll",
                table: "AdvanceInstallments",
                columns: new[] { "WorkerAdvanceId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceInstallments_WorkerAdvanceId_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceInstallments",
                columns: new[] { "WorkerAdvanceId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceIssues_PayingPersonId_FarmId",
                schema: "payroll",
                table: "AdvanceIssues",
                columns: new[] { "PayingPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceIssues_ReceivingWorkerId_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceIssues",
                columns: new[] { "ReceivingWorkerId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceIssues_RecordedByUserId",
                schema: "payroll",
                table: "AdvanceIssues",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceIssues_TenantId_FarmId_ExternalReference",
                schema: "payroll",
                table: "AdvanceIssues",
                columns: new[] { "TenantId", "FarmId", "ExternalReference" },
                unique: true,
                filter: "\"ExternalReference\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceIssues_TenantId_FarmId_IdempotencyKey",
                schema: "payroll",
                table: "AdvanceIssues",
                columns: new[] { "TenantId", "FarmId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceIssues_WorkerAdvanceId",
                schema: "payroll",
                table: "AdvanceIssues",
                column: "WorkerAdvanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvanceIssues_WorkerAdvanceId_TenantId_FarmId",
                schema: "payroll",
                table: "AdvanceIssues",
                columns: new[] { "WorkerAdvanceId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_AdvanceApprovalId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "AdvanceApprovalId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_AdvanceIssueId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "AdvanceIssueId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_AuditEventId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                column: "AuditEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_AuditEventId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "AuditEventId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_FarmId_TenantId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_PayrollPeriodId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "PayrollPeriodId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_TenantId_FarmId_PayrollPeriodId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "TenantId", "FarmId", "PayrollPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_TenantId_FarmId_WorkerAdvanceId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "TenantId", "FarmId", "WorkerAdvanceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAuditEventLinks_WorkerAdvanceId_TenantId_FarmId",
                schema: "payroll",
                table: "PayrollAuditEventLinks",
                columns: new[] { "WorkerAdvanceId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_CancelledByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "CancelledByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_CancelledByUserId",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_CreatedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "CreatedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_CreatedByUserId",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_FarmId_TenantId",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_OpenedByPersonId_FarmId",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "OpenedByPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_OpenedByUserId",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "OpenedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_PayrollPeriods_Farm_Year_Month",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "FarmId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAdvances_RecoveryStartPayrollPeriodId_TenantId_FarmId",
                schema: "payroll",
                table: "WorkerAdvances",
                columns: new[] { "RecoveryStartPayrollPeriodId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAdvances_RequestedByUserId",
                schema: "payroll",
                table: "WorkerAdvances",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAdvances_RequestingPersonId_FarmId",
                schema: "payroll",
                table: "WorkerAdvances",
                columns: new[] { "RequestingPersonId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAdvances_TenantId_FarmId_WorkerProfileId_Status",
                schema: "payroll",
                table: "WorkerAdvances",
                columns: new[] { "TenantId", "FarmId", "WorkerProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerAdvances_WorkerProfileId_TenantId_FarmId",
                schema: "payroll",
                table: "WorkerAdvances",
                columns: new[] { "WorkerProfileId", "TenantId", "FarmId" });

            migrationBuilder.Sql(
                """
                CREATE TRIGGER "TR_AdvanceApprovals_AppendOnly"
                BEFORE UPDATE OR DELETE ON payroll."AdvanceApprovals"
                FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();

                CREATE TRIGGER "TR_AdvanceIssues_AppendOnly"
                BEFORE UPDATE OR DELETE ON payroll."AdvanceIssues"
                FOR EACH ROW EXECUTE FUNCTION inventory."RejectAppendOnlyMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_AdvanceApprovals_AppendOnly" ON payroll."AdvanceApprovals";
                DROP TRIGGER IF EXISTS "TR_AdvanceIssues_AppendOnly" ON payroll."AdvanceIssues";
                """);

            migrationBuilder.DropTable(
                name: "AdvanceInstallments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollAuditEventLinks",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "AdvanceApprovals",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "AdvanceIssues",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "WorkerAdvances",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollPeriods",
                schema: "payroll");
        }
    }
}
