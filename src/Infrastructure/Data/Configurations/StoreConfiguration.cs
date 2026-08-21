using Cane360.Domain.Farms;
using Cane360.Domain.Common;
using Cane360.Domain.Activities;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores", "farm");
        builder.HasKey(store => store.Id);
        builder.Property(store => store.Id).ValueGeneratedNever();
        builder.Property(store => store.Code).HasMaxLength(20).IsRequired();
        builder.Property(store => store.Name).HasMaxLength(120).IsRequired();
        builder.Property(store => store.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(store => store.FarmId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Store> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
