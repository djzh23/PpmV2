using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the UserProfile entity.
/// </summary>
/// <remarks>
/// UserProfile stores application-specific user data that complements ASP.NET Core Identity.
/// A 1:1 relationship between AppUser (Identity) and UserProfile is enforced at database level.
/// </remarks>
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Firstname).IsRequired();
        builder.Property(p => p.Lastname).IsRequired();
        builder.Property(p => p.Email).IsRequired();

        // Ensures a single profile record per identity user (1:1).
        builder.HasIndex(p => p.IdentityUserId).IsUnique();

        // Identity (AppUser) owns exactly one profile. Deleting the identity user cascades the profile.
        builder.HasOne<AppUser>()
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.IdentityUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}