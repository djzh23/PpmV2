using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Firstname).IsRequired();
        builder.Property(p => p.Lastname).IsRequired();
        builder.Property(p => p.Email).IsRequired();

        builder.HasIndex(p => p.IdentityUserId).IsUnique();

        // AppUser <-> UserProfile 1:1 relation
        builder.HasOne<AppUser>()
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.IdentityUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}