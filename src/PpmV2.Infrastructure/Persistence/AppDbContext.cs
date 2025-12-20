using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PpmV2.Domain.Locations;
using PpmV2.Domain.Shifts;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Shift> Einsaetze => Set<Shift>();
    public DbSet<ShiftParticipant> EinsatzParticipants => Set<ShiftParticipant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        //builder.ApplyConfiguration(new EinsatzConfiguration());
        //builder.ApplyConfiguration(new EinsatzParticipantConfiguration());
        //builder.ApplyConfiguration(new LocationConfiguration());

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
