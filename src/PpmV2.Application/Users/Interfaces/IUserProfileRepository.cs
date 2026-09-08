using PpmV2.Domain.Users;

namespace PpmV2.Application.Users.Interfaces;


public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserProfile?> GetByIdentityUserIdAsync(Guid identityUserId, CancellationToken ct = default);
    Task<UserProfile?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task AddAsync(UserProfile profile, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
