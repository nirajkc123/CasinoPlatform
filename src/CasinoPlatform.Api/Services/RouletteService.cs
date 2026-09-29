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
/// European roulette (single zero, no "00"). One request = one full spin,
/// resolved atomically like the slot machine, and re-using the same
/// GameRound table so it shows up in unified history/statistics for free.
/// </summary>
public class RouletteService : IRouletteService
{
    private readonly ApplicationDbContext _db;
    private readonly GameOptions _gameOptions;
    private const int MaxConcurrencyRetries = 5;

    private static readonly HashSet<int> RedNumbers = new()
        { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };

    public RouletteService(ApplicationDbContext db, IOptions<GameOptions> gameOptions)
    {
        _db = db;
        _gameOptions = gameOptions.Value;
    }

    public async Task<RouletteSpinResponse> SpinAsync(Guid userId, RouletteSpinRequest request, CancellationToken ct = default)
    {
        if (request.BetAmount < _gameOptions.MinBet || request.BetAmount > _gameOptions.MaxBet)
            throw new ArgumentOutOfRangeException(nameof(request.BetAmount),
                $"Bet must be between {_gameOptions.MinBet} and {_gameOptions.MaxBet}.");

        ValidateBet(request.BetType, request.BetValue);

        var existing = await _db.GameRounds.AsNoTracking()
            .FirstOrDefaultAsync(g => g.RequestId == request.RequestId && g.UserId == userId, ct);
        if (existing is not null)
            return await BuildResponseFromExistingAsync(existing, ct);

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId, ct);
                if (wallet.Balance < request.BetAmount)
                    throw new InsufficientFundsException();

                var winningNumber = RandomNumberGenerator.GetInt32(0, 37); // 0-36 inclusive
                var color = ColorOf(winningNumber);
                var multiplier = EvaluateBet(request.BetType, request.BetValue, winningNumber);
                var payout = multiplier > 0 ? request.BetAmount * multiplier : 0m;

                wallet.Balance += payout - request.BetAmount;
                wallet.UpdatedAtUtc = DateTime.UtcNow;

                var round = new GameRound
                {
                    UserId = userId,
                    GameType = "Roulette",
                    BetAmount = request.BetAmount,
                    PayoutAmount = payout,
                    ResultJson = JsonSerializer.Serialize(new
                    {
                        WinningNumber = winningNumber,
                        Color = color,
                        request.BetType,
                        request.BetValue
                    }),
                    RequestId = request.RequestId
                };
                _db.GameRounds.Add(round);

                var balanceAfterBetOnly = wallet.Balance - payout;
                _db.Transactions.Add(new Transaction
                {
                    UserId = userId,
                    WalletId = wallet.Id,
                    Type = TransactionType.Bet,
                    Amount = -request.BetAmount,
                    BalanceAfter = balanceAfterBetOnly,
                    GameRoundId = round.Id,
                    Description = $"Roulette bet ({request.BetType})"
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
                        Description = $"Roulette win on {winningNumber} ({color})"
                    });
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return new RouletteSpinResponse(
                    round.Id, winningNumber, color, payout > 0, request.BetAmount, payout, wallet.Balance);
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
                if (await IsDuplicateAsync(request.RequestId, userId, ct))
                {
                    var winner = await _db.GameRounds.AsNoTracking()
                        .FirstAsync(g => g.RequestId == request.RequestId && g.UserId == userId, ct);
                    return await BuildResponseFromExistingAsync(winner, ct);
                }
                throw;
            }
        }

        throw new InvalidOperationException("Unreachable.");
    }

    private static void ValidateBet(string betType, string? betValue)
    {
        var validTypes = new[]
        {
            "Straight", "Red", "Black", "Odd", "Even", "Low", "High",
            "Dozen1", "Dozen2", "Dozen3", "Column1", "Column2", "Column3"
        };
        if (!validTypes.Contains(betType))
            throw new ArgumentOutOfRangeException(nameof(betType), $"Unknown bet type '{betType}'.");

        if (betType == "Straight")
        {
            if (!int.TryParse(betValue, out var n) || n < 0 || n > 36)
                throw new ArgumentOutOfRangeException(nameof(betValue), "Straight bets need a number from 0 to 36.");
        }
    }

    private static string ColorOf(int number)
    {
        if (number == 0) return "Green";
        return RedNumbers.Contains(number) ? "Red" : "Black";
    }

    /// <summary>Returns the payout multiplier (including the original stake) for a
    /// winning bet, or 0 for a loss. E.g. 2 means "you get your bet back plus
    /// an equal win" (a 1:1 bet), 36 means a 35:1 straight-up win.</summary>
    private static decimal EvaluateBet(string betType, string? betValue, int winningNumber)
    {
        if (winningNumber == 0 && betType != "Straight") return 0; // green loses every outside bet

        return betType switch
        {
            "Straight" => int.Parse(betValue!) == winningNumber ? 36m : 0m,
            "Red" => ColorOf(winningNumber) == "Red" ? 2m : 0m,
            "Black" => ColorOf(winningNumber) == "Black" ? 2m : 0m,
            "Odd" => winningNumber % 2 == 1 ? 2m : 0m,
            "Even" => winningNumber % 2 == 0 ? 2m : 0m,
            "Low" => winningNumber is >= 1 and <= 18 ? 2m : 0m,
            "High" => winningNumber is >= 19 and <= 36 ? 2m : 0m,
            "Dozen1" => winningNumber is >= 1 and <= 12 ? 3m : 0m,
            "Dozen2" => winningNumber is >= 13 and <= 24 ? 3m : 0m,
            "Dozen3" => winningNumber is >= 25 and <= 36 ? 3m : 0m,
            "Column1" => winningNumber is >= 1 and <= 36 && (winningNumber - 1) % 3 == 0 ? 3m : 0m,
            "Column2" => winningNumber is >= 1 and <= 36 && (winningNumber - 2) % 3 == 0 ? 3m : 0m,
            "Column3" => winningNumber is >= 1 and <= 36 && (winningNumber - 3) % 3 == 0 ? 3m : 0m,
            _ => 0m
        };
    }

    private Task<bool> IsDuplicateAsync(Guid requestId, Guid userId, CancellationToken ct)
        => _db.GameRounds.AsNoTracking().AnyAsync(g => g.RequestId == requestId && g.UserId == userId, ct);

    private async Task<RouletteSpinResponse> BuildResponseFromExistingAsync(GameRound round, CancellationToken ct)
    {
        var wallet = await _db.Wallets.AsNoTracking().FirstAsync(w => w.UserId == round.UserId, ct);
        using var doc = JsonDocument.Parse(round.ResultJson);
        var winningNumber = doc.RootElement.GetProperty("WinningNumber").GetInt32();
        var color = doc.RootElement.GetProperty("Color").GetString() ?? "Black";

        return new RouletteSpinResponse(
            round.Id, winningNumber, color, round.IsWin, round.BetAmount, round.PayoutAmount, wallet.Balance);
    }
}
