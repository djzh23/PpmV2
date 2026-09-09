using PpmV2.Application.Shifts.DTOs;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Interfaces;

public interface IShiftListQuery
{
    Task<IReadOnlyList<ShiftSummaryDto>> GetAllAsync(ShiftStatus? status, CancellationToken ct);
}
