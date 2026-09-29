using System.ComponentModel.DataAnnotations;

namespace CasinoPlatform.Api.DTOs;

public record SpinRequest(
    [Required, Range(1, 10_000)] decimal BetAmount,

    /// <summary>
    /// Generated client-side (a fresh Guid per button press) and sent with
    /// the request. If the client retries the same spin (double-click,
    /// network retry, replay attack), the server recognizes the RequestId
    /// and returns the ORIGINAL result instead of spinning again.
    /// </summary>
    [Required] Guid RequestId
);

public record SpinResponse(
    Guid GameRoundId,
    string[][] Reels,
    bool IsWin,
    decimal BetAmount,
    decimal PayoutAmount,
    decimal NewBalance,
    string? WinDescription
);

public record GameHistoryItem(
    Guid Id,
    string GameType,
    decimal BetAmount,
    decimal PayoutAmount,
    bool IsWin,
    DateTime CreatedAtUtc
);
