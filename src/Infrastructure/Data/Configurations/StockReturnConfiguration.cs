using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class StockReturnConfiguration : IEntityTypeConfiguration<StockReturn>
{
    public void Configure(EntityTypeBuilder<StockReturn> builder)
    {
        builder.ToTable("StockReturns", "inventory"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20); builder.Property(x => x.PostedByUserId).HasMaxLength(450); builder.Property(x => x.PostingIdempotencyKey).HasMaxLength(120); builder.Property(x => x.ReversalIdempotencyKey).HasMaxLength(120); builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne<Store>().WithMany().HasForeignKey(x => new { x.StoreId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Activity>().WithMany().HasForeignKey(x => new { x.ActivityId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.SenderPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.ReceiverPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PostedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => new { x.StockReturnId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PostingIdempotencyKey).IsUnique().HasFilter("\"PostingIdempotencyKey\" IS NOT NULL"); builder.HasIndex(x => x.ReversalIdempotencyKey).IsUnique().HasFilter("\"ReversalIdempotencyKey\" IS NOT NULL"); builder.HasIndex(x => new { x.TenantId, x.FarmId, x.ActivityId, x.Status });
    }
}
