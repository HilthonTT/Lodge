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
| Gateway (public API entry point) | <http://localhost:8000> |
| API (direct) and Scalar docs | <http://localhost:5000/scalar> |
| Hangfire dashboard | <http://localhost:5000/hangfire> |
| Seq (structured logs) | <http://localhost:8081> |
| Mailpit (captured e-mails) | <http://localhost:8025> |
| RabbitMQ management | <http://localhost:15672> (guest / guest) |

The API runs in the `Development` environment. On startup it applies EF Core migrations and seeds demo data, but only if the demo user doesn't exist yet:

* 10 apartments
* 4 users
* past stays with reviews
* bookings in every status for the demo user

| Account | Password | Role |
| --- | --- | --- |
| `demo@lodge.dev` | `Demo123!` | Guest: has bookings in every status |
| `admin@lodge.dev` | `Admin123!` | Admin: can open **Manage bookings** (`/admin/bookings`) |

Three more guests (`jamie.lee@`, `priya.patel@` and `lucas.martin@lodge.dev`) use `Demo123!` as well.

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

Alternatively, run the whole backend in Docker and only the frontend from source:

```bash
docker compose up -d --build gateway   # starts the gateway, the API and its dependencies, but not the web container
cd frontend && npm run dev
```

## Configuration

Settings are bound to options classes and validated on startup, so a missing or invalid value stops the app with a clear error.

| Section | Keys | Notes |
| --- | --- | --- |
| `ConnectionStrings` | `Database`, `Cache`, `BlobStorage` | PostgreSQL, Redis and Azure Blob Storage (Azurite locally). |
| `Jwt` | `Issuer`, `Audience`, `SecurityKey`, `TokenExpirationInMinutes` | `SecurityKey` must be ≥ 32 characters. **Override it outside local development.** |
| `MessageBroker` | `HostName`, `Port`, `UserName`, `Password`, `QueueName` | RabbitMQ |
| `Mail` | `SmtpServer`, `SmtpPort`, `SmtpUserName`, `SmtpPassword`, `SenderEmail`, `SenderDisplayName` | If no SMTP server is set, e-mails are logged instead of sent. |
| `Stripe` | `SecretKey`, `WebhookSecret`, `ReturnUrl` | Optional, see below. |
| `Assets` | `PublicBaseUrl` | Browser-reachable base URL for `/images/...`. |
| `Cors` | `AllowedOrigins` | `http://localhost:3000` by default. Enforced by the API; the gateway passes the CORS headers through. |
| `Database` | `ApplyMigrationsOnStartup`, `SeedDemoData` | Both on in Development. |
| `BackgroundJobs:Outbox` | `Schedule` | Cron expression with seconds; `0/15 * * * * *` by default. |
| `BackgroundJobs:CompleteBookings` | `Schedule` | Cron expression (UTC) for completing finished stays; `0 3 * * *` (daily at 03:00) by default. |

In Docker Compose, these are set through environment variables, e.g. `Jwt__SecurityKey`. A `.env` file next to `docker-compose.yml` can set:

* `JWT_SECURITY_KEY`
* `STRIPE_SECRET_KEY`
* `STRIPE_WEBHOOK_SECRET`
* `LODGE_PUBLIC_URL`

When running from source, use [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) for anything sensitive:

```bash
dotnet user-secrets --project backend/src/Lodge.Api set "Stripe:SecretKey" "sk_test_..."
```

### Stripe (optional)

Without Stripe keys, everything works except **Pay & confirm**, which shows a "Stripe is not configured" message. To test payments locally:

1. Create a test-mode account and copy the secret key (`sk_test_...`).
2. Forward webhooks with the [Stripe CLI](https://docs.stripe.com/stripe-cli):

   ```bash
   stripe listen --forward-to http://localhost:8000/api/v1/stripe/webhook
   ```

3. Configure the keys. Use the `whsec_...` secret that `stripe listen` prints as the webhook secret.
   * **Docker:** set `STRIPE_SECRET_KEY` and `STRIPE_WEBHOOK_SECRET` in `.env`, then run `docker compose up -d`.
   * **From source:** use user secrets as shown above.
4. Pay with test card `4242 4242 4242 4242`. The webhook moves the booking from *Reserved* to *Confirmed*.

## Database migrations

EF Core tools are pinned in the local tool manifest (`.config/dotnet-tools.json`):

```bash
dotnet tool restore
dotnet ef migrations add <Name> -p backend/src/Lodge.Persistence -s backend/src/Lodge.Api
dotnet ef database update      -p backend/src/Lodge.Persistence -s backend/src/Lodge.Api
```

Tables and columns use snake_case (EFCore.NamingConventions).

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
| Images don't load in the frontend | `Assets:PublicBaseUrl` must be reachable from the browser, and `NEXT_PUBLIC_API_BASE_URL` must point at the same host. |
| CORS errors | The frontend must be served from an origin listed in `Cors:AllowedOrigins`. |
| Migration errors against an existing database | Migrations were squashed into a single `InitialCreate`, so databases created before that need recreating. Remove the volume (see above). |
