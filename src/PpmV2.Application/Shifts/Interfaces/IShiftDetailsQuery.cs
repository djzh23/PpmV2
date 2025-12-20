using PpmV2.Application.Shifts.DTOs;

namespace PpmV2.Application.Shifts.Interfaces;

public interface IShiftDetailsQuery
{
    Task<ShiftDetailsDto?> GetByIdAsync(Guid einsatzId, CancellationToken ct);
}