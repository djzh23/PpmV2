using PpmV2.Application.Einsaetze.DTOs;
using PpmV2.Application.Einsaetze.Interfaces;

namespace PpmV2.Application.Einsaetze.Queries.GetEinsatzDetails;

public sealed record GetEinsatzDetailsQuery(Guid EinsatzId);

public sealed class GetEinsatzDetailsHandler
{
    private readonly IEinsatzDetailsQuery _query;

    public GetEinsatzDetailsHandler(IEinsatzDetailsQuery query) => _query = query;

    public Task<EinsatzDetailsDto?> Handle(GetEinsatzDetailsQuery query, CancellationToken ct) =>
        _query.GetByIdAsync(query.EinsatzId, ct);
}
