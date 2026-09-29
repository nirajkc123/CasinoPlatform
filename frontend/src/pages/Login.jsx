import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { ApiError } from "../api/client";
import "../styles/auth.css";

export default function Login() {
  const { login, loading } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ email: "", password: "" });
  const [error, setError] = useState(null);

  const update = (field) => (e) => setForm((f) => ({ ...f, [field]: e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    try {
      await login(form.email, form.password);
      navigate("/");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not log you in.");
    }
  };

  return (
    <div className="auth-screen">
      <div className="plaque auth-card">
        <h1>Welcome back</h1>
        <p className="subtitle">Log in to pick up right where you left off.</p>

        {error && <div className="error-banner">{error}</div>}

        <form onSubmit={handleSubmit}>
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
              value={form.password}
              onChange={update("password")}
              placeholder="Your password"
            />
          </div>

          <button className="btn btn-gold" type="submit" disabled={loading}>
            {loading ? "Logging in…" : "Log in"}
          </button>
        </form>

        <div className="auth-switch">
          New here? <Link to="/register">Create an account</Link>
        </div>
      </div>
    </div>
  );
}
