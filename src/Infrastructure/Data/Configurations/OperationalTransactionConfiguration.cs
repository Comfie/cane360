using Cane360.Domain.Farms;
using Cane360.Domain.Finance;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class OperationalTransactionConfiguration : IEntityTypeConfiguration<OperationalTransaction>
{
    public void Configure(EntityTypeBuilder<OperationalTransaction> builder)
    {
        builder.ToTable("OperationalTransactions", "finance", table =>
        {
            table.HasCheckConstraint("CK_OperationalTransactions_Amount", "\"AmountUsd\" > 0");
            table.HasCheckConstraint("CK_OperationalTransactions_Type", "\"Type\" IN ('Expense', 'Income')");
            table.HasCheckConstraint("CK_OperationalTransactions_Category", "\"Category\" IN ('Fuel', 'RepairsAndMaintenance', 'Utilities', 'Transport', 'ContractServices', 'CropInputs', 'CropSales', 'OtherExpense', 'OtherIncome')");
            table.HasCheckConstraint("CK_OperationalTransactions_Status", "\"Status\" IN ('Draft', 'Posted', 'Cancelled', 'Reversed')");
            table.HasCheckConstraint("CK_OperationalTransactions_PostedShape", "(\"Status\" = 'Draft' AND \"PostedAt\" IS NULL AND \"PostedByUserId\" IS NULL AND \"PostedIdempotencyKey\" IS NULL AND \"ReversalOfOperationalTransactionId\" IS NULL AND NOT \"IsClosedCycleCorrection\") OR (\"Status\" = 'Cancelled' AND \"PostedAt\" IS NULL AND \"PostedByUserId\" IS NULL AND \"PostedIdempotencyKey\" IS NULL AND \"ReversalOfOperationalTransactionId\" IS NULL AND NOT \"IsClosedCycleCorrection\") OR (\"Status\" = 'Posted' AND \"PostedAt\" IS NOT NULL AND \"PostedByUserId\" IS NOT NULL AND \"PostedIdempotencyKey\" IS NOT NULL AND \"ReversalOfOperationalTransactionId\" IS NULL) OR (\"Status\" = 'Reversed' AND \"PostedAt\" IS NOT NULL AND \"PostedByUserId\" IS NOT NULL AND \"PostedIdempotencyKey\" IS NOT NULL AND \"ReversalOfOperationalTransactionId\" IS NOT NULL AND length(trim(\"ReversalReason\")) > 0 AND \"ReversedAt\" IS NOT NULL AND \"ReversedByUserId\" IS NOT NULL AND NOT \"IsClosedCycleCorrection\")");
            table.HasCheckConstraint("CK_OperationalTransactions_ClosedCycleCorrection", "(NOT \"IsClosedCycleCorrection\" AND \"ClosedCycleCorrectionReason\" IS NULL AND \"ClosedCycleAuthorizedByUserId\" IS NULL AND \"ClosedCycleAuthorizedAt\" IS NULL) OR (\"IsClosedCycleCorrection\" AND \"Status\" = 'Posted' AND length(trim(\"ClosedCycleCorrectionReason\")) > 0 AND \"ClosedCycleAuthorizedByUserId\" IS NOT NULL AND \"ClosedCycleAuthorizedAt\" IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.EventDate).HasColumnType("date");
        builder.Property(x => x.PayeeOrPayer).HasMaxLength(160).IsRequired();
        builder.Property(x => x.AmountUsd).HasPrecision(20, 2);
        builder.Property(x => x.SourceReference).HasMaxLength(160);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.PostedByUserId).HasMaxLength(450);
        builder.Property(x => x.ReversedByUserId).HasMaxLength(450);
        builder.Property(x => x.PostedIdempotencyKey).HasMaxLength(128);
        builder.Property(x => x.ReversalReason).HasMaxLength(500);
        builder.Property(x => x.ClosedCycleCorrectionReason).HasMaxLength(500);
        builder.Property(x => x.ClosedCycleAuthorizedByUserId).HasMaxLength(450);
        builder.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PostedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReversedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ClosedCycleAuthorizedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationalTransaction>().WithMany()
            .HasForeignKey(x => new { x.ReversalOfOperationalTransactionId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Allocations).WithOne().HasForeignKey(x => new
            { x.OperationalTransactionId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.CreatedAt });
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.PostedIdempotencyKey }).IsUnique()
            .HasFilter("\"PostedIdempotencyKey\" IS NOT NULL");
        builder.HasIndex(x => x.ReversalOfOperationalTransactionId).IsUnique()
            .HasFilter("\"ReversalOfOperationalTransactionId\" IS NOT NULL");
    }
}
