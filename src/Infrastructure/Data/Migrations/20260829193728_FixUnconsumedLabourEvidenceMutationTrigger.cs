using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixUnconsumedLabourEvidenceMutationTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION payroll."RejectConsumedLabourEvidenceMutation"() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM payroll."PayrollEvidenceConsumptions" consumption WHERE consumption."EvidenceId" = OLD."Id") THEN
                        RAISE EXCEPTION 'Labour evidence consumed by an approved payroll is immutable.';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION payroll."RejectConsumedLabourEvidenceMutation"() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM payroll."PayrollEvidenceConsumptions" consumption WHERE consumption."EvidenceId" = OLD."Id") THEN
                        RAISE EXCEPTION 'Labour evidence consumed by an approved payroll is immutable.';
                    END IF;
                    RETURN OLD;
                END;
                $$;
                """);
        }
    }
}
