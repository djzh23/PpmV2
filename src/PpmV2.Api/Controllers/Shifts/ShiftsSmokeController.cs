using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PpmV2.Application.Shifts.Smokes;
using PpmV2.Domain.Shifts;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Persistence;

namespace PpmV2.Api.Controllers.Shifts;

[ApiController]
[Route("api/einsaetze/smoke")]
[Authorize(Policy = "EinsatzCreate")]
public class ShiftsSmokeController : ControllerBase
{
    private readonly AppDbContext _db;

    public ShiftsSmokeController(AppDbContext db) => _db = db;

    [HttpPost]
    [HttpPost]
    public async Task<ActionResult<ShiftDetailsSmokeResponse>> Create([FromBody] CreateShiftSmokeRequest request)
    {
        // Basic validation (Controller-level; später Service)
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Title is required." });

        if (request.EndAtUtc is not null && request.EndAtUtc <= request.StartAtUtc)
            return BadRequest(new { error = "EndAtUtc must be after StartAtUtc." });

        if (request.Participants is null || request.Participants.Count == 0)
            return BadRequest(new { error = "At least one participant is required." });

        var leaderCount = request.Participants.Count(p => p.Role == ShiftRole.Leader);
        if (leaderCount != 1)
            return BadRequest(new { error = "Exactly one Leader is required." });

        // FK checks
        var location = await _db.Locations
            .Where(l => l.Id == request.LocationId)
            .Select(l => new { l.Id, l.Name, l.District, l.Address })
            .FirstOrDefaultAsync();

        if (location is null)
            return BadRequest(new { error = "LocationId not found." });

        var userIds = request.Participants.Select(p => p.UserId).Distinct().ToList();
        var existingUsersCount = await _db.Users.CountAsync(u => userIds.Contains(u.Id));

        if (existingUsersCount != userIds.Count)
            return BadRequest(new { error = "One or more UserIds do not exist." });

        // Create
        var einsatzId = Guid.NewGuid();
        var status = request.Status ?? ShiftStatus.Draft;

        var einsatz = new Shift
        {
            Id = einsatzId,
            Title = request.Title.Trim(),
            Description = request.Description,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            LocationId = request.LocationId,
            Status = status
        };

        var participants = request.Participants
            .Select(p => new ShiftParticipant
            {
                ShiftId = einsatzId,
                UserId = p.UserId,
                Role = p.Role
            })
            .ToList();

        _db.Einsaetze.Add(einsatz);
        _db.EinsatzParticipants.AddRange(participants);
        await _db.SaveChangesAsync();

        // Return a clean, client-friendly response
        var response = new ShiftDetailsSmokeResponse
        {
            Id = einsatz.Id,
            Title = einsatz.Title,
            Description = einsatz.Description,
            StartAtUtc = einsatz.StartAtUtc,
            EndAtUtc = einsatz.EndAtUtc,
            Status = einsatz.Status,
            Location = new LocationSmokeDto
            {
                Id = location.Id,
                Name = location.Name,
                District = location.District,
                Address = location.Address
            },
            Participants = participants.Select(p => new ShiftParticipantDetailsSmokeDto
            {
                UserId = p.UserId,
                Role = p.Role
            }).ToList()
        };

        return Ok(response);
    }


    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShiftDetailsSmokeResponse>> GetById(Guid id)
    {
        var einsatz = await _db.Einsaetze
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.StartAtUtc,
                e.EndAtUtc,
                e.Status,
                e.LocationId
            })
            .FirstOrDefaultAsync();

        if (einsatz is null)
            return NotFound();

        var location = await _db.Locations
            .AsNoTracking()
            .Where(l => l.Id == einsatz.LocationId)
            .Select(l => new LocationSmokeDto
            {
                Id = l.Id,
                Name = l.Name,
                District = l.District,
                Address = l.Address
            })
            .FirstAsync();

        var participants = await _db.EinsatzParticipants
            .AsNoTracking()
            .Where(p => p.ShiftId == einsatz.Id)
            .Select(p => new ShiftParticipantDetailsSmokeDto
            {
                UserId = p.UserId,
                Role = p.Role
            })
            .ToListAsync();

        var (readiness, missing) = await ComputeReadinessAsync(einsatz.Id);

        return Ok(new ShiftDetailsSmokeResponse
        {
            Id = einsatz.Id,
            Title = einsatz.Title,
            Description = einsatz.Description,
            StartAtUtc = einsatz.StartAtUtc,
            EndAtUtc = einsatz.EndAtUtc,
            Status = einsatz.Status,
            Location = location,
            Participants = participants,
            Readiness = readiness,
            MissingRequirements = missing
        });

    }



    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var einsatz = await _db.Einsaetze.FirstOrDefaultAsync(e => e.Id == id);
        if (einsatz is null) return NotFound();

        if (einsatz.Status != ShiftStatus.Draft)
            return BadRequest(new { error = "Only Draft can be published." });

        var leaderCount = await _db.EinsatzParticipants
            .Where(p => p.ShiftId == id && p.Role == ShiftRole.Leader)
            .CountAsync();

        if (leaderCount != 1)
            return BadRequest(new { error = "Exactly one Leader is required before publish." });

        einsatz.Status = ShiftStatus.Planned;
        await _db.SaveChangesAsync();

        return Ok(new { id = einsatz.Id, status = einsatz.Status });
    }


    [HttpPost("{id:guid}/accept")]
    [Authorize] // jeder eingeloggte User darf versuchen; wir prüfen Leader-Berechtigung fachlich
    public async Task<IActionResult> Accept(Guid id)
    {
        var einsatz = await _db.Einsaetze.FirstOrDefaultAsync(e => e.Id == id);
        if (einsatz is null) return NotFound();

        if (einsatz.Status != ShiftStatus.Planned)
            return BadRequest(new { error = "Only Planned can be accepted." });

        // Current user id aus JWT
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "User id claim missing." });

        var isLeader = await _db.EinsatzParticipants.AnyAsync(p =>
            p.ShiftId == id && p.UserId == userId && p.Role == ShiftRole.Leader);

        if (!isLeader)
            return Forbid();

        einsatz.Status = ShiftStatus.Active;
        await _db.SaveChangesAsync();

        return Ok(new { id = einsatz.Id, status = einsatz.Status });
    }






    private async Task<(string readiness, List<string> missing)> ComputeReadinessAsync(Guid einsatzId)
    {
        var participants = await _db.EinsatzParticipants
            .AsNoTracking()
            .Where(p => p.ShiftId == einsatzId)
            .Select(p => new { p.UserId, p.Role })
            .ToListAsync();

        var missing = new List<string>();

        var leaderCount = participants.Count(p => p.Role == ShiftRole.Leader);
        if (leaderCount != 1)
            missing.Add("leader");

        // mindestens 1 Festmitarbeiter im Team (Systemrolle)
        var userIds = participants.Select(p => p.UserId).Distinct().ToList();

        var festCount = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .CountAsync(u => u.Role == UserRole.Festmitarbeiter);

        if (festCount < 1)
            missing.Add("festmitarbeiter");

        var readiness = missing.Count == 0 ? "ready" : "not_ready";
        return (readiness, missing);
    }


}
