using Cane360.Domain.Farms;
using Cane360.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class TransactionAllocationConfiguration : IEntityTypeConfiguration<TransactionAllocation>
{
    public void Configure(EntityTypeBuilder<TransactionAllocation> builder)
    {
        builder.ToTable("TransactionAllocations", "finance", table =>
        {
            table.HasCheckConstraint("CK_TransactionAllocations_Amount", "\"AmountUsd\" > 0");
            table.HasCheckConstraint("CK_TransactionAllocations_Category", "\"Category\" IN ('Fuel', 'RepairsAndMaintenance', 'Utilities', 'Transport', 'ContractServices', 'CropInputs', 'CropSales', 'OtherExpense', 'OtherIncome')");
            table.HasCheckConstraint("CK_TransactionAllocations_Shape", "(\"AllocationType\" = 'CropCycleDirect' AND \"CropCycleId\" IS NOT NULL AND \"FieldId\" IS NOT NULL) OR (\"AllocationType\" = 'Field' AND \"CropCycleId\" IS NULL AND \"FieldId\" IS NOT NULL) OR (\"AllocationType\" = 'FarmOverhead' AND \"CropCycleId\" IS NULL AND \"FieldId\" IS NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.AllocationType).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.AmountUsd).HasPrecision(20, 2);
        builder.HasOne<Field>().WithMany().HasForeignKey(x => new { x.FieldId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CropCycle>().WithMany().HasForeignKey(x => new { x.CropCycleId, x.FieldId })
            .HasPrincipalKey(x => new { x.Id, x.FieldId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.OperationalTransactionId });
    }
}
