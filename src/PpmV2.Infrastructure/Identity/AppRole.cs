using Microsoft.AspNetCore.Identity;

namespace PpmV2.Infrastructure.Identity;

public class AppRole : IdentityRole<Guid>
{
    public AppRole() : base(){}
    public AppRole(string roleName) : base(roleName) { }
}
