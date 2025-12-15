using PpmV2.Application.Auth.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Auth;

public interface IAuthService
{
    //Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    //Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
}
