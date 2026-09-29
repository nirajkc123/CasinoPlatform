using System.ComponentModel.DataAnnotations;

namespace CasinoPlatform.Api.DTOs;

public record DealRequest(
    [Required, Range(1, 10_000)] decimal BetAmount,
    [Required] Guid RequestId
);

public record HitRequest([Required] Guid RoundId);

public record StandRequest([Required] Guid RoundId);

public record BlackjackRoundResponse(
    Guid RoundId,
    string[] PlayerCards,
    int PlayerTotal,
    string[] DealerCards,
    int? DealerTotal, // null while dealer's hole card is still hidden (round in progress)
    string Status,    // "InProgress" | "PlayerBust" | "DealerBust" | "PlayerBlackjack" | "PlayerWin" | "DealerWin" | "Push"
    decimal BetAmount,
    decimal PayoutAmount,
    decimal NewBalance
);
