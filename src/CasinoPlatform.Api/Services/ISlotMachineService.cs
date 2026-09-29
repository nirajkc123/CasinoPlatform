using CasinoPlatform.Api.DTOs;

namespace CasinoPlatform.Api.Services;

public class DuplicateSpinException : Exception
{
    public DuplicateSpinException() : base("This spin request was already processed.") { }
}

public interface ISlotMachineService
{
    Task<SpinResponse> SpinAsync(Guid userId, SpinRequest request, CancellationToken ct = default);
}
