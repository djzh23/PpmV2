using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Shifts.Commands.Creation;
using PpmV2.Application.Shifts.DTOs;
using PpmV2.Application.Shifts.Queries.GetShiftDetails;
using PpmV2.Application.Shifts.Queries.GetShifts;
using PpmV2.Domain.Shifts;

namespace PpmV2.Api.Controllers.Einsaetze;

[ApiController]
[Route("api/shifts")]
public class ShiftsController : ControllerBase
{
    private readonly CreateShiftHandler _create;
    private readonly GetShiftDetailsHandler _get;
    private readonly GetShiftsHandler _list;

    public ShiftsController(CreateShiftHandler create, GetShiftDetailsHandler get, GetShiftsHandler list)
    {
        _create = create;
        _get = get;
        _list = list;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ShiftSummaryDto>>> GetAll(
        [FromQuery] ShiftStatus? status,
        CancellationToken ct)
    {
        var result = await _list.Handle(new GetShiftsQuery(status), ct);
        return Ok(result);
    }


    [HttpPost]
    [Authorize(Policy = "EinsatzCreate")]
    public async Task<ActionResult<ShiftDetailsDto>> Create([FromBody] CreateShiftRequest request, CancellationToken ct)
    {
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

        var id = await _create.Handle(cmd, ct);

        var details = await _get.Handle(new GetShiftDetailsQuery(id), ct);
        if (details is null)
            return Problem("Shift was created but could not be read back.");

        return Ok(details);
    }


    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ShiftDetailsDto>> GetById(Guid id, CancellationToken ct)
    {
        var details = await _get.Handle(new GetShiftDetailsQuery(id), ct);
        return details is null ? NotFound() : Ok(details);
    }


}
