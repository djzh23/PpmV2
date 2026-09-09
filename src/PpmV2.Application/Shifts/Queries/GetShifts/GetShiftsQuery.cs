using PpmV2.Application.Shifts.DTOs;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Queries.GetShifts;

public sealed record GetShiftsQuery(ShiftStatus? Status = null);

public sealed class GetShiftsHandler
{
    private readonly IShiftListQuery _query;

    public GetShiftsHandler(IShiftListQuery query) => _query = query;

    public Task<IReadOnlyList<ShiftSummaryDto>> Handle(GetShiftsQuery query, CancellationToken ct) =>
        _query.GetAllAsync(query.Status, ct);
}
