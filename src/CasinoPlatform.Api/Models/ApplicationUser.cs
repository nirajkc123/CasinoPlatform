using Microsoft.AspNetCore.Identity;

namespace CasinoPlatform.Api.Models;

/// <summary>
/// Extends the built-in Identity user with casino-specific profile fields.
/// Password hashing, security stamps, lockout, etc. are all handled by
/// ASP.NET Core Identity - we never touch raw passwords ourselves.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Wallet? Wallet { get; set; }
}
