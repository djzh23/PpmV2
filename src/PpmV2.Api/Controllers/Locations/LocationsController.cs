using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Locations.Interfaces;

namespace PpmV2.Api.Controllers.Locations;

[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationQueryService _service;

    public LocationsController(ILocationQueryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveLocations()
    {
        var locations = await _service.GetActiveAsync();
        return Ok(locations);
    }
}