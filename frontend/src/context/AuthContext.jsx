import { createContext, useContext, useEffect, useState, useCallback } from "react";
import { api } from "../api/client";

const AuthContext = createContext(null);
const STORAGE_KEY = "casino.auth";

function loadStored() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }) {
  const [auth, setAuth] = useState(loadStored);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (auth) localStorage.setItem(STORAGE_KEY, JSON.stringify(auth));
    else localStorage.removeItem(STORAGE_KEY);
  }, [auth]);

  const applyAuthResponse = (res) => {
    setAuth({
      token: res.token,
      userId: res.userId,
      email: res.email,
      displayName: res.displayName,
      walletBalance: res.walletBalance
    });
  };

  const register = useCallback(async (email, displayName, password) => {
    setLoading(true);
    try {
      const res = await api.register({ email, displayName, password });
      applyAuthResponse(res);
      return res;
    } finally {
      setLoading(false);
    }
  }, []);

  const login = useCallback(async (email, password) => {
    setLoading(true);
    try {
      const res = await api.login({ email, password });
      applyAuthResponse(res);
      return res;
    } finally {
      setLoading(false);
    }
  }, []);

  const logout = useCallback(() => setAuth(null), []);

  const setBalance = useCallback((walletBalance) => {
    setAuth((prev) => (prev ? { ...prev, walletBalance } : prev));
  }, []);

  const refreshBalance = useCallback(async () => {
    if (!auth?.token) return;
    const res = await api.getBalance(auth.token);
    setBalance(res.balance);
  }, [auth?.token, setBalance]);

  return (
    <AuthContext.Provider
      value={{ auth, loading, register, login, logout, setBalance, refreshBalance }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
