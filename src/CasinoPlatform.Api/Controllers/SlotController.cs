using System.Security.Claims;
using CasinoPlatform.Api.Data;
using CasinoPlatform.Api.DTOs;
using CasinoPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasinoPlatform.Api.Controllers;

[ApiController]
[Route("api/games/slot")]
[Authorize]
public class SlotController(ISlotMachineService slotMachineService, ApplicationDbContext db) : ControllerBase
{
    private readonly ISlotMachineService _slotMachineService = slotMachineService;
    private readonly ApplicationDbContext _db = db;

    [HttpPost("spin")]
    public async Task<ActionResult<SpinResponse>> Spin(SpinRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _slotMachineService.SpinAsync(GetUserId(), request, ct);
            return result;
        }
        catch (InsufficientFundsException)
        {
            return BadRequest(new { message = "Insufficient balance for this bet." });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<PagedResult<GameHistoryItem>>> GetHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = GetUserId();

        var query = _db.GameRounds.AsNoTracking().Where(g => g.UserId == userId);
        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(g => g.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GameHistoryItem(g.Id, g.GameType, g.BetAmount, g.PayoutAmount, g.IsWin, g.CreatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<GameHistoryItem>(items, page, pageSize, total);
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Missing user id claim."));
}
