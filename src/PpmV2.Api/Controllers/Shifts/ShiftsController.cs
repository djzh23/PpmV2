using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Api.Common;
using PpmV2.Application.Common.Results;
using PpmV2.Application.Shifts.Commands.Approve;
using PpmV2.Application.Shifts.Commands.Cancel;
using PpmV2.Application.Shifts.Commands.Complete;
using PpmV2.Application.Shifts.Commands.Creation;
using PpmV2.Application.Shifts.Commands.Propose;
using PpmV2.Application.Shifts.Commands.Respond;
using PpmV2.Application.Shifts.Commands.Start;
using PpmV2.Application.Shifts.DTOs;
using PpmV2.Application.Shifts.Interfaces;
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
    private readonly ProposeShiftTeamHandler _propose;
    private readonly ApproveShiftHandler _approve;
    private readonly CancelShiftHandler _cancel;
    private readonly RespondToShiftHandler _respond;
    private readonly StartShiftHandler _start;
    private readonly CompleteShiftHandler _complete;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public ShiftsController(
        CreateShiftHandler create,
        GetShiftDetailsHandler get,
        GetShiftsHandler list,
        ProposeShiftTeamHandler propose,
        ApproveShiftHandler approve,
        CancelShiftHandler cancel,
        RespondToShiftHandler respond,
        StartShiftHandler start,
        CompleteShiftHandler complete,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _create = create;
        _get = get;
        _list = list;
        _propose = propose;
        _approve = approve;
        _cancel = cancel;
        _respond = respond;
        _start = start;
        _complete = complete;
        _currentUser = currentUser;
        _time = time;
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

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ShiftDetailsDto>> GetById(Guid id, CancellationToken ct)
    {
        var details = await _get.Handle(new GetShiftDetailsQuery(id), ct);
        return details is null ? NotFound() : Ok(details);
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

    /// <summary>
    /// Festmitarbeiter proposes their team — transitions the shift from Draft to PendingApproval.
    /// All other participants are set to Invited and must respond.
    /// </summary>
    [HttpPut("{id:guid}/propose")]
    [Authorize(Policy = "EinsatzCreate")]
    public async Task<IActionResult> Propose(Guid id, CancellationToken ct)
    {
        var result = await _propose.Handle(new ProposeShiftTeamCommand(id, _currentUser.UserId), ct);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return NoContent();
    }

    /// <summary>
    /// Coordinator approves a shift (Draft or PendingApproval → Planned).
    /// </summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = "ShiftManage")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await _approve.Handle(new ApproveShiftCommand(id), ct);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return NoContent();
    }

    /// <summary>
    /// Coordinator cancels a shift.
    /// </summary>
    [HttpPut("{id:guid}/cancel")]
    [Authorize(Policy = "ShiftManage")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _cancel.Handle(new CancelShiftCommand(id), ct);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return NoContent();
    }

    /// <summary>
    /// Coordinator starts a planned shift: Planned → Active.
    /// </summary>
    [HttpPut("{id:guid}/start")]
    [Authorize(Policy = "ShiftManage")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var result = await _start.Handle(new StartShiftCommand(id), ct);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return NoContent();
    }

    /// <summary>
    /// Coordinator completes an active shift: Active → Completed.
    /// </summary>
    [HttpPut("{id:guid}/complete")]
    [Authorize(Policy = "ShiftManage")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _complete.Handle(new CompleteShiftCommand(id), ct);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return NoContent();
    }

    /// <summary>
    /// A participant accepts or declines their invitation to a PendingApproval shift.
    /// </summary>
    [HttpPut("{id:guid}/participants/{userId:guid}/respond")]
    [Authorize]
    public async Task<IActionResult> Respond(
        Guid id,
        Guid userId,
        [FromBody] RespondRequest request,
        CancellationToken ct)
    {
        if (_currentUser.UserId != userId)
            return Forbid();

        var result = await _respond.Handle(new RespondToShiftCommand(
            id,
            userId,
            request.Response,
            _time.GetUtcNow().UtcDateTime
        ), ct);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return NoContent();
    }
}

public sealed record RespondRequest(ParticipantConfirmationStatus Response);
