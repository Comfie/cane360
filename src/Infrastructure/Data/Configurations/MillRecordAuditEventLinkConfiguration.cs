using Cane360.Domain.Auditing;
using Cane360.Domain.MillRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class MillRecordAuditEventLinkConfiguration : IEntityTypeConfiguration<MillRecordAuditEventLink>
{
    public void Configure(EntityTypeBuilder<MillRecordAuditEventLink> builder)
    {
        builder.ToTable("MillRecordAuditEventLinks", "mill", table =>
            table.HasCheckConstraint("CK_MillRecordAuditEventLinks_OneSubject",
                "num_nonnulls(\"MillId\", \"WeighbridgeTicketId\", \"GrowerStatementId\", \"StatementTicketMatchId\", \"EvidenceDocumentId\", \"MillRecordExportId\") = 1"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<AuditEvent>().WithMany()
            .HasForeignKey(x => new { x.AuditEventId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Mill>().WithMany().HasForeignKey(x => new { x.MillId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WeighbridgeTicket>().WithMany()
            .HasForeignKey(x => new { x.WeighbridgeTicketId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GrowerStatement>().WithMany()
            .HasForeignKey(x => new { x.GrowerStatementId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StatementTicketMatch>().WithMany()
            .HasForeignKey(x => new { x.StatementTicketMatchId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EvidenceDocument>().WithMany()
            .HasForeignKey(x => new { x.EvidenceDocumentId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MillRecordExport>().WithMany()
            .HasForeignKey(x => new { x.MillRecordExportId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AuditEventId, x.TenantId, x.FarmId }).IsUnique();
    }
}
