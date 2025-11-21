using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Auth;
using PpmV2.Application.Auth.DTOs;
using PpmV2.Domain.Users;
using PpmV2.Domain.Users.Abstractions;
using PpmV2.Infrastructure.Identity;
using PpmV2.Infrastructure.Persistence;
using PpmV2.Infrastructure.Persistence.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    //private readonly AppDbContext _db;
    private readonly IUserProfileRepository _userProfileRepository;

    public AuthService(UserManager<AppUser> userManager, IUserProfileRepository userProfileRepository)
    {
        _userManager = userManager;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
            throw new InvalidOperationException("Invalid email or password.");

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!validPassword)
            throw new InvalidOperationException("Invalid email or password.");

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email!
        };
    }
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // check if Email exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        // Identity-user 
        var appUser = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            IsActive = true,
            IsApproved = true, // later set to true after admin approval
            IsProfileCompleted = false
        };

        var identityResult = await _userManager.CreateAsync(appUser, request.Password);

        if (!identityResult.Succeeded)
        {
            var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User creation failed: {errors}");
        }

        // Domain-Profile creation
        var profile = new UserProfile
        {
            IdentityUserId = appUser.Id,
            Email = request.Email,
            Firstname = string.Empty,
            Lastname = string.Empty,
            Role = Enum.Parse<UserRole>(request.Role, ignoreCase: true),
            IsActive = true,
            IsApproved = false, // later set to true after admin approval
        };

        await _userProfileRepository.AddAsync(profile);
        await _userProfileRepository.SaveChangesAsync();

        return new AuthResponse
        {
            UserId = appUser.Id,
            Email = appUser.Email!
        };
    }
}
