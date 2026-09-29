using CasinoPlatform.Api.DTOs;

namespace CasinoPlatform.Api.Services;

public interface IRouletteService
{
    Task<RouletteSpinResponse> SpinAsync(Guid userId, RouletteSpinRequest request, CancellationToken ct = default);
}
