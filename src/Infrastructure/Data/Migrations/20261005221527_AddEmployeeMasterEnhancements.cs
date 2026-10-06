using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeMasterEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                schema: "labour",
                table: "WorkerProfiles",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                schema: "labour",
                table: "WorkerProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNumber",
                schema: "labour",
                table: "WorkerProfiles",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "labour",
                table: "WorkerProfiles",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextOfKinAddress",
                schema: "labour",
                table: "WorkerProfiles",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextOfKinName",
                schema: "labour",
                table: "WorkerProfiles",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextOfKinPhone",
                schema: "labour",
                table: "WorkerProfiles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextOfKinRelationship",
                schema: "labour",
                table: "WorkerProfiles",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoReference",
                schema: "labour",
                table: "WorkerProfiles",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                schema: "labour",
                table: "WorkerProfiles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Surname",
                schema: "labour",
                table: "WorkerProfiles",
                type: "character varying(59)",
                maxLength: 59,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "labour",
                table: "WorkerProfiles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_WorkerProfiles_Tenant_EmployeeNumber",
                schema: "labour",
                table: "WorkerProfiles",
                columns: new[] { "TenantId", "EmployeeNumber" },
                unique: true,
                filter: "\"EmployeeNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_WorkerProfiles_Tenant_EmployeeNumber",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "Address",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "EmployeeNumber",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "NextOfKinAddress",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "NextOfKinName",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "NextOfKinPhone",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "NextOfKinRelationship",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "PhotoReference",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "Sex",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "Surname",
                schema: "labour",
                table: "WorkerProfiles");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "labour",
                table: "WorkerProfiles");
        }
    }
}
