using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Infrastructure.Identity;

public class AppRole : IdentityRole<Guid>
{
    public AppRole() : base(){}
    public AppRole(string roleName) : base(roleName) { }
}
