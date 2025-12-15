using PpmV2.Application.Locations.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Locations.Interfaces;

public interface ILocationQueryService
{
    Task<IReadOnlyList<LocationListItemDto>> GetActiveAsync();
}
