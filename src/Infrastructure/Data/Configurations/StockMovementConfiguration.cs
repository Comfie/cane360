using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", "inventory", table =>
        {
            table.HasCheckConstraint("CK_StockMovements_NonzeroQuantity", "\"SignedQuantity\" <> 0");
            table.HasCheckConstraint("CK_StockMovements_Signs", "sign(\"SignedQuantity\") = sign(\"SignedValueUsd\") OR \"SignedValueUsd\" = 0");
            table.HasCheckConstraint("CK_StockMovements_Reversal", "(\"MovementType\" IN ('ReceiptReversal', 'IssueReversal', 'ReturnReversal')) = (\"ReversalOfStockMovementId\" IS NOT NULL)");
            table.HasCheckConstraint("CK_StockMovements_OneSource", "num_nonnulls(\"StockReceiptLineId\", \"StockIssueLineId\", \"StockReturnLineId\") = 1");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.Property(entity => entity.ItemCodeSnapshot).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.ItemNameSnapshot).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.LotCodeSnapshot).HasMaxLength(60);
        builder.Property(entity => entity.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.MovementType).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.SignedQuantity).HasPrecision(18, 6);
        builder.Property(entity => entity.SignedValueUsd).HasPrecision(20, 6);
        builder.Property(entity => entity.EventDate).HasColumnType("date");
        builder.Property(entity => entity.PostedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(entity => entity.PostingSequence).UseIdentityAlwaysColumn();
        builder.Property(entity => entity.PostingIdentity).HasMaxLength(120).IsRequired();
        builder.HasOne<StockPosition>().WithMany().HasForeignKey(entity => new { entity.StockPositionId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(position => new { position.Id, position.TenantId, position.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockReceiptLine>().WithMany()
            .HasForeignKey(entity => new { entity.StockReceiptLineId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(line => new { line.Id, line.TenantId, line.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockIssueLine>().WithMany()
            .HasForeignKey(entity => new { entity.StockIssueLineId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(line => new { line.Id, line.TenantId, line.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockReturnLine>().WithMany()
            .HasForeignKey(entity => new { entity.StockReturnLineId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(line => new { line.Id, line.TenantId, line.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany().HasForeignKey(entity => entity.ReversalOfStockMovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.PostedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Cane360.Domain.Activities.Person>().WithMany()
            .HasForeignKey(entity => new { entity.OperationalPersonId, entity.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.PostingIdentity).IsUnique();
        builder.HasIndex(entity => entity.ReversalOfStockMovementId).IsUnique().HasFilter("\"ReversalOfStockMovementId\" IS NOT NULL");
        builder.HasIndex(entity => new { entity.TenantId, entity.FarmId, entity.StoreId, entity.InventoryItemId, entity.InventoryLotId, entity.PostingSequence });
        builder.HasIndex(entity => new { entity.StockPositionId, entity.PostingSequence });
        builder.HasIndex(entity => entity.StockReceiptLineId);
        builder.HasIndex(entity => entity.StockIssueLineId);
        builder.HasIndex(entity => entity.StockReturnLineId);
    }
}
