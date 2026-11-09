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

Prerequisites: .NET SDK 10, Node.js 22+.

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
Health check: http://localhost:5057/health

## Tests & quality

```bash
cd backend && dotnet test
cd frontend/red-star-vibe-app && npm run lint && npm run typecheck && npm test
```

## Production (Docker)

```bash
docker compose up --build
# open http://localhost:8080
```

nginx serves the built SPA and reverse-proxies `/api/*` to the backend container, so the browser talks to a
single origin and no CORS configuration is needed in production.

## Configuration

| Where | Setting | Purpose |
|---|---|---|
| `backend/src/RedStarVibe.Api/appsettings*.json` | `Cors:AllowedOrigins` | Origins allowed to call the API directly |
| | `RateLimiting:PermitLimit`, `RateLimiting:WindowSeconds` | Per-IP fixed-window rate limit |
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
