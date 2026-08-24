using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class StockIssueConfiguration : IEntityTypeConfiguration<StockIssue>
{
    public void Configure(EntityTypeBuilder<StockIssue> builder)
    {
        builder.ToTable("StockIssues", "inventory", table =>
        {
            table.HasCheckConstraint("CK_StockIssues_LateReason", "\"EntryDelayDays\" <= 2 OR length(trim(\"LateEntryReason\")) > 0");
            table.HasCheckConstraint("CK_StockIssues_EntryDelay", "\"EntryDelayDays\" >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.LateEntryReason).HasMaxLength(500);
        builder.Property(entity => entity.PostedByUserId).HasMaxLength(450);
        builder.Property(entity => entity.PostingIdempotencyKey).HasMaxLength(120);
        builder.Property(entity => entity.CorrectionReason).HasMaxLength(500);
        builder.Property(entity => entity.CorrectionRequestedByUserId).HasMaxLength(450);
        builder.Property(entity => entity.ReversalIdempotencyKey).HasMaxLength(120);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasOne<Store>().WithMany().HasForeignKey(entity => new { entity.StoreId, entity.FarmId })
            .HasPrincipalKey(store => new { store.Id, store.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InputRequest>().WithMany().HasForeignKey(entity => new { entity.InputRequestId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(request => new { request.Id, request.TenantId, request.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(entity => new { entity.IssuerPersonId, entity.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(entity => new { entity.RecipientPersonId, entity.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.PostedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.CorrectionRequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(entity => entity.Lines).WithOne().HasForeignKey(line => new { line.StockIssueId, line.TenantId, line.FarmId })
            .HasPrincipalKey(entity => new { entity.Id, entity.TenantId, entity.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.PostingIdempotencyKey).IsUnique().HasFilter("\"PostingIdempotencyKey\" IS NOT NULL");
        builder.HasIndex(entity => entity.ReversalIdempotencyKey).IsUnique().HasFilter("\"ReversalIdempotencyKey\" IS NOT NULL");
        builder.HasIndex(entity => new { entity.TenantId, entity.FarmId, entity.InputRequestId, entity.Status });
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
