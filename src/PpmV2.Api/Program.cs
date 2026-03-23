using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
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

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(renderPort, out var port) && port > 0)
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

var postgresConn = ResolvePostgresConnection(builder.Configuration);
var sqlServerConn = builder.Configuration.GetConnectionString("DefaultConnection");


// --- API setup (controllers + OpenAPI) ---
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- Proxy headers ---
// Required when running behind reverse proxies (e.g. Render) so ASP.NET correctly
// understands the original scheme and client IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});


// --- Persistence setup ---
// Database provider priority: PostgreSQL (primary) > SQL Server (legacy fallback) > In-Memory (dev/testing only)
// PostgreSQL is the primary database. SQL Server migrations are archived and excluded from compilation.
// In-memory database is only used as a fallback for local development without database configuration.


if (!string.IsNullOrWhiteSpace(postgresConn))
{
    builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(postgresConn));
}
else if (!string.IsNullOrWhiteSpace(sqlServerConn))
{
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(sqlServerConn));
}
else
{
    // fallback only for local Scenarios
    builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("PpmV2DevDb"));
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
    .Get<string[]>() ?? [];
var allowedOriginsCsv = builder.Configuration["Cors:AllowedOriginsCsv"] ?? string.Empty;
var allowedOriginHostSuffixes = builder.Configuration
    .GetSection("Cors:AllowedOriginHostSuffixes")
    .Get<string[]>() ?? [];

var normalizedOrigins = allowedOrigins
    .Concat(ParseDelimitedOrigins(allowedOriginsCsv))
    .Select(NormalizeOrigin)
    .OfType<string>()
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

var normalizedHostSuffixes = allowedOriginHostSuffixes
    .Select(s => s.Trim().TrimStart('.').ToLowerInvariant())
    .Where(s => !string.IsNullOrWhiteSpace(s))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(origin => IsOriginAllowed(origin, normalizedOrigins, normalizedHostSuffixes));
    });
});

var app = builder.Build();

// --- HTTP pipeline ---
var openApiEnabled = app.Configuration.GetValue("OpenApi:Enabled", app.Environment.IsDevelopment());
if (openApiEnabled)
{
    // Generates /openapi/v1.json
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseCors("FrontendCors");

// Central exception -> ProblemDetails mapping (currently handles ValidationException).
app.UseMiddleware<ExceptionHandlingMiddleware>();

var useHttpsRedirection = app.Configuration.GetValue("UseHttpsRedirection", !app.Environment.IsDevelopment());
if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();

app.UseAuthorization();
app.MapControllers();



// --- Seeding ---
// Seeds an initial admin user based on configuration (idempotent).
// Intended for controlled environments only (AdminSeed:Enabled).
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var dbContext = services.GetRequiredService<AppDbContext>();
    var configuration = services.GetRequiredService<IConfiguration>();
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();

    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }

    await RolesSeeder.SeedAsync(roleManager, loggerFactory.CreateLogger("RolesSeeder"));
    await AdminSeeder.SeedAsync(userManager, dbContext, configuration, loggerFactory.CreateLogger("AdminSeeder"));
    await DemoUsersSeeder.SeedAsync(userManager, dbContext, configuration, loggerFactory.CreateLogger("DemoUsersSeeder"));
    await LocationsSeeder.SeedAsync(dbContext, configuration, loggerFactory.CreateLogger("LocationsSeeder"));
}



app.Run();

static bool IsOriginAllowed(string origin, IReadOnlyCollection<string> exactOrigins, IReadOnlyCollection<string> hostSuffixes)
{
    var normalizedOrigin = NormalizeOrigin(origin);
    if (string.IsNullOrWhiteSpace(normalizedOrigin))
        return false;

    if (exactOrigins.Contains(normalizedOrigin, StringComparer.OrdinalIgnoreCase))
        return true;

    if (hostSuffixes.Count == 0 || !Uri.TryCreate(normalizedOrigin, UriKind.Absolute, out var uri))
        return false;

    var host = uri.Host.ToLowerInvariant();
    return hostSuffixes.Any(suffix =>
        host.Equals(suffix, StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith($".{suffix}", StringComparison.OrdinalIgnoreCase));
}

static IEnumerable<string> ParseDelimitedOrigins(string rawOrigins)
{
    if (string.IsNullOrWhiteSpace(rawOrigins))
        return [];

    return rawOrigins
        .Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}

static string? NormalizeOrigin(string? origin)
{
    if (string.IsNullOrWhiteSpace(origin))
        return null;

    var candidate = origin.Trim().TrimEnd('/');
    if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        return null;

    var builder = new UriBuilder(uri.Scheme, uri.Host, uri.IsDefaultPort ? -1 : uri.Port);
    return builder.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
}

static string? ResolvePostgresConnection(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    var fromDatabaseUrl = TryBuildConnectionStringFromDatabaseUrl(databaseUrl);
    if (!string.IsNullOrWhiteSpace(fromDatabaseUrl))
        return fromDatabaseUrl;

    return configuration.GetConnectionString("PostgresConnection");
}

static string? TryBuildConnectionStringFromDatabaseUrl(string? databaseUrl)
{
    if (string.IsNullOrWhiteSpace(databaseUrl))
        return null;

    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
        return null;

    if (!uri.Scheme.Equals("postgres", StringComparison.OrdinalIgnoreCase) &&
        !uri.Scheme.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    if (string.IsNullOrWhiteSpace(uri.UserInfo))
        return null;

    var credentials = uri.UserInfo.Split(':', 2, StringSplitOptions.TrimEntries);
    if (credentials.Length != 2)
        return null;

    var username = Uri.UnescapeDataString(credentials[0]);
    var password = Uri.UnescapeDataString(credentials[1]);
    var database = uri.AbsolutePath.Trim('/');

    if (string.IsNullOrWhiteSpace(database))
        return null;

    var port = uri.IsDefaultPort ? 5432 : uri.Port;
    return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}

