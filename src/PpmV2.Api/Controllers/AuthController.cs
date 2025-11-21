using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Auth;
using PpmV2.Application.Auth.DTOs;

namespace PpmV2.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _auth.RegisterAsync(request);
        return new OkObjectResult(result);
    }

    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        return new OkObjectResult(result);
    }


}
