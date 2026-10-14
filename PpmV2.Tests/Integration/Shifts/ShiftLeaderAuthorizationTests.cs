using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PpmV2.Domain.Shifts;

namespace PpmV2.Tests.Integration.Shifts;

/// <summary>
/// Business rule: once a shift is Planned, only the participant assigned as
/// Leader on that exact shift may start or complete it. Coordinator and Admin
/// no longer have start/complete rights, even though they still approve/cancel.
/// </summary>
public sealed class ShiftLeaderAuthorizationTests(PpmV2WebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Leader_CanStartAndCompleteTheirOwnShift()
    {
        var ctx = await CreatePlannedShiftAsync();

        Authorize(ctx.LeaderToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/start", null)).StatusCode);
        Assert.Equal("Active", await GetStatusAsync(ctx.ShiftId, ctx.CoordinatorToken));

        Authorize(ctx.LeaderToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/complete", null)).StatusCode);
        Assert.Equal("Completed", await GetStatusAsync(ctx.ShiftId, ctx.CoordinatorToken));
    }

    [Fact]
    public async Task Coordinator_CannotStartOrComplete()
    {
        var ctx = await CreatePlannedShiftAsync();

        Authorize(ctx.CoordinatorToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/start", null)).StatusCode);

        Authorize(ctx.LeaderToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/start", null)).StatusCode);

        Authorize(ctx.CoordinatorToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/complete", null)).StatusCode);
    }

    [Fact]
    public async Task Admin_CannotStartOrComplete()
    {
        var ctx = await CreatePlannedShiftAsync();
        var adminToken = await LoginAsAdminAsync();

        Authorize(adminToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/start", null)).StatusCode);

        Authorize(ctx.LeaderToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/start", null)).StatusCode);

        Authorize(adminToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/complete", null)).StatusCode);
    }

    [Fact]
    public async Task Member_CannotStartOrCompleteTheirOwnShift()
    {
        var ctx = await CreatePlannedShiftAsync();

        Authorize(ctx.MemberToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/start", null)).StatusCode);

        Authorize(ctx.LeaderToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/start", null)).StatusCode);

        Authorize(ctx.MemberToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/complete", null)).StatusCode);
    }

    [Fact]
    public async Task LeaderOfAnotherShift_CannotStartOrCompleteThisShift()
    {
        var ctx = await CreatePlannedShiftAsync();
        var otherCtx = await CreatePlannedShiftAsync();

        Authorize(otherCtx.LeaderToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/start", null)).StatusCode);

        Authorize(ctx.LeaderToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/start", null)).StatusCode);

        Authorize(otherCtx.LeaderToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsync($"/api/shifts/{ctx.ShiftId}/complete", null)).StatusCode);
    }

    [Fact]
    public async Task Start_DraftShift_ReturnsExisting400_EvenForTheLeader()
    {
        var (coordinatorToken, _) = await CreateApprovedUserAsync("Coordinator");
        var (leaderToken, leaderId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var locationId = await CreateLocationAsync(coordinatorToken, $"LeaderDraft-{Guid.NewGuid():N}");
        var shiftId = await CreateDraftShiftAsync(coordinatorToken, leaderId, locationId);

        Authorize(leaderToken);
        var response = await Client.PutAsync($"/api/shifts/{shiftId}/start", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<PlannedShiftContext> CreatePlannedShiftAsync()
    {
        var (coordinatorToken, _) = await CreateApprovedUserAsync("Coordinator");
        var (leaderToken, leaderId) = await CreateApprovedUserAsync("Festmitarbeiter");
        var member = await CreateApprovedUserWithCredentialsAsync("Honorarkraft");
        var locationId = await CreateLocationAsync(coordinatorToken, $"LeaderAuth-{Guid.NewGuid():N}");

        var shiftId = await CreateDraftShiftAsync(coordinatorToken, leaderId, locationId);

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
        Assert.Equal(HttpStatusCode.NoContent,
            (await Client.PutAsJsonAsync(
                $"/api/shifts/{shiftId}/participants/{member.UserId}/respond",
                new { response = ParticipantConfirmationStatus.Accepted.ToString() })).StatusCode);

        Assert.Equal("Planned", await GetStatusAsync(shiftId, coordinatorToken));

        return new PlannedShiftContext(shiftId, coordinatorToken, leaderToken, member.Token);
    }

    private async Task<Guid> CreateDraftShiftAsync(string coordinatorToken, string leaderId, Guid locationId)
    {
        Authorize(coordinatorToken);
        var start = DateTime.UtcNow.AddDays(10);
        var response = await Client.PostAsJsonAsync("/api/shifts", new
        {
            title = $"Leader authorization shift {Guid.NewGuid():N}",
            startAtUtc = start.ToString("o"),
            endAtUtc = start.AddHours(4).ToString("o"),
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

    private sealed record PlannedShiftContext(
        Guid ShiftId,
        string CoordinatorToken,
        string LeaderToken,
        string MemberToken);
}
