using PpmV2.Domain.Einsaetze;
using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Einsaetze.Commands.Creation;

public sealed record CreateEinsatzCommand(
    string Title,
    string? Description,
    DateTime StartAtUtc,
    DateTime? EndAtUtc,
    Guid LocationId,
    IReadOnlyList<CreateEinsatzParticipantDto> Participants
);
