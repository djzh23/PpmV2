using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Auth.DTOs;

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Token { get; set; }

    public Guid? UserId { get; set; }
    public string? Email { get; set; }

    public static AuthResult Ok(Guid userId, string email, string? token = null)
        => new()
        {
            Success = true,
            UserId = userId,
            Email = email,
            Token = token
        };

    public static AuthResult Fail(string error)
        => new() { Success = false, ErrorMessage = error };
}
