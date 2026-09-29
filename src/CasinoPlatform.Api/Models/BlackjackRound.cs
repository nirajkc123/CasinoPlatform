namespace CasinoPlatform.Api.Models;

public enum BlackjackStatus
{
    InProgress = 0,
    PlayerBust = 1,
    DealerBust = 2,
    PlayerBlackjack = 3,
    PlayerWin = 4,
    DealerWin = 5,
    Push = 6
}

/// <summary>
/// A single Blackjack hand, spanning several HTTP requests (deal, then zero or
/// more hits, then stand). Unlike the slot machine's one-shot spin, this table
/// IS the server-side session for an in-progress hand - the client never
/// tracks game state itself, it just displays whatever this row currently says.
/// </summary>
public class BlackjackRound
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    public decimal BetAmount { get; set; }
    public decimal PayoutAmount { get; set; }

    public BlackjackStatus Status { get; set; } = BlackjackStatus.InProgress;

    /// <summary>Comma-separated card codes, e.g. "AS,10H".</summary>
    public string PlayerCards { get; set; } = string.Empty;
    public string DealerCards { get; set; } = string.Empty;

    /// <summary>Remaining shoe, so Hit draws the next real card instead of a fresh random one.</summary>
    public string RemainingDeck { get; set; } = string.Empty;

    /// <summary>Idempotency key for the initial Deal request only.</summary>
    public Guid RequestId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }

    public bool IsResolved => Status != BlackjackStatus.InProgress;
}
