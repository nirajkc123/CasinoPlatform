import { useEffect, useState, useCallback } from "react";
import { useAuth } from "../context/AuthContext";
import { api, ApiError } from "../api/client";
import "../styles/slot.css";

const MIN_BET = 1;
const MAX_BET = 1000;
const BET_STEP = 10;

export default function SlotMachine() {
  const { auth, setBalance } = useAuth();
  const [bet, setBet] = useState(50);
  const [reels, setReels] = useState([
    ["🍒", "🍒", "🍒"],
    ["🍋", "🍋", "🍋"],
    ["🔔", "🔔", "🔔"]
  ]);
  const [spinning, setSpinning] = useState(false);
  const [result, setResult] = useState(null); // { isWin, payoutAmount, winDescription }
  const [error, setError] = useState(null);
  const [history, setHistory] = useState([]);
  const [loadingHistory, setLoadingHistory] = useState(true);

  const loadHistory = useCallback(async () => {
    setLoadingHistory(true);
    try {
      const res = await api.getSpinHistory(auth.token, 1, 8);
      setHistory(res.items);
    } catch {
      // history is supplementary - fail quietly
    } finally {
      setLoadingHistory(false);
    }
  }, [auth.token]);

  useEffect(() => {
    loadHistory();
  }, [loadHistory]);

  const adjustBet = (delta) => {
    setBet((b) => Math.min(MAX_BET, Math.max(MIN_BET, b + delta)));
  };

  const handleSpin = async () => {
    setError(null);
    setResult(null);
    setSpinning(true);
    try {
      const requestId = crypto.randomUUID();
      const res = await api.spin(auth.token, bet, requestId);
      setReels(res.reels);
      setBalance(res.newBalance);
      setResult({
        isWin: res.isWin,
        payoutAmount: res.payoutAmount,
        winDescription: res.winDescription
      });
      loadHistory();
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
          <h2>Slot Machine</h2>
          <p>Three of a kind on the middle row wins. Rarer symbols pay more.</p>

          {error && <div className="error-banner">{error}</div>}

          <div className={`reel-window ${result?.isWin ? "win" : ""}`}>
            <div className="reel-grid">
              {reels.map((col, colIdx) =>
                col.map((symbol, rowIdx) => (
                  <div key={`${colIdx}-${rowIdx}`} className={`reel-cell ${rowIdx === 1 ? "payline" : ""}`}>
                    {symbol}
                  </div>
                ))
              )}
            </div>
          </div>

          <div className="spin-controls">
            <div className="bet-field">
              <label htmlFor="bet">Bet amount</label>
              <div className="bet-stepper">
                <button type="button" onClick={() => adjustBet(-BET_STEP)} disabled={spinning || bet <= MIN_BET}>
                  −
                </button>
                <input
                  id="bet"
                  type="number"
                  min={MIN_BET}
                  max={MAX_BET}
                  value={bet}
                  onChange={(e) => setBet(Number(e.target.value) || MIN_BET)}
                  disabled={spinning}
                />
                <button type="button" onClick={() => adjustBet(BET_STEP)} disabled={spinning || bet >= MAX_BET}>
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

          {bet > (auth.walletBalance ?? 0) && (
            <p style={{ marginTop: "var(--space-3)", color: "var(--ruby-400)" }}>
              Your balance is below this bet. Lower the bet or top up your wallet.
            </p>
          )}

          {result && (
            <div className={`result-line ${result.isWin ? "win" : "loss"}`}>
              {result.isWin
                ? `${result.winDescription} You won ${result.payoutAmount.toLocaleString()} coins.`
                : "No match this time — try again."}
            </div>
          )}
        </div>

        <div className="plaque history-panel">
          <h3>Recent spins</h3>
          {loadingHistory && <p className="history-empty">Loading…</p>}
          {!loadingHistory && history.length === 0 && (
            <p className="history-empty">No spins yet — give the lever a pull.</p>
          )}
          {!loadingHistory && history.length > 0 && (
            <table className="ledger">
              <thead>
                <tr>
                  <th>Bet</th>
                  <th>Result</th>
                </tr>
              </thead>
              <tbody>
                {history.map((h) => (
                  <tr key={h.id}>
                    <td>{h.betAmount.toLocaleString()}</td>
                    <td>
                      {h.isWin ? (
                        <span className="tag tag-win">+{h.payoutAmount.toLocaleString()}</span>
                      ) : (
                        <span className="tag tag-loss">−{h.betAmount.toLocaleString()}</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}
