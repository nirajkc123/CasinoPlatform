import { useState, useCallback, useEffect } from "react";
import { useAuth } from "../context/AuthContext";
import { api, ApiError } from "../api/client";
import PlayingCard from "../components/PlayingCard";
import "../styles/table.css";

const MIN_BET = 1;
const MAX_BET = 1000;
const BET_STEP = 10;

const OUTCOME_TEXT = {
    PlayerBlackjack: "Blackjack! You win 3:2.",
    DealerBust: "Dealer busts — you win!",
    PlayerWin: "You win!",
    DealerWin: "Dealer wins this hand.",
    PlayerBust: "You busted.",
    Push: "Push — your bet is returned."
};

export default function Blackjack() {
    const { auth, setBalance } = useAuth();
    const [bet, setBet] = useState(50);
    const [round, setRound] = useState(null); // BlackjackRoundResponse or null
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState(null);

    const inProgress = round?.status === "InProgress";
    const resolved = round && !inProgress;

    const adjustBet = (delta) => setBet((b) => Math.min(MAX_BET, Math.max(MIN_BET, b + delta)));

    const applyRound = useCallback(
        (res) => {
            setRound(res);
            setBalance(res.newBalance);
        },
        [setBalance]
    );

    const handleDeal = async () => {
        setError(null);
        setBusy(true);
        try {
            const requestId = crypto.randomUUID();
            const res = await api.dealBlackjack(auth.token, bet, requestId);
            applyRound(res);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Could not start a new hand.");
        } finally {
            setBusy(false);
        }
    };

    const handleHit = async () => {
        setError(null);
        setBusy(true);
        try {
            const res = await api.hitBlackjack(auth.token, round.roundId);
            applyRound(res);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Hit failed.");
        } finally {
            setBusy(false);
        }
    };

    const handleStand = async () => {
        setError(null);
        setBusy(true);
        try {
            const res = await api.standBlackjack(auth.token, round.roundId);
            applyRound(res);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Stand failed.");
        } finally {
            setBusy(false);
        }
    };

    const dealerCardsToShow = round
        ? inProgress
            ? [round.dealerCards[0], "??"]
            : round.dealerCards
        : [];

    const outcomeClass = round?.payoutAmount > round?.betAmount ? "win"
        : round?.status === "Push" ? "push"
            : "loss";

    return (
        <div className="container">
            <div className="table-layout">
                <div className="plaque felt-table">
                    <h2>Blackjack</h2>
                    <p>Get closer to 21 than the dealer without going over. Blackjack pays 3:2.</p>

                    {error && <div className="error-banner">{error}</div>}

                    <div className="hand-row">
                        <div className="hand-label">
                            <span>Dealer</span>
                            {round && !inProgress && <span className="total">{round.dealerTotal}</span>}
                        </div>
                        <div className="card-row">
                            {dealerCardsToShow.map((c, i) => (
                                <PlayingCard key={i} code={c} />
                            ))}
                        </div>
                    </div>

                    <div className="hand-row">
                        <div className="hand-label">
                            <span>You</span>
                            {round && <span className="total">{round.playerTotal}</span>}
                        </div>
                        <div className="card-row">
                            {(round?.playerCards ?? []).map((c, i) => (
                                <PlayingCard key={i} code={c} />
                            ))}
                        </div>
                    </div>

                    {resolved && (
                        <div className={`outcome-banner ${outcomeClass}`}>
                            {OUTCOME_TEXT[round.status] ?? round.status}
                            {round.payoutAmount > 0 && ` (+${round.payoutAmount.toLocaleString()} coins)`}
                        </div>
                    )}

                    <div className="table-actions">
                        {!round || resolved ? (
                            <>
                                <div className="bet-field">
                                    <label htmlFor="bjbet">Bet amount</label>
                                    <div className="bet-stepper">
                                        <button type="button" onClick={() => adjustBet(-BET_STEP)} disabled={busy}>
                                            −
                                        </button>
                                        <input
                                            id="bjbet"
                                            type="number"
                                            value={bet}
                                            onChange={(e) => setBet(Number(e.target.value) || MIN_BET)}
                                            disabled={busy}
                                        />
                                        <button type="button" onClick={() => adjustBet(BET_STEP)} disabled={busy}>
                                            +
                                        </button>
                                    </div>
                                </div>
                                <button
                                    className="btn btn-gold"
                                    onClick={handleDeal}
                                    disabled={busy || bet > (auth.walletBalance ?? 0)}
                                >
                                    {busy ? "Dealing…" : "Deal"}
                                </button>
                            </>
                        ) : (
                            <>
                                <button className="btn btn-gold" onClick={handleHit} disabled={busy}>
                                    Hit
                                </button>
                                <button className="btn btn-ghost" onClick={handleStand} disabled={busy}>
                                    Stand
                                </button>
                            </>
                        )}
                    </div>
                </div>

                <div className="plaque">
                    <h3>Table rules</h3>
                    <p>Dealer hits until 17 and stands on soft 17.</p>
                    <p>Blackjack (an Ace + a 10-value card on the deal) pays 3:2.</p>
                    <p>A push returns your bet with no win or loss.</p>
                </div>
            </div>
        </div>
    );
}
