using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PpmV2.Domain.Locations;
using PpmV2.Domain.Shifts;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Infrastructure.Persistence;


/// <summary>
/// Central EF Core DbContext for the application.
/// </summary>
/// <remarks>
/// This DbContext is part of the Infrastructure (Persistence) layer and acts as the persistence boundary.
/// The application layer should not depend on this DbContext directly, but on repository/query abstractions.
/// </remarks>


public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    /// <summary>Read/write access to user profile data (separate from Identity tables).</summary>
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    /// <summary>Locations where shifts or events can take place.</summary>
    public DbSet<Location> Locations => Set<Location>();

    /// <summary>
    /// Shifts aggregate set.
    /// Note: The DbSet name is kept as legacy ("Einsaetze") for compatibility with existing code/migrations.
    /// </summary>
    public DbSet<Shift> Einsaetze => Set<Shift>();

    /// <summary>Participants assigned to a shift.</summary>
    public DbSet<ShiftParticipant> EinsatzParticipants => Set<ShiftParticipant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Automatically applies all IEntityTypeConfiguration<> mappings from this assembly.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // AppUser <-> UserProfile: 1:1 relationship (Identity user owns exactly one profile record).
        builder.Entity<AppUser>()
            .HasOne(u => u.Profile)
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.IdentityUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enforce uniqueness to guarantee the 1:1 mapping at database level.
        builder.Entity<UserProfile>()
            .HasIndex(p => p.IdentityUserId)
            .IsUnique();
    }
}
