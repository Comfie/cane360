using Cane360.Domain.Activities;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class InventoryLossConfiguration : IEntityTypeConfiguration<InventoryLoss>
{
    public void Configure(EntityTypeBuilder<InventoryLoss> builder)
    {
        builder.ToTable("InventoryLosses", "inventory", table => table.HasCheckConstraint("CK_InventoryLosses_Quantity", "\"Quantity\" > 0")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20); builder.Property(x => x.LossType).HasConversion<string>().HasMaxLength(20); builder.Property(x => x.Version).IsConcurrencyToken(); builder.Property(x => x.Quantity).HasPrecision(18, 6); builder.Property(x => x.IssueUnitCostUsdSnapshot).HasPrecision(20, 6);
        builder.Property(x => x.Reason).HasMaxLength(500); builder.Property(x => x.SubmittedByUserId).HasMaxLength(450); builder.Property(x => x.ItemCodeSnapshot).HasMaxLength(30); builder.Property(x => x.ItemNameSnapshot).HasMaxLength(120); builder.Property(x => x.LotCodeSnapshot).HasMaxLength(60); builder.Property(x => x.UnitCodeSnapshot).HasMaxLength(20);
        builder.HasOne<Activity>().WithMany().HasForeignKey(x => new { x.ActivityId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockIssueLine>().WithMany().HasForeignKey(x => new { x.StockIssueLineId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.StockIssueLineId, x.Status });
    }
}
