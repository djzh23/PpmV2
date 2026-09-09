namespace PpmV2.Domain.Users;

public class UserProfile
{
    public Guid Id { get; set; }

    /// <summary>
    /// Links to the ASP.NET Identity user (AppUser) via a 1:1 relation.
    /// Role and approval state are managed on the Identity side; UserProfile holds display data only.
    /// </summary>
    public Guid IdentityUserId { get; set; }

    public string Firstname { get; set; } = default!;
    public string Lastname { get; set; } = default!;
    public string Email { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
