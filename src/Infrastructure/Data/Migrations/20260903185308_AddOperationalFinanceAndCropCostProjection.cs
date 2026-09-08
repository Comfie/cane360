using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalFinanceAndCropCostProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OperationalCostPostings_ActiveSource",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OperationalCostPostings_OneSource",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.AlterColumn<Guid>(
                name: "ActivityId",
                schema: "finance",
                table: "OperationalCostPostings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "PayrollEarningLineId",
                schema: "finance",
                table: "OperationalCostPostings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionAllocationId",
                schema: "finance",
                table: "OperationalCostPostings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OperationalTransactions",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PayeeOrPayer = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(20,2)", precision: 20, scale: 2, nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostedIdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsClosedCycleCorrection = table.Column<bool>(type: "boolean", nullable: false),
                    ClosedCycleCorrectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClosedCycleAuthorizedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ClosedCycleAuthorizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReversalOfOperationalTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReversedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReversedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalTransactions", x => x.Id);
                    table.UniqueConstraint("AK_OperationalTransactions_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_OperationalTransactions_Amount", "\"AmountUsd\" > 0");
                    table.CheckConstraint("CK_OperationalTransactions_Category", "\"Category\" IN ('Fuel', 'RepairsAndMaintenance', 'Utilities', 'Transport', 'ContractServices', 'CropInputs', 'CropSales', 'OtherExpense', 'OtherIncome')");
                    table.CheckConstraint("CK_OperationalTransactions_ClosedCycleCorrection", "(NOT \"IsClosedCycleCorrection\" AND \"ClosedCycleCorrectionReason\" IS NULL AND \"ClosedCycleAuthorizedByUserId\" IS NULL AND \"ClosedCycleAuthorizedAt\" IS NULL) OR (\"IsClosedCycleCorrection\" AND \"Status\" = 'Posted' AND length(trim(\"ClosedCycleCorrectionReason\")) > 0 AND \"ClosedCycleAuthorizedByUserId\" IS NOT NULL AND \"ClosedCycleAuthorizedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_OperationalTransactions_PostedShape", "(\"Status\" = 'Draft' AND \"PostedAt\" IS NULL AND \"PostedByUserId\" IS NULL AND \"PostedIdempotencyKey\" IS NULL AND \"ReversalOfOperationalTransactionId\" IS NULL AND NOT \"IsClosedCycleCorrection\") OR (\"Status\" = 'Cancelled' AND \"PostedAt\" IS NULL AND \"PostedByUserId\" IS NULL AND \"PostedIdempotencyKey\" IS NULL AND \"ReversalOfOperationalTransactionId\" IS NULL AND NOT \"IsClosedCycleCorrection\") OR (\"Status\" = 'Posted' AND \"PostedAt\" IS NOT NULL AND \"PostedByUserId\" IS NOT NULL AND \"PostedIdempotencyKey\" IS NOT NULL AND \"ReversalOfOperationalTransactionId\" IS NULL) OR (\"Status\" = 'Reversed' AND \"PostedAt\" IS NOT NULL AND \"PostedByUserId\" IS NOT NULL AND \"PostedIdempotencyKey\" IS NOT NULL AND \"ReversalOfOperationalTransactionId\" IS NOT NULL AND length(trim(\"ReversalReason\")) > 0 AND \"ReversedAt\" IS NOT NULL AND \"ReversedByUserId\" IS NOT NULL AND NOT \"IsClosedCycleCorrection\")");
                    table.CheckConstraint("CK_OperationalTransactions_Status", "\"Status\" IN ('Draft', 'Posted', 'Cancelled', 'Reversed')");
                    table.CheckConstraint("CK_OperationalTransactions_Type", "\"Type\" IN ('Expense', 'Income')");
                    table.ForeignKey(
                        name: "FK_OperationalTransactions_AspNetUsers_ClosedCycleAuthorizedBy~",
                        column: x => x.ClosedCycleAuthorizedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationalTransactions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationalTransactions_AspNetUsers_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationalTransactions_AspNetUsers_ReversedByUserId",
                        column: x => x.ReversedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationalTransactions_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationalTransactions_OperationalTransactions_ReversalOfO~",
                        columns: x => new { x.ReversalOfOperationalTransactionId, x.TenantId, x.FarmId },
                        principalSchema: "finance",
                        principalTable: "OperationalTransactions",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransactionAllocations",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationalTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: true),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AmountUsd = table.Column<decimal>(type: "numeric(20,2)", precision: 20, scale: 2, nullable: false),
                    AllocationType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionAllocations", x => x.Id);
                    table.UniqueConstraint("AK_TransactionAllocations_Id_TenantId_FarmId", x => new { x.Id, x.TenantId, x.FarmId });
                    table.CheckConstraint("CK_TransactionAllocations_Amount", "\"AmountUsd\" > 0");
                    table.CheckConstraint("CK_TransactionAllocations_Category", "\"Category\" IN ('Fuel', 'RepairsAndMaintenance', 'Utilities', 'Transport', 'ContractServices', 'CropInputs', 'CropSales', 'OtherExpense', 'OtherIncome')");
                    table.CheckConstraint("CK_TransactionAllocations_Shape", "(\"AllocationType\" = 'CropCycleDirect' AND \"CropCycleId\" IS NOT NULL AND \"FieldId\" IS NOT NULL) OR (\"AllocationType\" = 'Field' AND \"CropCycleId\" IS NULL AND \"FieldId\" IS NOT NULL) OR (\"AllocationType\" = 'FarmOverhead' AND \"CropCycleId\" IS NULL AND \"FieldId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_TransactionAllocations_CropCycles_CropCycleId_FieldId",
                        columns: x => new { x.CropCycleId, x.FieldId },
                        principalSchema: "farm",
                        principalTable: "CropCycles",
                        principalColumns: new[] { "Id", "FieldId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionAllocations_Fields_FieldId_FarmId",
                        columns: x => new { x.FieldId, x.FarmId },
                        principalSchema: "farm",
                        principalTable: "Fields",
                        principalColumns: new[] { "Id", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionAllocations_OperationalTransactions_OperationalT~",
                        columns: x => new { x.OperationalTransactionId, x.TenantId, x.FarmId },
                        principalSchema: "finance",
                        principalTable: "OperationalTransactions",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceAuditEventLinks",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationalTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionAllocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperationalCostPostingId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAuditEventLinks", x => x.Id);
                    table.CheckConstraint("CK_FinanceAuditEventLinks_OneSubject", "num_nonnulls(\"OperationalTransactionId\", \"TransactionAllocationId\", \"OperationalCostPostingId\") = 1");
                    table.ForeignKey(
                        name: "FK_FinanceAuditEventLinks_AuditEvents_AuditEventId_TenantId_Fa~",
                        columns: x => new { x.AuditEventId, x.TenantId, x.FarmId },
                        principalSchema: "audit",
                        principalTable: "AuditEvents",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceAuditEventLinks_OperationalCostPostings_OperationalC~",
                        columns: x => new { x.OperationalCostPostingId, x.TenantId, x.FarmId },
                        principalSchema: "finance",
                        principalTable: "OperationalCostPostings",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceAuditEventLinks_OperationalTransactions_OperationalT~",
                        columns: x => new { x.OperationalTransactionId, x.TenantId, x.FarmId },
                        principalSchema: "finance",
                        principalTable: "OperationalTransactions",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceAuditEventLinks_TransactionAllocations_TransactionAl~",
                        columns: x => new { x.TransactionAllocationId, x.TenantId, x.FarmId },
                        principalSchema: "finance",
                        principalTable: "TransactionAllocations",
                        principalColumns: new[] { "Id", "TenantId", "FarmId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_PayrollEarningLineId_CropCycleId_Ca~",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "PayrollEarningLineId", "CropCycleId", "Category" },
                unique: true,
                filter: "\"PayrollEarningLineId\" IS NOT NULL AND \"ReversalOfOperationalCostPostingId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_PayrollEarningLineId_TenantId_FarmId",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "PayrollEarningLineId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_TransactionAllocationId_CropCycleId~",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "TransactionAllocationId", "CropCycleId", "Category" },
                unique: true,
                filter: "\"TransactionAllocationId\" IS NOT NULL AND \"ReversalOfOperationalCostPostingId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCostPostings_TransactionAllocationId_TenantId_Fa~",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "TransactionAllocationId", "TenantId", "FarmId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OperationalCostPostings_Amount",
                schema: "finance",
                table: "OperationalCostPostings",
                sql: "(\"ReversalOfOperationalCostPostingId\" IS NULL AND \"AmountUsd\" > 0) OR (\"ReversalOfOperationalCostPostingId\" IS NOT NULL AND \"AmountUsd\" < 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OperationalCostPostings_OneSource",
                schema: "finance",
                table: "OperationalCostPostings",
                sql: "num_nonnulls(\"InputApplicationLineId\", \"InventoryLossId\", \"PayrollEarningLineId\", \"TransactionAllocationId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OperationalCostPostings_SourceCategory",
                schema: "finance",
                table: "OperationalCostPostings",
                sql: "(\"Category\" = 'AppliedInput' AND \"InputApplicationLineId\" IS NOT NULL) OR (\"Category\" = 'ApprovedInventoryLoss' AND \"InventoryLossId\" IS NOT NULL) OR (\"Category\" = 'Labour' AND \"PayrollEarningLineId\" IS NOT NULL) OR (\"Category\" = 'DirectExpense' AND \"TransactionAllocationId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditEventLinks_AuditEventId_TenantId_FarmId",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                columns: new[] { "AuditEventId", "TenantId", "FarmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditEventLinks_OperationalCostPostingId_TenantId_Fa~",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                columns: new[] { "OperationalCostPostingId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditEventLinks_OperationalTransactionId_TenantId_Fa~",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                columns: new[] { "OperationalTransactionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditEventLinks_TransactionAllocationId_TenantId_Far~",
                schema: "finance",
                table: "FinanceAuditEventLinks",
                columns: new[] { "TransactionAllocationId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTransactions_ClosedCycleAuthorizedByUserId",
                schema: "finance",
                table: "OperationalTransactions",
                column: "ClosedCycleAuthorizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTransactions_CreatedByUserId",
                schema: "finance",
                table: "OperationalTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTransactions_FarmId_TenantId",
                schema: "finance",
                table: "OperationalTransactions",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTransactions_PostedByUserId",
                schema: "finance",
                table: "OperationalTransactions",
                column: "PostedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTransactions_ReversalOfOperationalTransactionId",
                schema: "finance",
                table: "OperationalTransactions",
                column: "ReversalOfOperationalTransactionId",
                unique: true,
                filter: "\"ReversalOfOperationalTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTransactions_ReversalOfOperationalTransactionId_~",
                schema: "finance",
                table: "OperationalTransactions",
                columns: new[] { "ReversalOfOperationalTransactionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTransactions_ReversedByUserId",
                schema: "finance",
                table: "OperationalTransactions",
                column: "ReversedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTransactions_TenantId_FarmId_CreatedAt",
                schema: "finance",
                table: "OperationalTransactions",
                columns: new[] { "TenantId", "FarmId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalTransactions_TenantId_FarmId_PostedIdempotencyKey",
                schema: "finance",
                table: "OperationalTransactions",
                columns: new[] { "TenantId", "FarmId", "PostedIdempotencyKey" },
                unique: true,
                filter: "\"PostedIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAllocations_CropCycleId_FieldId",
                schema: "finance",
                table: "TransactionAllocations",
                columns: new[] { "CropCycleId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAllocations_FieldId_FarmId",
                schema: "finance",
                table: "TransactionAllocations",
                columns: new[] { "FieldId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAllocations_OperationalTransactionId_TenantId_Fa~",
                schema: "finance",
                table: "TransactionAllocations",
                columns: new[] { "OperationalTransactionId", "TenantId", "FarmId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAllocations_TenantId_FarmId_OperationalTransacti~",
                schema: "finance",
                table: "TransactionAllocations",
                columns: new[] { "TenantId", "FarmId", "OperationalTransactionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_OperationalCostPostings_PayrollEarningLines_PayrollEarningL~",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "PayrollEarningLineId", "TenantId", "FarmId" },
                principalSchema: "payroll",
                principalTable: "PayrollEarningLines",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OperationalCostPostings_TransactionAllocations_TransactionA~",
                schema: "finance",
                table: "OperationalCostPostings",
                columns: new[] { "TransactionAllocationId", "TenantId", "FarmId" },
                principalSchema: "finance",
                principalTable: "TransactionAllocations",
                principalColumns: new[] { "Id", "TenantId", "FarmId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                CREATE FUNCTION finance."RejectLockedOperationalTransactionMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    IF OLD."Status" <> 'Draft' THEN
                        RAISE EXCEPTION 'Posted, reversed, and cancelled operational transactions are immutable.';
                    END IF;
                    RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
                END;
                $function$;

                CREATE TRIGGER "TR_OperationalTransactions_RejectLockedMutation"
                BEFORE UPDATE OR DELETE ON finance."OperationalTransactions"
                FOR EACH ROW EXECUTE FUNCTION finance."RejectLockedOperationalTransactionMutation"();
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION finance."ValidateTransactionAllocationMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    transaction_status text;
                    transaction_id uuid;
                    transaction_tenant_id uuid;
                    transaction_farm_id uuid;
                BEGIN
                    transaction_id := CASE WHEN TG_OP = 'DELETE' THEN OLD."OperationalTransactionId" ELSE NEW."OperationalTransactionId" END;
                    transaction_tenant_id := CASE WHEN TG_OP = 'DELETE' THEN OLD."TenantId" ELSE NEW."TenantId" END;
                    transaction_farm_id := CASE WHEN TG_OP = 'DELETE' THEN OLD."FarmId" ELSE NEW."FarmId" END;

                    SELECT "Status" INTO transaction_status
                    FROM finance."OperationalTransactions"
                    WHERE "Id" = transaction_id
                      AND "TenantId" = transaction_tenant_id
                      AND "FarmId" = transaction_farm_id;

                    IF transaction_status IS NULL THEN
                        RAISE EXCEPTION 'Allocation transaction scope is invalid.';
                    END IF;
                    IF TG_OP = 'INSERT' AND transaction_status NOT IN ('Draft', 'Reversed') THEN
                        RAISE EXCEPTION 'Allocations can only be appended to draft or newly-created reversal transactions.';
                    END IF;
                    IF TG_OP IN ('UPDATE', 'DELETE') AND transaction_status <> 'Draft' THEN
                        RAISE EXCEPTION 'Allocations of an authoritative transaction are immutable.';
                    END IF;
                    RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
                END;
                $function$;

                CREATE TRIGGER "TR_TransactionAllocations_ValidateMutation"
                BEFORE INSERT OR UPDATE OR DELETE ON finance."TransactionAllocations"
                FOR EACH ROW EXECUTE FUNCTION finance."ValidateTransactionAllocationMutation"();
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION finance."ValidateOperationalTransactionAllocationBalance"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    transaction_id uuid;
                    expected_amount numeric(20,2);
                    transaction_status text;
                    allocation_count integer;
                    allocation_total numeric(20,2);
                BEGIN
                    transaction_id := CASE
                        WHEN TG_TABLE_NAME = 'OperationalTransactions' THEN COALESCE(NEW."Id", OLD."Id")
                        ELSE COALESCE(NEW."OperationalTransactionId", OLD."OperationalTransactionId")
                    END;

                    SELECT "AmountUsd", "Status" INTO expected_amount, transaction_status
                    FROM finance."OperationalTransactions"
                    WHERE "Id" = transaction_id;

                    IF transaction_status IN ('Posted', 'Reversed') THEN
                        SELECT count(*), COALESCE(sum("AmountUsd"), 0)
                        INTO allocation_count, allocation_total
                        FROM finance."TransactionAllocations"
                        WHERE "OperationalTransactionId" = transaction_id;

                        IF allocation_count = 0 OR allocation_total <> expected_amount THEN
                            RAISE EXCEPTION 'Authoritative transaction allocations must reconcile exactly to the transaction amount.';
                        END IF;
                    END IF;
                    RETURN NULL;
                END;
                $function$;

                CREATE CONSTRAINT TRIGGER "TR_OperationalTransactions_AllocationBalance"
                AFTER INSERT OR UPDATE ON finance."OperationalTransactions"
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION finance."ValidateOperationalTransactionAllocationBalance"();

                CREATE CONSTRAINT TRIGGER "TR_TransactionAllocations_AllocationBalance"
                AFTER INSERT OR UPDATE OR DELETE ON finance."TransactionAllocations"
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION finance."ValidateOperationalTransactionAllocationBalance"();
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION finance."ValidateOperationalTransactionReversal"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    original finance."OperationalTransactions"%ROWTYPE;
                BEGIN
                    IF NEW."Status" <> 'Reversed' THEN
                        RETURN NEW;
                    END IF;

                    SELECT * INTO original
                    FROM finance."OperationalTransactions"
                    WHERE "Id" = NEW."ReversalOfOperationalTransactionId"
                      AND "TenantId" = NEW."TenantId"
                      AND "FarmId" = NEW."FarmId";

                    IF NOT FOUND OR original."Status" <> 'Posted'
                       OR NEW."Type" <> original."Type"
                       OR NEW."Category" <> original."Category"
                       OR NEW."EventDate" <> original."EventDate"
                       OR NEW."AmountUsd" <> original."AmountUsd"
                       OR NEW."PayeeOrPayer" <> original."PayeeOrPayer"
                       OR NEW."SourceReference" IS DISTINCT FROM original."SourceReference" THEN
                        RAISE EXCEPTION 'Operational transaction reversal does not match its authoritative source.';
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER "TR_OperationalTransactions_ValidateReversal"
                BEFORE INSERT ON finance."OperationalTransactions"
                FOR EACH ROW EXECUTE FUNCTION finance."ValidateOperationalTransactionReversal"();
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION finance."ValidateOperationalCostSource"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    original finance."OperationalCostPostings"%ROWTYPE;
                    allocation record;
                    earning record;
                    cycle_status text;
                BEGIN
                    IF NEW."ReversalOfOperationalCostPostingId" IS NOT NULL THEN
                        SELECT * INTO original
                        FROM finance."OperationalCostPostings"
                        WHERE "Id" = NEW."ReversalOfOperationalCostPostingId";

                        IF NOT FOUND
                           OR original."ReversalOfOperationalCostPostingId" IS NOT NULL
                           OR NEW."TenantId" <> original."TenantId"
                           OR NEW."FarmId" <> original."FarmId"
                           OR NEW."FieldId" <> original."FieldId"
                           OR NEW."CropCycleId" <> original."CropCycleId"
                           OR NEW."Category" <> original."Category"
                           OR NEW."AmountUsd" <> -original."AmountUsd"
                           OR NEW."SourceQuantitySnapshot" <> original."SourceQuantitySnapshot"
                           OR NEW."UnitCostUsdSnapshot" <> -original."UnitCostUsdSnapshot"
                           OR NEW."ActivityId" IS DISTINCT FROM original."ActivityId"
                           OR NEW."InputApplicationLineId" IS DISTINCT FROM original."InputApplicationLineId"
                           OR NEW."InventoryLossId" IS DISTINCT FROM original."InventoryLossId"
                           OR NEW."PayrollEarningLineId" IS DISTINCT FROM original."PayrollEarningLineId"
                           OR NEW."TransactionAllocationId" IS DISTINCT FROM original."TransactionAllocationId" THEN
                            RAISE EXCEPTION 'Cost reversal does not exactly reverse its authoritative source posting.';
                        END IF;
                        RETURN NEW;
                    END IF;

                    IF NEW."Category" = 'DirectExpense' THEN
                        SELECT a."TenantId", a."FarmId", a."FieldId", a."CropCycleId",
                               a."AmountUsd", a."AllocationType", t."Type", t."Status",
                               t."IsClosedCycleCorrection", t."ClosedCycleCorrectionReason",
                               t."ClosedCycleAuthorizedByUserId"
                        INTO allocation
                        FROM finance."TransactionAllocations" a
                        JOIN finance."OperationalTransactions" t
                          ON t."Id" = a."OperationalTransactionId"
                         AND t."TenantId" = a."TenantId"
                         AND t."FarmId" = a."FarmId"
                        WHERE a."Id" = NEW."TransactionAllocationId";

                        IF NOT FOUND OR allocation."Type" <> 'Expense'
                           OR allocation."Status" <> 'Posted'
                           OR allocation."AllocationType" <> 'CropCycleDirect'
                           OR NEW."TenantId" <> allocation."TenantId"
                           OR NEW."FarmId" <> allocation."FarmId"
                           OR NEW."FieldId" <> allocation."FieldId"
                           OR NEW."CropCycleId" <> allocation."CropCycleId"
                           OR NEW."SourceQuantitySnapshot" <> 1
                           OR NEW."UnitCostUsdSnapshot" <> allocation."AmountUsd"
                           OR NEW."AmountUsd" <> allocation."AmountUsd" THEN
                            RAISE EXCEPTION 'Direct expense cost does not match a posted expense allocation.';
                        END IF;

                        SELECT "Status" INTO cycle_status
                        FROM farm."CropCycles"
                        WHERE "Id" = NEW."CropCycleId" AND "FieldId" = NEW."FieldId";

                        IF cycle_status IN ('Closed', 'Cancelled') AND
                           (NOT allocation."IsClosedCycleCorrection"
                            OR allocation."ClosedCycleCorrectionReason" IS NULL
                            OR NOT EXISTS (
                                SELECT 1 FROM identity."TenantMemberships" membership
                                WHERE membership."TenantId" = NEW."TenantId"
                                  AND membership."UserId" = allocation."ClosedCycleAuthorizedByUserId"
                                  AND membership."SecurityRole" = 'Grower'
                                  AND membership."Status" = 'Active')) THEN
                            RAISE EXCEPTION 'Closed crop-cycle direct costs require an authorized Grower correction.';
                        END IF;
                    ELSIF NEW."Category" = 'Labour' THEN
                        SELECT line."TenantId", line."FarmId", line."FieldId", line."Quantity",
                               line."RateAmountUsd", line."EarningAmountUsd", calculation."PayrollRunId",
                               calculation."CalculationVersion", run."SubmittedCalculationVersion",
                               run."Status" AS "RunStatus", approval."Approved"
                        INTO earning
                        FROM payroll."PayrollEarningLines" line
                        JOIN payroll."PayrollCalculations" calculation
                          ON calculation."Id" = line."PayrollCalculationId"
                         AND calculation."TenantId" = line."TenantId"
                         AND calculation."FarmId" = line."FarmId"
                        JOIN payroll."PayrollRuns" run
                          ON run."Id" = calculation."PayrollRunId"
                         AND run."TenantId" = calculation."TenantId"
                         AND run."FarmId" = calculation."FarmId"
                        JOIN payroll."PayrollApprovals" approval
                          ON approval."PayrollRunId" = run."Id"
                         AND approval."PayrollCalculationId" = calculation."Id"
                         AND approval."CalculationVersion" = calculation."CalculationVersion"
                         AND approval."TenantId" = calculation."TenantId"
                         AND approval."FarmId" = calculation."FarmId"
                        WHERE line."Id" = NEW."PayrollEarningLineId";

                        IF NOT FOUND OR earning."Approved" IS NOT TRUE
                           OR earning."RunStatus" <> 'Approved'
                           OR earning."SubmittedCalculationVersion" <> earning."CalculationVersion"
                           OR NEW."TenantId" <> earning."TenantId"
                           OR NEW."FarmId" <> earning."FarmId"
                           OR NEW."FieldId" <> earning."FieldId"
                           OR NEW."SourceQuantitySnapshot" <> earning."Quantity"
                           OR NEW."UnitCostUsdSnapshot" <> earning."RateAmountUsd"
                           OR NEW."AmountUsd" <> earning."EarningAmountUsd" THEN
                            RAISE EXCEPTION 'Labour cost does not match an eligible component of the exact approved payroll version.';
                        END IF;
                    END IF;
                    RETURN NEW;
                END;
                $function$;

                CREATE TRIGGER "TR_OperationalCostPostings_ValidateSource"
                BEFORE INSERT ON finance."OperationalCostPostings"
                FOR EACH ROW EXECUTE FUNCTION finance."ValidateOperationalCostSource"();
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION finance."RejectFinanceAuditLinkMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    RAISE EXCEPTION 'Finance audit source links are append-only.';
                END;
                $function$;

                CREATE TRIGGER "TR_FinanceAuditEventLinks_AppendOnly"
                BEFORE UPDATE OR DELETE ON finance."FinanceAuditEventLinks"
                FOR EACH ROW EXECUTE FUNCTION finance."RejectFinanceAuditLinkMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_FinanceAuditEventLinks_AppendOnly" ON finance."FinanceAuditEventLinks";
                DROP FUNCTION IF EXISTS finance."RejectFinanceAuditLinkMutation"();
                DROP TRIGGER IF EXISTS "TR_OperationalCostPostings_ValidateSource" ON finance."OperationalCostPostings";
                DROP FUNCTION IF EXISTS finance."ValidateOperationalCostSource"();
                DROP TRIGGER IF EXISTS "TR_OperationalTransactions_ValidateReversal" ON finance."OperationalTransactions";
                DROP FUNCTION IF EXISTS finance."ValidateOperationalTransactionReversal"();
                DROP TRIGGER IF EXISTS "TR_TransactionAllocations_AllocationBalance" ON finance."TransactionAllocations";
                DROP TRIGGER IF EXISTS "TR_OperationalTransactions_AllocationBalance" ON finance."OperationalTransactions";
                DROP FUNCTION IF EXISTS finance."ValidateOperationalTransactionAllocationBalance"();
                DROP TRIGGER IF EXISTS "TR_TransactionAllocations_ValidateMutation" ON finance."TransactionAllocations";
                DROP FUNCTION IF EXISTS finance."ValidateTransactionAllocationMutation"();
                DROP TRIGGER IF EXISTS "TR_OperationalTransactions_RejectLockedMutation" ON finance."OperationalTransactions";
                DROP FUNCTION IF EXISTS finance."RejectLockedOperationalTransactionMutation"();
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_OperationalCostPostings_PayrollEarningLines_PayrollEarningL~",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.DropForeignKey(
                name: "FK_OperationalCostPostings_TransactionAllocations_TransactionA~",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.DropTable(
                name: "FinanceAuditEventLinks",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "TransactionAllocations",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "OperationalTransactions",
                schema: "finance");

            migrationBuilder.DropIndex(
                name: "IX_OperationalCostPostings_PayrollEarningLineId_CropCycleId_Ca~",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.DropIndex(
                name: "IX_OperationalCostPostings_PayrollEarningLineId_TenantId_FarmId",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.DropIndex(
                name: "IX_OperationalCostPostings_TransactionAllocationId_CropCycleId~",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.DropIndex(
                name: "IX_OperationalCostPostings_TransactionAllocationId_TenantId_Fa~",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OperationalCostPostings_Amount",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OperationalCostPostings_OneSource",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OperationalCostPostings_SourceCategory",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.DropColumn(
                name: "PayrollEarningLineId",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.DropColumn(
                name: "TransactionAllocationId",
                schema: "finance",
                table: "OperationalCostPostings");

            migrationBuilder.AlterColumn<Guid>(
                name: "ActivityId",
                schema: "finance",
                table: "OperationalCostPostings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_OperationalCostPostings_ActiveSource",
                schema: "finance",
                table: "OperationalCostPostings",
                sql: "\"ReversalOfOperationalCostPostingId\" IS NOT NULL OR ((\"Category\" = 'AppliedInput') = (\"InputApplicationLineId\" IS NOT NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OperationalCostPostings_OneSource",
                schema: "finance",
                table: "OperationalCostPostings",
                sql: "num_nonnulls(\"InputApplicationLineId\", \"InventoryLossId\") = 1");
        }
    }
}
