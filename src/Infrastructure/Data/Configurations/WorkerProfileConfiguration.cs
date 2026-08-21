using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class WorkerProfileConfiguration : IEntityTypeConfiguration<WorkerProfile>
{
    public void Configure(EntityTypeBuilder<WorkerProfile> builder)
    {
        builder.ToTable("WorkerProfiles", "labour", table =>
        {
            table.HasCheckConstraint("CK_WorkerProfiles_EmploymentType", "\"EmploymentType\" IN ('Permanent', 'Seasonal', 'Casual', 'Contract', 'TaskBased')");
            table.HasCheckConstraint("CK_WorkerProfiles_ActiveDates", "\"ActiveTo\" IS NULL OR \"ActiveTo\" >= \"ActiveFrom\"");
            table.HasCheckConstraint("CK_WorkerProfiles_Status", "\"Status\" IN ('Active', 'Archived')");
            table.HasCheckConstraint("CK_WorkerProfiles_ProtectedNationalId", "octet_length(\"NationalIdCiphertext\") > 0 AND octet_length(\"NationalIdNonce\") = 12 AND octet_length(\"NationalIdTag\") = 16 AND octet_length(\"NationalIdFingerprint\") = 32");
        });
        builder.HasKey(worker => worker.Id);
        builder.Property(worker => worker.Id).ValueGeneratedNever();
        builder.Property(worker => worker.EmploymentType).HasConversion<string>().HasMaxLength(24);
        builder.Property(worker => worker.ActiveFrom).HasColumnType("date");
        builder.Property(worker => worker.ActiveTo).HasColumnType("date");
        builder.Property(worker => worker.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(worker => worker.NationalIdCiphertext).IsRequired();
        builder.Property(worker => worker.NationalIdNonce).IsRequired();
        builder.Property(worker => worker.NationalIdTag).IsRequired();
        builder.Property(worker => worker.NationalIdKeyId).HasMaxLength(64).IsRequired();
        builder.Property(worker => worker.NationalIdFingerprint).IsRequired();
        builder.Property(worker => worker.NationalIdMask).HasMaxLength(16).IsRequired();
        builder.Property(worker => worker.Version).IsConcurrencyToken();
        builder.HasAlternateKey(worker => new { worker.Id, worker.TenantId, worker.FarmId });
        builder.HasOne<Farm>().WithMany()
            .HasForeignKey(worker => new { worker.FarmId, worker.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany()
            .HasForeignKey(worker => new { worker.PersonId, worker.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(worker => new { worker.FarmId, worker.PersonId }).IsUnique();
        builder.HasIndex(worker => new { worker.FarmId, worker.NationalIdFingerprint })
            .IsUnique()
            .HasDatabaseName("UX_WorkerProfiles_Farm_NationalIdFingerprint");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<WorkerProfile> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
