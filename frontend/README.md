# Lodge — frontend

Next.js 16 (App Router) client for the Lodge booking API.

## Getting started

```bash
cp .env.example .env.local   # point NEXT_PUBLIC_API_BASE_URL at the API
npm ci
npm run dev                  # http://localhost:3000
```

The app builds and renders without a running backend: data pages are rendered
on demand and show friendly error states when the API is unreachable.

## Scripts

| Script              | What it does                                   |
| ------------------- | ---------------------------------------------- |
| `npm run dev`       | Dev server (Turbopack)                         |
| `npm run build`     | Production build (`output: "standalone"`)      |
| `npm start`         | Serve the production build                     |
| `npm run lint`      | ESLint (flat config, `eslint .`)               |
| `npm run typecheck` | Generate route types and run `tsc --noEmit`    |
| `npm run check`     | lint + typecheck + build                       |

CI: `npm ci && npm run lint && npm run typecheck && npm run build`

## Environment

| Variable                   | Default                 | Notes                                                       |
| -------------------------- | ----------------------- | ----------------------------------------------------------- |
| `NEXT_PUBLIC_API_BASE_URL` | `http://localhost:8000` | API origin (no `/api` suffix). Inlined at build time.       |
| `NEXT_PUBLIC_API_VERSION`  | `v1`                    | Requests go to `{base}/api/{version}/…`                     |
| `API_INTERNAL_BASE_URL`    | –                       | Optional, server-only origin (e.g. docker service name).    |

Values are validated with zod in `src/lib/env.ts`.

## Structure

```
src/
  app/          routes: (main) shop pages, (auth) login/register, api/health
  components/   ui/ (shadcn primitives), shared/, layout/, providers
  features/     apartments | auth | bookings | users  → api/, components/, hooks/, schemas/
  lib/          env, api client (axios), auth (JWT cookie), dates, format, query keys
  types/        API contract types (mirrors backend/src/Lodge.Contracts)
  proxy.ts      auth gate for /bookings, /settings, /stays, /admin (admins only) (Next 16 "proxy")
```

## Docker

```bash
docker build -t lodge-frontend \
  --build-arg NEXT_PUBLIC_API_BASE_URL=http://localhost:8000 .
docker run -p 3000:3000 lodge-frontend
```
