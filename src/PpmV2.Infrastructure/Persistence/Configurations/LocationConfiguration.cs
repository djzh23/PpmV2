using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PpmV2.Domain.Locations;

namespace PpmV2.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Location.
/// </summary>
/// <remarks>
/// A Location represents a place(Unterkunft) where a Shift/Einsatz or an Event can happen
/// (e.g., Location accommodation, external activity venue, camp site).
/// </remarks>
public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.District)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.Address)
            .HasMaxLength(250);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();

        // Prevent duplicates within the same district (e.g. two entries with identical name + district).
        builder.HasIndex(x => new { x.District, x.Name }).IsUnique();

        // Useful for frequent filtering on active/inactive locations.
        builder.HasIndex(x => x.IsActive);
    }
}
