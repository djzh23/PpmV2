using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Domain.Users;

public enum UserStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Deactivated = 3
}
