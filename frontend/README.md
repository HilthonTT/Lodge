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
| `npm run lint`      | ESLint (flat config) + locale key check        |
| `npm run check:messages` | Only the locale key check (`scripts/check-messages.mjs`) |
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
messages/       UI strings per locale: en.json (source of truth), nl.json, fr.json
src/
  app/          routes: (main) shop pages + users/[userId] profiles, (auth) login/register,
                forgot-password, reset-password, verify-email, auth/callback (social login), api/health
  components/   ui/ (shadcn primitives), shared/, layout/, providers
  features/     apartments | auth | bookings | hosting | users | wishlists  → api/, components/, hooks/, schemas/
  i18n/         next-intl setup: locales, request config, language action, typed messages
  lib/          env, api client (axios), auth (JWT cookie), dates, format, query keys
  types/        API contract types (mirrors backend/src/Lodge.Contracts)
  proxy.ts      auth gate for /bookings, /settings, /stays, /wishlists, /hosting, /admin (admins only) (Next 16 "proxy")
```

## Localization

The UI is available in English (`en`, default), Dutch (`nl`) and French (`fr`), using
[next-intl](https://next-intl.dev) without i18n routing:

* **No URL prefix.** Routes are the same in every language.
* The locale comes from the `NEXT_LOCALE` cookie, set by the language switcher (globe in
  the navbar, compact switcher in the footer). Without a cookie, the browser's
  `Accept-Language` decides; anything unsupported falls back to English. See
  `src/i18n/request.ts`. The auth proxy (`src/proxy.ts`) is unaffected.
* `<html lang>` follows the active locale.

### Adding strings

**Every user-facing string goes into all three files in `messages/`, with the same keys.**
Write the English first, then a natural Dutch and French version (not word-for-word).
`npm run lint` fails when the files have different keys, empty values or different ICU
arguments, and `npm run typecheck` fails on unknown keys and on key drift between locales.

1. **Pick a namespace.** Keys are grouped by feature: `common`, `nav`, `footer`, `home`,
   `search`, `apartment`, `checkout`, `auth`, `validation`, `bookings`, `admin`,
   `settings`, `errors`. Shared words (Cancel, Try again, "3 nights") live in `common`.
   Add a new top-level namespace for a new feature.
2. **Use the right API:**

   | Where | How |
   | --- | --- |
   | Async server component, `generateMetadata`, route handler | `const t = await getTranslations("ns")` and `await getLocale()` from `next-intl/server` |
   | Client component, or a non-async server component | `const t = useTranslations("ns")` and `useLocale()` from `next-intl` |
   | Rich text with a link | `t.rich("key", { link: (chunks) => <Link href="…">{chunks}</Link> })` with `"… <link>My bookings</link> …"` |
   | zod schemas | Factories that take `useTranslations("validation")`, e.g. `createLoginSchema(t)`, built with `useMemo` |
   | API errors | `useApiErrorMessage()` (client) or `apiErrorMessage(error, await getTranslations("errors.api"))` (server). Well-known backend codes (e.g. `User.VerificationEmailThrottled`, `TwoFactor.InvalidCode`) are translated under `errors.api.codes.*` (map in `src/i18n/api-errors.ts`); other server messages are shown as returned, and the frontend's own fallbacks are translated. |

3. **Counts use ICU plurals**, never string concatenation:
   `"{count, plural, one {# night} other {# nights}}"` → `t("nights", { count })`.
   Use `select` for enums: `"{status, select, reserved {…} other {…}}"`.
4. **Numbers in plain arguments get grouped** (`{year}` with 2026 renders "2,026"), so pass
   years, page numbers and IDs as strings.
5. **Dates, prices and numbers** go through `src/lib/dates.ts` and `src/lib/format.ts`, which
   take the locale: `formatDateRange(from, to, locale)`, `formatPrice(amount, currency, locale)`.
   Prices keep the currency the API returns. `<Calendar>` picks up the locale (month names,
   Monday week start for nl/fr) by itself.
6. Keep labels out of constants: store a message key (`labelKey: "topRated"`) and call
   `t(option.labelKey)` when rendering.

Exception: `src/app/global-error.tsx` renders without providers, so its three strings are
inlined per locale.

### Display currency

The globe dialog also has a **Currency** tab (USD, EUR, GBP, JPY, CHF, CAD, AUD). The choice
lives in the `LODGE_CURRENCY` cookie; without it, `en` shows USD and `nl`/`fr` show EUR
(`src/lib/currency.ts`). Rates come from `GET /currencies`, fetched once by the root layout and
cached by TanStack Query.

* Browsing prices (cards, map pins, calendar, nightly headline) use `<Money>` from
  `features/payments/components/money.tsx`, which converts and marks the result
  ("≈ €170", tooltip "≈ converted from USD").
* Amounts that are charged (checkout, price breakdowns, receipts, refunds) stay in the
  listing's currency and use `<ChargedAmount>`, which adds the converted amount underneath.
* The price filter works in USD on the API side: the slider shows the display currency and
  converts back before writing `minPrice`/`maxPrice` to the URL.

### Payments

`POST /stripe/checkout` returns `{ url }`. In Simulated mode (`GET /payments/config`) that URL
is this app's `/checkout/simulated?bookingId=…`, a test-mode hosted payment page that confirms
the booking through `POST /payments/simulated/{id}/complete`.

## Docker

```bash
docker build -t lodge-frontend \
  --build-arg NEXT_PUBLIC_API_BASE_URL=http://localhost:8000 .
docker run -p 3000:3000 lodge-frontend
```
