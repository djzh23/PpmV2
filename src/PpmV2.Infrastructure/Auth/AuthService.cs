using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Admin.Interfaces;
using PpmV2.Application.Auth.DTOs;
using PpmV2.Application.Auth.Services;
using PpmV2.Application.Users.Interfaces;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    //private readonly AppDbContext _db;
    private readonly IUserProfileRepository _userProfileRepository;

    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(UserManager<AppUser> userManager, IUserProfileRepository userProfileRepository, IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _userProfileRepository = userProfileRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
            return AuthResult.Fail("Invalid email or password.");

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!validPassword)
            return AuthResult.Fail("Invalid email or password.");

        if (user.Status != UserStatus.Approved)
            return AuthResult.Fail("Your account has not been approved by an administrator yet.");


        // TODO: generating JWT 
        // var token = _jwtTokenGenerator.GenerateToken(user);

        var claims = new JwtUserClaims(
                UserId: user.Id,
                Email: user.Email!,
                Role: user.Role,
                Status: user.Status
            );

        var token = _jwtTokenService.GenerateToken(claims);

        return AuthResult.Ok(
            userId: user.Id,
            email: user.Email!,
            token: token // oder token, wenn du ihn schon generierst
        );
    }
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        // check if Email exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return AuthResult.Fail("A user with this email already exists.");

        // Identity-user 
        var appUser = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            IsActive = true,
            //IsApproved = true, // later set to true after admin approval
            Status = UserStatus.Pending,
            Role = UserRole.Honorarkraft,
            IsProfileCompleted = false
        };

        var identityResult = await _userManager.CreateAsync(appUser, request.Password);

        if (!identityResult.Succeeded)
        {
            var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
            return AuthResult.Fail($"User creation failed: {errors}");
        }

        // Domain-Profile creation
        var profile = new UserProfile
        {
            IdentityUserId = appUser.Id,
            Email = request.Email,
            Firstname = request.Firstname,
            Lastname = request.Lastname,
            //Role = Enum.Parse<UserRole>(request.Role, ignoreCase: true),
            IsActive = true,
        };

        await _userProfileRepository.AddAsync(profile);
        await _userProfileRepository.SaveChangesAsync();

        return AuthResult.Ok(
            userId: appUser.Id,
            email: appUser.Email!,
            token: null
        );


    }
}
