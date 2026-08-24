using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class OperationalCostPostingConfiguration : IEntityTypeConfiguration<OperationalCostPosting>
{
    public void Configure(EntityTypeBuilder<OperationalCostPosting> builder)
    {
        builder.ToTable("OperationalCostPostings", "finance", table =>
        {
            table.HasCheckConstraint("CK_OperationalCostPostings_OneSource", "num_nonnulls(\"InputApplicationLineId\", \"InventoryLossId\") = 1");
            table.HasCheckConstraint("CK_OperationalCostPostings_ActiveSource", "\"ReversalOfOperationalCostPostingId\" IS NOT NULL OR ((\"Category\" = 'AppliedInput') = (\"InputApplicationLineId\" IS NOT NULL))");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId }); builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.SourceQuantitySnapshot).HasPrecision(18, 6); builder.Property(x => x.UnitCostUsdSnapshot).HasPrecision(20, 6); builder.Property(x => x.AmountUsd).HasPrecision(20, 2); builder.Property(x => x.PostingIdentity).HasMaxLength(120);
        builder.HasOne<Activity>().WithMany().HasForeignKey(x => new { x.ActivityId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CropCycle>().WithMany().HasForeignKey(x => new { x.CropCycleId, x.FieldId }).HasPrincipalKey(x => new { x.Id, x.FieldId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InputApplicationLine>().WithMany().HasForeignKey(x => new { x.InputApplicationLineId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLoss>().WithMany().HasForeignKey(x => new { x.InventoryLossId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationalCostPosting>().WithMany().HasForeignKey(x => x.ReversalOfOperationalCostPostingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PostingIdentity).IsUnique(); builder.HasIndex(x => x.ReversalOfOperationalCostPostingId).IsUnique().HasFilter("\"ReversalOfOperationalCostPostingId\" IS NOT NULL");
        builder.HasIndex(x => new { x.InputApplicationLineId, x.Category }).IsUnique().HasFilter("\"InputApplicationLineId\" IS NOT NULL AND \"ReversalOfOperationalCostPostingId\" IS NULL"); builder.HasIndex(x => new { x.InventoryLossId, x.Category }).IsUnique().HasFilter("\"InventoryLossId\" IS NOT NULL AND \"ReversalOfOperationalCostPostingId\" IS NULL");
    }
}
