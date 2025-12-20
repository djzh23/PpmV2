namespace PpmV2.Application.Auth.DTOs;

public enum AuthErrorCode
{
    None = 0,

    ValidationFailed = 1,
    UserAlreadyExists = 2,
    InvalidCredentials = 3,
    NotApproved = 4,
    UserCreationFailed = 5
}