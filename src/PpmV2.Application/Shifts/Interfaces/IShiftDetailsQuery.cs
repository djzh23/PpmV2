using PpmV2.Application.Shifts.DTOs;

namespace PpmV2.Application.Shifts.Interfaces;

/// <summary>
/// Read-side query port for shift details.
/// </summary>
/// <remarks>
/// This interface represents the CQRS read model boundary.
/// Implementations should return DTO projections (read models) and avoid exposing EF Core entities.
/// </remarks>
public interface IShiftDetailsQuery
{
    /// <summary>Returns a detailed read model for the given shift(Einsatz) id or null if not found.</summary>
    Task<ShiftDetailsDto?> GetByIdAsync(Guid einsatzId, CancellationToken ct);
}