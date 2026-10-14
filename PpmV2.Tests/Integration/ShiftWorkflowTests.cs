using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PpmV2.Domain.Shifts;

namespace PpmV2.Tests.Integration;

public sealed class ShiftWorkflowTests(PpmV2WebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    private const string WindowStart = "2026-10-14T08:00:00.000Z";
    private const string WindowEnd = "2026-10-14T12:00:00.000Z";

    [Theory]
    [InlineData("Coordinator")]
    [InlineData("Festmitarbeiter")]
    [InlineData("Honorarkraft")]
    public async Task GetStaff_WithUtcWindow_IsAllowedForShiftCreationRoles(string role)
    {
        var (token, _) = await CreateApprovedUserAsync(role);
        Authorize(token);

        var response = await Client.GetAsync($"/api/users/staff?startAt={WindowStart}&endAt={WindowEnd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetStaff_IsForbiddenForAdmin()
    {
        Authorize(await LoginAsAdminAsync());

        var response = await Client.GetAsync($"/api/users/staff?startAt={WindowStart}&endAt={WindowEnd}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetStaff_IsUnauthorizedWithoutToken()
    {
        ClearAuthorization();

        var response = await Client.GetAsync($"/api/users/staff?startAt={WindowStart}&endAt={WindowEnd}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStaff_MarksParticipantWithOverlappingShiftAsConflict()
    {
        var (coordinatorToken, _) = await CreateApprovedUserAsync("Coordinator");
        var (_, leaderId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var locationId = await CreateLocationAsync(coordinatorToken, $"Conflict-{Guid.NewGuid():N}");

        var shiftId = await CreateShiftAsync(
            coordinatorToken,
            leaderId,
            locationId,
            DateTime.Parse(WindowStart).ToUniversalTime(),
            DateTime.Parse(WindowEnd).ToUniversalTime());

        Authorize(coordinatorToken);
        var response = await Client.GetAsync($"/api/users/staff?startAt={WindowStart}&endAt={WindowEnd}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var staff = await response.Content.ReadFromJsonAsync<List<StaffResult>>();
        var leader = Assert.Single(staff!, s => s.UserId == leaderId);
        Assert.True(leader.HasConflict, $"Leader {leaderId} was not marked busy for shift {shiftId}.");
    }

    [Fact]
    public async Task FullApiWorkflow_CreateProposeAcceptStartComplete()
    {
        var (coordinatorToken, _) = await CreateApprovedUserAsync("Coordinator");
        var (leaderToken, leaderId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var member = await CreateApprovedUserWithCredentialsAsync("Honorarkraft");
        var memberId = member.UserId;
        var locationId = await CreateLocationAsync(coordinatorToken, $"Workflow-{Guid.NewGuid():N}");

        var shiftId = await CreateShiftAsync(coordinatorToken, leaderId, locationId);

        Authorize(leaderToken);
        var addResponse = await Client.PostAsJsonAsync($"/api/shifts/{shiftId}/participants", new
        {
            userId = memberId,
            role = ShiftRole.Member.ToString()
        });
        Assert.Equal(HttpStatusCode.NoContent, addResponse.StatusCode);

        var proposeResponse = await Client.PutAsync($"/api/shifts/{shiftId}/propose", null);
        Assert.Equal(HttpStatusCode.NoContent, proposeResponse.StatusCode);
        Assert.Equal("PendingApproval", await GetStatusAsync(shiftId, coordinatorToken));

        Authorize(member.Token);
        var respondResponse = await Client.PutAsJsonAsync(
            $"/api/shifts/{shiftId}/participants/{memberId}/respond",
            new { response = ParticipantConfirmationStatus.Accepted.ToString() });
        Assert.Equal(HttpStatusCode.NoContent, respondResponse.StatusCode);
        Assert.Equal("Planned", await GetStatusAsync(shiftId, coordinatorToken));

        Authorize(coordinatorToken);
        var startResponse = await Client.PutAsync($"/api/shifts/{shiftId}/start", null);
        Assert.Equal(HttpStatusCode.NoContent, startResponse.StatusCode);
        Assert.Equal("Active", await GetStatusAsync(shiftId, coordinatorToken));

        var completeResponse = await Client.PutAsync($"/api/shifts/{shiftId}/complete", null);
        Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);
        Assert.Equal("Completed", await GetStatusAsync(shiftId, coordinatorToken));
    }

    [Fact]
    public async Task DeclinedMember_DoesNotMakeShiftPlanned_RemainsPendingApproval()
    {
        var (coordinatorToken, _) = await CreateApprovedUserAsync("Coordinator");
        var (leaderToken, leaderId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var member = await CreateApprovedUserWithCredentialsAsync("Honorarkraft");
        var locationId = await CreateLocationAsync(coordinatorToken, $"Declined-{Guid.NewGuid():N}");
        var shiftId = await CreateShiftAsync(coordinatorToken, leaderId, locationId);

        Authorize(leaderToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PostAsJsonAsync($"/api/shifts/{shiftId}/participants", new
            {
                userId = member.UserId,
                role = ShiftRole.Member.ToString()
            })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsync($"/api/shifts/{shiftId}/propose", null)).StatusCode);

        Authorize(member.Token);
        var response = await Client.PutAsJsonAsync(
            $"/api/shifts/{shiftId}/participants/{member.UserId}/respond",
            new { response = ParticipantConfirmationStatus.Declined.ToString() });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        // README defines Planned only after all participants accept; a decline therefore
        // leaves the shift in PendingApproval until the documented replacement flow exists.
        Assert.Equal("PendingApproval", await GetStatusAsync(shiftId, coordinatorToken));
    }

    [Fact]
    public async Task CoordinatorList_ContainsEveryShiftStatus()
    {
        var (coordinatorToken, _) = await CreateApprovedUserAsync("Coordinator");
        var (leaderToken, leaderId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var (_, memberId) = await CreateApprovedUserAsync("Honorarkraft");
        var locationId = await CreateLocationAsync(coordinatorToken, $"Statuses-{Guid.NewGuid():N}");

        var draftId = await CreateShiftAsync(coordinatorToken, leaderId, locationId);
        var pendingId = await CreateShiftAsync(coordinatorToken, leaderId, locationId);
        Authorize(leaderToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PostAsJsonAsync($"/api/shifts/{pendingId}/participants", new
            {
                userId = memberId,
                role = ShiftRole.Member.ToString()
            })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsync($"/api/shifts/{pendingId}/propose", null)).StatusCode);

        var plannedId = await CreateShiftAsync(coordinatorToken, leaderId, locationId);
        Authorize(coordinatorToken);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsync($"/api/shifts/{plannedId}/approve", null)).StatusCode);

        var activeId = await CreateShiftAsync(coordinatorToken, leaderId, locationId);
        Authorize(coordinatorToken);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsync($"/api/shifts/{activeId}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsync($"/api/shifts/{activeId}/start", null)).StatusCode);

        var completedId = await CreateShiftAsync(coordinatorToken, leaderId, locationId);
        Authorize(coordinatorToken);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsync($"/api/shifts/{completedId}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsync($"/api/shifts/{completedId}/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsync($"/api/shifts/{completedId}/complete", null)).StatusCode);

        var cancelledId = await CreateShiftAsync(coordinatorToken, leaderId, locationId);
        Authorize(coordinatorToken);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsync($"/api/shifts/{cancelledId}/cancel", null)).StatusCode);

        var response = await Client.GetAsync("/api/shifts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var shifts = await response.Content.ReadFromJsonAsync<List<ShiftListResult>>();

        var expected = new Dictionary<Guid, string>
        {
            [draftId] = "Draft",
            [pendingId] = "PendingApproval",
            [plannedId] = "Planned",
            [activeId] = "Active",
            [completedId] = "Completed",
            [cancelledId] = "Cancelled"
        };

        foreach (var (id, status) in expected)
            Assert.Equal(status, Assert.Single(shifts!, s => s.Id == id).Status);
    }

    [Fact]
    public async Task StaffList_ContainsOnlyOwnShifts()
    {
        var (coordinatorToken, _) = await CreateApprovedUserAsync("Coordinator");
        var (firstToken, firstId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var (_, secondId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var locationId = await CreateLocationAsync(coordinatorToken, $"OwnShifts-{Guid.NewGuid():N}");
        var ownShiftId = await CreateShiftAsync(coordinatorToken, firstId, locationId);
        var foreignShiftId = await CreateShiftAsync(coordinatorToken, secondId, locationId);

        Authorize(firstToken);
        var response = await Client.GetAsync("/api/shifts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var shifts = await response.Content.ReadFromJsonAsync<List<ShiftListResult>>();

        Assert.Contains(shifts!, s => s.Id == ownShiftId);
        Assert.DoesNotContain(shifts!, s => s.Id == foreignShiftId);
    }

    private async Task<Guid> CreateShiftAsync(
        string token,
        string leaderId,
        Guid locationId,
        DateTime? start = null,
        DateTime? end = null)
    {
        Authorize(token);
        var startAt = start ?? DateTime.UtcNow.AddDays(10);
        var endAt = end ?? startAt.AddHours(4);
        var response = await Client.PostAsJsonAsync("/api/shifts", new
        {
            title = $"Integration shift {Guid.NewGuid():N}",
            description = "Integration test",
            startAtUtc = startAt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
            endAtUtc = endAt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
            locationId,
            participants = new[] { new { userId = leaderId, role = ShiftRole.Leader.ToString() } }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return json!.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<string> GetStatusAsync(Guid shiftId, string token)
    {
        Authorize(token);
        var response = await Client.GetAsync($"/api/shifts/{shiftId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return json!.RootElement.GetProperty("status").GetString()!;
    }

    private sealed record StaffResult(string UserId, bool HasConflict);
    private sealed record ShiftListResult(Guid Id, string Status);
}
