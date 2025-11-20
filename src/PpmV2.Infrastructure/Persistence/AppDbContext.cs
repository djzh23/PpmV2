using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // AppUser <-> UserProfile 1:1 relation
        builder.Entity<AppUser>()
            .HasOne(u => u.Profile)
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.IdentityUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserProfile>()
            .HasIndex(p => p.IdentityUserId)
            .IsUnique();
    }
}
