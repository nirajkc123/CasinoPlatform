using CasinoPlatform.Api.Data;
using CasinoPlatform.Api.Models;
using CasinoPlatform.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CasinoPlatform.Api.Services;

/// <summary>
/// Every balance change goes through this one place, always as:
///   1. load wallet (tracked)
///   2. mutate Balance in memory
///   3. write a Transaction row
///   4. SaveChangesAsync() inside a DB transaction
/// If another request touched the same wallet in between steps 1 and 4,
/// the RowVersion mismatch throws DbUpdateConcurrencyException and we
/// retry from scratch (bounded retry loop) rather than lose an update.
/// </summary>
public class WalletService(ApplicationDbContext db, IOptions<GameOptions> gameOptions) : IWalletService
{
    private readonly ApplicationDbContext _db = db;
    private readonly GameOptions _gameOptions = gameOptions.Value;
    private const int MaxConcurrencyRetries = 5;

    public async Task<Wallet> CreateWalletWithInitialGrantAsync(Guid userId, CancellationToken ct = default)
    {
        var wallet = new Wallet { UserId = userId, Balance = _gameOptions.InitialGrantAmount };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.Wallets.Add(wallet);
        _db.Transactions.Add(new Transaction
        {
            UserId = userId,
            WalletId = wallet.Id,
            Type = TransactionType.InitialGrant,
            Amount = _gameOptions.InitialGrantAmount,
            BalanceAfter = wallet.Balance,
            Description = "Welcome bonus - virtual coins only, no cash value"
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return wallet;
    }

    public async Task<Wallet> GetWalletAsync(Guid userId, CancellationToken ct = default)
    {
        var wallet = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, ct);
        return wallet ?? throw new InvalidOperationException("Wallet not found for user.");
    }

    public Task<(Wallet wallet, Transaction transaction)> DepositAsync(
        Guid userId, decimal amount, string description, CancellationToken ct = default)
        => ApplyDeltaAsync(userId, amount, TransactionType.Deposit, description, gameRoundId: null, ct);

    public Task<(Wallet wallet, Transaction transaction)> WithdrawAsync(
        Guid userId, decimal amount, string description, CancellationToken ct = default)
        => ApplyDeltaAsync(userId, -amount, TransactionType.Withdraw, description, gameRoundId: null, ct);

    /// <summary>
    /// Core primitive used by deposits/withdrawals AND by the game engine
    /// (bets are negative deltas, wins are positive deltas) so every coin
    /// movement in the whole platform is auditable the same way.
    /// </summary>
    internal async Task<(Wallet wallet, Transaction transaction)> ApplyDeltaAsync(
        Guid userId, decimal delta, TransactionType type, string description,
        Guid? gameRoundId, CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId, ct);

                var newBalance = wallet.Balance + delta;
                if (newBalance < 0)
                    throw new InsufficientFundsException();

                wallet.Balance = newBalance;
                wallet.UpdatedAtUtc = DateTime.UtcNow;

                var transaction = new Transaction
                {
                    UserId = userId,
                    WalletId = wallet.Id,
                    Type = type,
                    Amount = delta,
                    BalanceAfter = newBalance,
                    GameRoundId = gameRoundId,
                    Description = description
                };
                _db.Transactions.Add(transaction);

                await _db.SaveChangesAsync(ct); // RowVersion check happens here
                await tx.CommitAsync(ct);

                return (wallet, transaction);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Someone else updated this wallet between our read and write.
                // Detach the stale entities and retry the whole operation.
                foreach (var entry in _db.ChangeTracker.Entries().ToList())
                    entry.State = EntityState.Detached;

                if (attempt == MaxConcurrencyRetries - 1) throw;
                await Task.Delay(25 * (attempt + 1), ct);
            }
        }

        throw new InvalidOperationException("Unreachable.");
    }

    public async Task<IReadOnlyList<Transaction>> GetHistoryAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        return await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> GetHistoryCountAsync(Guid userId, CancellationToken ct = default)
        => _db.Transactions.CountAsync(t => t.UserId == userId, ct);
}
