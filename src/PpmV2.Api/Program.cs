using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PpmV2.Api.Middleware;
using PpmV2.Application.Admin.Interfaces;
using PpmV2.Application.Auth.Interfaces;
using PpmV2.Application.Locations.Interfaces;
using PpmV2.Application.Shifts.Commands.Creation;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Application.Shifts.Queries.GetShiftDetails;
using PpmV2.Application.Users.Interfaces;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Admin.Seeding;
using PpmV2.Infrastructure.Admin.Services;
using PpmV2.Infrastructure.Auth;
using PpmV2.Infrastructure.Identity;
using PpmV2.Infrastructure.Persistence;
using PpmV2.Infrastructure.Persistence.Queries;
using PpmV2.Infrastructure.Persistence.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var environment = builder.Environment.EnvironmentName;

// --- API setup (controllers + OpenAPI) ---
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


// --- Persistence setup ---
// For local development we use SQL Server.
// For "Docker" environment we currently use an in-memory database to simplify container startup.
// Note: In-memory DB is not persistent and should be used only for development/testing scenarios.

if (environment == "Docker")
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseInMemoryDatabase("PpmV2DockerDb");

    });
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    });
}

// --- Identity setup ---
// Identity manages credentials, password hashing and user store.
// AppUser/AppRole are the domain-specific Identity models persisted via EF Core. 
builder.Services
    .AddIdentity<AppUser, AppRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();



// --- JWT authentication setup ---
// Token validation parameters are aligned with JwtTokenService configuration (Issuer/Audience/Key).
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key is missing (Jwt:Key).");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key is missing.");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        // Small clock skew to reduce token expiry issues caused by time drift.
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});


// --- Authorization policies ---
// Policies are used by controllers/endpoints to express access rules in a central, testable way.
builder.Services.AddAuthorization(options =>
{
    // Admin endpoints
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(UserRole.Admin.ToString()));

    // Shift creation is restricted to Coordinator and Festmitarbeiter
    // (legacy name "EinsatzCreate" kept for now; can be renamed to "ShiftCreate" later).
    options.AddPolicy("EinsatzCreate", policy =>
        policy.RequireRole(
            UserRole.Coordinator.ToString(),
            UserRole.Festmitarbeiter.ToString()
        ));
});


// --- Dependency injection registrations ---
// Infrastructure implementations for application ports.
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<ILocationQueryService, LocationQueryService>();

// Shifts: repository serves as write-port and details query for v1.
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
builder.Services.AddScoped<IShiftDetailsQuery, ShiftRepository>();

// Application handlers (use cases)
builder.Services.AddScoped<CreateShiftHandler>();
builder.Services.AddScoped<GetShiftDetailsHandler>();


// --- CORS ---
// Config-driven allowlist for frontend origins (e.g. local dev UI, hosted preview URL).
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// --- HTTP pipeline ---
if (app.Environment.IsDevelopment())
{
    // Generates /openapi/v1.json
    app.MapOpenApi();
}

app.UseCors("FrontendCors");

// Central exception -> ProblemDetails mapping (currently handles ValidationException).
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();
app.MapControllers();



// --- Seeding ---
// Seeds an initial admin user based on configuration (idempotent).
// Intended for controlled environments only (AdminSeed:Enabled).
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var configuration = services.GetRequiredService<IConfiguration>();
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");

    await AdminSeeder.SeedAsync(userManager, dbContext, configuration, logger);
}


app.Run();

