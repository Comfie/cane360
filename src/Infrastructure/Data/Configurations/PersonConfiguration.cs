using Cane360.Domain.Activities;
using Cane360.Domain.Common;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Persons", "farm", table =>
        {
            table.HasCheckConstraint("CK_Persons_ActiveDates", "\"ActiveTo\" IS NULL OR \"ActiveTo\" >= \"ActiveFrom\"");
            table.HasCheckConstraint("CK_Persons_Status", "\"Status\" IN ('Active', 'Archived')");
        });
        builder.HasKey(person => person.Id);
        builder.Property(person => person.Id).ValueGeneratedNever();
        builder.Property(person => person.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(person => person.Phone).HasMaxLength(30);
        builder.Property(person => person.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(person => person.Version).IsConcurrencyToken();
        builder.HasAlternateKey(person => new { person.Id, person.FarmId });
        builder.HasMany(person => person.RoleAssignments)
            .WithOne()
            .HasForeignKey(assignment => new { assignment.PersonId, assignment.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(person => new { person.FarmId, person.Status });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Person> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
