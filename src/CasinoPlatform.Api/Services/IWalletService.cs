using CasinoPlatform.Api.Models;

namespace CasinoPlatform.Api.Services;

public class InsufficientFundsException : Exception
{
    public InsufficientFundsException() : base("Wallet balance is insufficient for this operation.") { }
}

public interface IWalletService
{
    Task<Wallet> CreateWalletWithInitialGrantAsync(Guid userId, CancellationToken ct = default);
    Task<Wallet> GetWalletAsync(Guid userId, CancellationToken ct = default);

    Task<(Wallet wallet, Transaction transaction)> DepositAsync(
        Guid userId, decimal amount, string description, CancellationToken ct = default);

    Task<(Wallet wallet, Transaction transaction)> WithdrawAsync(
        Guid userId, decimal amount, string description, CancellationToken ct = default);

    Task<IReadOnlyList<Transaction>> GetHistoryAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);

    Task<int> GetHistoryCountAsync(Guid userId, CancellationToken ct = default);
}
