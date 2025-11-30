    using Microsoft.AspNetCore.Identity;
using PpmV2.Domain.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Infrastructure.Identity;

public class AppUser : IdentityUser<Guid>
{
    //public bool IsApproved { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public UserRole Role { get; set; } = UserRole.Honorarkraft;
    public bool IsActive { get; set; } = true;
    public bool IsProfileCompleted { get; set; }

    // navigation property to UserProfile (domain-level entity)
    public UserProfile? Profile { get; set; }
}
