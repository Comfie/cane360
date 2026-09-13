using Cane360.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class BudgetLineConfiguration : IEntityTypeConfiguration<BudgetLine>
{
    public void Configure(EntityTypeBuilder<BudgetLine> builder)
    {
        builder.ToTable("BudgetLines", "finance", table =>
        {
            table.HasCheckConstraint("CK_BudgetLines_Amount", "\"AmountUsd\" > 0");
            table.HasCheckConstraint("CK_BudgetLines_Quantity", "\"Quantity\" IS NULL OR \"Quantity\" > 0");
            table.HasCheckConstraint("CK_BudgetLines_UnitRate", "\"UnitRateUsd\" IS NULL OR \"UnitRateUsd\" > 0");
            table.HasCheckConstraint("CK_BudgetLines_UnitValues", "(\"Quantity\" IS NULL AND \"Unit\" IS NULL AND \"UnitRateUsd\" IS NULL) OR (\"Quantity\" IS NOT NULL AND \"Unit\" IS NOT NULL AND \"UnitRateUsd\" IS NOT NULL)");
            table.HasCheckConstraint("CK_BudgetLines_Category", "\"Category\" IN ('Labour', 'AppliedInput', 'DirectExpense', 'ApprovedVarianceCost')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Description).HasMaxLength(240).IsRequired();
        builder.Property(x => x.AmountUsd).HasPrecision(20, 2);
        builder.Property(x => x.Quantity).HasPrecision(18, 6);
        builder.Property(x => x.Unit).HasMaxLength(32);
        builder.Property(x => x.UnitRateUsd).HasPrecision(20, 6);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.BudgetId, x.Category });
    }
}
