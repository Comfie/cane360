using Cane360.Domain.Activities;
using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class ControlExceptionConfiguration : IEntityTypeConfiguration<ControlException>
{
    public void Configure(EntityTypeBuilder<ControlException> builder)
    {
        builder.ToTable("ControlExceptions", "audit", table => table.HasCheckConstraint("CK_ControlExceptions_Nonnegative", "\"UnaccountedQuantity\" >= 0")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Code).HasMaxLength(64); builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        foreach (var property in new[] { nameof(ControlException.IssuedQuantity), nameof(ControlException.AppliedQuantity), nameof(ControlException.ReturnedQuantity), nameof(ControlException.ApprovedLossQuantity), nameof(ControlException.UnaccountedQuantity) }) builder.Property(property).HasPrecision(18, 6);
        builder.HasOne<Activity>().WithMany().HasForeignKey(x => new { x.ActivityId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockIssueLine>().WithMany().HasForeignKey(x => new { x.StockIssueLineId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.StockIssueLineId, x.Code }).IsUnique().HasFilter("\"Status\" = 'Open'"); builder.HasIndex(x => new { x.TenantId, x.FarmId, x.ActivityId, x.Status });
    }
}
