using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Auth.DTOs;
using PpmV2.Application.Auth.Services;
using System.Runtime.Intrinsics.X86;

namespace PpmV2.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Optional: With [ApiController] you automatically get 400 if the model is invalid.
        // No needs to check ModelState manually.

        var result = await _auth.RegisterAsync(request);

        if (!result.Success)
        {
            // z. B. "User with this email already exists."
            return BadRequest(new
            {
                error = result.ErrorMessage
            });
        }

        return Ok(new
        {
            userId = result.UserId,
            email = result.Email,
            // token = result.Token // in case of giving a token directly after register
        });
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);

        if (!result.Success)
        {
            // Todo: Differentiate between invalid credentials and other errors.
            // Later, using ErrorMessage to return 403 for "not approved".
            return Unauthorized(new
            {
                error = result.ErrorMessage
            });
        }

        return Ok(new
        {
            token = result.Token,   // currently possibly still null, later JWT
            userId = result.UserId,
            email = result.Email
        });
    }



}
