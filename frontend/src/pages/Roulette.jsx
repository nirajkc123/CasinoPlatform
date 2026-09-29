import { useState, useEffect } from "react";
import { useAuth } from "../context/AuthContext";
import { api, ApiError } from "../api/client";
import "../styles/slot.css";
import "../styles/roulette.css";

const MIN_BET = 1;
const MAX_BET = 1000;
const BET_STEP = 10;

const BET_TYPES = [
    { key: "Red", label: "Red" },
    { key: "Black", label: "Black" },
    { key: "Odd", label: "Odd" },
    { key: "Even", label: "Even" },
    { key: "Low", label: "1–18" },
    { key: "High", label: "19–36" },
    { key: "Dozen1", label: "1st 12" },
    { key: "Dozen2", label: "2nd 12" },
    { key: "Dozen3", label: "3rd 12" },
    { key: "Column1", label: "Col 1" },
    { key: "Column2", label: "Col 2" },
    { key: "Column3", label: "Col 3" },
    { key: "Straight", label: "Straight up" }
];

export default function Roulette() {
    const { auth, setBalance } = useAuth();
    const [bet, setBet] = useState(50);
    const [betType, setBetType] = useState("Red");
    const [straightNumber, setStraightNumber] = useState(17);
    const [spinning, setSpinning] = useState(false);
    const [result, setResult] = useState(null);
    const [error, setError] = useState(null);

    const adjustBet = (delta) => setBet((b) => Math.min(MAX_BET, Math.max(MIN_BET, b + delta)));

    const handleSpin = async () => {
        setError(null);
        setSpinning(true);
        try {
            const requestId = crypto.randomUUID();
            const betValue = betType === "Straight" ? String(straightNumber) : null;
            const res = await api.spinRoulette(auth.token, bet, betType, betValue, requestId);
            setResult(res);
            setBalance(res.newBalance);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "The spin didn't go through. Try again.");
        } finally {
            setSpinning(false);
        }
    };

    return (
        <div className="container">
            <div className="slot-layout">
                <div className="plaque machine">
                    <h2>Roulette</h2>
                    <p>European wheel — single zero. Pick a bet, then spin.</p>

                    {error && <div className="error-banner">{error}</div>}

                    <div className="wheel-result">
                        <div className={`number-badge ${result?.winningColor ?? ""} ${result?.isWin ? "win-glow" : ""}`}>
                            {result?.winningNumber ?? "–"}
                        </div>
                        <div>
                            {result ? (
                                <div className={`result-line ${result.isWin ? "win" : "loss"}`}>
                                    {result.isWin
                                        ? `Winner! +${result.payoutAmount.toLocaleString()} coins.`
                                        : "No match this time."}
                                </div>
                            ) : (
                                <p style={{ margin: 0 }}>Place a bet and spin the wheel.</p>
                            )}
                        </div>
                    </div>

                    <div className="bet-type-grid">
                        {BET_TYPES.map((t) => (
                            <button
                                key={t.key}
                                type="button"
                                className={`bet-type-btn ${betType === t.key ? "selected" : ""}`}
                                onClick={() => setBetType(t.key)}
                                disabled={spinning}
                            >
                                {t.label}
                            </button>
                        ))}
                    </div>

                    {betType === "Straight" && (
                        <div className="straight-input">
                            <label htmlFor="straightNum">Number (0–36)</label>
                            <input
                                id="straightNum"
                                type="number"
                                min={0}
                                max={36}
                                value={straightNumber}
                                onChange={(e) => setStraightNumber(Number(e.target.value))}
                                disabled={spinning}
                            />
                        </div>
                    )}

                    <div className="spin-controls">
                        <div className="bet-field">
                            <label htmlFor="rbet">Bet amount</label>
                            <div className="bet-stepper">
                                <button type="button" onClick={() => adjustBet(-BET_STEP)} disabled={spinning}>
                                    −
                                </button>
                                <input
                                    id="rbet"
                                    type="number"
                                    value={bet}
                                    onChange={(e) => setBet(Number(e.target.value) || MIN_BET)}
                                    disabled={spinning}
                                />
                                <button type="button" onClick={() => adjustBet(BET_STEP)} disabled={spinning}>
                                    +
                                </button>
                            </div>
                        </div>

                        <button
                            className="btn btn-gold"
                            onClick={handleSpin}
                            disabled={spinning || bet > (auth.walletBalance ?? 0)}
                        >
                            {spinning ? "Spinning…" : "Spin"}
                        </button>
                    </div>
                </div>

                <div className="plaque">
                    <h3>Payouts</h3>
                    <table className="ledger">
                        <tbody>
                            <tr><td>Straight up</td><td>35:1</td></tr>
                            <tr><td>Red / Black / Odd / Even / 1–18 / 19–36</td><td>1:1</td></tr>
                            <tr><td>Dozen / Column</td><td>2:1</td></tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}
