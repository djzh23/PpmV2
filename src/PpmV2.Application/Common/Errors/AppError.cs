namespace PpmV2.Application.Common.Errors;


public sealed record AppError(
    string Code,
    string Message,
    int HttpStatus,
    Dictionary<string, string[]>? Errors = null
);
