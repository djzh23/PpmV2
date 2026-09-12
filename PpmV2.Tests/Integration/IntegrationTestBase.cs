using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PpmV2.Tests.Integration;

/// <summary>
/// Base class for all integration tests.
///
/// Provides a shared HttpClient and helpers for authentication and user management.
/// Each test class gets its own factory instance via IClassFixture, so the
/// Testcontainer lifecycle is scoped to the test class — not individual tests.
///
/// Design principle: tests are self-contained.
/// Each test creates its own users, locations, and shifts rather than relying on
/// pre-seeded data. The only seeded account is admin@test.local (via AdminSeeder),
/// which tests use to approve and role-assign newly registered users.
/// </summary>
[Collection("Integration")]
public abstract class IntegrationTestBase
{
    private const string AdminEmail = "admin@test.local";
    private const string AdminPassword = "Pass123$";
    private const string DefaultPassword = "Pass123$";

    protected readonly HttpClient Client;

    protected IntegrationTestBase(PpmV2WebApplicationFactory factory)
    {
        Client = factory.CreateClient();
    }

    // ── Auth helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Logs in as the seeded admin and returns the JWT token.
    /// Admin exists because AdminSeeder runs on factory startup.
    /// </summary>
    protected async Task<string> LoginAsAdminAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = AdminEmail,
            password = AdminPassword
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        return result!.Token;
    }

    /// <summary>
    /// Registers a new user with a random email, approves them via the admin endpoint,
    /// assigns the requested role, then logs them in and returns their JWT token.
    ///
    /// Why this sequence?
    /// Newly registered users have status Pending and cannot log in.
    /// Only an Admin can approve and assign roles. We use the seeded admin account.
    /// </summary>
    protected async Task<(string Token, string UserId)> CreateApprovedUserAsync(string role)
    {
        var email = $"{role.ToLower()}-{Guid.NewGuid():N}@test.local";

        // 1. Register
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            firstname = role,
            lastname = "TestUser",
            email,
            password = DefaultPassword
        });
        registerResponse.EnsureSuccessStatusCode();
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResult>();
        var userId = registered!.UserId;

        // 2. Admin approves + assigns role
        var adminToken = await LoginAsAdminAsync();
        var adminClient = Client;
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        var approveResponse = await adminClient.PutAsync($"/api/admin/users/approve/{userId}", null);
        approveResponse.EnsureSuccessStatusCode();

        var roleResponse = await adminClient.PutAsJsonAsync($"/api/admin/users/{userId}/role", new { role });
        roleResponse.EnsureSuccessStatusCode();

        // 3. Login as the new user
        adminClient.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new { email, password = DefaultPassword });
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        return (loginResult!.Token, userId.ToString());
    }

    /// <summary>
    /// Sets the Authorization header for subsequent requests.
    /// </summary>
    protected void Authorize(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    /// <summary>
    /// Clears the Authorization header.
    /// </summary>
    protected void ClearAuthorization() =>
        Client.DefaultRequestHeaders.Authorization = null;

    // ── Location helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Creates a location via the API as the given user (must be Coordinator).
    /// Returns the created location's ID.
    /// </summary>
    protected async Task<Guid> CreateLocationAsync(string coordinatorToken, string name = "Test Location", string district = "Berlin")
    {
        Authorize(coordinatorToken);
        var response = await Client.PostAsJsonAsync("/api/locations", new
        {
            name,
            district,
            address = "Teststraße 1",
            capacity = 10
        });
        response.EnsureSuccessStatusCode();
        var location = await response.Content.ReadFromJsonAsync<LocationResult>();
        ClearAuthorization();
        return location!.Id;
    }

    // ── DTO records (private — only for deserialization) ─────────────────────

    private sealed record LoginResult(string Token, string UserId, string Email);
    private sealed record RegisterResult(string UserId, string Email);
    private sealed record LocationResult(Guid Id, string Name, string District);
}
