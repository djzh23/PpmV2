using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Domain.Users.Abstractions;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id);
    Task<UserProfile?> GetByIdentityUserIdAsync(Guid identityUserId);
    Task<UserProfile?> GetByEmailAsync(string email);

    Task AddAsync(UserProfile profile);
    Task SaveChangesAsync();
}
