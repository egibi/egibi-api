<p align="center">
  <img src="./egibi-logo.png" alt="Egibi" width="300" />
</p>

<h3 align="center">Multi-Asset Algorithmic Trading Platform — API</h3>

<p align="center">
  .NET 9 backend powering strategy design, backtesting, and multi-exchange trading
</p>

<p align="center">
  <a href="#quick-start">Quick Start</a> •
  <a href="#architecture">Architecture</a> •
  <a href="#api-endpoints">API Endpoints</a> •
  <a href="#database">Database</a> •
  <a href="#security">Security</a> •
  <a href="https://github.com/egibi/egibi-ui">Frontend Repo</a>
</p>

---

## Overview

Egibi API is the backend for the Egibi algorithmic trading platform. It provides RESTful endpoints for managing trading strategies, running backtests against historical market data, connecting cryptocurrency exchange accounts, and integrating with banking services via Plaid and Mercury.

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | .NET 9 / ASP.NET Core |
| Database (relational) | PostgreSQL 16 via Entity Framework Core 9 |
| Database (time-series) | QuestDB 8.2.1 (OHLC candle storage) |
| Authentication | OpenIddict 5.8 (OAuth 2.0 / OpenID Connect) |
| MFA | TOTP via Otp.NET |
| Real-time | ASP.NET Core SignalR |
| API Docs | Swashbuckle (Swagger / OpenAPI) |
| Encryption | AES-256-GCM with per-user data encryption keys |
| Containerization | Docker Compose (dev infrastructure) |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL and QuestDB)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Rider](https://www.jetbrains.com/rider/) (recommended)
- [Node.js 24+ LTS](https://nodejs.org/) (only if running the frontend alongside)

## Quick Start

### 1. Clone

```bash
git clone https://github.com/egibi/egibi-api.git
cd egibi-api
```

### 2. Start Databases

```bash
docker compose up -d
```

This spins up PostgreSQL 16 and QuestDB 8.2.1. Verify both are healthy:

```bash
docker compose ps
```

### 3. Generate a Master Encryption Key

The API encrypts user credentials with AES-256-GCM. Generate a 32-byte base64 key using **one** of these methods:

**PowerShell:**
```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Max 256 }))
```

**C# Interactive (`dotnet script` or VS Interactive):**
```csharp
using System.Security.Cryptography;
Console.WriteLine(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
```

Place the key in `appsettings.json` → `Encryption:MasterKey`, or in User Secrets:

```bash
dotnet user-secrets set "Encryption:MasterKey" "<your-base64-key>"
```

### 4. Run EF Migrations

**Package Manager Console (Visual Studio):**
```powershell
Update-Database -Project egibi-api
```

**PowerShell / Terminal:**
```powershell
dotnet ef database update --project egibi-api
```

### 5. Run the API

```bash
dotnet run --project egibi-api
```

The API starts at `https://localhost:7182`. Swagger UI is available at `https://localhost:7182/swagger`.

### One-Command Dev Startup

A PowerShell script starts everything together:

```powershell
.\start-dev.ps1            # Start DBs + API
.\start-dev.ps1 -DbOnly    # Start DBs only
.\start-dev.ps1 -Migrate   # Start DBs, run EF migrations, then API
```

## Architecture

### Solution Structure

```
egibi-api.sln
├── egibi-api/                  # ASP.NET Core Web API (main project)
│   ├── Authorization/          # Role definitions and policies
│   ├── Configuration/          # Plaid, QuestDB, and infrastructure options
│   ├── Controllers/            # API endpoint controllers
│   ├── Data/                   # EF Core DbContext, entities, and seeding
│   ├── Hubs/                   # SignalR hubs (chat, file upload)
│   ├── MarketData/             # OHLC fetchers, repositories, and services
│   ├── Migrations/             # EF Core migrations
│   ├── Models/                 # Request/response DTOs
│   ├── Services/               # Business logic and external API clients
│   └── Strategies/             # Strategy execution engine and implementations
├── EgibiBinanceUsSdk/          # Binance US API client
├── EgibiCoinbaseSDK/           # Coinbase API client
├── EgibiCoreLibrary/           # Shared models and utilities
├── EgibiGeoDateTimeDataLibrary/# Country and timezone reference data
├── EgibiQuestDB/               # QuestDB ingestion and query SDK
├── EgibiStrategyLibrary/       # Strategy interface and test implementations
└── docker-compose.yml          # PostgreSQL + QuestDB dev infrastructure
```

### Three-Layer Design

**Controllers** handle HTTP routing, request validation, and response formatting. **Services** contain business logic, database operations, and external API coordination. **API Clients** (Plaid, exchange SDKs) manage outbound integrations with third-party services.

### Authentication Flow

1. User submits credentials to `/auth/login`
2. Server validates and sets an HTTP-only cookie
3. Silent redirect to `/connect/authorize` obtains an authorization code
4. Code is exchanged for JWT access + refresh tokens at `/connect/token`
5. Access tokens are attached to subsequent API requests via `Authorization: Bearer`
6. Tokens refresh automatically before expiry

## API Endpoints

| Controller | Route Prefix | Description |
|---|---|---|
| Authorization | `/auth/*`, `/connect/*` | Login, signup, password reset, OIDC token exchange |
| MFA | `/api/mfa/*` | TOTP setup, verification, recovery codes |
| Accounts | `/accounts/*` | Trading account CRUD and credential management |
| ExchangeAccounts | `/exchangeaccounts/*` | Exchange account linking and configuration |
| Exchanges | `/exchanges/*` | Exchange catalog and metadata |
| Strategies | `/api/strategies/*` | Strategy builder with indicator rules |
| Backtester | `/backtester/*` | Backtest CRUD, execution, and results |
| MarketData | `/marketdata/*` | OHLC candles, symbol discovery, data import |
| DataManager | `/datamanager/*` | Data provider CRUD and QuestDB operations |
| Funding | `/funding/*` | Mercury and Plaid funding sources |
| Plaid | `/plaid/*` | Plaid Link token creation and token exchange |
| Markets | `/markets/*` | Market catalog and metadata |
| Connections | `/connections/*` | Service catalog (exchanges, data providers) |
| Storage | `/storage/*` | File upload and management |
| AppConfigurations | `/appconfigurations/*` | User-level app settings (Plaid credentials, etc.) |
| UserManagement | `/usermanagement/*` | Admin user management |
| ApiTester | `/apitester/*` | Exchange API connection testing |
| Environment | `/environment/*` | App environment info |

## Database

### PostgreSQL (Relational)

Managed by Entity Framework Core. Key entity groups:

- **Users & Auth** — `AppUser`, `UserCredential`, `UserPlaidConfig`
- **Trading** — `Account`, `ExchangeAccount`, `Exchange`, `Market`
- **Strategies** — `Strategy`, `Backtest`, `BacktestStatus`
- **Data** — `DataProvider`, `Connection`, `FundingSource`
- **Platform** — `AppConfiguration`, `Country`, `TimeZone`

### QuestDB (Time-Series)

Stores OHLC market data with deduplication:

```sql
CREATE TABLE IF NOT EXISTS ohlc (
    symbol      SYMBOL,
    source      SYMBOL,
    interval    SYMBOL,
    timestamp   TIMESTAMP,
    open        DOUBLE,
    high        DOUBLE,
    low         DOUBLE,
    close       DOUBLE,
    volume      DOUBLE
) TIMESTAMP(timestamp) PARTITION BY MONTH
DEDUP UPSERT KEYS(symbol, source, interval, timestamp);
```

Accessible via REST API (port `9000`), ILP ingestion (port `9009`), and PostgreSQL wire protocol (port `8812`).

## Security

- **Encryption at rest** — User API credentials and Plaid developer configurations are encrypted with AES-256-GCM using per-user data encryption keys (DEKs) derived from a master key
- **Authentication** — OpenIddict with OAuth 2.0 / OpenID Connect, JWT access tokens with refresh token rotation
- **MFA** — Optional TOTP-based multi-factor authentication; required before accessing financial integrations (Plaid Link)
- **User-scoped data** — All API endpoints enforce user-scoped access; users can only read/modify their own data
- **Role-based access** — Admin and Standard User roles enforced via claims-based authorization
- **Rate limiting** — Applied to authentication endpoints to prevent brute-force attacks
- **CORS** — Restricted to configured allowed origins

## Configuration

### `appsettings.json`

```jsonc
{
  "ConnectionStrings": {
    "EgibiDb": "Host=localhost;Port=5432;Database=egibi_app_db;...",
    "QuestDb": "Host=localhost;Port=8812;Database=qdb;..."
  },
  "QuestDb": {
    "HttpUrl": "http://localhost:9000",
    "IlpHost": "localhost",
    "IlpPort": 9009
  },
  "Encryption": {
    "MasterKey": "<base64-encoded-32-byte-key>"
  },
  "Plaid": {
    "Products": ["auth", "transactions"],
    "CountryCodes": ["US"]
  },
  "Oidc": {
    "LoginRedirectUrl": "http://localhost:4200/auth/login",
    "RedirectUris": ["http://localhost:4200/auth/callback"],
    "PostLogoutRedirectUris": ["http://localhost:4200"]
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```

### Environment-Specific Overrides

- `appsettings.Development.json` — Local dev settings
- `appsettings.Production.json` — Production configuration
- **User Secrets** — Recommended for sensitive values (master key, credentials)

## Docker Commands

```bash
docker compose up -d          # Start PostgreSQL + QuestDB
docker compose down           # Stop containers
docker compose down -v        # Stop and destroy all data
docker compose logs -f        # Tail container logs
docker compose ps             # Check container health
```

## EF Migrations

**Package Manager Console (recommended):**
```powershell
Add-Migration <MigrationName> -Project egibi-api
Update-Database -Project egibi-api
```

**PowerShell / Terminal:**
```powershell
dotnet ef migrations add <MigrationName> --project egibi-api
dotnet ef database update --project egibi-api
```

## Related

- **Frontend** — [egibi-ui](https://github.com/egibi/egibi-ui)
- **Organization** — [github.com/egibi](https://github.com/egibi)

## License

Proprietary — Egibi LLC
