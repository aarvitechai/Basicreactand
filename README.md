# Red Star Vibe

Full-stack app: **ASP.NET Core 10 Web API** backend and a **React 19 + TypeScript + Vite** frontend.

```
.
├── backend/                         ASP.NET Core solution (Clean Architecture)
│   ├── src/
│   │   ├── RedStarVibe.Api/         HTTP layer: controllers, contracts (DTOs), middleware, DI wiring
│   │   ├── RedStarVibe.Application/ Use cases / services, ports (interfaces), validation
│   │   ├── RedStarVibe.Domain/      Entities and domain rules (no dependencies)
│   │   └── RedStarVibe.Infrastructure/ Adapters: data sources, external services, clock
│   ├── tests/
│   │   ├── RedStarVibe.UnitTests/
│   │   └── RedStarVibe.IntegrationTests/  (WebApplicationFactory, in-memory HTTP)
│   ├── Directory.Build.props        Shared build settings (warnings as errors, analyzers)
│   └── Dockerfile
├── frontend/red-star-vibe-app/      React SPA
│   ├── src/
│   │   ├── app/                     App shell: providers, router, layout, error route
│   │   ├── pages/                   Route-level components (thin; compose features)
│   │   ├── features/<name>/         Feature modules: api/, hooks/, components/, types.ts, index.ts
│   │   ├── shared/                  Cross-feature code: api client, config, UI components
│   │   ├── styles/                  Global CSS / design tokens
│   │   └── test/                    Test setup and helpers
│   ├── nginx/                       Production nginx config (SPA fallback + /api reverse proxy)
│   └── Dockerfile
├── docker-compose.yml
└── .github/workflows/ci.yml         Build, lint, and test both apps on every push/PR
```

Dependency direction (backend): `Api → Application → Domain`, `Infrastructure → Application`.
The Domain and Application layers never reference ASP.NET Core or infrastructure concerns.

## Local development

Prerequisites: .NET SDK 10, Node.js 22+, SQL Server (any edition), and the EF Core CLI (`dotnet tool install -g dotnet-ef`).

The Development connection string in `backend/src/RedStarVibe.Api/appsettings.Development.json` points to
`DESKTOP-2ERCDGB\MSSQLSERVER01`, database `RedStarVibeApp`, using Windows authentication. To use a different
server without editing the committed file, override it with user-secrets:

```bash
cd backend/src/RedStarVibe.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=RedStarVibeApp;Trusted_Connection=True;TrustServerCertificate=True"
```

In Development the API applies pending migrations on startup (`Database:ApplyMigrationsOnStartup`), so the
database is created automatically the first time you run it.

```bash
# Terminal 1 – API on http://localhost:5057
cd backend
dotnet run --project src/RedStarVibe.Api

# Terminal 2 – UI on http://localhost:5173 (proxies /api to the backend)
cd frontend/red-star-vibe-app
npm install
npm run dev
```

OpenAPI document (Development only): http://localhost:5057/openapi/v1.json
Health checks: `/health/live` (process up), `/health/ready` (database reachable), `/health` (all).

## Authentication

Accounts use **ASP.NET Core Identity** on EF Core / SQL Server. Sessions are an **httpOnly, SameSite=Strict
cookie** (`Secure` outside Development), so tokens are never readable by JavaScript. The frontend and API are
served from one origin (Vite proxy in dev, nginx in production), which is what makes cookie auth simple and safe here.

| Endpoint | Auth | Result |
|---|---|---|
| `POST /api/v1/auth/register` `{ email, password, displayName, phoneNumber? }` | anonymous | `201` user (not signed in; confirmation email sent) · `400` validation · `409` email taken |
| `POST /api/v1/auth/confirm-email` `{ userId, token }` | anonymous | `200` · `400` invalid/expired link (`token`) |
| `POST /api/v1/auth/resend-confirmation` `{ email }` | anonymous | always `202` with the same message |
| `POST /api/v1/auth/login` `{ email, password, rememberMe }` | anonymous | `200` user + cookie · `401` invalid credentials · `403` `code: email_not_confirmed` |
| `POST /api/v1/account/phone/send-code` `{ phoneNumber }` | signed in | `202` SMS sent · `400` invalid number · `429` · `503` SMS provider down |
| `POST /api/v1/account/phone/verify` `{ phoneNumber, code }` | signed in | `200` user with `phoneNumberConfirmed: true` · `400` wrong/expired code |
| `POST /api/v1/auth/logout` | signed in | `204`, cookie cleared |
| `GET /api/v1/auth/me` | signed in | `200` current user · `401` |
| `POST /api/v1/auth/forgot-password` `{ email }` | anonymous | always `202` with the same message (emails a link if the account exists) |
| `POST /api/v1/auth/reset-password` `{ email, token, newPassword }` | anonymous | `200` · `400` invalid/expired token (`token`) or weak password (`password`) |

Built-in protections:

- Password policy: 8+ chars with upper, lower, digit and symbol (mirrored client-side for instant feedback).
- Unique email enforced by Identity **and** a unique database index (safe under concurrent sign-ups).
- Lockout after 5 failed logins for 15 minutes; login errors never reveal whether an email exists.
- Auth endpoints rate-limited to 10 requests/minute per IP (`RateLimiting:Auth`).
- Data Protection keys (which encrypt the cookie) are stored in the `DataProtectionKeys` table, so sessions
  survive restarts and work across multiple instances.

### Password reset

1. `/forgot-password` → the API queues a reset email (only if the account exists) and always returns the same
   response. Emails are sent by a background worker, so response time doesn't reveal whether an account exists either.
