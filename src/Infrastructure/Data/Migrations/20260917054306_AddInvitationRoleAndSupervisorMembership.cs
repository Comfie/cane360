using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationRoleAndSupervisorMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TenantMemberships_RolePerson",
                schema: "identity",
                table: "TenantMemberships");

            migrationBuilder.AddColumn<string>(
                name: "SecurityRole",
                schema: "identity",
                table: "ManagerInvitations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "FarmManager");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TenantMemberships_RolePerson",
                schema: "identity",
                table: "TenantMemberships",
                sql: "(\"SecurityRole\" = 'Grower' AND \"FarmId\" IS NULL AND \"PersonId\" IS NULL) OR (\"SecurityRole\" IN ('FarmManager', 'Supervisor') AND \"FarmId\" IS NOT NULL AND \"PersonId\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TenantMemberships_RolePerson",
                schema: "identity",
                table: "TenantMemberships");

            migrationBuilder.DropColumn(
                name: "SecurityRole",
                schema: "identity",
                table: "ManagerInvitations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TenantMemberships_RolePerson",
                schema: "identity",
                table: "TenantMemberships",
                sql: "(\"SecurityRole\" = 'Grower' AND \"FarmId\" IS NULL AND \"PersonId\" IS NULL) OR (\"SecurityRole\" = 'FarmManager' AND \"FarmId\" IS NOT NULL AND \"PersonId\" IS NOT NULL)");
        }
    }
}
