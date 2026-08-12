using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cane360.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCropCycleLifecycleHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CropVarietyId",
                schema: "farm",
                table: "CropCycles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "farm",
                table: "CropCycles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "CropCycleStatusChanges",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropCycleStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CropCycleStatusChanges_AspNetUsers_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CropCycleStatusChanges_CropCycles_CropCycleId",
                        column: x => x.CropCycleId,
                        principalSchema: "farm",
                        principalTable: "CropCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CropVarieties",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropVarieties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CropVarieties_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HarvestResults",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CropCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    HarvestDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualTonnes = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HarvestResults", x => x.Id);
                    table.CheckConstraint("CK_HarvestResults_ActualTonnes", "\"ActualTonnes\" > 0");
                    table.ForeignKey(
                        name: "FK_HarvestResults_CropCycles_CropCycleId",
                        column: x => x.CropCycleId,
                        principalSchema: "farm",
                        principalTable: "CropCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    });

            migrationBuilder.Sql(
                """
                INSERT INTO farm."CropCycleStatusChanges"
                    ("Id", "CropCycleId", "FromStatus", "ToStatus", "RecordedAt", "RecordedBy", "Reason")
                SELECT
                    gen_random_uuid(),
                    cycle."Id",
                    NULL,
                    cycle."Status",
                    cycle."Created",
                    cycle."CreatedBy",
                    NULL
                FROM farm."CropCycles" AS cycle
                WHERE cycle."CreatedBy" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CropCycles_CropVarietyId",
                schema: "farm",
                table: "CropCycles",
                column: "CropVarietyId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CropCycles_CycleTypeRatoonNumber",
                schema: "farm",
                table: "CropCycles",
                sql: "(\"CycleType\" = 'Ratoon' AND \"RatoonNumber\" > 0) OR (\"CycleType\" = 'PlantCane' AND \"RatoonNumber\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CropCycles_ExpectedYieldTonnes",
                schema: "farm",
                table: "CropCycles",
                sql: "\"ExpectedYieldTonnes\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CropCycles_HarvestWindow",
                schema: "farm",
                table: "CropCycles",
                sql: "\"ExpectedHarvestStart\" >= \"StartDate\" AND \"ExpectedHarvestEnd\" >= \"ExpectedHarvestStart\"");

            migrationBuilder.CreateIndex(
                name: "IX_CropCycleStatusChanges_CropCycleId_RecordedAt",
                schema: "farm",
                table: "CropCycleStatusChanges",
                columns: new[] { "CropCycleId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CropCycleStatusChanges_RecordedBy",
                schema: "farm",
                table: "CropCycleStatusChanges",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CropVarieties_TenantId_Code",
                schema: "farm",
                table: "CropVarieties",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_HarvestResults_CropCycleId",
                schema: "farm",
                table: "HarvestResults",
                column: "CropCycleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CropCycles_CropVarieties_CropVarietyId",
                schema: "farm",
                table: "CropCycles",
                column: "CropVarietyId",
                principalSchema: "farm",
                principalTable: "CropVarieties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CropCycles_CropVarieties_CropVarietyId",
                schema: "farm",
                table: "CropCycles");

            migrationBuilder.DropTable(
                name: "CropCycleStatusChanges",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "CropVarieties",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "HarvestResults",
                schema: "farm");

            migrationBuilder.DropIndex(
                name: "IX_CropCycles_CropVarietyId",
                schema: "farm",
                table: "CropCycles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CropCycles_CycleTypeRatoonNumber",
                schema: "farm",
                table: "CropCycles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CropCycles_ExpectedYieldTonnes",
                schema: "farm",
                table: "CropCycles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CropCycles_HarvestWindow",
                schema: "farm",
                table: "CropCycles");

            migrationBuilder.DropColumn(
                name: "CropVarietyId",
                schema: "farm",
                table: "CropCycles");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "farm",
                table: "CropCycles");
        }
    }
}
