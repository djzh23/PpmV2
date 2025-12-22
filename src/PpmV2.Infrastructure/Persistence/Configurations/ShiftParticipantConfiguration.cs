using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PpmV2.Domain.Shifts;

namespace PpmV2.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ShiftParticipant.
/// </summary>
/// <remarks>
/// ShiftParticipant represents the association between a Shift and a User,
/// including the assigned role within the Shift (e.g. Lead, Member).
///
/// This entity acts as a join table with additional domain meaning (role),
/// therefore it is modeled explicitly instead of using a simple many-to-many mapping.
/// </remarks>
public class ShiftParticipantConfiguration : IEntityTypeConfiguration<ShiftParticipant>
{
    public void Configure(EntityTypeBuilder<ShiftParticipant> builder)
    {
        // Keep legacy table name for compatibility with existing schema.// Table name
        builder.ToTable("shift_participants");

        // Composite primary key ensures:
        // - a user can participate only once per shift
        // - no surrogate key is required for the join entity
        builder.HasKey(p => new { p.ShiftId, p.UserId });

        // Persist role enum as int to keep database representation stable.
        builder.Property(p => p.Role)
            .HasConversion<int>()
            .IsRequired();

        // Indexes to support common access patterns.
        builder.HasIndex(p => p.ShiftId);
        builder.HasIndex(p => p.UserId);

        // Optimizes queries such as "find the lead of a given shift".
        builder.HasIndex(p => new { p.ShiftId, p.Role });
    }
}
