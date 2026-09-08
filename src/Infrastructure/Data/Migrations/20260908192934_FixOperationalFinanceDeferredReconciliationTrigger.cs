using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixOperationalFinanceDeferredReconciliationTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION finance."ValidateOperationalTransactionAllocationBalance"()
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
                    IF TG_TABLE_SCHEMA <> 'finance' THEN
                        RAISE EXCEPTION 'Unexpected schema % for operational transaction allocation reconciliation.', TG_TABLE_SCHEMA;
                    ELSIF TG_TABLE_NAME = 'OperationalTransactions' THEN
                        IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
                            transaction_id := NEW."Id";
                        ELSE
                            RAISE EXCEPTION 'Unexpected operation % for finance.OperationalTransactions allocation reconciliation.', TG_OP;
                        END IF;
                    ELSIF TG_TABLE_NAME = 'TransactionAllocations' THEN
                        IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
                            transaction_id := NEW."OperationalTransactionId";
                        ELSIF TG_OP = 'DELETE' THEN
                            transaction_id := OLD."OperationalTransactionId";
                        ELSE
                            RAISE EXCEPTION 'Unexpected operation % for finance.TransactionAllocations allocation reconciliation.', TG_OP;
                        END IF;
                    ELSE
                        RAISE EXCEPTION 'Unexpected table %.% for operational transaction allocation reconciliation.', TG_TABLE_SCHEMA, TG_TABLE_NAME;
                    END IF;

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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the exact function body installed by the preceding Phase 7A migration.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION finance."ValidateOperationalTransactionAllocationBalance"()
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
                """);
        }
    }
}
