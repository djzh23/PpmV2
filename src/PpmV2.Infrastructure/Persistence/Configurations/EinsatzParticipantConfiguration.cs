using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PpmV2.Domain.Einsaetze;

namespace PpmV2.Infrastructure.Persistence.Configurations;

public class EinsatzParticipantConfiguration : IEntityTypeConfiguration<EinsatzParticipant>
{
    public void Configure(EntityTypeBuilder<EinsatzParticipant> builder)
    {
        builder.ToTable("EinsatzParticipants");

        // Composite Key
        builder.HasKey(p => new { p.EinsatzId, p.UserId });

        // Enum -> int
        builder.Property(p => p.Role)
            .HasConversion<int>()
            .IsRequired();

        // Indexe (praktisch für Abfragen)
        builder.HasIndex(p => p.EinsatzId);
        builder.HasIndex(p => p.UserId);

        // optional: häufige Query: "Leader eines Einsatzes"
        builder.HasIndex(p => new { p.EinsatzId, p.Role });
    }
}
