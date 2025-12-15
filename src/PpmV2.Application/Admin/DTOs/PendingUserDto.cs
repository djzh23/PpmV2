using PpmV2.Domain.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Admin.DTOs;

public record PendingUserDto(
    Guid Id,
    string Email,
    string Firstname,
    string Lastname,
    UserStatus Status
);