using System.Net;
using System.Net.Http.Json;

namespace PpmV2.Tests.Integration.Locations;

/// <summary>
/// Integration tests for Locations CRUD (P1).
///
/// Validates the full request → controller → application → DB → response cycle.
/// Covers: create, read detail, update (incl. reactivation), soft-deactivate,
/// duplicate prevention, and not-found handling.
/// </summary>
public sealed class LocationsIntegrationTests(PpmV2WebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetLocations_WithToken_Returns200()
    {
        var token = await LoginAsAdminAsync();
        Authorize(token);

        var response = await Client.GetAsync("/api/locations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLocations_WithoutToken_Returns401()
    {
        ClearAuthorization();
        var response = await Client.GetAsync("/api/locations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateLocation_AsCoordinator_Returns201WithIsActiveTrue()
    {
        var (token, _) = await CreateApprovedUserAsync("Coordinator");
        Authorize(token);

        var response = await Client.PostAsJsonAsync("/api/locations", new
        {
            name = $"Unterkunft-{Guid.NewGuid():N}",
            district = "München",
            address = "Musterstraße 5",
            capacity = 20
        });

        // 201 Created with Location header pointing to GET /api/locations/{id}
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(body);

        // F1-Validation: isActive must be true on creation — no extra activate step needed
        Assert.Equal(true.ToString(), body["isActive"].ToString(),
            ignoreCase: true);
    }

    [Fact]
    public async Task CreateLocation_DuplicateNameAndDistrict_Returns400()
    {
        var (token, _) = await CreateApprovedUserAsync("Coordinator");
        Authorize(token);

        var name = $"Duplicate-{Guid.NewGuid():N}";
        var district = "Hamburg";

        await Client.PostAsJsonAsync("/api/locations", new { name, district });

        // Second creation with identical name + district
        var response = await Client.PostAsJsonAsync("/api/locations", new { name, district });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLocationById_ExistingLocation_Returns200WithAllFields()
    {
        var (token, _) = await CreateApprovedUserAsync("Coordinator");
        var locationId = await CreateLocationAsync(token, $"Detail-{Guid.NewGuid():N}", "Berlin");

        Authorize(token);
        var response = await Client.GetAsync($"/api/locations/{locationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(body);
        Assert.True(body.ContainsKey("id"));
        Assert.True(body.ContainsKey("name"));
        Assert.True(body.ContainsKey("district"));
        Assert.True(body.ContainsKey("isActive"));
        Assert.True(body.ContainsKey("createdAt"));
    }

    [Fact]
    public async Task GetLocationById_NonExistent_Returns404()
    {
        var token = await LoginAsAdminAsync();
        Authorize(token);

        var response = await Client.GetAsync($"/api/locations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLocation_ValidRequest_Returns200()
    {
        var (token, _) = await CreateApprovedUserAsync("Coordinator");
        var locationId = await CreateLocationAsync(token, $"Update-{Guid.NewGuid():N}", "Köln");

        Authorize(token);
        var response = await Client.PutAsJsonAsync($"/api/locations/{locationId}", new
        {
            name = "Geänderte Unterkunft",
            district = "Köln",
            address = "Neue Adresse 10",
            capacity = 30,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("Geänderte Unterkunft", body!["name"].ToString());
    }

    [Fact]
    public async Task UpdateLocation_WithIsActiveFalse_ReactiavatesLocation()
    {
        // F2-Validation: PUT with isActive=true reactivates a deactivated location
        var (token, _) = await CreateApprovedUserAsync("Coordinator");
        var name = $"Reactivate-{Guid.NewGuid():N}";
        var locationId = await CreateLocationAsync(token, name, "Frankfurt");

        Authorize(token);

        // Deactivate via DELETE
        await Client.DeleteAsync($"/api/locations/{locationId}");

        // Verify deactivated (isActive=false — GET /api/locations/{id} still returns it)
        var afterDelete = await Client.GetAsync($"/api/locations/{locationId}");
        var bodyAfterDelete = await afterDelete.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("False", bodyAfterDelete!["isActive"].ToString(), ignoreCase: true);

        // Reactivate via PUT with isActive=true
        var reactivateResponse = await Client.PutAsJsonAsync($"/api/locations/{locationId}", new
        {
            name,
            district = "Frankfurt",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        var bodyAfterReactivate = await reactivateResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("True", bodyAfterReactivate!["isActive"].ToString(), ignoreCase: true);
    }

    [Fact]
    public async Task DeactivateLocation_ExistingLocation_Returns204AndIsActiveFalse()
    {
        var (token, _) = await CreateApprovedUserAsync("Coordinator");
        var locationId = await CreateLocationAsync(token, $"Deactivate-{Guid.NewGuid():N}", "Stuttgart");

        Authorize(token);
        var deleteResponse = await Client.DeleteAsync($"/api/locations/{locationId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Location still exists but isActive=false — not a hard delete
        var getResponse = await Client.GetAsync($"/api/locations/{locationId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var body = await getResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("False", body!["isActive"].ToString(), ignoreCase: true);
    }

    [Fact]
    public async Task DeactivateLocation_NonExistent_Returns404()
    {
        var (token, _) = await CreateApprovedUserAsync("Coordinator");
        Authorize(token);

        var response = await Client.DeleteAsync($"/api/locations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
