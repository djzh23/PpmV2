using PpmV2.Domain.Users;

namespace PpmV2.Application.Users.Interfaces;


public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id);
    Task<UserProfile?> GetByIdentityUserIdAsync(Guid identityUserId);
    Task<UserProfile?> GetByEmailAsync(string email);

    Task AddAsync(UserProfile profile);
    Task SaveChangesAsync();
}
