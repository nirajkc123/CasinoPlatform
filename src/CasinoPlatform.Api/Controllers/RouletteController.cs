using System.Security.Claims;
using CasinoPlatform.Api.DTOs;
using CasinoPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasinoPlatform.Api.Controllers;

[ApiController]
[Route("api/games/roulette")]
[Authorize]
public class RouletteController : ControllerBase
{
    private readonly IRouletteService _rouletteService;

    public RouletteController(IRouletteService rouletteService) => _rouletteService = rouletteService;

    [HttpPost("spin")]
    public async Task<ActionResult<RouletteSpinResponse>> Spin(RouletteSpinRequest request, CancellationToken ct)
    {
        try
        {
            return await _rouletteService.SpinAsync(GetUserId(), request, ct);
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

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Missing user id claim."));
}
