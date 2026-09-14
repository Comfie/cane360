using Cane360.Domain.MillRecords;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class StatementTicketMatchConfiguration : IEntityTypeConfiguration<StatementTicketMatch>
{
    public void Configure(EntityTypeBuilder<StatementTicketMatch> builder)
    {
        builder.ToTable("StatementTicketMatches", "mill", table =>
        {
            table.HasCheckConstraint("CK_StatementTicketMatches_Quantities",
                "\"MatchedTonnes\" > 0 AND (\"MatchedAmountUsd\" IS NULL OR \"MatchedAmountUsd\" >= 0)");
            table.HasCheckConstraint("CK_StatementTicketMatches_Action",
                "(\"Action\" = 'Added' AND \"ReversesMatchId\" IS NULL) OR (\"Action\" = 'Reversed' AND \"ReversesMatchId\" IS NOT NULL AND \"Reason\" IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.MatchedTonnes).HasPrecision(14, 3);
        builder.Property(x => x.MatchedAmountUsd).HasPrecision(14, 2);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
        builder.HasOne<GrowerStatement>().WithMany()
            .HasForeignKey(x => new { x.GrowerStatementId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WeighbridgeTicket>().WithMany()
            .HasForeignKey(x => new { x.WeighbridgeTicketId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StatementTicketMatch>().WithMany()
            .HasForeignKey(x => new { x.ReversesMatchId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.ReversesMatchId, x.TenantId, x.FarmId }).IsUnique()
            .HasFilter("\"ReversesMatchId\" IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.GrowerStatementId,
            x.WeighbridgeTicketId });
    }
}
