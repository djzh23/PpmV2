using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Einsaetze.Commands.Creation;
using PpmV2.Application.Einsaetze.DTOs;
using PpmV2.Application.Einsaetze.Queries.GetEinsatzDetails;

namespace PpmV2.Api.Controllers.Einsaetze;

[ApiController]
[Route("api/einsaetze")]
public class EinsaetzeController : ControllerBase
{
    private readonly CreateEinsatzHandler _create;
    private readonly GetEinsatzDetailsHandler _get;

    public EinsaetzeController( CreateEinsatzHandler create,GetEinsatzDetailsHandler get)
    {
        _create = create;
        _get = get;
    }

    // Nur Coordinator darf erstellen
    [HttpPost]
    [Authorize(Policy = "EinsatzCreate")]
    public async Task<ActionResult<EinsatzDetailsDto>> Create([FromBody] CreateEinsatzRequest request, CancellationToken ct)
    {
        // Request -> Command mapping
        var cmd = new CreateEinsatzCommand(
            request.Title,
            request.Description,
            request.StartAtUtc,
            request.EndAtUtc,
            request.LocationId,
            request.Participants
                .Select(p => new CreateEinsatzParticipantDto(p.UserId, p.Role))
                .ToList()
        );

        // Command (Write)
        var id = await _create.Handle(cmd, ct);

        // Query (Read)
        var details = await _get.Handle(new GetEinsatzDetailsQuery(id), ct);
        if (details is null)
            return Problem("Einsatz created but not readable.");

        return Ok(details);
    }


    [HttpGet("{id:guid}")]
    [Authorize] // oder offen lassen, aber fürs MVP ist authorize ok
    public async Task<ActionResult<EinsatzDetailsDto>> GetById(Guid id, CancellationToken ct)
    {
        var details = await _get.Handle(new GetEinsatzDetailsQuery(id), ct);
        return details is null ? NotFound() : Ok(details);
    }


}
