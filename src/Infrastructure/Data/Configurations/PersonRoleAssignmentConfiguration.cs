using Cane360.Domain.Activities;
using Cane360.Domain.Common;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PersonRoleAssignmentConfiguration : IEntityTypeConfiguration<PersonRoleAssignment>
{
    public void Configure(EntityTypeBuilder<PersonRoleAssignment> builder)
    {
        builder.ToTable("PersonRoleAssignments", "farm", table =>
        {
            table.HasCheckConstraint("CK_PersonRoleAssignments_EffectiveDates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            table.HasCheckConstraint("CK_PersonRoleAssignments_Role", "\"Role\" IN ('FarmManager', 'Supervisor', 'Storekeeper')");
            table.HasCheckConstraint("CK_PersonRoleAssignments_PrimaryRole", "NOT \"IsPrimary\" OR \"Role\" = 'FarmManager'");
        });
        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id).ValueGeneratedNever();
        builder.Property(assignment => assignment.Role).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(assignment => new { assignment.PersonId, assignment.Role })
            .IsUnique()
            .HasFilter("\"EffectiveTo\" IS NULL");
        builder.HasIndex(assignment => assignment.FarmId)
            .IsUnique()
            .HasFilter("\"Role\" = 'FarmManager' AND \"IsPrimary\" AND \"EffectiveTo\" IS NULL");
    }
}
