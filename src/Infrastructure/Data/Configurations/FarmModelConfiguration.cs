using Cane360.Domain.Farms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class FarmModelConfiguration : IEntityTypeConfiguration<FarmModel>
{
    public void Configure(EntityTypeBuilder<FarmModel> builder)
    {
        builder.ToTable("FarmModels", "farm");
        builder.HasKey(model => model.Id);
        builder.Property(model => model.Id).ValueGeneratedNever();
        builder.Property(model => model.Code).HasMaxLength(24).IsRequired();
        builder.Property(model => model.Name).HasMaxLength(100).IsRequired();
        builder.Property(model => model.Version).IsConcurrencyToken();
        builder.HasAlternateKey(model => new { model.Id, model.TenantId });
        builder.HasIndex(model => new { model.TenantId, model.Code }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(model => model.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(model => model.CreatedBy).HasMaxLength(450);
        builder.Property(model => model.LastModifiedBy).HasMaxLength(450);
    }
}
