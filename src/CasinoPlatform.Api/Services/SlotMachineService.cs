using System.Security.Cryptography;
using System.Text.Json;
using CasinoPlatform.Api.Data;
using CasinoPlatform.Api.DTOs;
using CasinoPlatform.Api.Models;
using CasinoPlatform.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CasinoPlatform.Api.Services;

/// <summary>
/// Server-side slot machine. The client NEVER decides the outcome - it only
/// sends a bet amount and a RequestId; every reel result and payout is
/// generated here using a cryptographically secure RNG (Stage 4, items 19-21).
/// </summary>
public class SlotMachineService(ApplicationDbContext db, IOptions<GameOptions> gameOptions) : ISlotMachineService
{
    private readonly ApplicationDbContext _db = db;
    private readonly GameOptions _gameOptions = gameOptions.Value;
    private const int MaxConcurrencyRetries = 5;

    // Symbol weights control the real house edge: lower weight = rarer = higher payout.
    // Cherry/Lemon/Bell/Star/Seven, weighted so Seven is rare and Cherry is common.
    private static readonly (string Symbol, int Weight)[] SymbolTable =
    {
        ("🍒", 40),
        ("🍋", 30),
        ("🔔", 15),
        ("⭐", 10),
        ("7️⃣", 5)
    };

    // Multiplier applied to the bet when all 3 symbols on the middle payline match.
    private static readonly Dictionary<string, decimal> PayoutMultipliers = new()
    {
        ["🍒"] = 2m,
        ["🍋"] = 3m,
        ["🔔"] = 5m,
        ["⭐"] = 10m,
        ["7️⃣"] = 50m
    };

    private readonly int _totalWeight = SymbolTable.Sum(s => s.Weight);

    public async Task<SpinResponse> SpinAsync(Guid userId, SpinRequest request, CancellationToken ct = default)
    {
        if (request.BetAmount < _gameOptions.MinBet || request.BetAmount > _gameOptions.MaxBet)
            throw new ArgumentOutOfRangeException(nameof(request.BetAmount),
                $"Bet must be between {_gameOptions.MinBet} and {_gameOptions.MaxBet}.");

        // Fast path: this exact spin was already processed (client retry / double submit).
        // Returning the cached result instead of re-spinning is what makes the
        // endpoint safe to call twice with the same RequestId (Stage 4, item 25).
        var existing = await _db.GameRounds.AsNoTracking()
            .FirstOrDefaultAsync(g => g.RequestId == request.RequestId && g.UserId == userId, ct);
        if (existing is not null)
            return await BuildResponseFromExistingRoundAsync(existing, ct);

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId, ct);

                if (wallet.Balance < request.BetAmount)
                    throw new InsufficientFundsException();

                var reels = GenerateReels();
                var payout = CalculatePayout(reels, request.BetAmount, out var winDescription);

                var netDelta = payout - request.BetAmount;
                wallet.Balance += netDelta;
                wallet.UpdatedAtUtc = DateTime.UtcNow;

                var round = new GameRound
                {
                    UserId = userId,
                    GameType = "SlotMachine",
                    BetAmount = request.BetAmount,
                    PayoutAmount = payout,
                    ResultJson = JsonSerializer.Serialize(reels),
                    RequestId = request.RequestId
                };
                _db.GameRounds.Add(round);

                // wallet.Balance already reflects netDelta (payout - bet) applied above,
                // so subtracting payout back out gives the balance as it stood right
                // after the bet was placed but before any win was credited.
                var balanceAfterBetOnly = wallet.Balance - payout;

                _db.Transactions.Add(new Transaction
                {
                    UserId = userId,
                    WalletId = wallet.Id,
                    Type = TransactionType.Bet,
                    Amount = -request.BetAmount,
                    BalanceAfter = balanceAfterBetOnly,
                    GameRoundId = round.Id,
                    Description = "Slot machine bet"
                });

