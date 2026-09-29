using System.ComponentModel.DataAnnotations;

namespace CasinoPlatform.Api.DTOs;

public record WalletBalanceResponse(Guid WalletId, decimal Balance, DateTime UpdatedAtUtc);

public record DepositRequest([Required, Range(0.01, 1_000_000)] decimal Amount);

public record WithdrawRequest([Required, Range(0.01, 1_000_000)] decimal Amount);

public record TransactionResponse(
    Guid Id,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Description,
    DateTime CreatedAtUtc
);

public record PagedResult<T>(IEnumerable<T> Items, int Page, int PageSize, int TotalCount);
