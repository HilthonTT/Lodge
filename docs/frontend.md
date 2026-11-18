# Frontend

The frontend is a Next.js 16 App Router app with React 19, in [`frontend/`](../frontend). The [frontend README](../frontend/README.md) lists its scripts, environment variables and Docker usage. This page covers how the app is organised.

## Stack

| Concern | Choice |
| --- | --- |
| Framework | Next.js 16 (App Router, Turbopack, `output: "standalone"`) |
| UI | Tailwind CSS v4 (CSS-first config in `src/app/globals.css`), shadcn-style primitives on Radix UI, lucide icons, Geist font |
| Server state | TanStack Query 5 |
| Forms | react-hook-form + zod 4 |
| HTTP | One typed axios client (`src/lib/api`) |
| Dates | date-fns 4, react-day-picker 10 |
| Feedback | sonner toasts, skeleton loading states |
| Theming | next-themes (light / dark / system) |
| Maps | react-leaflet, loaded lazily when the map scrolls into view |

## Folder layout

```
frontend/src/
├─ app/                     routes only: thin pages that compose feature components
│  ├─ (main)/               listing, search, apartment detail, stays (checkout), bookings, settings, admin/bookings
│  ├─ (auth)/               login, register
│  └─ api/health/           container health check
├─ features/<feature>/      apartments · auth · bookings · users
│  ├─ api/                  typed endpoint functions: (http, …args) => Promise<T>
│  ├─ components/           feature UI
│  ├─ hooks/                TanStack Query queries/mutations wrapping api/
│  └─ schemas/              zod schemas shared by forms
├─ components/
│  ├─ ui/                   design-system primitives (button, dialog, calendar, …)
│  ├─ layout/               navbar, footer, user menu
│  └─ shared/               cross-feature building blocks (empty/error states, …)
├─ lib/                     env (zod-validated), api clients, auth/session, dates, formatting, query keys
├─ types/api.ts             mirrors backend/src/Lodge.Contracts
└─ proxy.ts                 route protection (Next 16's replacement for middleware)
```

Rule of thumb: `app/` decides *what* is on a page, `features/` decides *how*, and `components/ui` must never import from `features/`.

## Data flow

* **Server components** fetch with `getServerHttp()` (`src/lib/api/server.ts`). It reads the JWT from the request cookie and calls the API directly. Inside Docker it uses `API_INTERNAL_BASE_URL` (e.g. `http://gateway:8080`).
* **Client components** use TanStack Query hooks from `features/*/hooks`, which call the browser HTTP client. Query keys all live in `src/lib/query-keys.ts`, so invalidation stays consistent.
* Nothing is fetched at build time. Every data page is rendered on demand, so `next build` works without a backend. When the API is down, pages show an error state instead of crashing.
* API errors arrive as RFC 9457 problem details. Toasts and form errors show the problem's `detail`.

## Authentication

1. Login and register store the JWT in a `SameSite=Lax` cookie. The cookie expires with the token and is marked `Secure` on HTTPS.
2. `src/proxy.ts` checks the cookie before rendering. Signed-out users who open `/bookings`, `/settings`, `/stays` or `/admin` are redirected to `/login?next=…`; signed-in users who open `/login` or `/register` are redirected away. The `next` parameter only accepts same-site paths.
3. `/admin/*` additionally requires the `Admin` role, read from the token's `role` claim. Guests are redirected to `/`. The admin pages also check the role on the server with `requireAdminSession()`, and the API enforces it again.
4. The client signs out automatically when the token expires and on any `401` from the API.

> [!NOTE]
> The proxy check is only optimistic (it decodes the token but cannot verify it). The API verifies the token's signature on every request.

## Booking UX

* `GET /apartments/{id}/booked-dates` returns booked **nights**. In the calendar, those days are crossed out and can't be picked as check-in. Once a check-in is picked, check-out is allowed up to and including the next booked night, so back-to-back stays are possible.
* Ratings (`averageRating`, `reviewCount`) come with every apartment from the API. Cards show a star with the average, or *New* when there are no reviews, and search can sort by *Top rated*.
* A completed booking shows *Write a review* until `hasReview` is true, then a *Reviewed* marker.
* The price breakdown comes from `GET /apartments/{id}/price`, so the totals always match what the backend charges.
* Reserving goes through `/stays/{id}` (a review page) and sends a fixed `X-Idempotency-Key` per page visit, so a double-click can't create two bookings.

## Admin area

Signing in as an admin (`admin@lodge.dev` / `Admin123!` in the demo data) adds **Manage bookings** to the user menu. The page at `/admin/bookings`:

* lists every booking with guest, stay, total, status and created date, as a table on desktop and cards on mobile;
* has status filter chips and pagination, both kept in the URL (`?status=reserved&page=2`);
* offers **Reject** for reserved bookings and **Mark completed** for confirmed stays that have ended, each behind a confirmation dialog, with optimistic updates that roll back on error.

## Design tokens

Colours, radii and fonts are CSS variables in `src/app/globals.css`, defined for `:root` and `.dark` in the OKLCH colour space. The primary colour is a pine green. To re-theme the app, change the variables; components only use semantic classes like `bg-primary` and `text-muted-foreground`.

## Adding a feature

1. Add types to `src/types/api.ts`, matching the backend's `Lodge.Contracts`.
2. Add endpoint functions in `src/features/<feature>/api/`.
3. Wrap them in query or mutation hooks in `hooks/`, with keys from `lib/query-keys.ts`.
4. Build UI in `features/<feature>/components/`, then compose it in a route under `app/`.
5. Run `npm run check`.
