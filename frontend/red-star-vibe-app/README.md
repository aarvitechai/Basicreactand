# Red Star Vibe – Frontend

React 19 + TypeScript + Vite SPA. See the [root README](../../README.md) for architecture and setup.

| Script | Purpose |
|---|---|
| `npm run dev` | Dev server on :5173, proxies `/api` to the backend |
| `npm run build` | Typecheck and production build to `dist/` |
| `npm run preview` | Serve the production build locally |
| `npm run lint` / `lint:fix` | ESLint |
| `npm run typecheck` | TypeScript project check |
| `npm test` / `test:watch` | Vitest + Testing Library |

Key libraries: `react-router` (routing), `@tanstack/react-query` (server state / caching),
a small typed `fetch` wrapper in `src/shared/api/httpClient.ts` that surfaces backend problem-details errors.
Import from `src` via the `@/` alias.
