namespace PpmV2.Application.Users.DTOs;

public sealed record UpdateUserLocationsRequest(IReadOnlyList<Guid> LocationIds);
