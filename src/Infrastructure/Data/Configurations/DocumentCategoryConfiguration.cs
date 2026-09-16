using Cane360.Domain.Farms;
using Cane360.Domain.MillRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class DocumentCategoryConfiguration : IEntityTypeConfiguration<DocumentCategory>
{
    public void Configure(EntityTypeBuilder<DocumentCategory> builder)
    {
        builder.ToTable("DocumentCategories", "mill");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.HasAlternateKey(item => new { item.Id, item.TenantId });
        builder.Property(item => item.Code).HasMaxLength(24).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(300);
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.TenantId, item.Code }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(item => item.CreatedBy).HasMaxLength(450);
        builder.Property(item => item.LastModifiedBy).HasMaxLength(450);
    }
}
