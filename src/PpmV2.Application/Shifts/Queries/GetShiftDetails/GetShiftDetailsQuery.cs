using PpmV2.Application.Shifts.DTOs;
using PpmV2.Application.Shifts.Interfaces;

namespace PpmV2.Application.Shifts.Queries.GetEinsatzDetails;

public sealed record GetShiftDetailsQuery(Guid EinsatzId);

public sealed class GetEinsatzDetailsHandler
{
    private readonly IShiftDetailsQuery _query;

    public GetEinsatzDetailsHandler(IShiftDetailsQuery query) => _query = query;

    public Task<ShiftDetailsDto?> Handle(GetShiftDetailsQuery query, CancellationToken ct) =>
        _query.GetByIdAsync(query.EinsatzId, ct);
}
