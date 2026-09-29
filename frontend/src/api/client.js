const API_URL = import.meta.env.VITE_API_URL || "https://localhost:7080/api";

class ApiError extends Error {
  constructor(message, status) {
    super(message);
    this.status = status;
  }
}

async function request(path, { method = "GET", body, token, params } = {}) {
  let url = `${API_URL}${path}`;
  if (params) {
    const qs = new URLSearchParams(
      Object.entries(params).filter(([, v]) => v !== undefined && v !== null)
    ).toString();
    if (qs) url += `?${qs}`;
  }

  const headers = { "Content-Type": "application/json" };
  if (token) headers.Authorization = `Bearer ${token}`;

  const res = await fetch(url, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined
  });

  const isJson = res.headers.get("content-type")?.includes("application/json");
  const data = isJson ? await res.json().catch(() => null) : null;

  if (!res.ok) {
    const message =
      data?.message ||
      data?.title ||
      (Array.isArray(data?.errors) ? data.errors.join(" ") : null) ||
      `Request failed (${res.status})`;
    throw new ApiError(message, res.status);
  }

  return data;
}

export const api = {
  register: (payload) => request("/auth/register", { method: "POST", body: payload }),
  login: (payload) => request("/auth/login", { method: "POST", body: payload }),
  me: (token) => request("/auth/me", { token }),

  getBalance: (token) => request("/wallet/balance", { token }),
  deposit: (token, amount) => request("/wallet/deposit", { method: "POST", token, body: { amount } }),
  withdraw: (token, amount) => request("/wallet/withdraw", { method: "POST", token, body: { amount } }),
  getTransactions: (token, page = 1, pageSize = 20) =>
    request("/wallet/transactions", { token, params: { page, pageSize } }),

  spin: (token, betAmount, requestId) =>
    request("/games/slot/spin", { method: "POST", token, body: { betAmount, requestId } }),
  getSpinHistory: (token, page = 1, pageSize = 20) =>
    request("/games/slot/history", { token, params: { page, pageSize } }),

  dealBlackjack: (token, betAmount, requestId) =>
    request("/games/blackjack/deal", { method: "POST", token, body: { betAmount, requestId } }),
  hitBlackjack: (token, roundId) =>
    request("/games/blackjack/hit", { method: "POST", token, body: { roundId } }),
  standBlackjack: (token, roundId) =>
    request("/games/blackjack/stand", { method: "POST", token, body: { roundId } }),

  spinRoulette: (token, betAmount, betType, betValue, requestId) =>
    request("/games/roulette/spin", {
      method: "POST",
      token,
      body: { betAmount, betType, betValue, requestId }
    }),

  getUnifiedHistory: (token, page = 1, pageSize = 20, gameType) =>
    request("/games/history", { token, params: { page, pageSize, gameType } }),
  getStatistics: (token) => request("/games/statistics", { token }),
  getGameConfig: () => request("/games/config")
};

export { ApiError };
