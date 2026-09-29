using System.Security.Claims;
using CasinoPlatform.Api.Data;
using CasinoPlatform.Api.DTOs;
using CasinoPlatform.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CasinoPlatform.Api.Controllers;

/// <summary>
/// Endpoints that span every game rather than belonging to one - history
/// across Slot/Roulette/Blackjack, aggregate statistics, and the public
/// payout/config table the frontend renders (Stage 5, items 28-30).
/// </summary>
[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly GameOptions _gameOptions;

    public GamesController(ApplicationDbContext db, IOptions<GameOptions> gameOptions)
    {
        _db = db;
        _gameOptions = gameOptions.Value;
    }

    [HttpGet("history")]
    [Authorize]
    public async Task<ActionResult<PagedResult<UnifiedHistoryItem>>> GetHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? gameType = null, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = GetUserId();

        var query = _db.GameRounds.AsNoTracking().Where(g => g.UserId == userId);
        if (!string.IsNullOrWhiteSpace(gameType))
            query = query.Where(g => g.GameType == gameType);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(g => g.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new UnifiedHistoryItem(g.Id, g.GameType, g.BetAmount, g.PayoutAmount, g.IsWin, g.CreatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<UnifiedHistoryItem>(items, page, pageSize, total);
    }

    [HttpGet("statistics")]
    [Authorize]
    public async Task<ActionResult<GameStatisticsResponse>> GetStatistics(CancellationToken ct)
    {
        var userId = GetUserId();
        var rounds = await _db.GameRounds.AsNoTracking()
            .Where(g => g.UserId == userId)
            .Select(g => new { g.GameType, g.BetAmount, g.PayoutAmount })
            .ToListAsync(ct);

        var totalWagered = rounds.Sum(r => r.BetAmount);
        var totalPayout = rounds.Sum(r => r.PayoutAmount);
        var roundsWon = rounds.Count(r => r.PayoutAmount > r.BetAmount);

        var byType = rounds
            .GroupBy(r => r.GameType)
            .Select(g => new GameTypeBreakdown(
                g.Key,
                g.Count(),
                g.Sum(r => r.BetAmount),
                g.Sum(r => r.PayoutAmount),
                g.Count(r => r.PayoutAmount > r.BetAmount)))
            .OrderByDescending(b => b.RoundsPlayed)
            .ToList();

        return new GameStatisticsResponse(
            rounds.Count,
            totalWagered,
            totalPayout,
            totalPayout - totalWagered,
            roundsWon,
            rounds.Count == 0 ? 0 : Math.Round(roundsWon * 100m / rounds.Count, 1),
            rounds.Count == 0 ? 0 : rounds.Max(r => r.PayoutAmount),
            byType
        );
    }

    /// <summary>
    /// Public (no auth needed) so the frontend can render accurate bet limits
    /// and payout tables without hardcoding them - one source of truth.
    /// </summary>
    [HttpGet("config")]
    [AllowAnonymous]
    public ActionResult<GameConfigResponse> GetConfig()
    {
        return new GameConfigResponse(
            _gameOptions.MinBet,
            _gameOptions.MaxBet,
            _gameOptions.InitialGrantAmount,
            new SlotConfig(new Dictionary<string, decimal>
            {
                ["🍒"] = 2m,
                ["🍋"] = 3m,
                ["🔔"] = 5m,
                ["⭐"] = 10m,
                ["7️⃣"] = 50m
            }),
            new RouletteConfig(new Dictionary<string, decimal>
            {
                ["Straight"] = 36m,
                ["Red/Black/Odd/Even/Low/High"] = 2m,
                ["Dozen/Column"] = 3m
            }, "European (single zero)"),
            new BlackjackConfig(2.5m, "Dealer hits until 17, stands on soft 17")
        );
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Missing user id claim."));
}
