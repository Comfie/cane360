using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents", "audit");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Id).ValueGeneratedNever();
        builder.HasAlternateKey(audit => new { audit.Id, audit.TenantId, audit.FarmId });
        builder.Property(audit => audit.SubjectType).HasMaxLength(80).IsRequired();
        builder.Property(audit => audit.Action).HasMaxLength(80).IsRequired();
        builder.Property(audit => audit.AuthenticatedUserId).HasMaxLength(450).IsRequired();
        builder.Property(audit => audit.SecurityRole).HasMaxLength(40).IsRequired();
        builder.Property(audit => audit.CorrelationId).HasMaxLength(120).IsRequired();
        builder.Property(audit => audit.Reason).HasMaxLength(500);
        builder.Property(audit => audit.SafeSummary).HasMaxLength(500).IsRequired();
        builder.HasOne<Farm>().WithMany()
            .HasForeignKey(audit => new { audit.FarmId, audit.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany()
            .HasForeignKey(audit => new { audit.OperationalPersonId, audit.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(audit => audit.AuthenticatedUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(audit => new { audit.TenantId, audit.FarmId, audit.SubjectType, audit.SubjectId, audit.OccurredAt });
    }
}
