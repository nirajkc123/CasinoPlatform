namespace CasinoPlatform.Api.Models;

public enum TransactionType
{
    InitialGrant = 0, // the free 10,000 coins on registration
    Deposit = 1,      // virtual "top up" (Stage 3)
    Withdraw = 2,     // virtual "cash out" (Stage 3)
    Bet = 3,          // coins staked on a game round
    Win = 4           // coins paid out from a game round
}

/// <summary>
/// Append-only ledger row. We NEVER update or delete a Transaction -
/// the wallet balance is always derived from Wallet.Balance, and this
/// table exists purely as an auditable history (Stage 3 item 17,
/// and the basis for Stage 10's immutable financial records).
/// </summary>
public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }

    public TransactionType Type { get; set; }

    /// <summary>Signed amount: positive for credits (deposit/win/initial),
    /// negative for debits (withdraw/bet).</summary>
    public decimal Amount { get; set; }

    /// <summary>Wallet balance immediately after this transaction was applied.</summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>Optional link back to the GameRound that generated this row.</summary>
    public Guid? GameRoundId { get; set; }

    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
