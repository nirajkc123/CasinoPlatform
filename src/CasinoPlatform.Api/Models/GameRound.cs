namespace CasinoPlatform.Api.Models;

/// <summary>
/// Every game round (currently just the slot machine) is recorded here,
/// server-computed outcome and all, before any coins move (Stage 4, item 24).
/// </summary>
public class GameRound
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    public string GameType { get; set; } = "SlotMachine";

    public decimal BetAmount { get; set; }
    public decimal PayoutAmount { get; set; }
    public bool IsWin => PayoutAmount > 0;

    /// <summary>JSON-serialized outcome, e.g. the 3x3 reel grid, so the
    /// frontend can render exactly what the server decided happened.</summary>
    public string ResultJson { get; set; } = string.Empty;

    /// <summary>
    /// Client-supplied idempotency key (one per spin attempt). A unique
    /// index on this column in the DbContext is what actually stops a
    /// resubmitted/replayed spin request from being paid out twice
    /// (Stage 4, item 25) - the application code never needs to "remember"
    /// anything, the database enforces it.
    /// </summary>
    public Guid RequestId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
