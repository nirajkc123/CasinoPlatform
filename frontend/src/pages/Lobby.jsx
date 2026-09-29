import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "../styles/lobby.css";

const GAMES = [
    {
        key: "slots",
        glyph: "🎰",
        name: "Slot Machine",
        description: "Three reels, one payline. Land three of a kind for a payout multiplier.",
        to: "/slots",
        available: true
    },
    {
        key: "blackjack",
        glyph: "🂡",
        name: "Blackjack",
        description: "Beat the dealer to 21 without going over. Blackjack pays 3:2.",
        to: "/blackjack",
        available: true
    },
    {
        key: "roulette",
        glyph: "🎲",
        name: "Roulette",
        description: "Red, black, or a lucky number — place your bet and watch it spin.",
        to: "/roulette",
        available: true
    }
];

export default function Lobby() {
    const { auth } = useAuth();

    return (
        <div className="container">
            <div className="lobby-header">
                <div className="eyebrow">Good to see you, {auth?.displayName}</div>
                <h1>The floor is open</h1>
                <p>
                    Every coin here is virtual — this room exists to learn and to play, never to win or lose
                    real money.
                </p>
            </div>

            <div className="game-grid">
                {GAMES.map((game) => (
                    <div key={game.key} className={`plaque game-card ${game.available ? "" : "disabled"}`}>
                        {!game.available && <span className="tag tag-neutral status">Coming soon</span>}
                        <span className="glyph">{game.glyph}</span>
                        <h3>{game.name}</h3>
                        <p>{game.description}</p>
                        {game.available ? (
                            <Link className="btn btn-gold" to={game.to}>
                                Play now
                            </Link>
                        ) : (
                            <button className="btn btn-ghost" disabled>
                                Play now
                            </button>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
}
