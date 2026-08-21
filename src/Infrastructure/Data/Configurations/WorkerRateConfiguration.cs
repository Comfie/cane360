using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class WorkerRateConfiguration : IEntityTypeConfiguration<WorkerRate>
{
    public void Configure(EntityTypeBuilder<WorkerRate> builder)
    {
        builder.ToTable("WorkerRates", "labour", table =>
        {
            table.HasCheckConstraint("CK_WorkerRates_Basis", "\"Basis\" IN ('Daily', 'Monthly', 'Hectare', 'StandardLine')");
            table.HasCheckConstraint("CK_WorkerRates_PositiveRate", "\"RateUsd\" > 0");
            table.HasCheckConstraint("CK_WorkerRates_EffectiveDates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            table.HasCheckConstraint("CK_WorkerRates_ActivityScope", "((\"Basis\" IN ('Hectare', 'StandardLine')) = (\"ActivityTypeId\" IS NOT NULL))");
        });
        builder.HasKey(rate => rate.Id);
        builder.Property(rate => rate.Id).ValueGeneratedNever();
        builder.Property(rate => rate.Basis).HasConversion<string>().HasMaxLength(24);
        builder.Property(rate => rate.RateUsd).HasPrecision(12, 4);
        builder.Property(rate => rate.EffectiveFrom).HasColumnType("date");
        builder.Property(rate => rate.EffectiveTo).HasColumnType("date");
        builder.Property(rate => rate.Version).IsConcurrencyToken();
        builder.HasOne<WorkerProfile>().WithMany()
            .HasForeignKey(rate => new { rate.WorkerProfileId, rate.TenantId, rate.FarmId })
            .HasPrincipalKey(worker => new { worker.Id, worker.TenantId, worker.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ActivityType>().WithMany()
            .HasForeignKey(rate => new { rate.ActivityTypeId, rate.TenantId })
            .HasPrincipalKey(type => new { type.Id, type.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rate => new { rate.WorkerProfileId, rate.Basis, rate.ActivityTypeId, rate.EffectiveFrom });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<WorkerRate> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
