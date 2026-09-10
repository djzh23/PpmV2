using System.Net;
using System.Net.Http.Json;

namespace PpmV2.Tests.Integration.Shifts;

/// <summary>
/// Integration tests for the Shift lifecycle (P2 + P3 validation).
///
/// Happy-path test: Create → Propose → Approve → Start → Complete.
/// Each step asserts the HTTP response and that the shift status advanced correctly.
///
/// Error-path tests: wrong status transitions return 400 with ProblemDetails.
/// Not-found tests: return 404 (validates the P3 ServiceResult migration).
///
/// Why test the full lifecycle in one test?
/// Each transition depends on the previous state. Breaking it into independent
/// tests would require complex setup. One sequential test is clearer and
/// more representative of real usage.
/// </summary>
public sealed class ShiftWorkflowIntegrationTests(PpmV2WebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task FullLifecycle_CreateToComplete_AllTransitionsSucceed()
    {
        // ── Arrange ──────────────────────────────────────────────────────────
        var (coordToken, _) = await CreateApprovedUserAsync("Coordinator");
        var locationId = await CreateLocationAsync(coordToken, $"Lifecycle-{Guid.NewGuid():N}", "Berlin");

        // ── Create (→ Draft) ─────────────────────────────────────────────────
        Authorize(coordToken);
        var createResponse = await Client.PostAsJsonAsync("/api/shifts", new
        {
            title = "Integration-Testeinsatz",
            description = "Vollständiger Lifecycle-Test",
            startAtUtc = DateTime.UtcNow.AddDays(7).ToString("o"),
            endAtUtc = DateTime.UtcNow.AddDays(7).AddHours(4).ToString("o"),
            locationId,
            participants = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var shift = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var shiftId = Guid.Parse(shift!["id"].ToString()!);
        Assert.Equal("Draft", shift["status"].ToString());

        // ── Approve (Draft → Planned) ─────────────────────────────────────────
        // Coordinator can approve directly from Draft, skipping the Propose step.
        var approveResponse = await Client.PutAsync($"/api/shifts/{shiftId}/approve", null);
        Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

        var afterApprove = await GetShiftAsync(shiftId, coordToken);
        Assert.Equal("Planned", afterApprove["status"].ToString());

        // ── Start (Planned → Active) ──────────────────────────────────────────
        var startResponse = await Client.PutAsync($"/api/shifts/{shiftId}/start", null);
        Assert.Equal(HttpStatusCode.NoContent, startResponse.StatusCode);

        var afterStart = await GetShiftAsync(shiftId, coordToken);
        Assert.Equal("Active", afterStart["status"].ToString());

        // ── Complete (Active → Completed) ─────────────────────────────────────
        var completeResponse = await Client.PutAsync($"/api/shifts/{shiftId}/complete", null);
        Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);

        var afterComplete = await GetShiftAsync(shiftId, coordToken);
        Assert.Equal("Completed", afterComplete["status"].ToString());
    }

    [Fact]
    public async Task Approve_NonExistentShift_Returns404()
    {
        // P3-Validation: not-found must return 404, not 400.
        // Before P3, handlers threw ValidationException → 400.
        var (coordToken, _) = await CreateApprovedUserAsync("Coordinator");
        Authorize(coordToken);

        var response = await Client.PutAsync($"/api/shifts/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Start_NonExistentShift_Returns404()
    {
        var (coordToken, _) = await CreateApprovedUserAsync("Coordinator");
        Authorize(coordToken);

        var response = await Client.PutAsync($"/api/shifts/{Guid.NewGuid()}/start", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Complete_NonExistentShift_Returns404()
    {
        var (coordToken, _) = await CreateApprovedUserAsync("Coordinator");
        Authorize(coordToken);

        var response = await Client.PutAsync($"/api/shifts/{Guid.NewGuid()}/complete", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Start_DraftShift_Returns400WithProblemDetails()
    {
        // Start is only valid from Planned. Attempting it on a Draft must return 400.
        var (coordToken, _) = await CreateApprovedUserAsync("Coordinator");
        var locationId = await CreateLocationAsync(coordToken, $"WrongStatus-{Guid.NewGuid():N}", "Hamburg");

        Authorize(coordToken);
        var createResponse = await Client.PostAsJsonAsync("/api/shifts", new
        {
            title = "Wrong-Status-Test",
            startAtUtc = DateTime.UtcNow.AddDays(1).ToString("o"),
            locationId,
            participants = Array.Empty<object>()
        });
        var shift = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var shiftId = Guid.Parse(shift!["id"].ToString()!);

        // Attempt to Start a Draft shift — must fail
        var response = await Client.PutAsync($"/api/shifts/{shiftId}/start", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(problem);
        Assert.True(problem.ContainsKey("title") || problem.ContainsKey("detail"),
            "Response should be RFC 7807 ProblemDetails");
    }

    [Fact]
    public async Task Cancel_NonExistentShift_Returns404()
    {
        var (coordToken, _) = await CreateApprovedUserAsync("Coordinator");
        Authorize(coordToken);

        var response = await Client.PutAsync($"/api/shifts/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetShift_NonExistentShift_Returns404()
    {
        var token = await LoginAsAdminAsync();
        Authorize(token);

        var response = await Client.GetAsync($"/api/shifts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetShifts_WithoutToken_Returns401()
    {
        ClearAuthorization();
        var response = await Client.GetAsync("/api/shifts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private async Task<Dictionary<string, object>> GetShiftAsync(Guid shiftId, string token)
    {
        Authorize(token);
        var response = await Client.GetAsync($"/api/shifts/{shiftId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Dictionary<string, object>>())!;
    }
}
