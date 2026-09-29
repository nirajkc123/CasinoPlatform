using System.Security.Claims;
using CasinoPlatform.Api.DTOs;
using CasinoPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasinoPlatform.Api.Controllers;

[ApiController]
[Route("api/games/blackjack")]
[Authorize]
public class BlackjackController : ControllerBase
{
    private readonly IBlackjackService _blackjackService;

    public BlackjackController(IBlackjackService blackjackService) => _blackjackService = blackjackService;

    [HttpPost("deal")]
    public async Task<ActionResult<BlackjackRoundResponse>> Deal(DealRequest request, CancellationToken ct)
    {
        try
        {
            return await _blackjackService.DealAsync(GetUserId(), request, ct);
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

    [HttpPost("hit")]
    public async Task<ActionResult<BlackjackRoundResponse>> Hit(HitRequest request, CancellationToken ct)
    {
        try
        {
            return await _blackjackService.HitAsync(GetUserId(), request, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("stand")]
    public async Task<ActionResult<BlackjackRoundResponse>> Stand(StandRequest request, CancellationToken ct)
    {
        try
        {
            return await _blackjackService.StandAsync(GetUserId(), request, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Missing user id claim."));
}
