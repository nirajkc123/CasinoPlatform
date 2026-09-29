using System.ComponentModel.DataAnnotations;

namespace CasinoPlatform.Api.Models;

/// <summary>
/// One wallet per user, holding a virtual coin balance only.
/// No real money ever touches this table (see Stage 10 for that architecture).
/// </summary>
public class Wallet
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>Current virtual coin balance. Never modified directly -
    /// always go through WalletService so a Transaction row is always written.</summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// SQL Server rowversion. EF Core uses this as an optimistic-concurrency
    /// token: if two requests try to update the same wallet at the same
    /// time, the second save throws DbUpdateConcurrencyException instead of
    /// silently overwriting the first (Stage 3, item 18).
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
