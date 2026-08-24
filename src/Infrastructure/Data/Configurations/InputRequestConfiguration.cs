using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class InputRequestConfiguration : IEntityTypeConfiguration<InputRequest>
{
    public void Configure(EntityTypeBuilder<InputRequest> builder)
    {
        builder.ToTable("InputRequests", "inventory");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.RequestedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(entity => entity.SubmissionIdempotencyKey).HasMaxLength(120);
        builder.Property(entity => entity.RejectionReason).HasMaxLength(500);
        builder.Property(entity => entity.CancellationReason).HasMaxLength(500);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasOne<Activity>().WithMany().HasForeignKey(entity => new { entity.ActivityId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(activity => new { activity.Id, activity.TenantId, activity.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Field>().WithMany().HasForeignKey(entity => new { entity.FieldId, entity.FarmId })
            .HasPrincipalKey(field => new { field.Id, field.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CropCycle>().WithMany().HasForeignKey(entity => new { entity.CropCycleId, entity.FieldId })
            .HasPrincipalKey(cycle => new { cycle.Id, cycle.FieldId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(entity => entity.Lines).WithOne().HasForeignKey(line => new { line.InputRequestId, line.TenantId, line.FarmId })
            .HasPrincipalKey(entity => new { entity.Id, entity.TenantId, entity.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.TenantId, entity.FarmId, entity.ActivityId, entity.Status });
        builder.HasIndex(entity => entity.SubmissionIdempotencyKey).IsUnique().HasFilter("\"SubmissionIdempotencyKey\" IS NOT NULL");
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
