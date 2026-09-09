using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Locations.Interfaces;
using PpmV2.Application.Users.Interfaces;

namespace PpmV2.Api.Controllers.Locations;

[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationQueryService _service;
    private readonly IUserLocationQuery _staffQuery;

    public LocationsController(ILocationQueryService service, IUserLocationQuery staffQuery)
    {
        _service = service;
        _staffQuery = staffQuery;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveLocations(CancellationToken ct)
    {
        var locations = await _service.GetActiveAsync(ct);
        return Ok(locations);
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