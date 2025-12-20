using System.ComponentModel.DataAnnotations;

namespace PpmV2.Application.Auth.DTOs;

public sealed class RegisterRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Firstname { get; set; } = default!;

    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Lastname { get; set; } = default!;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = default!;
}
