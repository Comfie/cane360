using Cane360.Domain.Farms;
using Cane360.Domain.Finance;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets", "finance", table =>
        {
            table.HasCheckConstraint("CK_Budgets_Version", "\"Version\" > 0");
            table.HasCheckConstraint("CK_Budgets_ReportingArea", "\"ReportingAreaHa\" IS NULL OR \"ReportingAreaHa\" > 0");
            table.HasCheckConstraint("CK_Budgets_ExpectedProduction", "\"ExpectedProductionTonnes\" IS NULL OR \"ExpectedProductionTonnes\" > 0");
            table.HasCheckConstraint("CK_Budgets_Lifecycle", "(\"Status\" = 'Draft' AND \"SubmittedByUserId\" IS NULL AND \"SubmittedAt\" IS NULL AND \"ApprovedAt\" IS NULL) OR (\"Status\" = 'Submitted' AND \"SubmittedByUserId\" IS NOT NULL AND \"SubmittedAt\" IS NOT NULL AND \"ApprovedAt\" IS NULL) OR (\"Status\" IN ('Approved', 'Superseded') AND \"SubmittedByUserId\" IS NOT NULL AND \"SubmittedAt\" IS NOT NULL AND \"ApprovedAt\" IS NOT NULL)");
            table.HasCheckConstraint("CK_Budgets_ApprovalIdentity", "(\"ApprovedAt\" IS NULL AND \"ApprovedByUserId\" IS NULL AND \"ApprovalIdempotencyKey\" IS NULL) OR (\"ApprovedAt\" IS NOT NULL AND \"ApprovedByUserId\" IS NOT NULL AND \"ApprovalIdempotencyKey\" IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ReportingAreaHa).HasPrecision(12, 4);
        builder.Property(x => x.ExpectedProductionTonnes).HasPrecision(14, 3);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.SubmittedByUserId).HasMaxLength(450);
        builder.Property(x => x.ApprovedByUserId).HasMaxLength(450);
        builder.Property(x => x.ApprovalIdempotencyKey).HasMaxLength(120);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.Ignore(x => x.TotalUsd);
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Field>().WithMany().HasForeignKey(x => new { x.FieldId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CropCycle>().WithMany().HasForeignKey(x => new { x.CropCycleId, x.FieldId })
            .HasPrincipalKey(x => new { x.Id, x.FieldId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Budget>().WithMany().HasForeignKey(x => new { x.SupersedesBudgetId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => new { x.BudgetId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.CropCycleId, x.Version }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.CropCycleId }).IsUnique()
            .HasFilter("\"Status\" = 'Approved'");
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.ApprovalIdempotencyKey }).IsUnique()
            .HasFilter("\"ApprovalIdempotencyKey\" IS NOT NULL");
        builder.HasIndex(x => new { x.SupersedesBudgetId, x.TenantId, x.FarmId }).IsUnique()
            .HasFilter("\"SupersedesBudgetId\" IS NOT NULL");
    }
}
