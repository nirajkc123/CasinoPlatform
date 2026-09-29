# Casino Platform (Virtual Coins Only) — Stage 1-4 MVP

This is a working slice of the roadmap you laid out:

```
Register → Login → 10,000 virtual coins → Lobby (list) → Slot Machine
   → Spin → Win/Lose virtual coins → Transaction History
```

**No real money anywhere in this code.** Everything is a virtual coin
ledger. Stage 10 in your roadmap (real payments, KYC, AML, licensing) is a
completely different, heavily-regulated project and is intentionally not
started here.

## What's implemented

| Stage | Item | Where |
|---|---|---|
| 1 | Web API project, EF Core, architecture, Swagger | `Program.cs`, whole solution |
| 2 | Register / Login / password hashing / JWT / roles / profile | `Controllers/AuthController.cs` |
| 3 | Wallet, balance, deposit/withdraw, history, concurrency-safe updates | `Services/WalletService.cs`, `Controllers/WalletController.cs` |
| 4 | Slot machine engine, server-side RNG, payouts, round history, replay protection | `Services/SlotMachineService.cs`, `Controllers/SlotController.cs` |

Not yet built (next stages, on request): Blackjack/Roulette, React frontend,
SignalR, Admin panel, Docker/deployment, real-money architecture.

## Project layout

```
CasinoPlatform.sln
src/CasinoPlatform.Api/
  Controllers/     AuthController, WalletController, SlotController
  Data/            ApplicationDbContext (Identity + wallet/transaction/game tables)
  Models/          ApplicationUser, Wallet, Transaction, GameRound
  DTOs/            request/response records
  Services/        TokenService, WalletService, SlotMachineService
  Options/         JwtOptions, GameOptions (bound from appsettings.json)
  Middleware/      global exception handler
  Program.cs       DI wiring, JWT config, Swagger, middleware pipeline
```

## Prerequisites

- .NET 8 SDK
- SQL Server (a local instance, or the Docker command below)
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

## 1. Start SQL Server (Docker, easiest option)

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name casino-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

This matches the connection string already in `appsettings.json`. If you
use your own SQL Server instance, edit `ConnectionStrings:DefaultConnection`.

## 2. Set the JWT signing key as a user secret (never commit real secrets)

```bash
cd src/CasinoPlatform.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"
```

## 3. Create the database migration and apply it

```bash
dotnet ef migrations add InitialCreate -o Data/Migrations
dotnet ef database update
```

(`Program.cs` also calls `db.Database.MigrateAsync()` automatically on
startup in the Development environment, so step 3's `database update` is
mostly for your first run / whenever you add a new migration.)

## 4. Run it

```bash
dotnet run
```

Swagger UI opens at `https://localhost:7080/swagger`.

## 5. Try the flow

```bash
# Register -> creates user + wallet with 10,000 coins, returns a JWT
curl -k -X POST https://localhost:7080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"player1@test.com","displayName":"Player1","password":"Password123"}'

# Copy the "token" from the response, then:
TOKEN="paste-token-here"

curl -k https://localhost:7080/api/wallet/balance -H "Authorization: Bearer $TOKEN"

# Spin the slot machine - requestId must be a fresh GUID per spin
curl -k -X POST https://localhost:7080/api/games/slot/spin \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"betAmount":50,"requestId":"'"$(uuidgen)"'"}'

# Full transaction history
curl -k https://localhost:7080/api/wallet/transactions -H "Authorization: Bearer $TOKEN"

# Full game round history
curl -k https://localhost:7080/api/games/slot/history -H "Authorization: Bearer $TOKEN"
```

## Design notes worth knowing

- **Password hashing** is entirely handled by ASP.NET Core Identity
  (`UserManager.CreateAsync` / `CheckPasswordAsync`) — plaintext passwords
  are never stored or logged.
- **Concurrency protection**: `Wallet.RowVersion` is a SQL Server
  `rowversion` column mapped as an EF Core concurrency token. Two
  simultaneous requests touching the same wallet can't silently clobber
  each other's balance update — the loser gets `DbUpdateConcurrencyException`
  and the code retries automatically.
- **Duplicate-spin protection**: `GameRound.RequestId` has a unique index.
  The client generates one GUID per spin attempt; if that request is ever
  resent (double-click, network retry, replay attack), the server detects
  the existing row and returns the *original* result instead of spinning
  (and paying out) again.
- **Every coin movement is a `Transaction` row** — deposits, withdrawals,
  bets and wins all write an append-only ledger entry with the balance
  immediately after, which is what Stage 3 item 17 and Stage 10's
  "immutable financial records" both build on.
- **RNG**: `System.Security.Cryptography.RandomNumberGenerator`, not
  `System.Random` — outcomes must not be predictable even in a
  virtual-coin game.

## Suggested next step

Pick the next item from your roadmap (Blackjack, the React frontend, or
SignalR live events are the natural next three) and we'll build that
against this same foundation.
