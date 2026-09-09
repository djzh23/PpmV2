using PpmV2.Application.Shifts.DTOs;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Interfaces;

public interface IShiftListQuery
{
    /// <summary>
    /// Returns shifts filtered by status and optionally restricted to a participant.
    /// Pass participantId to return only shifts the user is assigned to (Festmitarbeiter/Honorarkraft visibility).
    /// </summary>
    Task<IReadOnlyList<ShiftSummaryDto>> GetAllAsync(ShiftStatus? status, Guid? participantId, CancellationToken ct);
}
