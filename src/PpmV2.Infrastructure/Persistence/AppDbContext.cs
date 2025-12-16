using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PpmV2.Domain.Assignments;
using PpmV2.Domain.Einsaetze;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;
using PpmV2.Infrastructure.Persistence.Assignments;
using System.Reflection.Emit;

namespace PpmV2.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Location> Locations => Set<Location>();

    //public DbSet<Einsatz> Einsaetze => Set<Einsatz>();
    //public DbSet<EinsatzParticipant> EinsatzParticipants => Set<EinsatzParticipant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // AppUser <-> UserProfile 1:1 relation
        builder.Entity<AppUser>()
            .HasOne(u => u.Profile)
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.IdentityUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserProfile>()
            .HasIndex(p => p.IdentityUserId)
            .IsUnique();


        // Apply Assignements/Location configurations
        //builder.ApplyConfiguration(new LocationConfiguration());
    }
}
