using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Einsaetze.Smokes;

public sealed class LocationSmokeDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? District { get; set; }
    public string? Address { get; set; }
}