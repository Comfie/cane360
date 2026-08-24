using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class FieldReceiptConfiguration : IEntityTypeConfiguration<FieldReceipt>
{
    public void Configure(EntityTypeBuilder<FieldReceipt> builder)
    {
        builder.ToTable("FieldReceipts", "inventory", table =>
        {
            table.HasCheckConstraint("CK_FieldReceipts_Delay", "\"EntryDelayDays\" >= 0");
            table.HasCheckConstraint("CK_FieldReceipts_LateReason", "\"EntryDelayDays\" <= 2 OR length(trim(\"LateEntryReason\")) > 0");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); builder.Property(x => x.EnteredByUserId).HasMaxLength(450); builder.Property(x => x.LateEntryReason).HasMaxLength(500); builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne<StockIssue>().WithMany().HasForeignKey(x => new { x.StockIssueId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Field>().WithMany().HasForeignKey(x => new { x.FieldId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CropCycle>().WithMany().HasForeignKey(x => new { x.CropCycleId, x.FieldId }).HasPrincipalKey(x => new { x.Id, x.FieldId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Activity>().WithMany().HasForeignKey(x => new { x.ActivityId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.RecipientPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.EnteredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => new { x.FieldReceiptId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.StockIssueId, x.Status });
    }
}
