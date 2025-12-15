using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Auth.DTOs;

public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    //public string Role { get; set; } // later enum mapping (Admin, etc.)
}
