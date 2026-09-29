using System.ComponentModel.DataAnnotations;

namespace CasinoPlatform.Api.DTOs;

/// <summary>
/// BetType is one of: Straight, Red, Black, Odd, Even, Low, High,
/// Dozen1, Dozen2, Dozen3, Column1, Column2, Column3.
/// BetValue is only required for "Straight" (a number 0-36 as a string).
/// </summary>
public record RouletteSpinRequest(
    [Required, Range(1, 10_000)] decimal BetAmount,
    [Required] string BetType,
    string? BetValue,
    [Required] Guid RequestId
);

public record RouletteSpinResponse(
    Guid GameRoundId,
    int WinningNumber,
    string WinningColor, // "Red" | "Black" | "Green"
    bool IsWin,
    decimal BetAmount,
    decimal PayoutAmount,
    decimal NewBalance
);
