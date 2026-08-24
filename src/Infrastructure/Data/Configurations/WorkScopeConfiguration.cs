using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class WorkScopeConfiguration : IEntityTypeConfiguration<WorkScope>
{
    public void Configure(EntityTypeBuilder<WorkScope> builder)
    {
        builder.ToTable("WorkScopes", "labour", table =>
        {
            table.HasCheckConstraint("CK_WorkScopes_Type", "\"ScopeType\" IN ('LineRange', 'NamedSection')");
            table.HasCheckConstraint("CK_WorkScopes_Shape", "(\"ScopeType\" = 'LineRange' AND \"FieldLineProfileId\" IS NOT NULL AND \"StartLine\" > 0 AND \"EndLine\" >= \"StartLine\" AND \"SectionName\" IS NULL AND \"NormalizedSectionName\" IS NULL) OR (\"ScopeType\" = 'NamedSection' AND \"FieldLineProfileId\" IS NULL AND \"StartLine\" IS NULL AND \"EndLine\" IS NULL AND length(trim(\"NormalizedSectionName\")) > 0)");
        });
        builder.HasKey(scope => scope.Id);
        builder.Property(scope => scope.Id).ValueGeneratedNever();
        builder.Property(scope => scope.ScopeType).HasConversion<string>().HasMaxLength(24);
        builder.Property(scope => scope.SectionName).HasMaxLength(120);
        builder.Property(scope => scope.NormalizedSectionName).HasMaxLength(120);
        builder.HasOne<Activity>().WithMany()
            .HasForeignKey(scope => new { scope.ActivityId, scope.TenantId, scope.FarmId })
            .HasPrincipalKey(activity => new { activity.Id, activity.TenantId, activity.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldLineProfile>().WithMany().HasForeignKey(scope => scope.FieldLineProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(scope => new { scope.ActivityId, scope.NormalizedSectionName })
            .HasFilter("\"ScopeType\" = 'NamedSection' AND \"SupersededAt\" IS NULL")
            .HasDatabaseName("IX_WorkScopes_Activity_NamedSection");
    }
}
