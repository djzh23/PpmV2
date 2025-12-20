using Microsoft.AspNetCore.Mvc;
using PpmV2.Api.Common;
using PpmV2.Application.Auth.DTOs;
using PpmV2.Application.Auth.Interfaces;

namespace PpmV2.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>
/// Public authentication endpoints (register/login).
/// </summary>
/// <remarks>
/// This controller acts as a thin HTTP boundary:
/// - delegates all authentication logic to the application/infrastructure service
/// - converts domain/application errors into ProblemDetails via ApiProblem
/// </remarks>
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _auth.RegisterAsync(request);

        // Standardized error response mapping (ProblemDetails) for failed auth results.
        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        // Registration does not return a token (approval workflow).
        return Ok(new
        {
            userId = result.UserId,
            email = result.Email
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return Ok(new
        {
            token = result.Token,
            userId = result.UserId,
            email = result.Email
        });
    }
}
