const SUITS = {
    S: { symbol: "♠", red: false },
    H: { symbol: "♥", red: true },
    D: { symbol: "♦", red: true },
    C: { symbol: "♣", red: false }
};

export default function PlayingCard({ code }) {
    if (code === "??") {
        return <div className="playing-card face-down" aria-label="Hidden card" />;
    }

    const suitChar = code[code.length - 1];
    const rank = code.slice(0, -1);
    const suit = SUITS[suitChar] ?? { symbol: "?", red: false };

    return (
        <div className={`playing-card ${suit.red ? "red" : ""}`}>
            <span>{rank}</span>
            <span className="suit">{suit.symbol}</span>
        </div>
    );
}
