import { NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "../styles/navbar.css";

export default function NavBar() {
    const { auth, logout } = useAuth();
    const navigate = useNavigate();

    if (!auth?.token) return null;

    const handleLogout = () => {
        logout();
        navigate("/login");
    };

    return (
        <header className="navbar">
            <div className="container navbar-row">
                <NavLink to="/" className="navbar-brand">
                    The Coin Room
                </NavLink>

                <nav className="navbar-links">
                    <NavLink to="/" end className={({ isActive }) => (isActive ? "active" : "")}>
                        Lobby
                    </NavLink>
                    <NavLink to="/slots" className={({ isActive }) => (isActive ? "active" : "")}>
                        Slots
                    </NavLink>
                    <NavLink to="/blackjack" className={({ isActive }) => (isActive ? "active" : "")}>
                        Blackjack
                    </NavLink>
                    <NavLink to="/roulette" className={({ isActive }) => (isActive ? "active" : "")}>
                        Roulette
                    </NavLink>
                    <NavLink to="/statistics" className={({ isActive }) => (isActive ? "active" : "")}>
                        Stats
                    </NavLink>
                    <NavLink to="/wallet" className={({ isActive }) => (isActive ? "active" : "")}>
                        Wallet
                    </NavLink>
                    <NavLink to="/profile" className={({ isActive }) => (isActive ? "active" : "")}>
                        Profile
                    </NavLink>
                </nav>

                <div className="navbar-right">
                    <span className="balance-chip">
                        <span className="amount">{Math.floor(auth.walletBalance ?? 0).toLocaleString()}</span>
                        <span className="unit">coins</span>
                    </span>
                    <button className="btn btn-ghost" onClick={handleLogout}>
                        Log out
                    </button>
                </div>
            </div>
        </header>
    );
}
