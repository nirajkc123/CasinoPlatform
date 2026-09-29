using System.Security.Cryptography;

namespace CasinoPlatform.Api.Services.Cards;

/// <summary>A single playing card, e.g. "AS" (Ace of Spades), "10H" (Ten of Hearts).</summary>
public record Card(string Rank, char Suit)
{
    public override string ToString() => $"{Rank}{Suit}";

    public static Card Parse(string code)
    {
        var suit = code[^1];
        var rank = code[..^1];
        return new Card(rank, suit);
    }
}

/// <summary>
/// A single standard 52-card deck. Serialized to/from a list of card codes so a
/// Blackjack round can persist "what's left in the shoe" between Hit requests.
/// </summary>
public class Deck
{
    private static readonly string[] Ranks =
        { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
    private static readonly char[] Suits = { 'S', 'H', 'D', 'C' };

    private readonly List<Card> _cards;

    private Deck(List<Card> cards) => _cards = cards;

    /// <summary>A freshly shuffled 52-card deck, using a cryptographically secure RNG.</summary>
    public static Deck NewShuffled()
    {
        var cards = new List<Card>(52);
        foreach (var suit in Suits)
            foreach (var rank in Ranks)
                cards.Add(new Card(rank, suit));

        // Fisher-Yates shuffle
        for (var i = cards.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(0, i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }

        return new Deck(cards);
    }

    public static Deck FromCodes(IEnumerable<string> codes) => new(codes.Select(Card.Parse).ToList());

    public List<string> ToCodes() => _cards.Select(c => c.ToString()).ToList();

    public Card Draw()
    {
        if (_cards.Count == 0) throw new InvalidOperationException("Deck is empty.");
        var card = _cards[0];
        _cards.RemoveAt(0);
        return card;
    }
}
