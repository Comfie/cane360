using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrationConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentCategoryCodeSnapshot",
                schema: "mill",
                table: "EvidenceDocuments",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentCategoryId",
                schema: "mill",
                table: "EvidenceDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentCategories",
                schema: "mill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCategories", x => x.Id);
                    table.UniqueConstraint("AK_DocumentCategories_Id_TenantId", x => new { x.Id, x.TenantId });
                    table.ForeignKey(
                        name: "FK_DocumentCategories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FarmSettings",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmSettings", x => x.Id);
                    table.CheckConstraint("CK_FarmSettings_Dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_FarmSettings_Key", "\"Key\" = 'ActivityLateEntryReasonDays'");
                    table.CheckConstraint("CK_FarmSettings_Value", "\"Value\" BETWEEN 0 AND 30");
                    table.ForeignKey(
                        name: "FK_FarmSettings_Farms_FarmId_TenantId",
                        columns: x => new { x.FarmId, x.TenantId },
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumns: new[] { "Id", "TenantId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceDocuments_DocumentCategoryId_TenantId",
                schema: "mill",
                table: "EvidenceDocuments",
                columns: new[] { "DocumentCategoryId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCategories_TenantId_Code",
                schema: "mill",
                table: "DocumentCategories",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmSettings_FarmId_TenantId",
                schema: "farm",
                table: "FarmSettings",
                columns: new[] { "FarmId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_FarmSettings_TenantId_FarmId_Key_EffectiveFrom",
                schema: "farm",
                table: "FarmSettings",
                columns: new[] { "TenantId", "FarmId", "Key", "EffectiveFrom" });

            migrationBuilder.AddForeignKey(
                name: "FK_EvidenceDocuments_DocumentCategories_DocumentCategoryId_Ten~",
                schema: "mill",
                table: "EvidenceDocuments",
                columns: new[] { "DocumentCategoryId", "TenantId" },
                principalSchema: "mill",
                principalTable: "DocumentCategories",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                ALTER TABLE farm."FarmSettings"
                ADD CONSTRAINT "EX_FarmSettings_NoOverlap"
                EXCLUDE USING gist (
                    "TenantId" WITH =,
                    "FarmId" WITH =,
                    "Key" WITH =,
                    daterange("EffectiveFrom", COALESCE("EffectiveTo" + 1, 'infinity'::date), '[)') WITH &&
                );
                """);

            migrationBuilder.Sql("""
                ALTER TABLE mill."DocumentCategories"
                ADD CONSTRAINT "CK_DocumentCategories_NormalizedCode"
                CHECK ("Code" = upper(btrim("Code")));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvidenceDocuments_DocumentCategories_DocumentCategoryId_Ten~",
                schema: "mill",
                table: "EvidenceDocuments");

            migrationBuilder.DropTable(
                name: "DocumentCategories",
                schema: "mill");

            migrationBuilder.DropTable(
                name: "FarmSettings",
                schema: "farm");

            migrationBuilder.DropIndex(
                name: "IX_EvidenceDocuments_DocumentCategoryId_TenantId",
                schema: "mill",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentCategoryCodeSnapshot",
                schema: "mill",
                table: "EvidenceDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentCategoryId",
                schema: "mill",
                table: "EvidenceDocuments");
        }
    }
}
