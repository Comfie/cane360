using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmOwnerProfileEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Active",
                schema: "identity",
                table: "GrowerProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Association",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GrowerNumber",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MembershipNumber",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "NationalIdCiphertext",
                schema: "identity",
                table: "GrowerProfiles",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "NationalIdFingerprint",
                schema: "identity",
                table: "GrowerProfiles",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalIdKeyId",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalIdMask",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "NationalIdNonce",
                schema: "identity",
                table: "GrowerProfiles",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "NationalIdTag",
                schema: "identity",
                table: "GrowerProfiles",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoReference",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredAddress",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Surname",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "identity",
                table: "GrowerProfiles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FarmModelId",
                schema: "farm",
                table: "Farms",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FarmModels",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmModels", x => x.Id);
                    table.UniqueConstraint("AK_FarmModels_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_FarmModels_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GrowerProfiles_TenantId_NationalIdFingerprint",
                schema: "identity",
                table: "GrowerProfiles",
                columns: new[] { "TenantId", "NationalIdFingerprint" },
                unique: true,
                filter: "\"NationalIdFingerprint\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GrowerProfiles_ProtectedNationalId",
                schema: "identity",
                table: "GrowerProfiles",
                sql: "(\"NationalIdCiphertext\" IS NULL AND \"NationalIdNonce\" IS NULL AND \"NationalIdTag\" IS NULL AND \"NationalIdFingerprint\" IS NULL AND \"NationalIdKeyId\" IS NULL AND \"NationalIdMask\" IS NULL) OR (\"NationalIdCiphertext\" IS NOT NULL AND octet_length(\"NationalIdCiphertext\") > 0 AND \"NationalIdNonce\" IS NOT NULL AND octet_length(\"NationalIdNonce\") = 12 AND \"NationalIdTag\" IS NOT NULL AND octet_length(\"NationalIdTag\") = 16 AND \"NationalIdFingerprint\" IS NOT NULL AND octet_length(\"NationalIdFingerprint\") = 32 AND \"NationalIdKeyId\" IS NOT NULL AND length(\"NationalIdKeyId\") > 0 AND \"NationalIdMask\" IS NOT NULL AND length(\"NationalIdMask\") > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_Farms_FarmModelId_TenantId",
                schema: "farm",
                table: "Farms",
                columns: new[] { "FarmModelId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmModels_TenantId_Code",
                schema: "farm",
                table: "FarmModels",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Farms_FarmModels_FarmModelId_TenantId",
                schema: "farm",
                table: "Farms",
                columns: new[] { "FarmModelId", "TenantId" },
                principalSchema: "farm",
                principalTable: "FarmModels",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Farms_FarmModels_FarmModelId_TenantId",
                schema: "farm",
                table: "Farms");

            migrationBuilder.DropTable(
                name: "FarmModels",
                schema: "farm");

            migrationBuilder.DropIndex(
                name: "IX_GrowerProfiles_TenantId_NationalIdFingerprint",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GrowerProfiles_ProtectedNationalId",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Farms_FarmModelId_TenantId",
                schema: "farm",
                table: "Farms");

            migrationBuilder.DropColumn(
                name: "Active",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "Association",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "GrowerNumber",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "MembershipNumber",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "NationalIdCiphertext",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "NationalIdFingerprint",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "NationalIdKeyId",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "NationalIdMask",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "NationalIdNonce",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "NationalIdTag",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "PhotoReference",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "RegisteredAddress",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "Sex",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "Surname",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "identity",
                table: "GrowerProfiles");

            migrationBuilder.DropColumn(
                name: "FarmModelId",
                schema: "farm",
                table: "Farms");
        }
    }
}