                if (payout > 0)
                {
                    _db.Transactions.Add(new Transaction
                    {
                        UserId = userId,
                        WalletId = wallet.Id,
                        Type = TransactionType.Win,
                        Amount = payout,
                        BalanceAfter = wallet.Balance,
                        GameRoundId = round.Id,
                        Description = winDescription ?? "Slot machine win"
                    });
                }

                // Both the wallet's RowVersion check and the GameRound.RequestId
                // unique index are enforced right here, atomically.
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return new SpinResponse(
                    round.Id, reels, payout > 0, request.BetAmount, payout, wallet.Balance, winDescription);
            }
            catch (DbUpdateConcurrencyException)
            {
                foreach (var entry in _db.ChangeTracker.Entries().ToList())
                    entry.State = EntityState.Detached;
                if (attempt == MaxConcurrencyRetries - 1) throw;
                await Task.Delay(25 * (attempt + 1), ct);
            }
            catch (DbUpdateException)
            {
                // Two concurrent requests raced with the same RequestId; the unique
                // index rejected the second insert and the whole transaction rolled
                // back (including the wallet change), so the bet was NOT double-charged.
                // Whichever request won gets its row back here.
                if (await IsDuplicateRequestIdAsync(request.RequestId, userId, ct))
                {
                    var winner = await _db.GameRounds.AsNoTracking()
                        .FirstAsync(g => g.RequestId == request.RequestId && g.UserId == userId, ct);
                    return await BuildResponseFromExistingRoundAsync(winner, ct);
                }

                // Not a duplicate RequestId; rethrow original exception.
                throw;
            }
        }

        throw new InvalidOperationException("Unreachable.");
    }

    private Task<bool> IsDuplicateRequestIdAsync(Guid requestId, Guid userId, CancellationToken ct)
        => _db.GameRounds.AsNoTracking().AnyAsync(g => g.RequestId == requestId && g.UserId == userId, ct);

    private async Task<SpinResponse> BuildResponseFromExistingRoundAsync(GameRound round, CancellationToken ct)
    {
        var wallet = await _db.Wallets.AsNoTracking().FirstAsync(w => w.UserId == round.UserId, ct);
        var reels = JsonSerializer.Deserialize<string[][]>(round.ResultJson) ?? Array.Empty<string[]>();
        return new SpinResponse(round.Id, reels, round.IsWin, round.BetAmount, round.PayoutAmount,
            wallet.Balance, round.IsWin ? "Replayed result" : null);
    }

    /// <summary>3 reels x 3 rows, each cell drawn independently using a
    /// cryptographically secure RNG (not System.Random - that's predictable
    /// and must never drive real payouts).</summary>
    private string[][] GenerateReels()
    {
        var reels = new string[3][];
        for (var col = 0; col < 3; col++)
        {
            reels[col] = new string[3];
            for (var row = 0; row < 3; row++)
                reels[col][row] = WeightedRandomSymbol();
        }
        return reels;
    }

    private string WeightedRandomSymbol()
    {
        var roll = RandomNumberGenerator.GetInt32(0, _totalWeight);
        var cumulative = 0;
        foreach (var (symbol, weight) in SymbolTable)
        {
            cumulative += weight;
            if (roll < cumulative) return symbol;
        }
        return SymbolTable[^1].Symbol; // unreachable in practice
    }

    /// <summary>Pays out only on the middle row (the classic single payline).
    /// Three-of-a-kind on that row wins bet * multiplier for that symbol.</summary>
    private decimal CalculatePayout(string[][] reels, decimal betAmount, out string? winDescription)
    {
        var middleRow = new[] { reels[0][1], reels[1][1], reels[2][1] };

        if (middleRow[0] == middleRow[1] && middleRow[1] == middleRow[2])
        {
            var symbol = middleRow[0];
            var multiplier = PayoutMultipliers[symbol];
            winDescription = $"Three {symbol} - {multiplier}x payout!";
            return betAmount * multiplier;
        }

        winDescription = null;
        return 0m;
    }
}