2. The email links to `{App:FrontendBaseUrl}/reset-password?email=…&token=…`. The base URL comes from config,
   never from the request's Host header (which could be spoofed to point the link at an attacker's site).
3. `/reset-password` removes the token from the address bar, asks for a new password, and calls the API.
   Tokens are **single-use** and expire after **1 hour**. A successful reset also lifts any lockout and rotates
   the security stamp, which signs out other sessions within 5 minutes.

**Email delivery**

- **Development:** with no `Email:Smtp:Host` set, emails are written to the backend console instead of being sent.
  Look for `===== DEV EMAIL (not sent) =====` in the API output and open the link from there.
- **Production:** SMTP is required (the app refuses to start without it). Configure `Email:Smtp:*` and
  `App:FrontendBaseUrl`, with the SMTP password supplied as a secret (`Email__Smtp__Password`).
- To test real delivery locally, run a mail catcher such as [Mailpit](https://mailpit.axllent.org/)
  and set `Email:Smtp:Host=localhost`, `Port=1025`, `UseStartTls=false`.
- The email queue is in memory: an email that hasn't been sent yet is lost if the process stops. Use a durable
  outbox or message queue if you need guaranteed delivery.

### Email confirmation

- Registering creates the account and emails a link to `/confirm-email?userId=…&token=…` (valid **24 hours**,
  separate token provider from password reset). The user is **not** signed in.
- Sign-in is refused until the email is confirmed. The `403 email_not_confirmed` response is only given when the
  password is correct (wrong passwords get the generic `401` and count toward lockout), so it can't be used to
  probe which emails have unconfirmed accounts. The sign-in page then offers to resend the link.
- The confirm page POSTs from JavaScript instead of confirming on GET, so email link scanners can't confirm
  accounts on the user's behalf. Confirming twice is harmless. Completing a password reset also confirms the email.

### Phone verification (SMS)

- A mobile number is optional at registration (stored in E.164, e.g. `+919876543210`, unverified). After signing in,
  the account page texts a **6-digit code** and verifies it; changing the number works the same way.
- Codes are time-based and bound to the specific number (a code for one number can't verify another).
- Rate limits **per user**: 3 codes per 15 minutes (`RateLimiting:Sms`) to control SMS cost and toll fraud, and
  10 verify attempts per 15 minutes (`RateLimiting:SmsVerify`) to stop brute-forcing codes.
- **Development:** codes are written to the API console (`===== DEV SMS (not sent) =====`).
- **Production:** Twilio is required (the app refuses to start without it): set `Sms:Twilio:AccountSid`,
  `Sms:Twilio:AuthToken` (secret) and either `FromNumber` or `MessagingServiceSid`.

Not yet included: SMS two-factor sign-in (the verified phone and SMS sender are in place), and roles/authorization policies.

## Database migrations

```bash
cd backend
# Add a migration after changing entities / AppDbContext
dotnet ef migrations add <Name> -p src/RedStarVibe.Infrastructure -s src/RedStarVibe.Api -o Persistence/Migrations
# Apply to the Development database
dotnet ef database update -p src/RedStarVibe.Infrastructure -s src/RedStarVibe.Api
# Production: generate an idempotent script and run it as a deployment step
dotnet ef migrations script --idempotent -p src/RedStarVibe.Infrastructure -s src/RedStarVibe.Api -o migrate.sql
```

## Tests & quality

```bash
cd backend && dotnet test
cd frontend/red-star-vibe-app && npm run lint && npm run typecheck && npm test
```

## Production (Docker)

```bash
cp .env.example .env   # then set DB_CONNECTION_STRING (SQL login; containers can't use Windows auth)
docker compose up --build
# open http://localhost:8080
```

Production doesn't auto-migrate: apply the idempotent migration script to the database first.

nginx serves the built SPA and reverse-proxies `/api/*` to the backend container, so the browser talks to a
single origin and no CORS configuration is needed in production.

## Configuration

| Where | Setting | Purpose |
|---|---|---|
| `backend/src/RedStarVibe.Api/appsettings*.json` | `ConnectionStrings:DefaultConnection` | SQL Server connection (required) |
| | `Database:ApplyMigrationsOnStartup` | Auto-migrate on startup (true in Development only) |
| | `Cors:AllowedOrigins` | Origins allowed to call the API directly (with credentials) |
| | `RateLimiting:Default`, `RateLimiting:Auth` (`PermitLimit`, `WindowSeconds`) | Per-IP fixed-window rate limits |
| | `App:FrontendBaseUrl` | Public frontend URL used in email links (required) |
| | `Email:FromAddress`, `Email:FromName` | Sender shown on outgoing emails |
| | `Email:Smtp:Host`, `Port`, `Username`, `Password`, `UseStartTls` | SMTP server (required outside Development) |
| Env vars (override any setting) | e.g. `Cors__AllowedOrigins__0=https://app.example.com` | Standard ASP.NET Core config binding |
| `frontend/.../.env.local` (see `.env.example`) | `VITE_API_BASE_URL` | API base URL; empty = same origin |
| Frontend container | `API_UPSTREAM` | Where nginx proxies `/api` |

Secrets: never commit them. Use `dotnet user-secrets` locally and environment variables or a secret store in production.

## Adding a feature

**Backend:** add the entity to `Domain/`, a service and any ports (interfaces) to `Application/<Feature>/`,
implementations to `Infrastructure/<Feature>/`, register both in each layer's `DependencyInjection.cs`, then
add a controller under `Api/Controllers/V1/` with request/response records in `Api/Contracts/`.

**Frontend:** create `src/features/<feature>/` with `api/`, `hooks/`, `components/`, `types.ts` and an
`index.ts` that exports the public pieces. Pages import from `@/features/<feature>` only.
