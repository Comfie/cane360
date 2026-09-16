using Cane360.Domain.Farms;
using Cane360.Domain.MillRecords;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class EvidenceDocumentConfiguration : IEntityTypeConfiguration<EvidenceDocument>
{
    public void Configure(EntityTypeBuilder<EvidenceDocument> builder)
    {
        builder.ToTable("EvidenceDocuments", "mill", table =>
        {
            table.HasCheckConstraint("CK_EvidenceDocuments_OneSubject",
                "num_nonnulls(\"WeighbridgeTicketId\", \"GrowerStatementId\") = 1");
            table.HasCheckConstraint("CK_EvidenceDocuments_Size", "\"SizeBytes\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(180).IsRequired();
        builder.Property(x => x.UploadedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.DocumentCategoryCodeSnapshot).HasMaxLength(24);
        builder.HasOne<DocumentCategory>().WithMany()
            .HasForeignKey(x => new { x.DocumentCategoryId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WeighbridgeTicket>().WithMany()
            .HasForeignKey(x => new { x.WeighbridgeTicketId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GrowerStatement>().WithMany()
            .HasForeignKey(x => new { x.GrowerStatementId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.StorageKey).IsUnique();
        builder.HasIndex(x => new { x.WeighbridgeTicketId, x.TenantId, x.FarmId });
        builder.HasIndex(x => new { x.GrowerStatementId, x.TenantId, x.FarmId });
    }
}
