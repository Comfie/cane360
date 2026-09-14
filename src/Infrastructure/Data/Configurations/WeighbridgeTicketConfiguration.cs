using Cane360.Domain.Farms;
using Cane360.Domain.MillRecords;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class WeighbridgeTicketConfiguration : IEntityTypeConfiguration<WeighbridgeTicket>
{
    public void Configure(EntityTypeBuilder<WeighbridgeTicket> builder)
    {
        builder.ToTable("WeighbridgeTickets", "mill", table =>
        {
            table.HasCheckConstraint("CK_WeighbridgeTickets_Weights",
                "\"GrossTonnes\" >= 0 AND \"NetTonnes\" > 0 AND (\"TareTonnes\" IS NULL OR (\"TareTonnes\" >= 0 AND \"GrossTonnes\" >= \"TareTonnes\" AND \"NetTonnes\" = \"GrossTonnes\" - \"TareTonnes\"))");
            table.HasCheckConstraint("CK_WeighbridgeTickets_Lifecycle",
                "(\"Status\" = 'Draft' AND \"RecordedByUserId\" IS NULL AND \"RecordedAt\" IS NULL AND \"RecordingIdempotencyKey\" IS NULL) OR (\"Status\" = 'Recorded' AND \"RecordedByUserId\" IS NOT NULL AND \"RecordedAt\" IS NOT NULL AND \"RecordingIdempotencyKey\" IS NOT NULL)");
            table.HasCheckConstraint("CK_WeighbridgeTickets_Correction",
                "(\"CorrectsTicketId\" IS NULL AND \"CorrectionReason\" IS NULL) OR (\"CorrectsTicketId\" IS NOT NULL AND \"CorrectionReason\" IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.TicketReference).HasMaxLength(120).IsRequired();
        builder.Property(x => x.TicketDate).HasColumnType("date");
        builder.Property(x => x.GrossTonnes).HasPrecision(14, 3);
        builder.Property(x => x.TareTonnes).HasPrecision(14, 3);
        builder.Property(x => x.NetTonnes).HasPrecision(14, 3);
        builder.Property(x => x.SourceReference).HasMaxLength(200);
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
        builder.HasOne<Field>().WithMany().HasForeignKey(x => new { x.FieldId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CropCycle>().WithMany().HasForeignKey(x => new { x.CropCycleId, x.FieldId })
            .HasPrincipalKey(x => new { x.Id, x.FieldId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WeighbridgeTicket>().WithMany()
            .HasForeignKey(x => new { x.CorrectsTicketId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.MillId, x.TicketReference })
            .IsUnique().HasFilter("\"CorrectsTicketId\" IS NULL");
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.RecordingIdempotencyKey }).IsUnique()
            .HasFilter("\"RecordingIdempotencyKey\" IS NOT NULL");
        builder.HasIndex(x => new { x.CorrectsTicketId, x.TenantId, x.FarmId }).IsUnique()
            .HasFilter("\"CorrectsTicketId\" IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.TicketDate });
    }
}
