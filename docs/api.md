# API reference

All endpoints are versioned under `/api/v1`. When running locally:

| | URL |
| --- | --- |
| Through the gateway (what the frontend uses) | `http://localhost:8000/api/v1/...` |
| Directly against the API | `http://localhost:5000/api/v1/...` |
| Interactive docs (Scalar) | <http://localhost:5000/scalar> |
| OpenAPI document | <http://localhost:5000/openapi/v1.json> |

The OpenAPI document is the source of truth. This page gives an overview.

## Authentication

1. Call `POST /authentication/register` or `POST /authentication/login`. Either returns `{ "token": "<jwt>" }`.
2. Send the token as `Authorization: Bearer <jwt>` on requests that need it.

The JWT contains the claims `userId`, `email`, `name`, `imageId` and `role` (`Guest` or `Admin`). Endpoints marked 🛡️ require the `Admin` role and return `403` for guests. Tokens expire after `Jwt:TokenExpirationInMinutes` minutes (60 by default).

Endpoints that take a `userId` in the route or body check it against the token. Using another user's id returns `403`.

## Endpoints

🔒 means a bearer token is required; 🛡️ means the caller must be an admin.

### Authentication

| Method | Route | Body | Notes |
| --- | --- | --- | --- |
| POST | `/authentication/register` | `{ firstName, lastName, email, password }` | Returns a token. |
| POST | `/authentication/login` | `{ email, password }` | Returns a token. A wrong e-mail or password returns `400 Authentication.InvalidEmailOrPassword`. |

### Apartments (public)

| Method | Route | Query | Notes |
| --- | --- | --- | --- |
| GET | `/apartments` | `page`, `pageSize` | Paged list. Every apartment includes `averageRating` (one decimal, `null` without reviews) and `reviewCount`. |
| GET | `/apartments/search` | `searchTerm`, `guestCount`, `roomCount`, `startDate`, `endDate`, `sortColumn`, `sortOrder`, `page`, `pageSize` | `searchTerm` matches country, city or name, ignoring case. `startDate` and `endDate` must be sent together; apartments booked in that range are excluded. `sortColumn` is one of `name`, `description`, `country`, `price_amount`, `currency` or `rating` (unreviewed apartments always last); `sortOrder` is `asc` or `desc`. |
| GET | `/apartments/{apartmentId}` | | |
| GET | `/apartments/{apartmentId}/price` | `startDate`, `endDate` (required) | Price breakdown: nights, amenity up-charge, cleaning fee, total. |
| GET | `/apartments/{apartmentId}/booked-dates` | | Dates that cannot be selected. |
| GET | `/apartments/{apartmentId}/reviews` | `page`, `pageSize` | Each review includes `authorName` (e.g. `Jamie L.`). |

Paging:

* `page` defaults to 1 and `pageSize` defaults to 10.
* `pageSize` can be at most 100, and `page` must be at least 1.
* Responses are shaped as `{ items, page, pageSize, totalCount, hasNextPage, hasPreviousPage }`.

### Bookings 🔒

| Method | Route | Body | Notes |
| --- | --- | --- | --- |
| POST | `/bookings` | `{ apartmentId, userId, startDate, endDate }` | Creates a booking with status *Reserved*. Overlapping bookings return `409`; a start date in the past returns `400`. |
| GET | `/bookings/{bookingId}` | | Only the booking's owner can read it. |
| PUT | `/bookings/{bookingId}/cancel` | `{ userId }` | Owner only. Allowed for *Reserved* or *Confirmed* bookings, until the start date. |
| GET 🛡️ | `/bookings?status=&page=&pageSize=` | | All bookings, newest first. `status` filters by status (1–5). |
| PUT 🛡️ | `/bookings/{bookingId}/reject` | — | *Reserved* → *Rejected*. Returns `204`. |
| PUT 🛡️ | `/bookings/{bookingId}/complete` | — | *Confirmed* → *Completed*, only after the end date. Returns `204`. A daily background job also does this automatically. |

A booking includes the price breakdown, apartment name and image, `guestName`, `guestEmail` and `hasReview`. `status` is an integer: `1` Reserved, `2` Confirmed, `3` Rejected, `4` Cancelled, `5` Completed.

The booking state machine is shown in [architecture.md](architecture.md#booking-lifecycle).

### Users 🔒

| Method | Route | Body | Notes |
| --- | --- | --- | --- |
| GET | `/users/auth` | | The current user, from the token, including `role`. |
| GET | `/users/{userId}` | | Own profile only. |
| PUT | `/users/{userId}` | `{ firstName, lastName, imageId }` | `imageId` must be a file uploaded by the same user. |
| PUT | `/users/{userId}/change-password` | `{ password }` | |
| GET | `/users/{userId}/bookings` | | The user's bookings. |

### Reviews 🔒

| Method | Route | Body |
| --- | --- | --- |
| POST | `/reviews` | `{ userId, bookingId, rating (1–5), comment }` |
| PUT | `/reviews/{reviewId}` | `{ userId, rating, comment }` |
| DELETE | `/reviews/{reviewId}` | |

You can only review a *Completed* booking that belongs to you.

### Files

| Method | Route | Notes |
| --- | --- | --- |
| POST 🔒 | `/files` | `multipart/form-data` with the fields `userId` and `file` (5 MB max). |
| GET | `/files/{fileId}` | Streams the blob. |
| DELETE 🔒 | `/files/{fileId}/users/{userId}` | |

### Payments (Stripe)

| Method | Route | Notes |
| --- | --- | --- |
| POST 🔒 | `/stripe/checkout` | `{ userId, bookingId }`. Returns the URL of a Stripe Checkout session for a *Reserved* booking you own. |
| POST | `/stripe/webhook` | Called by Stripe and verified with `Stripe-Signature`. When `checkout.session.completed` arrives with `payment_status = paid`, the booking moves to *Confirmed*. Other event types are acknowledged and ignored. |

Without Stripe keys (see [development.md](development.md#stripe-optional)), both Stripe endpoints return `400 Stripe.NotConfigured`.

### Health

| Route | Description |
| --- | --- |
| `GET /health` | Detailed JSON health report (PostgreSQL, Redis, blob storage, RabbitMQ, Hangfire). |
| `GET /health/live` | Liveness probe. |
| `GET /gateway/health` | The gateway's own probe. It is only available on port 8000. |

## Errors

Errors follow [RFC 9457 problem details](https://www.rfc-editor.org/rfc/rfc9457):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Booking.Overlap",
  "status": 409,
  "detail": "The current booking is overlapping with an existing one",
  "errors": []
}
```

For validation errors, `errors` lists every failed rule as `{ code, description, type }`.

## Idempotency

These endpoints need an `X-Idempotency-Key: <guid>` header:

* `POST /bookings`
* `POST /reviews`
* `POST /files`

If a request repeats a key that already succeeded, the API returns the stored result instead of running the command again. This makes retries after a network error safe. A key that failed can be reused.
