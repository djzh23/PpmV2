using PpmV2.Application.Einsaetze.DTOs;

namespace PpmV2.Application.Einsaetze.Interfaces;

public interface IEinsatzDetailsQuery
{
    Task<EinsatzDetailsDto?> GetByIdAsync(Guid einsatzId, CancellationToken ct);
}