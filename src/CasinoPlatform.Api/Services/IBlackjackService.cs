using CasinoPlatform.Api.DTOs;

namespace CasinoPlatform.Api.Services;

public interface IBlackjackService
{
    Task<BlackjackRoundResponse> DealAsync(Guid userId, DealRequest request, CancellationToken ct = default);
    Task<BlackjackRoundResponse> HitAsync(Guid userId, HitRequest request, CancellationToken ct = default);
    Task<BlackjackRoundResponse> StandAsync(Guid userId, StandRequest request, CancellationToken ct = default);
}
