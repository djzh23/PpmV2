using PpmV2.Application.Shifts.DTOs;
using PpmV2.Application.Shifts.Interfaces;

namespace PpmV2.Application.Shifts.Queries.GetShiftDetails;

public sealed record GetShiftDetailsQuery(Guid EinsatzId);

public sealed class GetShiftDetailsHandler
{
    private readonly IShiftDetailsQuery _query;

    public GetShiftDetailsHandler(IShiftDetailsQuery query) => _query = query;

    public Task<ShiftDetailsDto?> Handle(GetShiftDetailsQuery query, CancellationToken ct) =>
        _query.GetByIdAsync(query.EinsatzId, ct);
}
