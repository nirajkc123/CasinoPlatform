using System.Text.Json;
using CasinoPlatform.Api.Data;
using CasinoPlatform.Api.DTOs;
using CasinoPlatform.Api.Models;
using CasinoPlatform.Api.Options;
using CasinoPlatform.Api.Services.Cards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CasinoPlatform.Api.Services;

/// <summary>
/// Server-authoritative Blackjack: the deck is shuffled and every card drawn
/// server-side; the client only ever sees the state this service hands back.
/// Dealer hits until 17 or higher (stands on soft 17). Blackjack pays 3:2.
/// </summary>
public class BlackjackService : IBlackjackService
{
    private readonly ApplicationDbContext _db;
    private readonly GameOptions _gameOptions;
    private const int MaxConcurrencyRetries = 5;

    public BlackjackService(ApplicationDbContext db, IOptions<GameOptions> gameOptions)
    {
        _db = db;
        _gameOptions = gameOptions.Value;
    }

    public async Task<BlackjackRoundResponse> DealAsync(Guid userId, DealRequest request, CancellationToken ct = default)
    {
        if (request.BetAmount < _gameOptions.MinBet || request.BetAmount > _gameOptions.MaxBet)
            throw new ArgumentOutOfRangeException(nameof(request.BetAmount),
                $"Bet must be between {_gameOptions.MinBet} and {_gameOptions.MaxBet}.");

        var existing = await _db.BlackjackRounds.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RequestId == request.RequestId && r.UserId == userId, ct);
        if (existing is not null)
            return await BuildResponseAsync(existing, ct);

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId, ct);
                if (wallet.Balance < request.BetAmount)
                    throw new InsufficientFundsException();

                wallet.Balance -= request.BetAmount;
                wallet.UpdatedAtUtc = DateTime.UtcNow;

                var deck = Deck.NewShuffled();
                var playerCards = new List<Card> { deck.Draw(), deck.Draw() };
                var dealerCards = new List<Card> { deck.Draw(), deck.Draw() };

                var round = new BlackjackRound
                {
                    UserId = userId,
                    BetAmount = request.BetAmount,
                    PlayerCards = string.Join(',', playerCards),
                    DealerCards = string.Join(',', dealerCards),
                    RemainingDeck = string.Join(',', deck.ToCodes()),
                    RequestId = request.RequestId
                };

                _db.BlackjackRounds.Add(round);
                _db.Transactions.Add(new Transaction
                {
                    UserId = userId,
                    WalletId = wallet.Id,
                    Type = TransactionType.Bet,
                    Amount = -request.BetAmount,
                    BalanceAfter = wallet.Balance,
                    Description = "Blackjack bet"
                });

                var playerBlackjack = HandEvaluator.IsBlackjack(playerCards);
                var dealerBlackjack = HandEvaluator.IsBlackjack(dealerCards);

                if (playerBlackjack || dealerBlackjack)
                    ResolveRound(round, wallet, playerBlackjack, dealerBlackjack);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return MapToResponse(round, wallet.Balance);
            }
            catch (DbUpdateConcurrencyException)
            {
                DetachAll();
                if (attempt == MaxConcurrencyRetries - 1) throw;
                await Task.Delay(25 * (attempt + 1), ct);
            }
            catch (DbUpdateException)
            {
                if (await IsDuplicateRequestIdAsync(request.RequestId, userId, ct))
                {
                    var winner = await _db.BlackjackRounds.AsNoTracking()
                        .FirstAsync(r => r.RequestId == request.RequestId && r.UserId == userId, ct);
                    return await BuildResponseAsync(winner, ct);
                }
                throw;
            }
        }

        throw new InvalidOperationException("Unreachable.");
    }

    public async Task<BlackjackRoundResponse> HitAsync(Guid userId, HitRequest request, CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var round = await _db.BlackjackRounds.FirstAsync(r => r.Id == request.RoundId && r.UserId == userId, ct);
                if (round.IsResolved)
                    throw new InvalidOperationException("This round has already finished.");

                var deck = Deck.FromCodes(round.RemainingDeck.Split(',', StringSplitOptions.RemoveEmptyEntries));
                var playerCards = ParseCards(round.PlayerCards);
                playerCards.Add(deck.Draw());
                round.PlayerCards = string.Join(',', playerCards);
                round.RemainingDeck = string.Join(',', deck.ToCodes());

                var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId, ct);

                if (HandEvaluator.IsBust(playerCards))
                {
                    round.Status = BlackjackStatus.PlayerBust;
                    round.PayoutAmount = 0;
                    round.ResolvedAtUtc = DateTime.UtcNow;
                    RecordGameRound(round);
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return MapToResponse(round, wallet.Balance);
            }
            catch (DbUpdateConcurrencyException)
            {
                DetachAll();
                if (attempt == MaxConcurrencyRetries - 1) throw;
                await Task.Delay(25 * (attempt + 1), ct);
            }
        }

        throw new InvalidOperationException("Unreachable.");
    }

    public async Task<BlackjackRoundResponse> StandAsync(Guid userId, StandRequest request, CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var round = await _db.BlackjackRounds.FirstAsync(r => r.Id == request.RoundId && r.UserId == userId, ct);
                if (round.IsResolved)
                    throw new InvalidOperationException("This round has already finished.");

                var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId, ct);

                var deck = Deck.FromCodes(round.RemainingDeck.Split(',', StringSplitOptions.RemoveEmptyEntries));
                var dealerCards = ParseCards(round.DealerCards);
                var playerCards = ParseCards(round.PlayerCards);

                // Dealer hits until 17 or more (stands on soft 17, the common simple rule).
                while (HandEvaluator.Value(dealerCards) < 17)
                    dealerCards.Add(deck.Draw());

                round.DealerCards = string.Join(',', dealerCards);
                round.RemainingDeck = string.Join(',', deck.ToCodes());

                var playerTotal = HandEvaluator.Value(playerCards);
                var dealerTotal = HandEvaluator.Value(dealerCards);

                if (HandEvaluator.IsBust(dealerCards))
                {
                    round.Status = BlackjackStatus.DealerBust;
                    round.PayoutAmount = round.BetAmount * 2;
                }
                else if (playerTotal > dealerTotal)
                {
                    round.Status = BlackjackStatus.PlayerWin;
                    round.PayoutAmount = round.BetAmount * 2;
                }
                else if (playerTotal < dealerTotal)
                {
                    round.Status = BlackjackStatus.DealerWin;
                    round.PayoutAmount = 0;
                }
                else
                {
                    round.Status = BlackjackStatus.Push;
                    round.PayoutAmount = round.BetAmount; // return the original bet only
                }

                round.ResolvedAtUtc = DateTime.UtcNow;

                if (round.PayoutAmount > 0)
                {
                    wallet.Balance += round.PayoutAmount;
                    wallet.UpdatedAtUtc = DateTime.UtcNow;
                    _db.Transactions.Add(new Transaction
                    {
                        UserId = userId,
                        WalletId = wallet.Id,
                        Type = TransactionType.Win,
                        Amount = round.PayoutAmount,
                        BalanceAfter = wallet.Balance,
                        Description = $"Blackjack {round.Status}"
                    });
                }

                RecordGameRound(round);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return MapToResponse(round, wallet.Balance);
            }
            catch (DbUpdateConcurrencyException)
            {
                DetachAll();
                if (attempt == MaxConcurrencyRetries - 1) throw;
                await Task.Delay(25 * (attempt + 1), ct);
            }
        }

        throw new InvalidOperationException("Unreachable.");
    }

    /// <summary>Resolves a round immediately at deal-time (natural blackjack or push).</summary>
    private void ResolveRound(BlackjackRound round, Wallet wallet, bool playerBlackjack, bool dealerBlackjack)
    {
        round.ResolvedAtUtc = DateTime.UtcNow;

        if (playerBlackjack && dealerBlackjack)
        {
            round.Status = BlackjackStatus.Push;
            round.PayoutAmount = round.BetAmount;
        }
        else if (playerBlackjack)
        {
            round.Status = BlackjackStatus.PlayerBlackjack;
            round.PayoutAmount = round.BetAmount * 2.5m; // 3:2 payout plus the original bet back
        }
        else
        {
            round.Status = BlackjackStatus.DealerWin;
            round.PayoutAmount = 0;
        }

        if (round.PayoutAmount > 0)
        {
            wallet.Balance += round.PayoutAmount;
            wallet.UpdatedAtUtc = DateTime.UtcNow;
            _db.Transactions.Add(new Transaction
            {
                UserId = round.UserId,
                WalletId = wallet.Id,
                Type = TransactionType.Win,
                Amount = round.PayoutAmount,
                BalanceAfter = wallet.Balance,
                Description = $"Blackjack {round.Status}"
            });
        }

        RecordGameRound(round);
    }

    /// <summary>
    /// Writes a matching row into the shared GameRound ledger once a hand
    /// resolves, so Blackjack shows up in unified history/statistics
    /// alongside the slot machine and roulette without any extra queries.
    /// </summary>
    private void RecordGameRound(BlackjackRound round)
    {
        _db.GameRounds.Add(new GameRound
        {
            UserId = round.UserId,
            GameType = "Blackjack",
            BetAmount = round.BetAmount,
            PayoutAmount = round.PayoutAmount,
            ResultJson = JsonSerializer.Serialize(new
            {
                round.PlayerCards,
                round.DealerCards,
                Status = round.Status.ToString()
            }),
            RequestId = Guid.NewGuid(),
            CreatedAtUtc = round.ResolvedAtUtc ?? DateTime.UtcNow
        });
    }

    private Task<bool> IsDuplicateRequestIdAsync(Guid requestId, Guid userId, CancellationToken ct)
        => _db.BlackjackRounds.AsNoTracking().AnyAsync(r => r.RequestId == requestId && r.UserId == userId, ct);

    private async Task<BlackjackRoundResponse> BuildResponseAsync(BlackjackRound round, CancellationToken ct)
    {
        var wallet = await _db.Wallets.AsNoTracking().FirstAsync(w => w.UserId == round.UserId, ct);
        return MapToResponse(round, wallet.Balance);
    }

    private static BlackjackRoundResponse MapToResponse(BlackjackRound round, decimal newBalance)
    {
        var playerCards = ParseCards(round.PlayerCards).Select(c => c.ToString()).ToArray();
        var dealerCardsAll = ParseCards(round.DealerCards).Select(c => c.ToString()).ToArray();

        // While the hand is still in progress, hide the dealer's hole card -
        // the client should never be able to see it before it's revealed.
        var dealerCardsVisible = round.IsResolved ? dealerCardsAll : new[] { dealerCardsAll[0] };
        int? dealerTotal = round.IsResolved
            ? HandEvaluator.Value(ParseCards(round.DealerCards))
            : null;

        return new BlackjackRoundResponse(
            round.Id,
            playerCards,
            HandEvaluator.Value(ParseCards(round.PlayerCards)),
            dealerCardsVisible,
            dealerTotal,
            round.Status.ToString(),
            round.BetAmount,
            round.PayoutAmount,
            newBalance
        );
    }

    private static List<Card> ParseCards(string joined) =>
        joined.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Card.Parse).ToList();

    private void DetachAll()
    {
        foreach (var entry in _db.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;
    }
}
