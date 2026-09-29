import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { ApiError } from "../api/client";
import "../styles/auth.css";

export default function Register() {
  const { register, loading } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ email: "", displayName: "", password: "" });
  const [error, setError] = useState(null);

  const update = (field) => (e) => setForm((f) => ({ ...f, [field]: e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    try {
      await register(form.email, form.displayName, form.password);
      navigate("/");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not create your account.");
    }
  };

  return (
    <div className="auth-screen">
      <div className="plaque auth-card">
        <h1>Join the room</h1>
        <p className="subtitle">
          Create an account and we'll stake you 10,000 virtual coins to start. No real money, ever.
        </p>

        {error && <div className="error-banner">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="field">
            <label htmlFor="displayName">Display name</label>
            <input
              id="displayName"
              required
              minLength={3}
              maxLength={30}
              value={form.displayName}
              onChange={update("displayName")}
              placeholder="Lucky Player"
            />
          </div>

          <div className="field">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              required
              value={form.email}
              onChange={update("email")}
              placeholder="you@example.com"
            />
          </div>

          <div className="field">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              required
              minLength={8}
              value={form.password}
              onChange={update("password")}
              placeholder="At least 8 characters"
            />
          </div>

          <button className="btn btn-gold" type="submit" disabled={loading}>
            {loading ? "Creating account…" : "Create account"}
          </button>
        </form>

        <div className="auth-switch">
          Already have an account? <Link to="/login">Log in</Link>
        </div>
      </div>
    </div>
  );
}
