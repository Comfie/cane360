using Cane360.Domain.Activities;
using Cane360.Domain.Common;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class EvidenceLinkConfiguration : IEntityTypeConfiguration<EvidenceLink>
{
    public void Configure(EntityTypeBuilder<EvidenceLink> builder)
    {
        builder.ToTable("EvidenceLinks", "activities", table =>
            table.HasCheckConstraint("CK_EvidenceLinks_Role", "\"Role\" = 'SourceSheet'"));
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).ValueGeneratedNever();
        builder.Property(link => link.Role).HasConversion<string>().HasMaxLength(24);
        builder.Property(link => link.SourceSheetReference).HasMaxLength(160).IsRequired();
        builder.Property(link => link.RecordedBy).HasMaxLength(450).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(link => link.RecordedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(link => new { link.ActivityId, link.RecordedAt });
    }
}
