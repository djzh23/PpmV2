using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PpmV2.Domain.Assignments;
using PpmV2.Domain.Einsaetze;

namespace PpmV2.Infrastructure.Persistence.Configurations;

public class EinsatzConfiguration : IEntityTypeConfiguration<Einsatz>
{
    public void Configure(EntityTypeBuilder<Einsatz> builder)
    {
        builder.ToTable("Einsaetze");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.StartAtUtc)
            .IsRequired();

        builder.Property(e => e.EndAtUtc);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.LocationId)
            .IsRequired();

        // FK -> Locations, ohne Navigation Property
        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:n Einsatz -> Participants
        builder.HasMany(e => e.Participants)
            .WithOne()
            .HasForeignKey(p => p.EinsatzId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
