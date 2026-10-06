using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCategoryAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryCategories",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, computedColumnSql: "upper(\"Code\")", stored: true),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCategories", x => x.Id);
                    table.UniqueConstraint("AK_InventoryCategories_Code_TenantId", x => new { x.Code, x.TenantId });
                    table.CheckConstraint("CK_InventoryCategories_DisplayOrder", "\"DisplayOrder\" BETWEEN 0 AND 10000");
                    table.ForeignKey(
                        name: "FK_InventoryCategories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Category_TenantId",
                schema: "inventory",
                table: "InventoryItems",
                columns: new[] { "Category", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_TenantId_NormalizedCode",
                schema: "inventory",
                table: "InventoryCategories",
                columns: new[] { "TenantId", "NormalizedCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_TenantId_NormalizedName",
                schema: "inventory",
                table: "InventoryCategories",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            // Preserve every existing item code verbatim; seed the four legacy choices once per tenant.
            // IDs are deterministic per (tenant, code). No InventoryItems or ledger rows are updated.
            migrationBuilder.Sql("""
                WITH legacy("Code", "Name", "DisplayOrder") AS (
                    VALUES ('Fertiliser', 'Fertiliser', 0),
                           ('Chemical', 'Chemical', 1),
                           ('SeedAndPlantingMaterial', 'Seed And Planting Material', 2),
                           ('Other', 'Other', 3)
                ), category_values AS (
                    SELECT tenant."Id" AS "TenantId", legacy."Code", legacy."Name", legacy."DisplayOrder"
                    FROM identity."Tenants" tenant CROSS JOIN legacy
                    UNION ALL
                    SELECT DISTINCT item."TenantId", item."Category", item."Category", 4
                    FROM inventory."InventoryItems" item
                    WHERE NOT EXISTS (SELECT 1 FROM legacy WHERE legacy."Code" = item."Category")
                )
                INSERT INTO inventory."InventoryCategories"
                    ("Id", "TenantId", "Code", "Name", "NormalizedName", "Description", "DisplayOrder", "Active", "Version", "Created", "LastModified")
                SELECT md5("TenantId"::text || ':inventory-category:' || "Code")::uuid,
                       "TenantId", "Code", "Name", upper("Name"), NULL, "DisplayOrder", true, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM category_values;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_InventoryCategories_Category_TenantId",
                schema: "inventory",
                table: "InventoryItems",
                columns: new[] { "Category", "TenantId" },
                principalSchema: "inventory",
                principalTable: "InventoryCategories",
                principalColumns: new[] { "Code", "TenantId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Configurable names and new categories cannot safely return to the enum-only application.
            // Refuse destructive rollback and require an explicit forward remediation.
            migrationBuilder.Sql("""
                DO $block$
                BEGIN
                    RAISE EXCEPTION 'CR-01.4 category administration cannot be rolled back safely; use a forward remediation migration.';
                END
                $block$;
                """);
        }
    }
}
