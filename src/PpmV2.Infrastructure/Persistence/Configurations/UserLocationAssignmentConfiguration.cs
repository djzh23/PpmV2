using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PpmV2.Domain.Users;

namespace PpmV2.Infrastructure.Persistence.Configurations;

public sealed class UserLocationAssignmentConfiguration : IEntityTypeConfiguration<UserLocationAssignment>
{
    public void Configure(EntityTypeBuilder<UserLocationAssignment> builder)
    {
        builder.ToTable("user_location_assignments");

        builder.HasKey(x => new { x.UserId, x.LocationId });

        builder.Property(x => x.AssignedAt).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.LocationId);
    }
}
