namespace CasinoPlatform.Api.DTOs;

public record GameStatisticsResponse(
    int TotalRoundsPlayed,
    decimal TotalWagered,
    decimal TotalPayout,
    decimal NetProfit, // TotalPayout - TotalWagered, negative means net loss
    int RoundsWon,
    decimal WinRatePercent,
    decimal BiggestWin,
    IEnumerable<GameTypeBreakdown> ByGameType
);

public record GameTypeBreakdown(
    string GameType,
    int RoundsPlayed,
    decimal TotalWagered,
    decimal TotalPayout,
    int RoundsWon
);

public record UnifiedHistoryItem(
    Guid Id,
    string GameType,
    decimal BetAmount,
    decimal PayoutAmount,
    bool IsWin,
    DateTime CreatedAtUtc
);

public record GameConfigResponse(
    decimal MinBet,
    decimal MaxBet,
    decimal InitialGrantAmount,
    SlotConfig Slot,
    RouletteConfig Roulette,
    BlackjackConfig Blackjack
);

public record SlotConfig(IDictionary<string, decimal> PayoutMultipliers);

public record RouletteConfig(IDictionary<string, decimal> PayoutMultipliers, string Variant);

public record BlackjackConfig(decimal BlackjackPayout, string DealerRule);
