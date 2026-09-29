using System.Security.Claims;
using CasinoPlatform.Api.DTOs;
using CasinoPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasinoPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController(IWalletService walletService) : ControllerBase
{
    private readonly IWalletService _walletService = walletService;

    [HttpGet("balance")]
    public async Task<ActionResult<WalletBalanceResponse>> GetBalance(CancellationToken ct)
    {
        var wallet = await _walletService.GetWalletAsync(GetUserId(), ct);
        return new WalletBalanceResponse(wallet.Id, wallet.Balance, wallet.UpdatedAtUtc);
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<WalletBalanceResponse>> Deposit(DepositRequest request, CancellationToken ct)
    {
        var (wallet, _) = await _walletService.DepositAsync(
            GetUserId(), request.Amount, "Virtual coin deposit", ct);
        return new WalletBalanceResponse(wallet.Id, wallet.Balance, wallet.UpdatedAtUtc);
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<WalletBalanceResponse>> Withdraw(WithdrawRequest request, CancellationToken ct)
    {
        try
        {
            var (wallet, _) = await _walletService.WithdrawAsync(
                GetUserId(), request.Amount, "Virtual coin withdrawal", ct);
            return new WalletBalanceResponse(wallet.Id, wallet.Balance, wallet.UpdatedAtUtc);
        }
        catch (InsufficientFundsException)
        {
            return BadRequest(new { message = "Insufficient balance." });
        }
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResult<TransactionResponse>>> GetHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var userId = GetUserId();
        var items = await _walletService.GetHistoryAsync(userId, page, pageSize, ct);
        var total = await _walletService.GetHistoryCountAsync(userId, ct);

        var mapped = items.Select(t => new TransactionResponse(
            t.Id, t.Type.ToString(), t.Amount, t.BalanceAfter, t.Description, t.CreatedAtUtc));

        return new PagedResult<TransactionResponse>(mapped, page, pageSize, total);
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Missing user id claim."));
}
