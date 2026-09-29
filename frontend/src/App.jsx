import { Routes, Route, Navigate } from "react-router-dom";
import NavBar from "./components/NavBar";
import ProtectedRoute from "./components/ProtectedRoute";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Lobby from "./pages/Lobby";
import SlotMachine from "./pages/SlotMachine";
import Blackjack from "./pages/Blackjack";
import Roulette from "./pages/Roulette";
import Statistics from "./pages/Statistics";
import Wallet from "./pages/Wallet";
import Profile from "./pages/Profile";

export default function App() {
    return (
        <>
            <NavBar />
            <Routes>
                <Route path="/login" element={<Login />} />
                <Route path="/register" element={<Register />} />

                <Route
                    path="/"
                    element={
                        <ProtectedRoute>
                            <Lobby />
                        </ProtectedRoute>
                    }
                />
                <Route
                    path="/slots"
                    element={
                        <ProtectedRoute>
                            <SlotMachine />
                        </ProtectedRoute>
                    }
                />
                <Route
                    path="/blackjack"
                    element={
                        <ProtectedRoute>
                            <Blackjack />
                        </ProtectedRoute>
                    }
                />
                <Route
                    path="/roulette"
                    element={
                        <ProtectedRoute>
                            <Roulette />
                        </ProtectedRoute>
                    }
                />
                <Route
                    path="/statistics"
                    element={
                        <ProtectedRoute>
                            <Statistics />
                        </ProtectedRoute>
                    }
                />
                <Route
                    path="/wallet"
                    element={
                        <ProtectedRoute>
                            <Wallet />
                        </ProtectedRoute>
                    }
                />
                <Route
                    path="/profile"
                    element={
                        <ProtectedRoute>
                            <Profile />
                        </ProtectedRoute>
                    }
                />

                <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
        </>
    );
}
