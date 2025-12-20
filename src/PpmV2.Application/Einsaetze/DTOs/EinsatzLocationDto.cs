namespace PpmV2.Application.Einsaetze.DTOs;

public sealed class EinsatzLocationDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? District { get; set; }
    public string? Address { get; set; }
}
