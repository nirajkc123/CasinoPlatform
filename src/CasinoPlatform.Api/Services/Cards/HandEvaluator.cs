namespace CasinoPlatform.Api.Services.Cards;

public static class HandEvaluator
{
    /// <summary>
    /// Best Blackjack total for a hand, treating Aces as 11 unless that would
    /// bust the hand, in which case they count as 1 (the standard "soft/hard" rule).
    /// </summary>
    public static int Value(IEnumerable<Card> hand)
    {
        var cards = hand.ToList();
        var total = 0;
        var aceCount = 0;

        foreach (var card in cards)
        {
            total += card.Rank switch
            {
                "A" => 11,
                "K" or "Q" or "J" or "10" => 10,
                _ => int.Parse(card.Rank)
            };
            if (card.Rank == "A") aceCount++;
        }

        while (total > 21 && aceCount > 0)
        {
            total -= 10; // demote one Ace from 11 to 1
            aceCount--;
        }

        return total;
    }

    public static bool IsBust(IEnumerable<Card> hand) => Value(hand) > 21;

    public static bool IsBlackjack(IEnumerable<Card> hand)
    {
        var cards = hand.ToList();
        return cards.Count == 2 && Value(cards) == 21;
    }
}
