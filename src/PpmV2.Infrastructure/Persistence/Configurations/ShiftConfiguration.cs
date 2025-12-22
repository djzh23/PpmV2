using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PpmV2.Domain.Locations;
using PpmV2.Domain.Shifts;

namespace PpmV2.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Shift aggregate.
/// </summary>
/// <remarks>
/// This configuration defines the persistence mapping for Shifts,
/// including table mapping, required fields and relationships.
/// 
/// Note: The underlying database table name ("Einsaetze") is kept
/// for compatibility with existing migrations and data.
/// The refactor from "Einsaetze" to "Shifts" is applied at code level only.
/// </remarks>
public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        // Keep legacy table name to avoid schema-breaking migrations during refactoring.
        builder.ToTable("shifts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.StartAtUtc)
            .IsRequired();

        builder.Property(e => e.EndAtUtc);

        // Persist enum as int to keep database representation stable.
        builder.Property(e => e.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.LocationId)
            .IsRequired();

        // Foreign key to Location without navigation property on Location side.
        // A Shift always belongs to exactly one Location.
        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many relationship: Shift -> Participants.
        // Participants are deleted automatically when the owning Shift is deleted.
        builder.HasMany(e => e.Participants)
            .WithOne()
            .HasForeignKey(p => p.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
