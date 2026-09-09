using PpmV2.Application.Shifts.DTOs;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;
using PpmV2.Domain.Users;

namespace PpmV2.Application.Shifts.Queries.GetShifts;

public sealed record GetShiftsQuery(ShiftStatus? Status = null);

public sealed class GetShiftsHandler
{
    private readonly IShiftListQuery _query;
    private readonly ICurrentUser _currentUser;

    public GetShiftsHandler(IShiftListQuery query, ICurrentUser currentUser)
    {
        _query = query;
        _currentUser = currentUser;
    }

    public Task<IReadOnlyList<ShiftSummaryDto>> Handle(GetShiftsQuery query, CancellationToken ct)
    {
        // Coordinators and Admins see all shifts.
        // Festmitarbeiter and Honorarkraft see only shifts they are assigned to.
        var filterByParticipant = !_currentUser.IsInRole(UserRole.Coordinator.ToString())
                                && !_currentUser.IsInRole(UserRole.Admin.ToString());

        var participantId = filterByParticipant ? _currentUser.UserId : (Guid?)null;

        return _query.GetAllAsync(query.Status, participantId, ct);
    }
}
