using System.Net;
using System.Net.Http.Json;

namespace PpmV2.Tests.Integration.Auth;

/// <summary>
/// Integration tests for the authentication flow.
///
/// These tests verify the full HTTP → middleware → application → DB round-trip
/// for registration, login, and protected endpoint access. No mocks are used.
/// </summary>
public sealed class AuthIntegrationTests(PpmV2WebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_ValidRequest_Returns200WithUserId()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            firstname = "Anna",
            lastname = "Müller",
            email = $"anna-{Guid.NewGuid():N}@test.local",
            password = "Pass123$"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.True(body.ContainsKey("userId"));
        Assert.True(Guid.TryParse(body["userId"], out _));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsProblemDetails()
    {
        var email = $"dup-{Guid.NewGuid():N}@test.local";

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            firstname = "Max",
            lastname = "Muster",
            email,
            password = "Pass123$"
        });

        // Second registration with the same email
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            firstname = "Max",
            lastname = "Muster",
            email,
            password = "Pass123$"
        });

        // Expect a 4xx — exact code depends on auth service implementation
        Assert.True((int)response.StatusCode >= 400 && (int)response.StatusCode < 500);
    }

    [Fact]
    public async Task Login_SeededAdmin_Returns200WithToken()
    {
        // The admin account is guaranteed to exist via AdminSeeder on startup.
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@test.local",
            password = "Pass123$"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.True(body.ContainsKey("token"));
        Assert.NotEmpty(body["token"]);
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        ClearAuthorization();
        var response = await Client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithAdminToken_Returns200WithProfile()
    {
        var token = await LoginAsAdminAsync();
        Authorize(token);

        var response = await Client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(body);
        Assert.True(body.ContainsKey("email"));
        Assert.True(body.ContainsKey("role"));
    }
}
