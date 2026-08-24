using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class StockReceiptConfiguration : IEntityTypeConfiguration<StockReceipt>
{
    public void Configure(EntityTypeBuilder<StockReceipt> builder)
    {
        builder.ToTable("StockReceipts", "inventory", table =>
        {
            table.HasCheckConstraint("CK_StockReceipts_Supplier", "(\"ReceiptType\" = 'Purchase' AND \"SupplierId\" IS NOT NULL) OR (\"ReceiptType\" = 'OpeningBalance' AND \"SupplierId\" IS NULL)");
            table.HasCheckConstraint("CK_StockReceipts_OpeningReason", "\"ReceiptType\" <> 'OpeningBalance' OR length(trim(\"Reason\")) > 0");
            table.HasCheckConstraint("CK_StockReceipts_PostingMetadata", "(\"Status\" NOT IN ('Posted', 'Reversed')) OR (\"PostedAt\" IS NOT NULL AND length(trim(\"PostedByUserId\")) > 0 AND length(trim(\"PostingIdempotencyKey\")) > 0)");
            table.HasCheckConstraint("CK_StockReceipts_ReversalMetadata", "\"Status\" <> 'Reversed' OR (\"ReversedAt\" IS NOT NULL AND length(trim(\"ReversedByUserId\")) > 0 AND length(trim(\"ReversalIdempotencyKey\")) > 0)");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.ReceiptType).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.ReceiptDate).HasColumnType("date");
        builder.Property(entity => entity.SourceReference).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.Property(entity => entity.LateEntryReason).HasMaxLength(500);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.PostedByUserId).HasMaxLength(450);
        builder.Property(entity => entity.PostingIdempotencyKey).HasMaxLength(120);
        builder.Property(entity => entity.ReversedByUserId).HasMaxLength(450);
        builder.Property(entity => entity.ReversalIdempotencyKey).HasMaxLength(120);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.HasOne<Farm>().WithMany()
            .HasForeignKey(entity => new { entity.FarmId, entity.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Store>().WithMany().HasForeignKey(entity => new { entity.StoreId, entity.FarmId })
            .HasPrincipalKey(store => new { Id = store.Id, store.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Supplier>().WithMany()
            .HasForeignKey(entity => new { entity.SupplierId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(supplier => new { supplier.Id, supplier.TenantId, supplier.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Cane360.Domain.Activities.Person>().WithMany()
            .HasForeignKey(entity => new { entity.ReceivedByPersonId, entity.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(entity => entity.Lines).WithOne()
            .HasForeignKey(line => new { line.StockReceiptId, line.TenantId, line.FarmId })
            .HasPrincipalKey(receipt => new { Id = receipt.Id, receipt.TenantId, receipt.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockReceipt>().WithMany().HasForeignKey(entity => entity.CorrectsStockReceiptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.PostedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.ReversedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.FarmId, entity.Status, entity.ReceiptDate });
        builder.HasIndex(entity => new { entity.FarmId, entity.SupplierId, entity.SourceReference });
        builder.HasIndex(entity => entity.PostingIdempotencyKey).IsUnique().HasFilter("\"PostingIdempotencyKey\" IS NOT NULL");
        builder.HasIndex(entity => entity.ReversalIdempotencyKey).IsUnique().HasFilter("\"ReversalIdempotencyKey\" IS NOT NULL");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<StockReceipt> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
