using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Auth.DTOs;

public class RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Firstname { get; init; }
    public required string Lastname { get; init; }
    //public string Role { get; set; } // later enum mapping (Admin, etc.)
}
