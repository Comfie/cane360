using Cane360.Domain.Farms;
using Cane360.Domain.MillRecords;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class GrowerStatementConfiguration : IEntityTypeConfiguration<GrowerStatement>
{
    public void Configure(EntityTypeBuilder<GrowerStatement> builder)
    {
        builder.ToTable("GrowerStatements", "mill", table =>
        {
            table.HasCheckConstraint("CK_GrowerStatements_Period", "\"PeriodEnd\" >= \"PeriodStart\"");
            table.HasCheckConstraint("CK_GrowerStatements_Totals", "\"TotalTonnes\" >= 0 AND \"TotalAmountUsd\" >= 0");
            table.HasCheckConstraint("CK_GrowerStatements_Lifecycle",
                "(\"Status\" = 'Draft' AND \"RecordedByUserId\" IS NULL AND \"RecordedAt\" IS NULL AND \"RecordingIdempotencyKey\" IS NULL) OR (\"Status\" = 'Recorded' AND \"RecordedByUserId\" IS NOT NULL AND \"RecordedAt\" IS NOT NULL AND \"RecordingIdempotencyKey\" IS NOT NULL)");
            table.HasCheckConstraint("CK_GrowerStatements_Correction",
                "(\"CorrectsStatementId\" IS NULL AND \"CorrectionReason\" IS NULL) OR (\"CorrectsStatementId\" IS NOT NULL AND \"CorrectionReason\" IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.StatementReference).HasMaxLength(120).IsRequired();
        builder.Property(x => x.PeriodStart).HasColumnType("date");
        builder.Property(x => x.PeriodEnd).HasColumnType("date");
        builder.Property(x => x.TotalTonnes).HasPrecision(14, 3);
        builder.Property(x => x.TotalAmountUsd).HasPrecision(14, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.RecordedByUserId).HasMaxLength(450);
        builder.Property(x => x.RecordingIdempotencyKey).HasMaxLength(120);
        builder.Property(x => x.CorrectionReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Mill>().WithMany().HasForeignKey(x => new { x.MillId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GrowerStatement>().WithMany()
            .HasForeignKey(x => new { x.CorrectsStatementId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.MillId, x.StatementReference })
            .IsUnique().HasFilter("\"CorrectsStatementId\" IS NULL");
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.RecordingIdempotencyKey }).IsUnique()
            .HasFilter("\"RecordingIdempotencyKey\" IS NOT NULL");
        builder.HasIndex(x => new { x.CorrectsStatementId, x.TenantId, x.FarmId }).IsUnique()
            .HasFilter("\"CorrectsStatementId\" IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.PeriodStart, x.PeriodEnd });
    }
}
