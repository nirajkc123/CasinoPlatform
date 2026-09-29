using System.ComponentModel.DataAnnotations;

namespace CasinoPlatform.Api.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(3), MaxLength(30)] string DisplayName,
    [Required, MinLength(8)] string Password
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Email,
    string DisplayName,
    IEnumerable<string> Roles,
    decimal WalletBalance
);

public record UserProfileResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    decimal WalletBalance
);
