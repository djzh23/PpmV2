using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Auth.DTOs;

public sealed class LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
