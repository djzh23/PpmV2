using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PpmV2.Domain.Auth;
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

    /// <summary>Location assignments defining a Festmitarbeiter's regular work profile.</summary>
    public DbSet<UserLocationAssignment> UserLocationAssignments => Set<UserLocationAssignment>();

    /// <summary>Refresh tokens for JWT token rotation.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // === Identity tables (CUSTOM NAMES) ===
        builder.Entity<AppUser>().ToTable("users");
        builder.Entity<AppRole>().ToTable("roles");

        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        // Automatically applies all IEntityTypeConfiguration<> mappings from this assembly.
        // Table names for domain entities are defined there, no need to repeat them here.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // AppUser <-> UserProfile: 1:1 relationship (Identity user owns exactly one profile record).
        builder.Entity<AppUser>()
            .HasOne(u => u.Profile)
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.IdentityUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes on AppUser columns used in admin queries and staff availability filters.
        // AppUser has no IEntityTypeConfiguration, so these are defined here.
        builder.Entity<AppUser>().HasIndex(u => u.Status);
        builder.Entity<AppUser>().HasIndex(u => u.Role);
    }
}
