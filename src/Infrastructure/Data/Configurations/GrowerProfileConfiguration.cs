using Cane360.Domain.Farms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class GrowerProfileConfiguration : IEntityTypeConfiguration<GrowerProfile>
{
    public void Configure(EntityTypeBuilder<GrowerProfile> builder)
    {
        builder.ToTable("GrowerProfiles", "identity", table => table.HasCheckConstraint(
            "CK_GrowerProfiles_ProtectedNationalId",
            "(\"NationalIdCiphertext\" IS NULL AND \"NationalIdNonce\" IS NULL AND \"NationalIdTag\" IS NULL AND \"NationalIdFingerprint\" IS NULL AND \"NationalIdKeyId\" IS NULL AND \"NationalIdMask\" IS NULL) OR " +
            "(\"NationalIdCiphertext\" IS NOT NULL AND octet_length(\"NationalIdCiphertext\") > 0 AND \"NationalIdNonce\" IS NOT NULL AND octet_length(\"NationalIdNonce\") = 12 AND \"NationalIdTag\" IS NOT NULL AND octet_length(\"NationalIdTag\") = 16 AND \"NationalIdFingerprint\" IS NOT NULL AND octet_length(\"NationalIdFingerprint\") = 32 AND \"NationalIdKeyId\" IS NOT NULL AND length(\"NationalIdKeyId\") > 0 AND \"NationalIdMask\" IS NOT NULL AND length(\"NationalIdMask\") > 0)"));
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.Property(profile => profile.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(profile => profile.Phone).HasMaxLength(30);
        builder.HasIndex(profile => profile.TenantId).IsUnique();
        builder.Property(profile => profile.Title).HasMaxLength(40);
        builder.Property(profile => profile.FirstName).HasMaxLength(100);
        builder.Property(profile => profile.Surname).HasMaxLength(100);
        builder.Property(profile => profile.Sex).HasMaxLength(40);
        builder.Property(profile => profile.GrowerNumber).HasMaxLength(60);
        builder.Property(profile => profile.Association).HasMaxLength(120);
        builder.Property(profile => profile.MembershipNumber).HasMaxLength(60);
        builder.Property(profile => profile.RegisteredAddress).HasMaxLength(240);
        builder.Property(profile => profile.Email).HasMaxLength(254);
        builder.Property(profile => profile.PhotoReference).HasMaxLength(240);
        builder.Property(profile => profile.Active).HasDefaultValue(true);
        builder.Property(profile => profile.NationalIdKeyId).HasMaxLength(64);
        builder.Property(profile => profile.NationalIdMask).HasMaxLength(16);
        builder.HasIndex(profile => new { profile.TenantId, profile.NationalIdFingerprint }).IsUnique()
            .HasFilter("\"NationalIdFingerprint\" IS NOT NULL");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<GrowerProfile> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
