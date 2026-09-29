namespace CasinoPlatform.Api.Options;

public class GameOptions
{
    public const string SectionName = "Game";

    public decimal InitialGrantAmount { get; set; } = 10_000m;
    public decimal MinBet { get; set; } = 1m;
    public decimal MaxBet { get; set; } = 1000m;
}
