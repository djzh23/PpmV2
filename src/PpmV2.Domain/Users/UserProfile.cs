using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Domain.Users;

public class UserProfile
{
    /// <summary>
    /// Primary Key for Profil Tabel.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Refers to the technical identity user (ASP.NET Identity).
    /// Infrastructure "translates" AppUser : IdentityUser<Guid> TO UserProfile
    /// 1:1 Relation via IdentityUserId
    /// </summary>
    public Guid IdentityUserId { get; set; }

    public string Firstname { get; set; } = default!;
    public string Lastname { get; set; } = default!;

    public string Email { get; set; } = default!; // convenient for UI, even if Identity has it too

    public UserRole Role { get; set; }

    public bool IsApproved { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
}
