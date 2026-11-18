# Development guide

## Prerequisites

| Tool | Version | Used for |
| --- | --- | --- |
| [.NET SDK](https://dotnet.microsoft.com/download) | 11.0.100+ (pinned in `global.json`) | Backend |
| [Node.js](https://nodejs.org/) | 22.12+ (24 LTS recommended) | Frontend |
| [Docker](https://www.docker.com/) with Compose v2 | recent | Infrastructure and integration tests |
| IDE (optional) | Visual Studio 2026, Rider or VS Code with C# Dev Kit | All of them open `Lodge.slnx` |

## Option 1: everything in Docker

```bash
docker compose up -d --build
```

| Service | URL |
| --- | --- |
| Web app | <http://localhost:3000> |
| Gateway (public API entry point and SignalR hub) | <http://localhost:8000> |
| API (direct) and Scalar docs | <http://localhost:5000/scalar> |
| Hangfire dashboard | <http://localhost:5000/hangfire> |
| Seq (structured logs) | <http://localhost:8081> |
| Mailpit (captured e-mails) | <http://localhost:8025> |
| RabbitMQ management | <http://localhost:15672> (guest / guest) |

The API runs in the `Development` environment. On startup it applies EF Core migrations and seeds demo data. Payments run in **Simulated** mode unless Stripe keys are configured (see [Payments](#payments)).

### Seeded data

The seeder is idempotent:

* On an empty database, it creates the full demo data set.
* On a database seeded by an older version, it only adds the missing pieces (bios, verified e-mail addresses, the host account, listing details, the wishlist, experiences, conversations and notifications).

All booking dates are relative to today.

Every seeded account has a verified e-mail address and a bio, and none has two-factor authentication:

| Account | Password | Role | What it demonstrates |
| --- | --- | --- | --- |
| `demo@lodge.dev` (Alex Morgan) | `Demo123!` | Guest | A booking in almost every status: Confirmed and paid (Hillside, in 14 days), Reserved and unpaid (Maple Grove, in 30 days), PendingApproval with a message (Aurora), a cancelled request (Lavender), a declined request (Château), and past Completed stays. Also: a paid Aurora stay cancelled 10 days ahead under the Strict policy (50 % refunded, refund Succeeded); a finished Redwood Treehouse stay that can still be reviewed (`canReview: true`); the "Dream trips" wishlist; a paid Canal Food Tour experience in 5 days; a past surf lesson to review; three conversations with Sam (one unread reply); and unread notifications. |
| `host@lodge.dev` (Sam Rivera) | `Host123!` | Guest (hosting) | A **Superhost** with 8 listings. Three of them are request to book: Lavender Hill Cottage, Aurora Glass Igloo and Château des Vignes. He has an active (simulated) payout account with Paid and Scheduled payouts, pending requests from the demo user and Jamie, replies on 2 reviews and 2 host → guest reviews. He also hosts 4 experiences. The calendar shows Pine Ridge blocked from today+20 to today+23 and Hillside priced at $850 from today+60 to today+62. |
| `priya.patel@lodge.dev` (Priya Patel) | `Demo123!` | Guest (hosting) | Host of Canal Houseboat and Minimalist Lake House, plus 2 experiences. She has **no payout account**, so her payouts are OnHold. She also travels as a guest. |
| `jamie.lee@lodge.dev`, `lucas.martin@lodge.dev` | `Demo123!` | Guest | Other guests with past reviewed stays and upcoming bookings. Jamie has a pending Château request (unread by Sam), and Sam reviewed both of them as guests. |
| `admin@lodge.dev` | `Admin123!` | Admin | Can open **Manage bookings** (`/admin/bookings`) to reject or complete any booking, and can manage every listing. |

The listings use these cancellation policies:

| Policy | Listings |
| --- | --- |
| Flexible | Lavender Hill Cottage, Canal Houseboat, Redwood Treehouse |
| Moderate | Pine Ridge Cabin, Maple Grove Family Home, Château des Vignes, Minimalist Lake House |
| Strict | Seaside Family Retreat, Aurora Glass Igloo, Hillside Luxury Villa |

All 12 seeded stay reviews are published and have category ratings. The 6 experiences have no photos, so the frontend shows category placeholders.

The seeder doesn't send any e-mails.

To start over with fresh data:

```bash
docker compose down
docker volume rm lodge_postgres-data
docker compose up -d
```

## Option 2: run the API or the frontend from source

This is the usual inner loop: infrastructure runs in Docker, and the code you're editing runs on your machine with hot reload.

```bash
# 1. Infrastructure only
docker compose up -d postgres redis rabbitmq azurite seq mailpit

# 2. API on http://localhost:5000 (uses appsettings.Development.json → localhost connection strings)
dotnet run --project backend/src/Lodge.Api

# 3. Frontend on http://localhost:3000
cd frontend
cp .env.example .env.local
# Edit NEXT_PUBLIC_API_BASE_URL=http://localhost:5000 if you're not running the gateway
npm ci
npm run dev
```

> [!NOTE]
> `App:ApiPublicBaseUrl` defaults to the gateway (`http://localhost:8000`). Avatar URLs and the OAuth callback URLs are built from it. If you run the API without the gateway, set `App__ApiPublicBaseUrl=http://localhost:5000`.

Alternatively, run the whole backend in Docker and only the frontend from source:

```bash
docker compose up -d --build gateway   # starts the gateway, the API and its dependencies, but not the web container
cd frontend && npm run dev
```

## Configuration

Settings are bound to options classes and validated on startup, so a missing or invalid value stops the app with a clear error.

| Section | Keys | Notes |
| --- | --- | --- |
| `ConnectionStrings` | `Database`, `Cache`, `BlobStorage` | PostgreSQL, Redis (also the SignalR backplane) and Azure Blob Storage (Azurite locally). |
| `Jwt` | `Issuer`, `Audience`, `SecurityKey`, `TokenExpirationInMinutes` | `SecurityKey` must be ≥ 32 characters. **Override it outside local development.** |
| `App` | `FrontendBaseUrl`, `ApiPublicBaseUrl` | Browser-facing URLs. E-mail links (`/verify-email`, `/reset-password`, `/messages/…`), the social-login result (`/auth/callback`), the simulated checkout and the payout onboarding return URLs use `FrontendBaseUrl` (`http://localhost:3000`). OAuth redirect URIs and avatar URLs use `ApiPublicBaseUrl` (the gateway, `http://localhost:8000`). |
| `Authentication:External` | `Google:ClientId`, `Google:ClientSecret`, `GitHub:ClientId`, `GitHub:ClientSecret` | A provider is enabled when both values are set. See [Social login](#social-login-google-and-github). |
| `MessageBroker` | `HostName`, `Port`, `UserName`, `Password`, `QueueName` | RabbitMQ |
| `Mail` | `SmtpServer`, `SmtpPort`, `SmtpUserName`, `SmtpPassword`, `SenderEmail`, `SenderDisplayName` | If no SMTP server is set, e-mails are logged instead of sent. |
| `Payments` | `Mode`, `GuestServiceFeePercent`, `HostServiceFeePercent`, `TaxRates:<Country>`, `DefaultTaxRatePercent` | `Mode` is `Stripe`, `Simulated` or empty (see [Payments](#payments)). Fees default to 12 and 3 %. Tax rates by listing country default to Netherlands 12.5, France 5, United States 10, Portugal 4, Finland 10, New Zealand 0 and United Kingdom 0; other countries use `DefaultTaxRatePercent` (0). |
| `Currencies` | `Base`, `Rates:<Code>`, `UpdatedOnUtc` | Static display rates for `GET /currencies` and the USD price filter. The base is `USD`; the default rates are EUR 0.92, GBP 0.79, JPY 151.5, CHF 0.88, CAD 1.39 and AUD 1.53. |
| `Stripe` | `SecretKey`, `WebhookSecret`, `ConnectWebhookSecret`, `ReturnUrl` | Optional; see [Payments](#payments). `ConnectWebhookSecret` is the signing secret of a separate Connect webhook endpoint. `ReturnUrl` (`http://localhost:3000/bookings`) is where Stripe Checkout returns. |
| `Hosting:Superhost` | `MinCompletedStays`, `MinAverageRating`, `MaxCancellationRate` | 5, 4.8 and 0.10 by default. The cancellation rate (declined, rejected or host-cancelled bookings among the bookings the host accepted or declined) must be strictly below the maximum. |
| `Assets` | `PublicBaseUrl` | Browser-reachable base URL for `/images/...` (seeded listing images). |
| `BlobStorage` | `ContainerName` | `files` by default. |
| `Cors` | `AllowedOrigins` | `http://localhost:3000` by default. Enforced by the API, including the SignalR hub (with credentials); the gateway passes the CORS headers through. |
| `Database` | `ApplyMigrationsOnStartup`, `SeedDemoData` | Both on in Development. |
| `BackgroundJobs:*` | see [Background jobs](#background-jobs) | Hangfire cron expressions in UTC, plus a few job settings. |

The gateway has its own settings in `backend/src/Lodge.Gateway/appsettings.json`: `RateLimiting:PermitLimitPerMinute` (600) and the `ReverseProxy` routes and clusters.

In Docker Compose, settings are passed as environment variables (e.g. `Jwt__SecurityKey`). A `.env` file next to `docker-compose.yml` can set:

| Variable | Maps to | Default |
| --- | --- | --- |
| `JWT_SECURITY_KEY` | `Jwt:SecurityKey` | A development-only key. |
| `LODGE_PUBLIC_URL` | `App:ApiPublicBaseUrl`, `Assets:PublicBaseUrl` and the web image's `NEXT_PUBLIC_API_BASE_URL` | `http://localhost:8000` |
| `LODGE_FRONTEND_URL` | `App:FrontendBaseUrl` | `http://localhost:3000` |
| `PAYMENTS_MODE` | `Payments:Mode` | empty (automatic) |
| `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET` | `Stripe:SecretKey`, `Stripe:WebhookSecret` | empty |
| `STRIPE_CONNECT_WEBHOOK_SECRET` | `Stripe:ConnectWebhookSecret` | empty |
| `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` | `Authentication:External:Google:*` | empty |
| `GITHUB_CLIENT_ID`, `GITHUB_CLIENT_SECRET` | `Authentication:External:GitHub:*` | empty |

When running from source, use [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) for anything sensitive:

```bash
dotnet user-secrets --project backend/src/Lodge.Api set "Stripe:SecretKey" "sk_test_..."
```

### Background jobs

Hangfire stores its jobs in PostgreSQL. The dashboard is at <http://localhost:5000/hangfire> (Development only), where you can also trigger a job manually.

| Job id | What it does | Config key | Default schedule (UTC) |
| --- | --- | --- | --- |
| `outbox-processor` | Dispatches domain events from `outbox_messages` (also `BatchSize`, 20). | `BackgroundJobs:Outbox:Schedule` | `0/10 * * * * *` (every 10 s, 6-field cron) |
| `expire-booking-requests` | Moves unanswered requests (status 6) to 7 Expired, after `ExpiryHours` (24) or once the check-in date has passed. | `BackgroundJobs:ExpireBookingRequests:Schedule` | `*/15 * * * *` |
| `complete-finished-bookings` | Completes confirmed stays whose end date has passed. | `BackgroundJobs:CompleteBookings:Schedule` | `0 3 * * *` |
| `complete-past-experience-sessions` | Completes confirmed experience bookings whose session has ended. | `BackgroundJobs:Experiences:CompleteSchedule` | `15 3 * * *` |
| `publish-expired-reviews` | Publishes reviews whose 14-day window has closed (double-blind). | `BackgroundJobs:PublishReviews:Schedule` | `30 3 * * *` |
| `update-superhost-status` | Grants or revokes the Superhost badge. | `BackgroundJobs:Superhost:Schedule` | `0 4 * * *` |
| `pay-due-payouts` | Pays due payouts, or puts them on hold. | `BackgroundJobs:Payouts:Schedule` | `0 5 * * *` |
| `send-reminders` | Sends trip, arrival, review and experience reminders (notification + e-mail). | `BackgroundJobs:Reminders:Schedule` | `0 8 * * *` |
| `expire-unpaid-experience-holds` | Releases the spots of unpaid experience bookings older than `PaymentHoldMinutes` (60), or whose session has started. | `BackgroundJobs:Experiences:ExpireHoldsSchedule` | `*/15 * * * *` |

### E-mails (Mailpit)

Locally, every e-mail is captured by [Mailpit](https://mailpit.axllent.org/) at <http://localhost:8025>. This includes:

* verification and password-reset links;
* welcome and password-changed e-mails;
* booking e-mails;
* new-message and reminder e-mails, sent according to the notification preferences.

To verify a new account:

1. Register.
2. Open the newest message in Mailpit.
3. Click the `http://localhost:3000/verify-email?token=…` link (valid 24 h). Password-reset links (`/reset-password?token=…`) are valid for 1 h.

You can also call the API with the token directly (`POST /api/v1/authentication/verify-email`). Mailpit's JSON API (`GET http://localhost:8025/api/v1/messages`, `GET /api/v1/message/{ID}`) is handy in scripts and tests.

When running from source, `appsettings.Development.json` points SMTP at `localhost:1025`, which is Mailpit's SMTP port.

<a id="stripe-optional"></a>

### Payments

`Payments:Mode` decides how payments, refunds and payouts are processed:

| `Payments:Mode` | Effective mode |
| --- | --- |
| `Simulated` | Simulated, in any environment. |
| `Stripe` | Stripe. Without `Stripe:SecretKey`, checkout answers `400 Stripe.NotConfigured`. |
| empty (default) | `Stripe` when `Stripe:SecretKey` is set. Otherwise `Simulated` in Development and `Stripe` (not configured) elsewhere, so simulated payments must be enabled explicitly outside Development. |

**Simulated mode** needs no keys:

* **Pay & confirm** opens the frontend's `/checkout/simulated?bookingId=…` page, which calls `POST /api/v1/payments/simulated/{bookingId}/complete`.
* Refunds succeed within seconds.
* Payout onboarding activates the host's account immediately.
* `pay-due-payouts` marks due payouts as Paid with `sim_tr_…` ids.

**Stripe mode** (test keys):

1. Create a test-mode account and copy the secret key (`sk_test_...`).
2. For host payouts, enable **Connect** in the Stripe dashboard. Lodge creates **Express** accounts with the `transfers` capability and sends payouts as Transfers from the platform balance.
3. Forward webhooks with the [Stripe CLI](https://docs.stripe.com/stripe-cli). The `--forward-connect-to` flag also forwards events from connected accounts, which is how `account.updated` arrives:

   ```bash
   stripe listen \
     --events checkout.session.completed,checkout.session.async_payment_succeeded,account.updated \
     --forward-to http://localhost:8000/api/v1/stripe/webhook \
     --forward-connect-to http://localhost:8000/api/v1/stripe/webhook
   ```

4. Configure the keys. Use the `whsec_...` secret that `stripe listen` prints as the webhook secret.
   * **Docker:** set `STRIPE_SECRET_KEY` and `STRIPE_WEBHOOK_SECRET` in `.env` (and optionally `PAYMENTS_MODE=Stripe`), then run `docker compose up -d`.
   * **From source:** use user secrets as shown above.
5. Pay with test card `4242 4242 4242 4242`. The `checkout.session.completed` webhook moves the booking from *Reserved* to *Confirmed* and schedules the host payout.
6. As a host, start the payout onboarding from the host payouts page (`POST /api/v1/hosting/payouts/onboarding`) and complete Stripe's test onboarding. The `account.updated` webhook switches the payout account to *Active*, and payouts on hold are paid right away.

> [!TIP]
> Transfers need an *available* platform balance. In test mode, pay with `4000 0000 0000 0077`, which makes the funds available immediately, or the first transfers fail with an insufficient-funds error.

> [!IMPORTANT]
> With the Stripe CLI, one secret (`Stripe:WebhookSecret`) covers both platform and Connect events. In a real deployment, the Connect webhook endpoint (`account.updated`) has its own signing secret: set it as `Stripe:ConnectWebhookSecret` (`STRIPE_CONNECT_WEBHOOK_SECRET`). The webhook tries `WebhookSecret` first, then `ConnectWebhookSecret`.

Payments created in one mode keep using that mode's gateway (simulated ids start with `sim_`). Switching modes on a database with simulated payments is therefore safe.

### Social login (Google and GitHub)

Social login is optional. `GET /api/v1/authentication/providers` lists only the configured providers, so the frontend can show buttons for those alone. Register these **exact** callback URLs, which are built from `App:ApiPublicBaseUrl`, the gateway:

| Provider | Where | Callback / redirect URL |
| --- | --- | --- |
| Google | Google Cloud console → APIs & Services → Credentials → *OAuth client ID* (type *Web application*) → **Authorized redirect URIs** | `http://localhost:8000/api/v1/authentication/external/google/callback` |
| GitHub | GitHub → Settings → Developer settings → *OAuth Apps* → **Authorization callback URL** | `http://localhost:8000/api/v1/authentication/external/github/callback` |

Then set the credentials:

* **Docker:** `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GITHUB_CLIENT_ID` and `GITHUB_CLIENT_SECRET` in `.env`, then `docker compose up -d`.
* **From source:**

  ```bash
  dotnet user-secrets --project backend/src/Lodge.Api set "Authentication:External:Google:ClientId" "..."
  dotnet user-secrets --project backend/src/Lodge.Api set "Authentication:External:Google:ClientSecret" "..."
  dotnet user-secrets --project backend/src/Lodge.Api set "Authentication:External:GitHub:ClientId" "..."
  dotnet user-secrets --project backend/src/Lodge.Api set "Authentication:External:GitHub:ClientSecret" "..."
  ```

The scopes are `openid email profile` (Google) and `read:user user:email` (GitHub). The provider must report a **verified** e-mail address, otherwise the login ends with `#error=External.EmailNotVerified`.

If you change `LODGE_PUBLIC_URL` / `App:ApiPublicBaseUrl`, update the registered URLs too. The providers compare them exactly.

## Database migrations

EF Core tools are pinned in the local tool manifest (`.config/dotnet-tools.json`):

```bash
dotnet tool restore
dotnet ef migrations add <Name> -p backend/src/Lodge.Persistence -s backend/src/Lodge.Api
dotnet ef database update      -p backend/src/Lodge.Persistence -s backend/src/Lodge.Api
```

Tables and columns use snake_case (EFCore.NamingConventions). The migrations are `InitialCreate`, `AccountsAndTrust`, `HostingAndListings`, `PaymentsAndReviews` and `MessagingNotificationsExperiences`.

## Tests

```bash
dotnet test Lodge.slnx
```

| Project | Kind | Needs Docker |
| --- | --- | --- |
| `Lodge.Domain.UnitTests` | Domain rules and value objects | no |
| `Lodge.Application.UnitTests` | Handlers and pipeline behaviors, with NSubstitute | no |
| `Lodge.ArchitectureTests` | Layer dependency rules (NetArchTest) | no |
| `Lodge.Application.IntegrationTests` | Handlers against real PostgreSQL and Redis | **yes** (Testcontainers) |
| `Lodge.Api.FunctionalTests` | HTTP endpoints via `WebApplicationFactory` | **yes** (Testcontainers) |

To run only the fast tests:

```bash
dotnet test Lodge.slnx --filter "FullyQualifiedName!~IntegrationTests&FullyQualifiedName!~FunctionalTests"
```

Tests use xunit v3 and AwesomeAssertions.

Frontend checks:

```bash
cd frontend
npm run check      # lint + typecheck + production build
```

## Regenerating screenshots

The images in [`docs/images`](images) are produced by a Playwright script against a running stack seeded with demo data:

```bash
docker compose up -d --build
cd scripts/screenshots
npm install
npm run capture                   # all; or: node capture.mjs screenshots | gifs
```

By default the script uses the locally installed Microsoft Edge. Set `BROWSER_CHANNEL=chrome` to use Chrome, or run `npx playwright install chromium` and set `BROWSER_CHANNEL=chromium` to use Playwright's bundled browser.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| Integration tests fail with `Docker is either not running or misconfigured` | Start Docker Desktop. |
| Port already in use (5432, 6379, 3000, …) | Stop the local Postgres, Redis or dev server, or change the host port in `docker-compose.yml`. |
| Images or avatars don't load in the frontend | `Assets:PublicBaseUrl` and `App:ApiPublicBaseUrl` must be reachable from the browser, and `NEXT_PUBLIC_API_BASE_URL` must point at the same host. |
| CORS errors | The frontend must be served from an origin listed in `Cors:AllowedOrigins`. |
| `403 User.EmailNotVerified` when booking or hosting | Verify the account through the link in Mailpit, then call `POST /authentication/refresh-token` (or sign in again) so the token's `emailVerified` claim is updated. The seeded accounts are already verified. |
| No verification or reset e-mail | Check Mailpit at <http://localhost:8025>. When running from source, Mailpit must be running (`docker compose up -d mailpit`). Without `Mail:SmtpServer`, e-mails are only logged (see Seq). Resending is throttled to once per minute (`429`). |
| Social login redirects to `/auth/callback#error=External.ProviderNotConfigured` | Both the client id and the secret of that provider must be set. Restart the API after changing them. |
| Google shows `redirect_uri_mismatch` (GitHub: "redirect_uri is not associated") | The registered callback must be exactly `{App:ApiPublicBaseUrl}/api/v1/authentication/external/{google\|github}/callback`. |
| `#error=External.InvalidState` after social login | The correlation cookie was lost. Finish the login in the same browser within 10 minutes, and use the same host name (`localhost` vs `127.0.0.1`) for the gateway throughout. |
| Pay & confirm returns `Stripe.NotConfigured` | `Payments:Mode` is `Stripe` without a secret key. Set the keys, or set `PAYMENTS_MODE=Simulated`. |
| Stripe payment succeeded but the booking stays *Reserved* | The webhook didn't arrive: is `stripe listen` running, and does `Stripe:WebhookSecret` (or `Stripe:ConnectWebhookSecret` for Connect events) match its `whsec_…`? Check Seq for `Rejected Stripe webhook call`. |
| Host payout account stays *Pending* in Stripe mode | `account.updated` comes from the connected account. Run `stripe listen` with `--forward-connect-to` (see above). |
| Real-time updates don't arrive (no new messages or badges without a refresh) | See the SignalR rows below. Notifications and booking system messages arrive through the outbox, about 10 s after the action; only user messages are instant. |
| SignalR `negotiate` returns 401 | The client must send the access token: `accessTokenFactory` in `@microsoft/signalr`, which adds `?access_token=…`. The query string token is only accepted under `/hubs`. An expired token also gives 401, so reconnect with a fresh token. |
| SignalR `negotiate` returns 404 | The hub is at `{gateway}/hubs/notifications`, **not** under `/api/v1`. Check that the gateway's `lodge-hubs` route (`/hubs/{**catch-all}`, `Order: -1`) is present. |
| SignalR CORS error on `negotiate` | The page origin must be in `Cors:AllowedOrigins`. The hub uses credentials, so wildcard origins don't work. |
| SignalR falls back to long polling, or WebSockets close after about 2 minutes | Something between the browser and the gateway doesn't proxy WebSockets. The `lodge-hubs` cluster's `ActivityTimeout` (2 min) must stay above SignalR's keep-alive interval (15 s). |
| SignalR works with one API instance but not with several | Each instance needs the same Redis (`ConnectionStrings:Cache`, the backplane), and the gateway's `.Lodge.Hubs.Affinity` cookie must reach the browser so negotiate and connect hit the same instance. |
| Migration errors against an existing database | Migrations were squashed into `InitialCreate` at one point, so databases created before that need recreating. Remove the volume (see above). |
