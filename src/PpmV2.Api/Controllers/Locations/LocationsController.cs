using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Api.Common;
using PpmV2.Application.Common.Results;
using PpmV2.Application.Locations.DTOs;
using PpmV2.Application.Locations.Interfaces;
using PpmV2.Application.Users.Interfaces;

namespace PpmV2.Api.Controllers.Locations;

[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationQueryService _query;
    private readonly ILocationCommandService _command;
    private readonly IUserLocationQuery _staffQuery;

    public LocationsController(
        ILocationQueryService query,
        ILocationCommandService command,
        IUserLocationQuery staffQuery)
    {
        _query = query;
        _command = command;
        _staffQuery = staffQuery;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        // includeInactive is restricted to Coordinator and Admin.
        // For all other roles the parameter is silently ignored — no 403, just active-only results.
        var canSeeInactive = User.IsInRole("Coordinator") || User.IsInRole("Admin");
        var locations = await _query.GetAllAsync(includeInactive && canSeeInactive, ct);
        return Ok(locations);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var location = await _query.GetByIdAsync(id, ct);
        return location is null ? NotFound() : Ok(location);
    }

    [HttpPost]
    [Authorize(Policy = "ShiftManage")]
    public async Task<ActionResult<LocationDetailDto>> Create(
        [FromBody] CreateLocationRequest request,
        CancellationToken ct)
    {
        var result = await _command.CreateAsync(request, ct);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ShiftManage")]
    public async Task<ActionResult<LocationDetailDto>> Update(
        Guid id,
        [FromBody] UpdateLocationRequest request,
        CancellationToken ct)
    {
        var result = await _command.UpdateAsync(id, request, ct);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ShiftManage")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _command.DeactivateAsync(id, ct);

        if (!result.Success)
            return ApiProblem.From(result.ToAppError(), HttpContext);

        return NoContent();
    }

    /// <summary>
    /// Returns Festmitarbeiter assigned to this location who are available on the given date.
    /// </summary>
    [HttpGet("{id:guid}/available-staff")]
    public async Task<IActionResult> GetAvailableStaff(Guid id, [FromQuery] DateOnly date, CancellationToken ct)
    {
        var staff = await _staffQuery.GetAvailableStaffAsync(id, date, ct);
        return Ok(staff);
    }
}
