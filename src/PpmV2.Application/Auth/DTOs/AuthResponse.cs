using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Auth.DTOs;

public class AuthResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; }

    // later for JWT:
    // public string Token { get; set; }
}
