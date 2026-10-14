using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PpmV2.Domain.Shifts;

namespace PpmV2.Tests.Integration;

public sealed class AuthorizationMatrixTests(PpmV2WebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Admin_CannotCreateShift()
    {
        Authorize(await LoginAsAdminAsync());

        var response = await Client.PostAsJsonAsync("/api/shifts", new
        {
            title = "Forbidden",
            startAtUtc = DateTime.UtcNow.AddDays(10).ToString("o"),
            locationId = Guid.NewGuid(),
            participants = new[] { new { userId = Guid.NewGuid(), role = ShiftRole.Leader.ToString() } }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Festmitarbeiter_CannotApprove_ButCanStartAndCompleteAsLeader()
    {
        var (coordinatorToken, _) = await CreateApprovedUserAsync("Coordinator");
        // staffId is registered as the shift's Leader participant below, so staffToken
        // is both "a Festmitarbeiter" and "the shift leader" — only the latter matters
        // for start/complete now.
        var (staffToken, staffId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var locationId = await CreateLocationAsync(coordinatorToken, $"StaffRights-{Guid.NewGuid():N}");
        var shiftId = await CreateShiftAsync(coordinatorToken, staffId, locationId);

        Authorize(staffToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{shiftId}/approve", null)).StatusCode);

        Authorize(coordinatorToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsync($"/api/shifts/{shiftId}/approve", null)).StatusCode);

        // The coordinator approved the shift but is not its leader, so start/complete
        // are forbidden even to them now.
        Authorize(coordinatorToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{shiftId}/start", null)).StatusCode);

        Authorize(staffToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsync($"/api/shifts/{shiftId}/start", null)).StatusCode);

        Authorize(coordinatorToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{shiftId}/complete", null)).StatusCode);

        Authorize(staffToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsync($"/api/shifts/{shiftId}/complete", null)).StatusCode);
    }

    [Fact]
    public async Task ForeignStaff_CannotSeeOrChangeAnotherStaffsShift()
    {
        var (coordinatorToken, _) = await CreateApprovedUserAsync("Coordinator");
        var (firstToken, firstId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var (_, secondId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var (_, extraId) = await CreateApprovedUserAsync("Honorarkraft");
        var locationId = await CreateLocationAsync(coordinatorToken, $"ForeignRights-{Guid.NewGuid():N}");
        var foreignShiftId = await CreateShiftAsync(coordinatorToken, secondId, locationId);

        Authorize(firstToken);
        var listResponse = await Client.GetAsync("/api/shifts");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var shifts = await listResponse.Content.ReadFromJsonAsync<List<ShiftListResult>>();
        Assert.DoesNotContain(shifts!, s => s.Id == foreignShiftId);

        var proposeResponse = await Client.PutAsync($"/api/shifts/{foreignShiftId}/propose", null);
        Assert.Equal(HttpStatusCode.BadRequest, proposeResponse.StatusCode);

        var addResponse = await Client.PostAsJsonAsync($"/api/shifts/{foreignShiftId}/participants", new
        {
            userId = extraId,
            role = ShiftRole.Member.ToString()
        });
        Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
    }

    [Fact]
    public async Task RoleChange_DoesNotChangeExistingToken_ButNewLoginUsesNewRole()
    {
        var switchUser = await CreateApprovedUserWithCredentialsAsync("Honorarkraft");
        var (coordinatorToken, _) = await CreateApprovedUserAsync("Coordinator");
        var (_, leaderId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var locationId = await CreateLocationAsync(coordinatorToken, $"RoleChange-{Guid.NewGuid():N}");
        var shiftId = await CreateShiftAsync(coordinatorToken, leaderId, locationId);

        var adminToken = await LoginAsAdminAsync();
        Authorize(adminToken);
        var roleResponse = await Client.PutAsJsonAsync(
            $"/api/admin/users/{switchUser.UserId}/role",
            new { role = "Coordinator" });
        Assert.Equal(HttpStatusCode.NoContent, roleResponse.StatusCode);

        Authorize(switchUser.Token);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{shiftId}/approve", null)).StatusCode);

        var newToken = await LoginAsync(switchUser.Email, switchUser.Password);
        Authorize(newToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsync($"/api/shifts/{shiftId}/approve", null)).StatusCode);
    }

    private async Task<Guid> CreateShiftAsync(string token, string leaderId, Guid locationId)
    {
        Authorize(token);
        var start = DateTime.UtcNow.AddDays(10);
        var response = await Client.PostAsJsonAsync("/api/shifts", new
        {
            title = $"Authorization shift {Guid.NewGuid():N}",
            startAtUtc = start.ToString("o"),
            endAtUtc = start.AddHours(4).ToString("o"),
            locationId,
            participants = new[] { new { userId = leaderId, role = ShiftRole.Leader.ToString() } }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return json!.RootElement.GetProperty("id").GetGuid();
    }

    private sealed record ShiftListResult(Guid Id, string Status);
}
