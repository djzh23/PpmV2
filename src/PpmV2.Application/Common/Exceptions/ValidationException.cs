namespace PpmV2.Application.Common.Exceptions;

public sealed class ValidationException : Exception
{
    public string Code { get; }
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string message, string code = "VALIDATION_ERROR")
        : base(message)
    {
        Code = code;
        Errors = new();
    }

    public ValidationException(Dictionary<string, string[]> errors, string code = "VALIDATION_ERROR")
        : base("Validation failed.")
    {
        Code = code;
        Errors = errors;
    }
}
