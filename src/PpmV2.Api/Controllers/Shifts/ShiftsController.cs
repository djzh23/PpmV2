using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Shifts.Commands.Creation;
using PpmV2.Application.Shifts.DTOs;
using PpmV2.Application.Shifts.Queries.GetShiftDetails;

namespace PpmV2.Api.Controllers.Einsaetze;

[ApiController]
[Route("api/einsaetze")]
public class ShiftsController : ControllerBase
{
    private readonly CreateShiftHandler _create;
    private readonly GetShiftDetailsHandler _get;

    public ShiftsController( CreateShiftHandler create, GetShiftDetailsHandler get)
    {
        _create = create;
        _get = get;
    }

    // Nur Coordinator darf erstellen
    [HttpPost]
    [Authorize(Policy = "EinsatzCreate")]
    public async Task<ActionResult<ShiftDetailsDto>> Create([FromBody] CreateShiftRequest request, CancellationToken ct)
    {
        // Request -> Command mapping
        var cmd = new CreateShiftCommand(
            request.Title,
            request.Description,
            request.StartAtUtc,
            request.EndAtUtc,
            request.LocationId,
            request.Participants
                .Select(p => new CreateShiftParticipantDto(p.UserId, p.Role))
                .ToList()
        );

        // Command (Write)
        var id = await _create.Handle(cmd, ct);

        // Query (Read)
        var details = await _get.Handle(new GetShiftDetailsQuery(id), ct);
        if (details is null)
            return Problem("Einsatz created but not readable.");

        return Ok(details);
    }


    [HttpGet("{id:guid}")]
    [Authorize] // oder offen lassen, aber fürs MVP ist authorize ok
    public async Task<ActionResult<ShiftDetailsDto>> GetById(Guid id, CancellationToken ct)
    {
        var details = await _get.Handle(new GetShiftDetailsQuery(id), ct);
        return details is null ? NotFound() : Ok(details);
    }


}
