import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { api } from "../api/client";

export default function Profile() {
  const { auth } = useAuth();
  const [profile, setProfile] = useState(null);

  useEffect(() => {
    api.me(auth.token).then(setProfile).catch(() => {});
  }, [auth.token]);

  return (
    <div className="container">
      <div style={{ padding: "var(--space-6) 0 var(--space-8)", maxWidth: 480 }}>
        <h2>Profile</h2>
        <div className="plaque">
          <div className="field">
            <label>Display name</label>
            <div>{profile?.displayName ?? auth.displayName}</div>
          </div>
          <div className="field">
            <label>Email</label>
            <div>{profile?.email ?? auth.email}</div>
          </div>
          <div className="field">
            <label>Member since</label>
            <div>
              {profile?.createdAtUtc ? new Date(profile.createdAtUtc).toLocaleDateString() : "—"}
            </div>
          </div>
          <div className="field">
            <label>Last login</label>
            <div>
              {profile?.lastLoginAtUtc ? new Date(profile.lastLoginAtUtc).toLocaleString() : "—"}
            </div>
          </div>
          <div className="field" style={{ marginBottom: 0 }}>
            <label>Wallet balance</label>
            <div className="balance-chip">
              <span className="amount">
                {Math.floor(profile?.walletBalance ?? auth.walletBalance ?? 0).toLocaleString()}
              </span>
              <span className="unit">coins</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
