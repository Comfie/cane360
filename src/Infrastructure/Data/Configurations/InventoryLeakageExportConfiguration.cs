using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class InventoryLeakageExportConfiguration : IEntityTypeConfiguration<InventoryLeakageExport>
{
    public void Configure(EntityTypeBuilder<InventoryLeakageExport> builder)
    { builder.ToTable("InventoryLeakageExports", "inventory"); builder.HasKey(entity => entity.Id); builder.Property(entity => entity.Id).ValueGeneratedNever(); builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId }); builder.Property(entity => entity.FilterSnapshot).HasColumnType("jsonb").IsRequired(); builder.Property(entity => entity.ExportedByUserId).HasMaxLength(450).IsRequired(); builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.ExportedByUserId).OnDelete(DeleteBehavior.Restrict); builder.HasIndex(entity => new { entity.TenantId, entity.FarmId, entity.ExportedAt }); }
}
