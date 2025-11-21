using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Auth.DTOs;

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}
