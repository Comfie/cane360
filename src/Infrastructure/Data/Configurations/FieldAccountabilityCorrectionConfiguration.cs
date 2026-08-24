using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class FieldAccountabilityCorrectionConfiguration : IEntityTypeConfiguration<FieldAccountabilityCorrection>
{
    public void Configure(EntityTypeBuilder<FieldAccountabilityCorrection> builder)
    {
        builder.ToTable("FieldAccountabilityCorrections", "inventory", table =>
            table.HasCheckConstraint("CK_FieldAccountabilityCorrections_OneOriginal",
                "num_nonnulls(\"FieldReceiptId\", \"InputApplicationId\", \"StockReturnId\", \"InventoryLossId\") = 1"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RequestedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.RequestIdempotencyKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne<Cane360.Domain.Activities.Activity>().WithMany()
            .HasForeignKey(x => new { x.ActivityId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldReceipt>().WithMany().HasForeignKey(x => new { x.FieldReceiptId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InputApplication>().WithMany().HasForeignKey(x => new { x.InputApplicationId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockReturn>().WithMany().HasForeignKey(x => new { x.StockReturnId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLoss>().WithMany().HasForeignKey(x => new { x.InventoryLossId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.RequestIdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.ActivityId, x.Status });
    }
}
