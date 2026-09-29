import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { api } from "../api/client";

export default function Statistics() {
    const { auth } = useAuth();
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        api
            .getStatistics(auth.token)
            .then(setStats)
            .finally(() => setLoading(false));
    }, [auth.token]);

    if (loading) return <div className="container" style={{ padding: "var(--space-6) 0" }}>Loading…</div>;
    if (!stats) return null;

    const netPositive = stats.netProfit >= 0;

    return (
        <div className="container">
            <div style={{ padding: "var(--space-6) 0 var(--space-8)" }}>
                <h2>Your statistics</h2>

                <div
                    style={{
                        display: "grid",
                        gridTemplateColumns: "repeat(auto-fit, minmax(160px, 1fr))",
                        gap: "var(--space-4)",
                        marginBottom: "var(--space-5)"
                    }}
                >
                    <StatCard label="Rounds played" value={stats.totalRoundsPlayed.toLocaleString()} />
                    <StatCard label="Total wagered" value={stats.totalWagered.toLocaleString()} />
                    <StatCard label="Total payout" value={stats.totalPayout.toLocaleString()} />
                    <StatCard
                        label="Net"
                        value={`${netPositive ? "+" : ""}${stats.netProfit.toLocaleString()}`}
                        accent={netPositive ? "var(--gold-300)" : "var(--ruby-400)"}
                    />
                    <StatCard label="Win rate" value={`${stats.winRatePercent}%`} />
                    <StatCard label="Biggest win" value={stats.biggestWin.toLocaleString()} />
                </div>

                <div className="plaque">
                    <h3>By game</h3>
                    {stats.byGameType.length === 0 ? (
                        <p className="history-empty">No rounds played yet.</p>
                    ) : (
                        <table className="ledger">
                            <thead>
                                <tr>
                                    <th>Game</th>
                                    <th>Rounds</th>
                                    <th>Wagered</th>
                                    <th>Payout</th>
                                    <th>Wins</th>
                                </tr>
                            </thead>
                            <tbody>
                                {stats.byGameType.map((row) => (
                                    <tr key={row.gameType}>
                                        <td>{row.gameType}</td>
                                        <td>{row.roundsPlayed}</td>
                                        <td>{row.totalWagered.toLocaleString()}</td>
                                        <td>{row.totalPayout.toLocaleString()}</td>
                                        <td>{row.roundsWon}</td>
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

function StatCard({ label, value, accent }) {
    return (
        <div className="plaque" style={{ padding: "var(--space-4)" }}>
            <div style={{ fontSize: "0.8rem", color: "var(--cream-500)", marginBottom: "6px" }}>{label}</div>
            <div style={{ fontFamily: "var(--font-display)", fontSize: "1.5rem", fontWeight: 700, color: accent || "var(--cream-100)" }}>
                {value}
            </div>
        </div>
    );
}
