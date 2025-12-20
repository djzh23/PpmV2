using PpmV2.Application.Common.Exceptions;
using PpmV2.Application.Shifts.Interfaces;
using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.Commands.Creation;

/// <summary>
/// Application use case handler for creating a Shift in Draft status.
/// </summary>
/// <remarks>
/// Responsibilities:
/// - Validate input and enforce creation-time invariants
/// - Validate referenced entities (Location, Users)
/// - Create the Shift aggregate and persist it via repository
///
/// Note: Publishing rules (e.g. readiness) are handled in dedicated use cases.
public sealed class CreateShiftHandler
{
    private readonly IShiftRepository _repo;

    public CreateShiftHandler(IShiftRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateShiftCommand cmd, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();

        // Collect field-level validation errors to return a consistent ProblemDetails response.
        if (string.IsNullOrWhiteSpace(cmd.Title))
            errors["title"] = new[] { "Title is required." };

        if (cmd.EndAtUtc is not null && cmd.EndAtUtc <= cmd.StartAtUtc)
            errors["endAtUtc"] = new[] { "EndAtUtc must be after StartAtUtc." };

        // Invariant (creation): exactly one Leader must be present.
        if (cmd.Participants is null || cmd.Participants.Count == 0)
            errors["participants"] = new[] { "At least one participant is required." };
        else
        {
            if (cmd.Participants.Any(p => p.UserId == Guid.Empty))
                errors["participants.userId"] = new[] { "UserId is required." };

            if (cmd.Participants.Count(p => p.Role == ShiftRole.Leader) != 1)
                errors["participants.role"] = new[] { "Exactly one Leader is required." };
        }

        if (errors.Count > 0)
            throw new ValidationException(errors);

        // Referential integrity checks (defensive validation before persistence).
        if (!await _repo.LocationExistsAsync(cmd.LocationId, ct))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["locationId"] = new[] { "LocationId not found." }
            });

        var userIds = cmd.Participants!.Select(p => p.UserId).Distinct().ToList();
        var existing = await _repo.CountExistingUsersAsync(userIds, ct);
        if (existing != userIds.Count)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["participants.userId"] = new[] { "One or more UserIds do not exist." }
            });

        // Create Shift aggregate in Draft status.
        var einsatzId = Guid.NewGuid();

        var einsatz = new Shift
        {
            Id = einsatzId,
            Title = cmd.Title.Trim(),
            Description = cmd.Description,
            StartAtUtc = cmd.StartAtUtc,
            EndAtUtc = cmd.EndAtUtc,
            LocationId = cmd.LocationId,
            Status = ShiftStatus.Draft
        };

        var participants = cmd.Participants!.Select(p => new ShiftParticipant
        {
            EinsatzId = einsatzId,
            UserId = p.UserId,
            Role = p.Role
        }).ToList();

        await _repo.AddAsync(einsatz, participants, ct);
        await _repo.SaveChangesAsync(ct);

        return einsatzId;
    }
}
