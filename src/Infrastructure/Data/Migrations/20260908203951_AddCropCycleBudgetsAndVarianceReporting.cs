using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCropCycleBudgetsAndVarianceReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_FinanceAuditEventLinks_OneSubject",
                schema: "finance",
                table: "FinanceAuditEventLinks");

            migrationBuilder.AddColumn<Guid>(
                name: "BudgetId",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BudgetLineId",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Budgets",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ReportingAreaHa = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    ExpectedProductionTonnes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SupersedesBudgetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalIdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                    table.UniqueConstraint("AK_Budgets_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_Budgets_ApprovalIdentity", "(\"ApprovedAt\" IS NULL AND \"ApprovedByUserId\" IS NULL AND \"ApprovalIdempotencyKey\" IS NULL) OR (\"ApprovedAt\" IS NOT NULL AND \"ApprovedByUserId\" IS NOT NULL AND \"ApprovalIdempotencyKey\" IS NOT NULL)");
                    table.CheckConstraint("CK_Budgets_ExpectedProduction", "\"ExpectedProductionTonnes\" IS NULL OR \"ExpectedProductionTonnes\" > 0");
                    table.CheckConstraint("CK_Budgets_Lifecycle", "(\"Status\" = 'Draft' AND \"SubmittedByUserId\" IS NULL AND \"SubmittedAt\" IS NULL AND \"ApprovedAt\" IS NULL) OR (\"Status\" = 'Submitted' AND \"SubmittedByUserId\" IS NOT NULL AND \"SubmittedAt\" IS NOT NULL AND \"ApprovedAt\" IS NULL) OR (\"Status\" IN ('Approved', 'Superseded') AND \"SubmittedByUserId\" IS NOT NULL AND \"SubmittedAt\" IS NOT NULL AND \"ApprovedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_Budgets_ReportingArea", "\"ReportingAreaHa\" IS NULL OR \"ReportingAreaHa\" > 0");
                    table.CheckConstraint("CK_Budgets_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Budgets_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Budgets_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Budgets_AspNetUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Budgets_Budgets_SupersedesBudgetId_TenantId_FarmId",
                        columns: x => new { x.SupersedesBudgetId, x.TenantId, x.FarmId },
                        principalSchema: "finance",
                        principalTable: "Budgets",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Budgets_CropCycles_CropCycleId_FieldId",
                        columns: x => new { x.CropCycleId, x.FieldId },
                        principalSchema: "farm",
                        principalTable: "CropCycles",
                        principalColumns: new[] { "Id", "FieldId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Budgets_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Budgets_Fields_FieldId_FarmId",
                        columns: x => new { x.FieldId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Fields",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetLines",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(20,2)", precision: 20, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    UnitRateUsd = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetLines", x => x.Id);
                    table.UniqueConstraint("AK_BudgetLines_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_BudgetLines_Amount", "\"AmountUsd\" > 0");
                    table.CheckConstraint("CK_BudgetLines_Category", "\"Category\" IN ('Labour', 'AppliedInput', 'DirectExpense', 'ApprovedVarianceCost')");
                    table.CheckConstraint("CK_BudgetLines_Quantity", "\"Quantity\" IS NULL OR \"Quantity\" > 0");
                    table.CheckConstraint("CK_BudgetLines_UnitRate", "\"UnitRateUsd\" IS NULL OR \"UnitRateUsd\" > 0");
                    table.CheckConstraint("CK_BudgetLines_UnitValues", "(\"Quantity\" IS NULL AND \"Unit\" IS NULL AND \"UnitRateUsd\" IS NULL) OR (\"Quantity\" IS NOT NULL AND \"Unit\" IS NOT NULL AND \"UnitRateUsd\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BudgetLines_Budgets_BudgetId_TenantId_FarmId",
                        columns: x => new { x.BudgetId, x.TenantId, x.FarmId },
                        principalSchema: "finance",
                        principalTable: "Budgets",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditEventLinks_BudgetId_TenantId_FarmId",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                columns: new[] { "BudgetId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditEventLinks_BudgetLineId_TenantId_FarmId",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                columns: new[] { "BudgetLineId", "TenantId", "FarmId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_FinanceAuditEventLinks_OneSubject",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                sql: "num_nonnulls(\"OperationalTransactionId\", \"TransactionAllocationId\", \"OperationalCostPostingId\", \"BudgetId\", \"BudgetLineId\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_BudgetId_Category",
                schema: "finance",
                table: "BudgetLines",
                columns: new[] { "BudgetId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetLines_BudgetId_TenantId_FarmId",
                schema: "finance",
                table: "BudgetLines",
                columns: new[] { "BudgetId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_ApprovedByUserId",
                schema: "finance",
                table: "Budgets",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_CreatedByUserId",
                schema: "finance",
                table: "Budgets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_CropCycleId_FieldId",
                schema: "finance",
                table: "Budgets",
                columns: new[] { "CropCycleId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_FarmId_TenantId",
                schema: "finance",
                table: "Budgets",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_FieldId_FarmId",
                schema: "finance",
                table: "Budgets",
                columns: new[] { "FieldId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_SubmittedByUserId",
                schema: "finance",
                table: "Budgets",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_SupersedesBudgetId_TenantId_FarmId",
                schema: "finance",
                table: "Budgets",
                columns: new[] { "SupersedesBudgetId", "TenantId", "FarmId" },
                unique: true,
                filter: "\"SupersedesBudgetId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_FarmId_ApprovalIdempotencyKey",
                schema: "finance",
                table: "Budgets",
                columns: new[] { "TenantId", "FarmId", "ApprovalIdempotencyKey" },
                unique: true,
                filter: "\"ApprovalIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_FarmId_CropCycleId",
                schema: "finance",
                table: "Budgets",
                columns: new[] { "TenantId", "FarmId", "CropCycleId" },
                unique: true,
                filter: "\"Status\" = 'Approved'");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_TenantId_FarmId_CropCycleId_Version",
                schema: "finance",
                table: "Budgets",
                columns: new[] { "TenantId", "FarmId", "CropCycleId", "Version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceAuditEventLinks_BudgetLines_BudgetLineId_TenantId_Fa~",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                columns: new[] { "BudgetLineId", "TenantId", "FarmId" },
                principalSchema: "finance",
                principalTable: "BudgetLines",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceAuditEventLinks_Budgets_BudgetId_TenantId_FarmId",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                columns: new[] { "BudgetId", "TenantId", "FarmId" },
                principalSchema: "finance",
                principalTable: "Budgets",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION finance."ValidateBudgetMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    cycle_status text;
                    prior_version integer;
                    prior_status text;
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        IF OLD."Status" IN ('Approved', 'Superseded') THEN
                            RAISE EXCEPTION 'Approved and superseded budgets are immutable and cannot be deleted.';
                        END IF;
                        RETURN OLD;
                    END IF;

                    IF TG_OP = 'INSERT' THEN
                        SELECT cycle."Status"
                        INTO cycle_status
                        FROM farm."CropCycles" cycle
                        JOIN farm."Fields" field ON field."Id" = cycle."FieldId"
                        JOIN farm."Farms" farm ON farm."Id" = field."FarmId"
                        WHERE cycle."Id" = NEW."CropCycleId"
                          AND cycle."FieldId" = NEW."FieldId"
                          AND field."FarmId" = NEW."FarmId"
                          AND farm."TenantId" = NEW."TenantId"
                        FOR SHARE OF cycle;
                        IF cycle_status IS NULL THEN
                            RAISE EXCEPTION 'Budget tenant, farm, field, and crop-cycle scope is invalid.';
                        END IF;
                        IF cycle_status IN ('Harvested', 'Closed', 'Cancelled') THEN
                            RAISE EXCEPTION 'Ordinary budgets cannot be created for harvested, closed, or cancelled crop cycles.';
                        END IF;
                        IF NEW."Version" = 1 AND NEW."SupersedesBudgetId" IS NOT NULL THEN
                            RAISE EXCEPTION 'Budget version 1 cannot supersede another budget.';
                        ELSIF NEW."Version" > 1 THEN
                            IF NEW."SupersedesBudgetId" IS NULL THEN
                                RAISE EXCEPTION 'A budget revision must identify the approved version it supersedes.';
                            END IF;
                            SELECT prior."Version", prior."Status"
                            INTO prior_version, prior_status
                            FROM finance."Budgets" prior
                            WHERE prior."Id" = NEW."SupersedesBudgetId"
                              AND prior."TenantId" = NEW."TenantId"
                              AND prior."FarmId" = NEW."FarmId"
                              AND prior."FieldId" = NEW."FieldId"
                              AND prior."CropCycleId" = NEW."CropCycleId";
                            IF prior_version IS NULL OR prior_version <> NEW."Version" - 1
                               OR prior_status <> 'Approved' THEN
                                RAISE EXCEPTION 'Budget revision lineage must reference the immediately prior current approved version.';
                            END IF;
                        END IF;
                        RETURN NEW;
                    END IF;

                    IF ROW(NEW."TenantId", NEW."FarmId", NEW."FieldId", NEW."CropCycleId",
                        NEW."Version", NEW."SupersedesBudgetId", NEW."CreatedByUserId", NEW."CreatedAt")
                       IS DISTINCT FROM
                       ROW(OLD."TenantId", OLD."FarmId", OLD."FieldId", OLD."CropCycleId",
                        OLD."Version", OLD."SupersedesBudgetId", OLD."CreatedByUserId", OLD."CreatedAt") THEN
                        RAISE EXCEPTION 'Budget scope, version, lineage, and creation identity are immutable.';
                    END IF;

                    IF OLD."Status" = 'Draft' THEN
                        IF NEW."Status" NOT IN ('Draft', 'Submitted') THEN
                            RAISE EXCEPTION 'A draft budget must be submitted before approval.';
                        END IF;
                        IF NEW."Status" = 'Submitted' AND NOT EXISTS (
                            SELECT 1 FROM finance."BudgetLines" line
                            WHERE line."BudgetId" = OLD."Id"
                              AND line."TenantId" = OLD."TenantId"
                              AND line."FarmId" = OLD."FarmId") THEN
                            RAISE EXCEPTION 'A budget requires at least one line before submission.';
                        END IF;
                    ELSIF OLD."Status" = 'Submitted' THEN
                        IF NEW."Status" <> 'Approved' OR
                           ROW(NEW."Name", NEW."ReportingAreaHa", NEW."ExpectedProductionTonnes",
                               NEW."Notes", NEW."SubmittedByUserId", NEW."SubmittedAt")
                           IS DISTINCT FROM
                           ROW(OLD."Name", OLD."ReportingAreaHa", OLD."ExpectedProductionTonnes",
                               OLD."Notes", OLD."SubmittedByUserId", OLD."SubmittedAt") THEN
                            RAISE EXCEPTION 'A submitted budget is immutable except for approval.';
                        END IF;
                        IF NOT EXISTS (
                            SELECT 1 FROM finance."BudgetLines" line
                            WHERE line."BudgetId" = OLD."Id"
                              AND line."TenantId" = OLD."TenantId"
                              AND line."FarmId" = OLD."FarmId") THEN
                            RAISE EXCEPTION 'A budget requires at least one line before approval.';
                        END IF;
                    ELSIF OLD."Status" = 'Approved' THEN
                        IF NEW."Status" <> 'Superseded' OR
                           ROW(NEW."Name", NEW."ReportingAreaHa", NEW."ExpectedProductionTonnes",
                               NEW."Notes", NEW."SubmittedByUserId", NEW."SubmittedAt",
                               NEW."ApprovedByUserId", NEW."ApprovedAt", NEW."ApprovalIdempotencyKey")
                           IS DISTINCT FROM
                           ROW(OLD."Name", OLD."ReportingAreaHa", OLD."ExpectedProductionTonnes",
                               OLD."Notes", OLD."SubmittedByUserId", OLD."SubmittedAt",
                               OLD."ApprovedByUserId", OLD."ApprovedAt", OLD."ApprovalIdempotencyKey") THEN
                            RAISE EXCEPTION 'Approved budget content and approval facts are immutable.';
                        END IF;
                        IF NOT EXISTS (
                            SELECT 1 FROM finance."Budgets" revision
                            WHERE revision."SupersedesBudgetId" = OLD."Id"
                              AND revision."TenantId" = OLD."TenantId"
                              AND revision."FarmId" = OLD."FarmId"
                              AND revision."CropCycleId" = OLD."CropCycleId"
                              AND revision."Version" = OLD."Version" + 1
                              AND revision."Status" = 'Submitted') THEN
                            RAISE EXCEPTION 'An approved budget may be superseded only by its submitted next revision.';
                        END IF;
                    ELSE
                        RAISE EXCEPTION 'Superseded budgets are immutable.';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE OR REPLACE FUNCTION finance."RejectLockedBudgetLineMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    budget_status text;
                    budget_id uuid;
                    tenant_id uuid;
                    farm_id uuid;
                BEGIN
                    budget_id := CASE WHEN TG_OP = 'DELETE' THEN OLD."BudgetId" ELSE NEW."BudgetId" END;
                    tenant_id := CASE WHEN TG_OP = 'DELETE' THEN OLD."TenantId" ELSE NEW."TenantId" END;
                    farm_id := CASE WHEN TG_OP = 'DELETE' THEN OLD."FarmId" ELSE NEW."FarmId" END;
                    SELECT budget."Status" INTO budget_status
                    FROM finance."Budgets" budget
                    WHERE budget."Id" = budget_id
                      AND budget."TenantId" = tenant_id
                      AND budget."FarmId" = farm_id
                    FOR UPDATE;
                    IF budget_status IS DISTINCT FROM 'Draft' THEN
                        RAISE EXCEPTION 'Only draft budget lines may be inserted, updated, or deleted.';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER "TR_Budgets_ValidateMutation"
                BEFORE INSERT OR UPDATE OR DELETE ON finance."Budgets"
                FOR EACH ROW EXECUTE FUNCTION finance."ValidateBudgetMutation"();

                CREATE TRIGGER "TR_BudgetLines_RejectLockedMutation"
                BEFORE INSERT OR UPDATE OR DELETE ON finance."BudgetLines"
                FOR EACH ROW EXECUTE FUNCTION finance."RejectLockedBudgetLineMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_BudgetLines_RejectLockedMutation" ON finance."BudgetLines";
                DROP TRIGGER IF EXISTS "TR_Budgets_ValidateMutation" ON finance."Budgets";
                DROP FUNCTION IF EXISTS finance."RejectLockedBudgetLineMutation"();
                DROP FUNCTION IF EXISTS finance."ValidateBudgetMutation"();
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceAuditEventLinks_BudgetLines_BudgetLineId_TenantId_Fa~",
                schema: "finance",
                table: "FinanceAuditEventLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceAuditEventLinks_Budgets_BudgetId_TenantId_FarmId",
                schema: "finance",
                table: "FinanceAuditEventLinks");

            migrationBuilder.DropTable(
                name: "BudgetLines",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "Budgets",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "IX_FinanceAuditEventLinks_BudgetId_TenantId_FarmId",
                schema: "finance",
                table: "FinanceAuditEventLinks");

            migrationBuilder.DropIndex(
                name: "IX_FinanceAuditEventLinks_BudgetLineId_TenantId_FarmId",
                schema: "finance",
                table: "FinanceAuditEventLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FinanceAuditEventLinks_OneSubject",
                schema: "finance",
                table: "FinanceAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "BudgetId",
                schema: "finance",
                table: "FinanceAuditEventLinks");

            migrationBuilder.DropColumn(
                name: "BudgetLineId",
                schema: "finance",
                table: "FinanceAuditEventLinks");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FinanceAuditEventLinks_OneSubject",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                sql: "num_nonnulls(\"OperationalTransactionId\", \"TransactionAllocationId\", \"OperationalCostPostingId\") = 1");
        }
    }
}
