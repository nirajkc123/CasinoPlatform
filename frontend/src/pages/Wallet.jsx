import { useEffect, useState, useCallback } from "react";
import { useAuth } from "../context/AuthContext";
import { api, ApiError } from "../api/client";
import "../styles/wallet.css";

const PAGE_SIZE = 12;

function formatType(type) {
  return type.replace(/([a-z])([A-Z])/g, "$1 $2");
}

export default function Wallet() {
  const { auth, setBalance } = useAuth();
  const [amount, setAmount] = useState(100);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(null);
  const [transactions, setTransactions] = useState([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loadingHistory, setLoadingHistory] = useState(true);

  const loadTransactions = useCallback(
    async (targetPage) => {
      setLoadingHistory(true);
      try {
        const res = await api.getTransactions(auth.token, targetPage, PAGE_SIZE);
        setTransactions(res.items);
        setTotalCount(res.totalCount);
      } catch {
        // non-fatal
      } finally {
        setLoadingHistory(false);
      }
    },
    [auth.token]
  );

  useEffect(() => {
    loadTransactions(page);
  }, [loadTransactions, page]);

  const handleDeposit = async () => {
    setError(null);
    setBusy(true);
    try {
      const res = await api.deposit(auth.token, amount);
      setBalance(res.balance);
      loadTransactions(1);
      setPage(1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Deposit failed.");
    } finally {
      setBusy(false);
    }
  };

  const handleWithdraw = async () => {
    setError(null);
    setBusy(true);
    try {
      const res = await api.withdraw(auth.token, amount);
      setBalance(res.balance);
      loadTransactions(1);
      setPage(1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Withdrawal failed.");
    } finally {
      setBusy(false);
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="container">
      <div className="wallet-layout">
        <div className="plaque balance-plaque">
          <h2>Your wallet</h2>
          <div className="amount-large">{Math.floor(auth.walletBalance ?? 0).toLocaleString()}</div>

          {error && <div className="error-banner">{error}</div>}

          <div className="field">
            <label htmlFor="amount">Amount</label>
            <input
              id="amount"
              type="number"
              min={1}
              value={amount}
              onChange={(e) => setAmount(Number(e.target.value) || 0)}
              disabled={busy}
            />
          </div>

          <div className="wallet-action-row">
            <button className="btn btn-gold" onClick={handleDeposit} disabled={busy || amount <= 0}>
              Deposit
            </button>
            <button
              className="btn btn-ghost"
              onClick={handleWithdraw}
              disabled={busy || amount <= 0 || amount > (auth.walletBalance ?? 0)}
            >
              Withdraw
            </button>
          </div>
          <p style={{ marginTop: "var(--space-4)", fontSize: "0.82rem" }}>
            Deposits and withdrawals here move virtual coins only — there's no connection to a real
            payment method.
          </p>
        </div>

        <div className="plaque">
          <h3>Transaction history</h3>
          {loadingHistory && <p className="history-empty">Loading…</p>}
          {!loadingHistory && transactions.length === 0 && (
            <p className="history-empty">No transactions yet.</p>
          )}
          {!loadingHistory && transactions.length > 0 && (
            <>
              <table className="ledger">
                <thead>
                  <tr>
                    <th>Type</th>
                    <th>Amount</th>
                    <th>Balance after</th>
                    <th>When</th>
                  </tr>
                </thead>
                <tbody>
                  {transactions.map((t) => (
                    <tr key={t.id}>
                      <td>{formatType(t.type)}</td>
                      <td>
                        <span className={`tag ${t.amount >= 0 ? "tag-win" : "tag-loss"}`}>
                          {t.amount >= 0 ? "+" : ""}
                          {t.amount.toLocaleString()}
                        </span>
                      </td>
                      <td>{t.balanceAfter.toLocaleString()}</td>
                      <td>{new Date(t.createdAtUtc).toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>

              <div className="pagination">
                <span>
                  Page {page} of {totalPages}
                </span>
                <div style={{ display: "flex", gap: "8px" }}>
                  <button
                    className="btn btn-ghost"
                    disabled={page <= 1}
                    onClick={() => setPage((p) => p - 1)}
                  >
                    Previous
                  </button>
                  <button
                    className="btn btn-ghost"
                    disabled={page >= totalPages}
                    onClick={() => setPage((p) => p + 1)}
                  >
                    Next
                  </button>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
