using PpmV2.Domain.Shifts;

namespace PpmV2.Application.Shifts.DTOs;

public record ShiftSummaryDto(
    Guid Id,
    string Title,
    ShiftStatus Status,
    DateTime StartAtUtc,
    DateTime? EndAtUtc,
    ShiftLocationDto Location,
    int ParticipantCount
);
