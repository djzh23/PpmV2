namespace PpmV2.Application.Shifts.DTOs;

public sealed class ShiftLocationDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? District { get; set; }
    public string? Address { get; set; }
}
