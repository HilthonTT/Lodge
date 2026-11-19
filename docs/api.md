# API reference

All endpoints are versioned under `/api/v1`, except the SignalR hub (`/hubs/notifications`) and the health probes. When running locally:

| | URL |
| --- | --- |
| Through the gateway (what the frontend uses) | `http://localhost:8000/api/v1/...` |
| Directly against the API | `http://localhost:5000/api/v1/...` |
| SignalR hub (through the gateway) | `http://localhost:8000/hubs/notifications` |
| Interactive docs (Scalar, Development only) | <http://localhost:5000/scalar> |
| OpenAPI document | <http://localhost:5000/openapi/v1.json> |

The OpenAPI document lists every route and request body. It doesn't describe response bodies, because the endpoints return `IResult`. This page fills that gap: it documents the response shapes, the enums, the error codes and the rules behind each endpoint.

**Contents**

* [Conventions](#conventions)
* [Authentication and accounts](#authentication-and-accounts)
* [Users and profiles](#users-and-profiles)
* [Apartments and search](#apartments-and-search)
* [Locations](#locations)
* [Wishlists](#wishlists)
* [Bookings](#bookings)
* [Payments](#payments)
* [Cancellations and refunds](#cancellations-and-refunds)
* [Reviews](#reviews)
* [Hosting](#hosting)
* [Messaging](#messaging)
* [Notifications](#notifications)
* [Real-time (SignalR)](#real-time-signalr)
* [Experiences](#experiences)
* [Files](#files)
* [Health](#health)
* [Enum reference](#enum-reference)

## Conventions

### Auth levels

The **Auth** column in the tables below uses these values:

| Value | Meaning |
| --- | --- |
| — | Anonymous. |
| Optional | Anonymous, but the response changes when a bearer token is sent. |
| User | `Authorization: Bearer <jwt>` is required. A missing or invalid token returns `401`. |
| Verified | A bearer token **and** a verified e-mail address. Otherwise `403 User.EmailNotVerified`. The API checks the database, not the token's `emailVerified` claim. |
| Admin | A bearer token with `role = Admin`. Other users get `403`. |
| Self | A bearer token whose user is the `{userId}` in the route, or the `userId` in the body. Otherwise `403 User.InvalidPermissions`. |

### Tokens

Access tokens are HMAC-SHA256 JWTs. They expire after `Jwt:TokenExpirationInMinutes` minutes (60 by default) and carry these claims:

* `sub` and `userId`
* `email`, `name`, `imageId`
* `role`: `Guest` or `Admin`
* `emailVerified`: `"true"` or `"false"`. This is a snapshot taken when the token was issued.

Call `POST /authentication/refresh-token` to get fresh claims, for example after verifying the e-mail address or changing the avatar.

### Paging

List endpoints that take `page` and `pageSize` return:

```json
{ "items": [], "page": 1, "pageSize": 10, "totalCount": 42, "hasNextPage": true, "hasPreviousPage": false }
```

`page` defaults to 1 and `pageSize` to 10. If `page` is below 1, the request fails with `Paging.PageMustBePositive`. If `pageSize` is outside 1–100, it fails with `Paging.PageSizeOutOfRange`.

Messages use a cursor instead of pages. See [Messaging](#messaging).

### Formats

* JSON uses camelCase.
* Dates (`yyyy-MM-dd`) are calendar dates. Fields ending in `Utc` are UTC timestamps.
* Money is a decimal amount with a separate currency code (`USD` or `EUR` for listings and experiences).
* Booking `status` is an **integer** (see [Booking statuses](#booking-statuses)). Every other enum is serialized **by name**.

### Idempotency

These commands need an `X-Idempotency-Key: <guid>` header:

* `POST /bookings`
* `POST /reviews`
* `POST /files`
* `POST /experience-bookings`

Generate one key per user action and reuse it to retry a request that failed or timed out:

* **Same key, same endpoint, after a success:** the first result is replayed with the same status code and body (for example the same booking id), and the command doesn't run again. The replay doesn't check the new request body.
* **Same key on another endpoint:** `409 Request.KeyReused`.
* **Same key while the first request is still running:** `409 Request.InProgress`. Retry after it has finished.
* **Failed commands** don't store the key, so a key whose command failed can be reused.
* **Missing header, or not a GUID (the empty GUID included):** `400 Request.InvalidIdempotencyKey`.

Keys recorded before results were stored (older databases) can't be replayed and still answer `409 Request.AlreadyProcessed`.

### Errors

Errors follow [RFC 9457 problem details](https://www.rfc-editor.org/rfc/rfc9457). `title` holds the error code:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Booking.Overlap",
  "status": 409,
  "detail": "The selected dates are not available",
  "traceId": "00-2184fa7a8ad72675fb531f0f750b4fd3-75909e3164bbc99d-02"
}
```

When a FluentValidation rule fails, the response is `400` with the title `Validation.General`. Each failed rule is listed in `errors` as `{ code, description, type }`, where `type` is the numeric error type (`1` = validation):

```json
{
  "title": "Validation.General",
  "status": 400,
  "detail": "One or more validation errors occured",
  "errors": [
    { "code": "SearchApartments.BothDatesRequired", "description": "Both the start date and the end date must be provided", "type": 1 }
  ]
}
```

Some domain rules return a single validation error directly, with `title` set to its own code (for example `400 Apartment.InvalidImage`). Clients should therefore check `title` first and then `errors[].code`.

| Error type | HTTP status |
| --- | --- |
| Validation, Problem | 400 |
| Permission | 403 |
| NotFound | 404 |
| Conflict | 409 |
| TooManyRequests | 429 |
| Unexpected (exception) | 500, with the title `Server failure` |
| Request binding failure | 400 (or the framework's status code, e.g. 413), with the title `Request.InvalidBody` for a missing or malformed JSON/form body and `Request.InvalidParameter` for a missing or wrongly typed route, query or header value (e.g. `/apartments/search?guestCount=abc`). `detail` names the parameter. |

* A missing or invalid JWT returns `401` with a generic problem body (`"title": "Unauthorized"`). Role failures return `403` the same way (`"title": "Forbidden"`).
* The gateway returns `429` (empty body) when an IP exceeds its rate limit (600 requests per minute by default). The SignalR hub is exempt.

The tables below list the important error codes for each section. The OpenAPI document and the `*Errors.cs` / `ValidationErrors.cs` files have the full catalogue.

## Authentication and accounts

| Method | Route | Auth | Body | Notes |
| --- | --- | --- | --- | --- |
| POST | `/authentication/register` | — | `{ firstName, lastName, email, password }` | `200 { token }`. The account starts **unverified**, and a verification e-mail is sent. |
| POST | `/authentication/login` | — | `{ email, password }` | `200 { token, requiresTwoFactor, twoFactorToken }`; see below. |
| POST | `/authentication/login/two-factor` | — | `{ twoFactorToken, code?, recoveryCode? }` | Send either `code` (TOTP) or `recoveryCode`. Returns `200 { token }`. |
| POST | `/authentication/refresh-token` | User | — | `200 { token }` with the current claims. |
| POST | `/authentication/verify-email` | — | `{ token }` | `204`. The token comes from the e-mail link `{App:FrontendBaseUrl}/verify-email?token=…` and is valid for 24 h. |
| POST | `/authentication/resend-verification` | User | — | `202`. At most one e-mail per 60 s (including the one sent at registration). |
| POST | `/authentication/forgot-password` | — | `{ email }` | Always `202`, whether or not the account exists. Sends `{App:FrontendBaseUrl}/reset-password?token=…`, which is valid for 1 h. |
| POST | `/authentication/reset-password` | — | `{ token, password }` | `204`. Sets the password (social-only accounts get one too) and marks the e-mail address as verified. |
| GET | `/authentication/providers` | — | — | `200 [{ name, displayName }]`. Lists only the configured social providers (`google`, `github`), so it is usually `[]` locally. |
| GET | `/authentication/external/{provider}?returnUrl=/path` | — | — | **Browser navigation, not XHR.** Sets an HTTP-only correlation cookie (10 min) and redirects (`302`) to Google or GitHub. |
| GET | `/authentication/external/{provider}/callback` | — | — | The OAuth redirect URI that is registered at the provider. Redirects (`302`) to the frontend; see below. |

**Login response.**

* Without 2FA, `token` is set, `requiresTwoFactor` is `false` and `twoFactorToken` is `null`.
* With 2FA, `token` is `null`, `requiresTwoFactor` is `true` and `twoFactorToken` is a 5-minute challenge token for `POST /authentication/login/two-factor`. After 5 wrong codes, the challenge token is rejected.

**Social login.**

1. The frontend navigates to `{gateway}/api/v1/authentication/external/{provider}?returnUrl=/trips`. `returnUrl` must be a relative path that starts with a single `/` and defaults to `/`.
2. After the provider redirects back, the callback redirects to `{App:FrontendBaseUrl}/auth/callback#...`. The result is in the URL fragment, so it never reaches server logs:
   * `#token=<jwt>&returnUrl=<encoded path>`
   * `#twoFactorToken=<token>&returnUrl=<encoded path>`, when the user has 2FA
   * `#error=<code>`
3. The user is resolved in this order:
   1. by an existing linked login;
   2. otherwise by the same e-mail address, which must be verified by the provider **and** by Lodge (the login is then linked);
   3. otherwise a new user is created with a verified e-mail address and no password.

| Errors | |
| --- | --- |
| `400 Authentication.InvalidEmailOrPassword` | Wrong credentials. Also returned for social-only accounts, which have no password. |
| `400 Authentication.InvalidTwoFactorToken` | The challenge token is expired, invalid, or has had 5 failed attempts. |
| `400 Authentication.InvalidTwoFactorCode` | Wrong TOTP or recovery code. |
| `400 Authentication.InvalidToken` | The verification or reset token is unknown, used or expired. |
| `409 User.DuplicateEmail` | Registering an e-mail address that's already taken. |
| `400 User.EmailAlreadyVerified` / `429 User.VerificationEmailThrottled` | Resending verification. |
| `400 Password.TooShort` (and `Empty`, `MissingUppercaseLetter`, `MissingLowercaseLetter`, `MissingDigit`, `MissingNonAlphaNumeric`) | Password policy: at least 6 characters with an upper-case letter, a lower-case letter, a digit and a symbol. |
| Validation: `CreateUser.*`, `Login.*`, `LoginTwoFactor.CodeIsRequired`, `LoginTwoFactor.TwoFactorTokenIsRequired`, `ForgotPassword.EmailIsRequired` | |
| `#error=` codes | `External.ProviderNotConfigured`, `External.InvalidReturnUrl`, `External.InvalidState`, `External.AccessDenied`, `External.ProviderError`, `External.EmailNotVerified` (the provider didn't confirm the e-mail address), `External.EmailInUse` (an unverified Lodge account already uses the e-mail address), `User.NotFound`. |

## Users and profiles

| Method | Route | Auth | Body | Notes |
| --- | --- | --- | --- | --- |
| GET | `/users/auth` | User | — | The current user (`UserResponse`). |
| GET | `/users/{userId}` | Self | — | `UserResponse`. |
| PUT | `/users/{userId}` | Self | `{ firstName, lastName, imageId, bio? }` | `204`. `imageId` must be a file uploaded by the same user. A `null` `bio` leaves it unchanged, `""` clears it, and it can be at most 500 characters. |
| PUT | `/users/{userId}/change-password` | Self | `{ currentPassword, password }` | `204`. A wrong current password gives `400 User.InvalidCurrentPassword`. Social-only accounts get `400 User.PasswordNotSet` and must use forgot-password. |
| GET | `/users/{userId}/profile` | — | — | Public profile; see below. |
| POST | `/users/{userId}/avatar` | Self | multipart `file` | JPEG, PNG or WebP, at most 5 MB. The **file content** is checked (magic bytes), not just the declared type. Returns `200 { avatarUrl }`. |
| DELETE | `/users/{userId}/avatar` | Self | — | `204`. |
| POST | `/users/{userId}/two-factor/setup` | Self | — | `200 { secret, otpauthUri }` (Base32 secret). 2FA isn't on yet; calling this again replaces the pending secret. |
| POST | `/users/{userId}/two-factor/enable` | Self | `{ code }` | `200 { recoveryCodes }`: 10 codes formatted `xxxxx-xxxxx`, shown once. |
| POST | `/users/{userId}/two-factor/disable` | Self | `{ password, code }` | `204`. `password` is ignored for accounts without one. |
| POST | `/users/{userId}/two-factor/recovery-codes` | Self | `{ code }` | `200 { recoveryCodes }`. The old codes stop working. |
| GET | `/users/{userId}/bookings` | Self | — | The user's stays, as `BookingResponse[]`. |
| GET / PUT | `/users/{userId}/notification-preferences` | Self | see [Notifications](#notifications) | |
| GET | `/users/{userId}/experience-bookings` | Self | — | See [Experiences](#guest-endpoints). |

`UserResponse`:

```text
{ id, email, firstName, lastName, imageId|null, createdOnUtc, role: "Guest"|"Admin",
  emailVerified, twoFactorEnabled, hasPassword, externalLogins: ["google"|"github"],
  avatarUrl|null, bio|null }
```

`avatarUrl` is absolute: `{App:ApiPublicBaseUrl}/api/v1/files/{imageId}`.

`UserProfileResponse` (public):

```text
{ id, firstName, lastInitial, avatarUrl|null, bio|null, joinedOnUtc, emailVerified, reviewsWrittenCount,
  isSuperhost, hostingSince|null, listings: ApartmentResponse[] (max 12), hostReviewCount, hostAverageRating|null,
  guestReviewCount, guestAverageRating|null,
  guestReviews: [{ id, rating, comment, hostFirstName, hostAvatarUrl|null, listingName, createdOnUtc }] (latest 10) }
```

| Errors | |
| --- | --- |
| `403 User.InvalidPermissions` | `{userId}` isn't the caller. |
| `404 User.NotFound`, `404 Blob.NotFound` | Unknown user, or an `imageId` that isn't a file of this user. |
| `409 TwoFactor.AlreadyEnabled` | Setup or enable when 2FA is already on. |
| `400 TwoFactor.NotSetUp`, `TwoFactor.NotEnabled`, `TwoFactor.InvalidCode`, `TwoFactor.InvalidPassword` | |
| `400 User.InvalidAvatar` | Not a JPEG/PNG/WebP image, or no `file`. Returned as the title, or as a validation code together with `User.AvatarTooLarge`. |
| Validation: `User.BioTooLong`, `UpdateUser.*`, `FirstName.*`, `LastName.*` | |

## Apartments and search

| Method | Route | Auth | Query | Notes |
| --- | --- | --- | --- | --- |
| GET | `/apartments` | — | `page`, `pageSize` | Listed apartments only. |
| GET | `/apartments/search` | — | see below | Paged `ApartmentResponse` list, listed apartments only. |
| GET | `/apartments/price-range` | — | — | `{ min, max, currency: "USD" }`, converted to USD and rounded outward to whole units. |
| GET | `/apartments/{apartmentId}` | Optional | — | `404` when the listing is unlisted or deleted. The owner (or an admin) gets the `HostListingResponse` instead. |
| GET | `/apartments/{apartmentId}/price` | — | `startDate`, `endDate` (required) | Price breakdown; see below. Minimum and maximum nights are **not** checked here; booking checks them. |
| GET | `/apartments/{apartmentId}/calendar` | — | `from`, `to` | `[{ date, available, price }]`. Defaults to today plus 90 days; at most 366 days. |
| GET | `/apartments/{apartmentId}/booked-dates` | — | — | `DateTime[]` of every unavailable night: bookings, pending requests and host-blocked nights. |
| GET | `/apartments/{apartmentId}/reviews` | Optional | `page`, `pageSize` | Published reviews, plus the caller's own unpublished review. See [Reviews](#reviews). |

### Search parameters

| Parameter | Notes |
| --- | --- |
| `searchTerm` | Matches the name, city, state or country, ignoring case and accents. |
| `startDate`, `endDate` | Must be sent together. Apartments with unavailable nights in that range are excluded. |
| `flexible` = `weekend` \| `week` \| `month` | Flexible dates: 2 nights from a Friday, 7 nights, or 28 nights. Can't be combined with exact dates. |
| `months` = `2026-12,2027-01` | Only together with `flexible`; at most 12 months. Defaults to the current month and the next two. A listing matches when it has a free window that starts in one of those months. |
| `guestCount`, `bedrooms`, `beds`, `bathrooms` | Minimums. `bedrooms` replaces the old `roomCount`. |
| `minPrice`, `maxPrice` | Nightly base price, compared in **USD**. |
| `amenities` | Repeat the parameter or comma-separate the values. A listing must have **all** of them. |
| `propertyTypes` | Repeat the parameter or comma-separate the values. A listing must match **any** of them. |
| `instantBook`, `superhost` | Booleans. |
| `minLat`, `maxLat`, `minLng`, `maxLng` | Map bounds. `minLng > maxLng` is treated as a box that crosses the antimeridian. |
| `sortColumn` | `name` (default), `rating` (unreviewed listings last), `price` / `price_amount` (in USD), `newest`, `country`, `description` or `currency`. |
| `sortOrder` | `asc` or `desc`. |
| `page`, `pageSize` | See [Paging](#paging). |

### `ApartmentResponse`

```text
{ id, name, description, propertyType, price (base nightly), currency, cleaningFee,
  weekendPrice|null, weeklyDiscountPercent, monthlyDiscountPercent,
  imageUrl (cover = images[0]), images: [{ id, url }],
  bedrooms, beds, bathrooms (0.5 steps), maximumGuestCount, latitude, longitude, amenities: string[],
  checkInTime "HH:mm", checkOutTime "HH:mm", petsAllowed, smokingAllowed, partiesAllowed, houseRules|null,
  instantBook, cancellationPolicy, minNights, maxNights,
  averageRating|null, reviewCount,
  ratingBreakdown: { cleanliness, accuracy, checkIn, communication, location, value } | null,
  address: { country, state, zipCode, city, street },
  host: { id, firstName, avatarUrl|null, isSuperhost, joinedOnUtc } }
```

`HostListingResponse` adds `checkInInstructions|null`, `isListed`, `createdOnUtc` and `modifiedOnUtc|null`.

Averages count published reviews only. `averageRating` has one decimal and is `null` without reviews.

### Price breakdown

```text
{ currency, nights, pricePerPeriod, averageNightlyPrice, discountAmount, discountType: "weekly"|"monthly"|null,
  cleaningFee, amenitiesUpCharge, serviceFee, taxes, taxRatePercent, totalPrice }
```

1. **Nightly price.** Each night costs the host's price override for that date if there is one, otherwise `weekendPrice` on Friday and Saturday nights, otherwise the base price. `pricePerPeriod` is the sum of the nights.
2. **Discount.** Stays of 28 nights or more get the monthly discount, and stays of 7 nights or more get the weekly discount. Only the larger discount that applies is used.
3. **Amenity up-charge.** A percentage of the discounted nights.
4. **Stay subtotal.** Nights − discount + up-charge + cleaning fee.
5. **`serviceFee`.** `Payments:GuestServiceFeePercent` (12 %) of the stay subtotal.
6. **`taxes`.** `taxRatePercent` × (nights − discount). The rate depends on the listing's country (`Payments:TaxRates`: Netherlands 12.5, France 5, United States 10, Portugal 4, Finland 10, others 0).
7. **`totalPrice`.** The stay subtotal + service fee + taxes.

Every amount is rounded to cents.

| Errors | |
| --- | --- |
| `404 Apartment.NotFound` | |
| Validation: `SearchApartments.*` | `BothDatesRequired`, `StartDateMustBeLessThanEndDate`, `InvalidAmenity`, `InvalidPropertyType`, `NegativeFilter`, `InvalidPriceRange`, `InvalidBounds`, `InvalidFlexible`, `FlexibleWithExactDates`, `InvalidMonths`, `MonthsRequireFlexible`. |
| Validation: `CalculatePrice.StartDateMustBeLessThanEndDate`, `CalculatePrice.TooManyNights` (> 365) | |
| Validation: `Apartment.InvalidCalendarRange` | The calendar range is reversed or longer than 366 days. |

## Locations

| Method | Route | Auth | Query | Notes |
| --- | --- | --- | --- | --- |
| GET | `/locations/suggest` | — | `q` (2–100 characters) | Up to 8 suggestions built from listed apartments: `[{ label: "Rovaniemi, Finland", city, country, latitude, longitude, listingCount }]`. Validation: `Locations.InvalidQuery`. |

## Wishlists

Every route needs **User**. Another user's wishlist returns `404 Wishlist.NotFound`.

| Method | Route | Body | Notes |
| --- | --- | --- | --- |
| GET | `/wishlists` | — | `[{ id, name, itemCount, coverImageUrl\|null, updatedOnUtc }]` |
| POST | `/wishlists` | `{ name }` | `201 { id }`. A user can have at most 50 wishlists. |
| GET | `/wishlists/saved-apartment-ids` | — | `uuid[]` of every saved apartment, for the heart icons. |
| GET | `/wishlists/{wishlistId}` | — | `{ id, name, items: ApartmentResponse[] }` |
| PUT | `/wishlists/{wishlistId}` | `{ name }` | `204` (rename). |
| DELETE | `/wishlists/{wishlistId}` | — | `204` |
| PUT | `/wishlists/{wishlistId}/items/{apartmentId}` | — | `204`. Idempotent. |
| DELETE | `/wishlists/{wishlistId}/items/{apartmentId}` | — | `204`. Idempotent. |

| Errors | |
| --- | --- |
| `404 Wishlist.NotFound`, `404 Apartment.NotFound` | |
| `409 Wishlist.LimitReached` | |
| Validation: `Wishlist.NameRequired`, `Wishlist.NameTooLong` (> 50 characters) | |

## Bookings

| Method | Route | Auth | Body | Notes |
| --- | --- | --- | --- | --- |
| POST | `/bookings` | Verified + `X-Idempotency-Key` | `{ apartmentId, userId, startDate, endDate, guestCount, messageToHost? }` | `200 "<bookingId>"` (a bare JSON string). Instant-book listings create status **1 Reserved**; request-to-book listings create **6 PendingApproval**. `messageToHost` can be at most 1,000 characters and is also posted to the conversation. |
| GET | `/bookings/{bookingId}` | User | — | Only the guest or the listing's host can read it. |
| PUT | `/bookings/{bookingId}/dates` | User (guest) | `{ startDate, endDate }` | `204`. Only for status 6 or 1. The price is recalculated and the host is notified. |
| GET | `/bookings/{bookingId}/cancellation-quote` | User (guest) | — | See [Cancellations and refunds](#cancellations-and-refunds). |
| PUT | `/bookings/{bookingId}/cancel` | User (guest) | `{ userId }` | `204`. For status 6, 1 or 2, until the check-in date. |
| GET | `/bookings?status=&page=&pageSize=` | Admin | — | All bookings, newest first. `status` is 1–7. |
| PUT | `/bookings/{bookingId}/reject` | Admin | — | `204`. 1 or 6 → **3 Rejected**. |
| PUT | `/bookings/{bookingId}/complete` | Admin | — | `204`. 2 → **5 Completed**, only after the end date. The daily `complete-finished-bookings` job does this automatically. |

The host side (accept, decline, cancel) is under [Hosting → Reservations](#reservations). The state machine is in [architecture.md](architecture.md#booking-lifecycle).

### `BookingResponse`

```text
{ id, apartmentId, userId, status: 1..7, createdOnUtc,
  durationStart, durationEnd, guestCount, messageToHost|null,
  apartmentName, apartmentImageUrl, listingHostId, guestName, guestEmail,
  priceAmount (nights before discount), priceCurrency, discountAmount,
  amenitiesUpChargeAmount, amenitiesUpChargeCurrency, cleaningFeeAmount, cleaningFeeCurrency,
  serviceFeeAmount, taxAmount, taxRatePercent, totalPriceAmount, totalPriceCurrency,
  cancellationPolicy, amountPaid|null, paidOnUtc|null,
  refundAmount|null, refundStatus, cancelledByHost,
  checkInInstructions|null, hasReview, hasGuestReview, canReview, reviewDeadline|null,
  requestExpiresOnUtc|null }
```

* `totalPriceAmount` = `priceAmount − discountAmount + amenitiesUpChargeAmount + cleaningFeeAmount + serviceFeeAmount + taxAmount`.
* `checkInInstructions` is filled in only for the listing's host, or for the guest once the booking is **2 Confirmed**.
* `canReview` means "may review the listing" for the guest and "may review the guest" for the host. `reviewDeadline` is check-out + 14 days.
* `requestExpiresOnUtc` is set only for status 6. It is the earlier of creation + `BackgroundJobs:ExpireBookingRequests:ExpiryHours` (24 h) and the end of the check-in day.

| Errors | |
| --- | --- |
| `403 User.EmailNotVerified` | Booking with an unverified e-mail address. |
| `403 User.InvalidPermissions` | Another user's booking, or a `userId` that isn't the caller. |
| `404 Apartment.NotFound`, `404 Booking.NotFound` | |
| `409 Booking.Overlap` | The dates overlap another active booking or a host-blocked night. |
| `400 Booking.StartDateInPast`, `Booking.StayTooShort`, `Booking.StayTooLong` | Checked against the listing's `minNights` and `maxNights`. |
| `400 Booking.InvalidGuestCount`, `Booking.OwnListing` | |
| `409 Booking.ChangeRequiresCancellation` | Changing the dates of a confirmed booking; the guest has to cancel and rebook. |
| `400 Booking.NotModifiable`, `Booking.NotCancellable`, `Booking.AlreadyStarted` | |
| `400 Booking.NotReserved`, `Booking.NotConfirmed`, `Booking.NotEnded` | Admin reject and complete. |
| `400 Request.InvalidIdempotencyKey` | The `X-Idempotency-Key` header is missing or isn't a GUID. |
| `409 Request.KeyReused`, `409 Request.InProgress` | See [Idempotency](#idempotency). A retry with the key of a successful booking returns the same booking id. |
| Validation: `ReserveBooking.*` (including `GuestCountIsRequired`), `Booking.MessageTooLong`, `ChangeBookingDates.StartDateMustBeLessThanEndDate`, `GetBookings.InvalidStatus` | |

## Payments

Payments run in one of two modes, reported by `GET /payments/config`:

* **Simulated** (the default in Development without Stripe keys). Checkout, refunds, Connect onboarding and payouts succeed immediately without calling Stripe. Simulated ids start with `sim_`.
* **Stripe.** Uses Stripe Checkout, Refunds, Connect Express accounts and Transfers.

The mode is resolved from `Payments:Mode`; see [development.md](development.md#payments). An existing payment, refund or payout is always handled by the gateway that created it, whatever the current mode.

| Method | Route | Auth | Body | Notes |
| --- | --- | --- | --- | --- |
| GET | `/payments/config` | — | — | `{ mode: "Stripe"\|"Simulated" }` |
| GET | `/currencies` | — | — | `{ base: "USD", rates: { USD: 1, EUR: 0.92, GBP: 0.79, JPY: 151.5, CHF: 0.88, CAD: 1.39, AUD: 1.53 }, updatedOnUtc }`. These are static display rates from `Currencies:*`. To convert, use `amount ÷ rates[from] × rates[to]`. |
| POST | `/stripe/checkout` | Self | `{ userId, bookingId }` | `200 { url }` for a **1 Reserved** booking of the caller. Stripe mode returns a Checkout URL; Simulated mode returns `{App:FrontendBaseUrl}/checkout/simulated?bookingId={id}`. |
| POST | `/payments/simulated/{bookingId}/complete` | User (guest) | — | `204`. Simulated mode only. Confirms the booking exactly as the Stripe webhook would. |
| POST | `/stripe/webhook` | Stripe signature | raw Stripe event | `204`. Not in the OpenAPI document. |

**Webhook events.** The webhook is verified with the `Stripe-Signature` header, first against `Stripe:WebhookSecret` and then against the optional `Stripe:ConnectWebhookSecret`, so platform and Connect events can come from two Stripe endpoints. It is idempotent because Stripe retries deliveries. It handles these events:

| Event | Effect |
| --- | --- |
| `checkout.session.completed` with `payment_status = paid`, or `checkout.session.async_payment_succeeded` | Records the payment intent and the amount paid, moves the booking from 1 to **2 Confirmed** and schedules the host payout. Experience bookings are confirmed the same way, matched through the session metadata. |
| `account.updated` (Connect) | Updates the host's payout account. When payouts become enabled, the account turns **Active** and payouts that are on hold are paid right away. |
| Anything else | Acknowledged and ignored. |

Confirming a booking also posts a `BookingConfirmed` system message and sends notifications to both parties.

| Errors | |
| --- | --- |
| `400 Stripe.NotConfigured` | Stripe mode without `Stripe:SecretKey` (checkout), or neither `Stripe:WebhookSecret` nor `Stripe:ConnectWebhookSecret` (webhook). |
| `400 Stripe.BookingNotPayable` | The booking isn't 1 Reserved. |
| `400 Payments.NotSimulated` | Calling a simulated endpoint in Stripe mode. |
| `400 Stripe.SignatureMissing`, `Stripe.InvalidSignature`, `Stripe.InvalidMetadata` | Webhook. |
| `403 User.InvalidPermissions`, `404 Booking.NotFound` | |

## Cancellations and refunds

| Method | Route | Auth | Notes |
| --- | --- | --- | --- |
| GET | `/bookings/{bookingId}/cancellation-quote` | User (guest) | `{ policy, refundPercent, refundAmount, nonRefundableAmount, currency, timeline: [{ until: "yyyy-MM-dd"\|null, refundPercent }], isPaid }`. Unpaid bookings (status 6 or 1) return `isPaid: false` with both amounts set to 0. |
| PUT | `/bookings/{bookingId}/cancel` | User (guest) | Guest cancellation; refunds follow the policy. |
| PUT | `/hosting/bookings/{bookingId}/cancel` | Verified (owner) | Host cancellation of a **2 Confirmed** booking before check-in. The guest gets a 100 % refund, and the cancellation counts against the host's Superhost cancellation rate. |

**Policies.** A listing's `cancellationPolicy` is copied onto every booking. The refund depends on the number of days from the cancellation date to the check-in date:

| Policy | Refund |
| --- | --- |
| `Flexible` | ≥ 1 day: 100 %; otherwise 0 % |
| `Moderate` | ≥ 5 days: 100 %; otherwise 50 % |
| `Strict` | ≥ 14 days: 100 %; 7–13 days: 50 %; otherwise 0 % |

The percentage applies to the nights after the discount plus the amenity up-charge:

* The cleaning fee is always refunded.
* The guest service fee is refunded only with a 100 % refund.
* Taxes are refunded in proportion.

The host's payout shrinks to the part of the stay that isn't refunded, minus the host fee, or is cancelled when nothing is left.

**Refund lifecycle.** `refundStatus` is one of these values:

| Value | Meaning |
| --- | --- |
| `None` | Nothing to refund: the booking is unpaid or not cancelled, or the refund is 0 %. |
| `Pending` | The refund was requested from the payment provider. It changes to `Succeeded` or `Failed` asynchronously, usually within seconds (outbox → RabbitMQ → refund processor). |
| `Succeeded` | Refunded. The guest gets a `RefundSucceeded` notification. |
| `Failed` | The provider call failed. The error is logged; there is no automatic retry. |
| `Manual` | The booking was paid before payment tracking existed (no payment intent), so it has to be refunded by hand. |

| Errors | |
| --- | --- |
| `400 Booking.NotCancellable` | The status isn't 6, 1 or 2. |
| `400 Booking.AlreadyStarted` | Cancelling on or after the check-in date. |
| `400 Booking.NotConfirmed` | Host cancellation of a booking that isn't confirmed. |
| `403 User.InvalidPermissions`, `403 Apartment.NotOwner`, `404 Booking.NotFound` | |

## Reviews

Reviews are **two-way and double-blind**:

* The guest reviews the listing, with category ratings.
* The host reviews the guest.
* A review stays visible only to its author until **both** sides have reviewed the stay, or until the 14-day window after check-out closes (the daily `publish-expired-reviews` job). Then it is published.

Only published reviews count towards averages. A published review can no longer be edited, but the host can reply to it.

| Method | Route | Auth | Body | Notes |
| --- | --- | --- | --- | --- |
| POST | `/reviews` | Self + `X-Idempotency-Key` | `{ userId, bookingId, rating, comment, categories: { cleanliness, accuracy, checkIn, communication, location, value } }` | `200 "<reviewId>"`. The guest's own **5 Completed** booking, within 14 days of check-out, one review per booking. `rating` and every category are 1–5. `comment` can be at most 500 characters. |
| PUT | `/reviews/{reviewId}` | Self | `{ userId, rating, comment, categories }` | `204`. Only while unpublished. |
| DELETE | `/reviews/{reviewId}` | User (author) | — | `204`. |
| GET | `/apartments/{apartmentId}/reviews` | Optional | — | Paged `ReviewResponse` list. |
| POST | `/hosting/bookings/{bookingId}/guest-review` | Verified (owner) | `{ rating, comment }` | `201 { id }`. Host → guest review. It appears on the guest's public profile once published. |
| GET | `/hosting/reviews?listingId=&page=&pageSize=` | Verified | — | Published reviews of the caller's listings. |
| PUT | `/hosting/reviews/{reviewId}/reply` | Verified (owner) | `{ text }` (1–1,000 characters) | `204`. Creates or edits the reply. The guest is notified. |
| DELETE | `/hosting/reviews/{reviewId}/reply` | Verified (owner) | — | `204` |

`ReviewResponse`:

```text
{ id, apartmentId, listingName, bookingId, userId, rating,
  categories: { cleanliness, accuracy, checkIn, communication, location, value },
  comment, createdOnUtc, modifiedOnUtc|null, authorName ("Jamie L."), authorAvatarUrl|null,
  hostReply: { text, createdOnUtc, modifiedOnUtc|null } | null, isPublished }
```

| Errors | |
| --- | --- |
| `403 Review.NotEligible` | The booking isn't completed. |
| `400 Booking.ReviewWindowClosed` | More than 14 days after check-out. |
| `400 Review.AlreadyReviewed` | |
| `409 Review.AlreadyPublished` | Editing a published review. |
| `400 Review.NotPublished` | Replying to an unpublished review. |
| `404 Review.NotFound`, `403 User.InvalidPermissions`, `403 Apartment.NotOwner` | |
| Validation: `Review.InvalidCategoryRating`, `Review.ReplyEmpty`, `Review.ReplyTooLong`, `CreateReview.*`, `UpdateReview.*` | |

## Hosting

Any user with a verified e-mail address can host. Every `/hosting/*` route needs **Verified**: an unverified caller gets `403 User.EmailNotVerified`, and another host's resource returns `403 Apartment.NotOwner` (`Experience.NotHost` for experiences). Admins can manage every listing and experience. Validation runs before the ownership check.

### Listings and photos

| Method | Route | Body | Notes |
| --- | --- | --- | --- |
| POST | `/hosting/photos` | multipart `file` | JPEG, PNG or WebP, at most 10 MB, checked by magic bytes. Returns `200 { fileId, url }`. Use `fileId` in `imageIds`. |
| GET | `/hosting/listings?page=&pageSize=` | — | Paged `{ id, name, propertyType, city, country, imageUrl, price, currency, isListed, instantBook, averageRating\|null, reviewCount, upcomingBookingCount, pendingRequestCount, createdOnUtc, modifiedOnUtc\|null }`. |
| GET | `/hosting/listings/{apartmentId}` | — | `HostListingResponse` |
| POST | `/hosting/listings` | `ListingRequest` | `201 { id }` |
| PUT | `/hosting/listings/{apartmentId}` | `ListingRequest` | `204` |
| PUT | `/hosting/listings/{apartmentId}/status` | `{ isListed }` | `204`. Unlisted listings disappear from search, but existing bookings stay. |
| DELETE | `/hosting/listings/{apartmentId}` | — | `204` (soft delete). Fails with `409 Apartment.HasUpcomingBookings` while there are bookings in status 6, 1 or 2. |

`ListingRequest`:

| Field | Rules |
| --- | --- |
| `name` / `description` | Required; at most 256 / 1,000 characters. |
| `propertyType` | See [Property types](#property-types). |
| `address` | `{ country, state, zipCode, city, street }`. `country`, `city` and `street` are required; each field can be at most 200 characters. |
| `latitude`, `longitude` | Required, valid coordinates. |
| `price`, `currency`, `cleaningFee`, `weekendPrice?` | `price` > 0; `currency` is `USD` or `EUR`; `cleaningFee` ≥ 0. |
| `weeklyDiscountPercent`, `monthlyDiscountPercent` | 0–90. |
| `bedrooms`, `beds`, `bathrooms`, `maximumGuestCount` | 0–50, 1–100, 0.5 steps, 1–50. |
| `amenities` | See [Amenities](#amenities). |
| `checkInTime`, `checkOutTime` | `"HH:mm"`; `"15:00"` / `"11:00"` by default. |
| `petsAllowed`, `smokingAllowed`, `partiesAllowed`, `houseRules?`, `checkInInstructions?` | The texts can be at most 2,000 characters. `checkInInstructions` is shown to guests only once their booking is confirmed. |
| `instantBook` | `true` by default. With `false`, guests send a request (status 6). |
| `cancellationPolicy` | `Flexible`, `Moderate` (default) or `Strict`. |
| `minNights`, `maxNights` | 1 and 365 by default; at most 365. |
| `imageIds` | 1–20 file ids, in display order. The first one is the cover. |
| `isListed?` | |

Listing validation codes: `Name.*`, `Description.*`, `Listing.InvalidPropertyType`, `Listing.InvalidAddress`, `Listing.InvalidCurrency`, `Listing.InvalidAmenity`, `Listing.InvalidTime`, `Apartment.InvalidCoordinates`, `Apartment.InvalidPrice`, `Apartment.InvalidDiscount`, `Apartment.InvalidCapacity`, `Apartment.InvalidStayLimits`, `Apartment.InvalidCancellationPolicy`, `Apartment.HouseRulesTooLong`, `Apartment.CheckInInstructionsTooLong`, `Apartment.InvalidImageCount`, `Apartment.DuplicateImage`. A photo that doesn't exist or isn't the caller's returns `400 Apartment.InvalidImage`. Photo uploads can fail with `Apartment.InvalidPhoto` or `Apartment.PhotoTooLarge`.

### Calendar

| Method | Route | Body | Notes |
| --- | --- | --- | --- |
| GET | `/hosting/listings/{apartmentId}/calendar?from=&to=` | — | `[{ date, available, blocked, booked, price, customPrice }]`. Defaults to today plus 90 days; at most 366 days. |
| PUT | `/hosting/listings/{apartmentId}/calendar` | `{ from, to, blocked?, price?, clearPrice? }` | `204`. The range is inclusive. `blocked` blocks or unblocks the nights. `price` sets a nightly override, and `clearPrice: true` removes it. |

Calendar errors: validation `Apartment.InvalidCalendarRange`, `Calendar.NothingToChange`, `Calendar.PriceAndClearPrice`, `Apartment.InvalidPrice`; `400 Apartment.CalendarInPast`.

### Reservations

| Method | Route | Notes |
| --- | --- | --- |
| GET | `/hosting/bookings?status=&listingId=&page=&pageSize=` | Paged `BookingResponse` list for the caller's listings, newest first. |
| PUT | `/hosting/bookings/{bookingId}/accept` | `204`. **6 → 1 Reserved**; the guest can then pay. Fails with `400 Booking.AlreadyStarted` after the check-in date. |
| PUT | `/hosting/bookings/{bookingId}/decline` | `204`. **6 → 3 Rejected**. |
| PUT | `/hosting/bookings/{bookingId}/cancel` | `204`. **2 → 4 Cancelled** with a full refund. See [Cancellations and refunds](#cancellations-and-refunds). |
| POST | `/hosting/bookings/{bookingId}/guest-review` | See [Reviews](#reviews). |

Requests the host doesn't answer in time move to **7 Expired** (the `expire-booking-requests` job). Errors: `400 Booking.NotPendingApproval`, `Booking.AlreadyStarted`, `Booking.NotConfirmed`, `403 Apartment.NotOwner`, `404 Booking.NotFound`.

### Payouts

| Method | Route | Notes |
| --- | --- | --- |
| GET | `/hosting/payouts/account` | `{ status: "NotConnected"\|"Pending"\|"Active", payoutsEnabled, mode }` |
| POST | `/hosting/payouts/onboarding` | `200 { url }`. In Stripe mode this creates or resumes a Connect Express account and returns the onboarding link. Stripe then returns the host to `{App:FrontendBaseUrl}/hosting/payouts?onboarding=complete` (or `?onboarding=refresh` when the link expired). In Simulated mode, the account is activated immediately and the `onboarding=complete` URL is returned. Fails with `400 Payments.PayoutsNotConfigured` in Stripe mode without keys. |
| GET | `/hosting/payouts?status=&page=&pageSize=` | Paged `{ id, bookingId\|null, experienceBookingId\|null, listingName, guestName, amount, currency, status, scheduledFor, paidOnUtc\|null }`. For experience payouts, `listingName` holds the experience title. An unknown `status` fails validation with `Payouts.InvalidStatus`. |

A payout is created when a booking is paid:

* For a stay, the amount is the stay subtotal (nights − discount + up-charge + cleaning fee) minus `Payments:HostServiceFeePercent` (3 %). The guest service fee and taxes aren't paid out. It is scheduled for check-in + 1 day.
* For an experience, the amount is the price of the spots minus the host fee, scheduled for the day after the session.

The daily `pay-due-payouts` job pays payouts that are due. A host without an active payout account gets **OnHold** payouts, which are paid as soon as the account becomes active. See [Payout statuses](#payout-statuses).

### Summary

`GET /hosting/summary` returns:

```text
{ listingCount, upcomingStays, pendingRequests, earningsThisMonth, paidTotal, upcomingPayouts, currency,
  averageRating|null, reviewCount, isSuperhost,
  superhost: { completedStays, averageRating|null, cancellationRate (0..1), meetsRequirements,
               requirements: { minCompletedStays, minAverageRating, maxCancellationRate } } }
```

**Superhost.** The daily `update-superhost-status` job grants or revokes the badge using `Hosting:Superhost:*`. The defaults are:

* at least 5 completed stays;
* an average rating of at least 4.8;
* a cancellation rate below 10 %. The rate is the share of declined or rejected bookings and host-cancelled bookings among the bookings the host accepted or declined. Instant bookings count as accepted. Pending requests, expired requests and requests the guest cancelled before the host answered aren't counted. Guest cancellations of accepted bookings count as bookings but never as cancellations.

## Messaging

Every route needs **User**. A conversation is between one guest and the host of one listing. Callers outside a conversation get `404 Conversation.NotFound`.

| Method | Route | Body | Notes |
| --- | --- | --- | --- |
| POST | `/conversations` | `{ apartmentId, message }` | `201 { conversationId, message: MessageResponse }`. Reuses the existing conversation for that listing and guest. |
| GET | `/conversations?page=&pageSize=` | — | Paged `ConversationResponse` list, most recent activity first. |
| GET | `/conversations/unread-count` | — | `{ count }` (unread messages across all conversations). |
| GET | `/conversations/{conversationId}` | — | `ConversationResponse` |
| GET | `/conversations/{conversationId}/messages?before=&limit=` | — | `{ items: MessageResponse[], hasMore, nextBefore\|null }`. The newest page comes first, and `items` are oldest first within a page. To load older messages, pass `nextBefore` (a message id) or an ISO timestamp as `before`. `limit` is 1–100 (30 by default). |
| POST | `/conversations/{conversationId}/messages` | `{ body }` | `201 MessageResponse`. Also marks the conversation read for the sender. |
| PUT | `/conversations/{conversationId}/read` | — | `204` |

```text
MessageResponse      { id, conversationId, senderId|null, type: "User"|"System", body, systemEvent|null, bookingId|null, sentOnUtc }

ConversationResponse { id, apartment: { id, name, imageUrl|null },
                       otherParticipant: { id, firstName, avatarUrl|null, role: "Host"|"Guest" },
                       lastMessage: { body, type, sentOnUtc, senderId|null, systemEvent|null } | null,
                       unreadCount,
                       latestBooking: { id, status: "PendingApproval"|"Reserved"|…, durationStart, durationEnd } | null,
                       lastMessageOnUtc }
```

> [!NOTE]
> `latestBooking.status` is the status **name**, unlike `BookingResponse.status`, which is an integer.

**System messages.** Booking events are posted into the conversation as `type: "System"` messages with `senderId: null`. They carry a `systemEvent` code and an English fallback `body`; clients should localize them by `systemEvent` (see [System events](#system-events)).

* `BookingRequested` and `BookingReserved` are posted immediately when the booking is created, followed by the guest's `messageToHost` (if any) as a User message.
* The other events are posted by the outbox processor, about 10 s after the action.

The recipient of a new message gets an e-mail (`Messages` preference) unless they have already read the conversation. These e-mails are throttled to one per conversation every 15 minutes.

| Errors | |
| --- | --- |
| `404 Conversation.NotFound`, `404 Apartment.NotFound` | |
| `400 Conversation.OwnListing` | Messaging yourself about your own listing. |
| Validation: `Message.Empty`, `Message.TooLong` (> 2,000 characters), `Message.InvalidCursor`, `Message.InvalidLimit` | |

## Notifications

Every route needs **User**.

| Method | Route | Body | Notes |
| --- | --- | --- | --- |
| GET | `/notifications?unreadOnly=false&page=&pageSize=` | — | Paged `NotificationResponse` list, newest first. |
| GET | `/notifications/unread-count` | — | `{ count }` |
| PUT | `/notifications/{notificationId}/read` | — | `204`. Unknown or foreign ids return `404 Notification.NotFound`. |
| PUT | `/notifications/read-all` | — | `204` |
| GET | `/users/{userId}/notification-preferences` | — | Self. `NotificationPreferences`. |
| PUT | `/users/{userId}/notification-preferences` | `NotificationPreferences` | Self. `200` with the saved preferences. |

```text
NotificationResponse    { id, type, data: object, link (relative frontend path), createdOnUtc, readOnUtc|null }
NotificationPreferences { email: { messages, reservations, reviews, payouts, reminders },
                          inApp: { messages, reservations, reviews, payouts, reminders } }   // all true by default
```

* Each type belongs to one preference category (see [Notification types](#notification-types)).
* A notification whose category is turned off in-app is recorded but hidden.
* E-mails are sent only when the e-mail channel of the category is on. Account e-mails (verification, password reset, password changed, welcome) are always sent.
* A source event creates at most one notification per recipient.

Clients should build the text from `type` and `data`; `link` says where a click goes.

## Real-time (SignalR)

| | |
| --- | --- |
| Hub | `/hubs/notifications`, at `http://localhost:8000/hubs/notifications` through the gateway. It is outside `/api/v1`. |
| Auth | Required. Browsers send the access token as the `access_token` query string parameter; with `@microsoft/signalr`, use `accessTokenFactory`. The query string token is only accepted under `/hubs`. |
| CORS | Origins from `Cors:AllowedOrigins`, with credentials (`withCredentials: true` is the client default). |
| Transports | WebSockets, then Server-Sent Events, then long polling. |
| Scale-out | Redis backplane (channel prefix `lodge-signalr`). The gateway's `.Lodge.Hubs.Affinity` cookie keeps negotiate and connect on the same API instance. |

Clients don't call any hub methods. The server sends these events, with camelCase payloads:

| Event | Payload | Sent to / when |
| --- | --- | --- |
| `messageReceived` | `{ conversationId, message: MessageResponse }` | Both participants, after a message is committed. |
| `conversationUpdated` | `{ conversationId }` | Both participants, on a new message or a read-state change. |
| `notificationReceived` | `{ notification: NotificationResponse }` | The recipient, when an in-app notification is created (not for categories turned off in-app). |
| `unreadCounts` | `{ messages, notifications }` | The user, right after connecting and after every change to either count. |

Events are pushed only after the database transaction commits. Delivery is best effort: a missed event is never replayed, so clients should refetch after reconnecting.

## Experiences

Experiences are hosted activities (tours, classes, …) booked per guest for a scheduled session.

* Categories: `Food`, `Outdoors`, `Art`, `Culture`, `Sports`, `Wellness`, `Nightlife`, `Workshops`.
* Currency: `USD` or `EUR`.
* Booking statuses: `Reserved`, `Confirmed`, `Cancelled`, `Completed` (see [architecture.md](architecture.md#experience-booking-lifecycle)).

### Public endpoints

| Method | Route | Auth | Notes |
| --- | --- | --- | --- |
| GET | `/experiences?q=&city=&category=&date=&guests=&minPrice=&maxPrice=&page=&pageSize=` | — | Paged `{ id, title, category, city, country, latitude, longitude, durationMinutes, pricePerGuest, currency, imageUrl\|null, languages, averageRating\|null, reviewCount, nextSessionStartsAtUtc\|null, hostId, hostFirstName }`. Listed experiences only, sorted by the next session. Prices are compared in USD. An unknown `category` returns `400 Experience.InvalidCategory`. |
| GET | `/experiences/{experienceId}` | Optional | `ExperienceResponse`. Unlisted experiences are visible only to their host. |
| GET | `/experiences/{experienceId}/sessions?from=&to=` | — | `[{ id, startsAtUtc, capacity, spotsLeft }]`. Defaults to now through +60 days. |
| GET | `/experiences/{experienceId}/reviews?page=&pageSize=` | — | Paged `{ id, rating, comment, createdOnUtc, guest: { id, firstName, avatarUrl\|null } }`. |

```text
ExperienceResponse { id, title, description, category, city, country, latitude, longitude, meetingPoint,
                     durationMinutes, pricePerGuest, currency, maxGuests, languages, included, images: [{ id, url }],
                     host: { id, firstName, avatarUrl|null, isSuperhost }, averageRating|null, reviewCount,
                     isListed, freeCancellationHours (24), createdOnUtc }
```

### Guest endpoints

| Method | Route | Auth | Body | Notes |
| --- | --- | --- | --- | --- |
| POST | `/experience-bookings` | Verified + `X-Idempotency-Key` | `{ sessionId, guestCount }` | `201 { id }`, status `Reserved`. The spots are held for `BackgroundJobs:Experiences:PaymentHoldMinutes` (60) until the booking is paid. |
| POST | `/experience-bookings/{id}/checkout` | User (guest) | — | `200 { url }`: Stripe Checkout, or `{App:FrontendBaseUrl}/checkout/simulated?experienceBookingId={id}`. |
| POST | `/payments/simulated/experience-bookings/{id}/complete` | User (guest) | — | `204`. Simulated mode only; idempotent. |
| GET | `/experience-bookings/{id}` | User (guest or host) | — | `ExperienceBookingResponse` |
| GET | `/users/{userId}/experience-bookings` | Self | — | `ExperienceBookingResponse[]`, newest session first. |
| PUT | `/experience-bookings/{id}/cancel` | User (guest) | — | `204`. A **full** refund (including the service fee) when cancelled at least 24 h before the start, otherwise none. |
| POST | `/experience-bookings/{id}/review` | User (guest) | `{ rating, comment }` | `201 { id }`. Only for `Completed` bookings, one review each; `comment` can be at most 500 characters. Experience reviews are published immediately (they aren't double-blind). |

```text
ExperienceBookingResponse { id, experienceId, experienceTitle, category, imageUrl|null, city, country, meetingPoint,
                            durationMinutes, sessionId, startsAtUtc, hostId, hostFirstName, guestId, guestName, guestCount,
                            priceTotal, serviceFee, totalPrice, currency, status, amountPaid|null, paidOnUtc|null,
                            refundAmount|null, refundStatus, cancelledByHost, cancelledOnUtc|null,
                            freeCancellationUntilUtc, canCancel, canReview, hasReview, createdOnUtc }
```

`serviceFee` is `Payments:GuestServiceFeePercent` of `priceTotal` (price per guest × guests).

### Host endpoints

Every route needs **Verified**. Experiences of another host return `403 Experience.NotHost`.

| Method | Route | Body | Notes |
| --- | --- | --- | --- |
| GET | `/hosting/experiences?page=&pageSize=` | — | Paged `{ id, title, category, city, country, pricePerGuest, currency, isListed, imageUrl\|null, upcomingSessionCount, averageRating\|null, reviewCount, createdOnUtc }`. |
| GET | `/hosting/experiences/{experienceId}` | — | `ExperienceResponse` |
| POST | `/hosting/experiences` | `ExperienceRequest` | `201 { id }` |
| PUT | `/hosting/experiences/{experienceId}` | `ExperienceRequest` | `204` |
| PUT | `/hosting/experiences/{experienceId}/status` | `{ isListed }` | `204` |
| DELETE | `/hosting/experiences/{experienceId}` | — | `204`, or `409 Experience.HasUpcomingBookings`. |
| GET | `/hosting/experiences/{experienceId}/sessions?from=&to=` | — | `[{ id, startsAtUtc, capacity, bookedGuests, spotsLeft, bookingCount, isCancelled }]`, including cancelled sessions. |
| POST | `/hosting/experiences/{experienceId}/sessions` | `CreateSessionsRequest` | `201 { ids }` |
| DELETE | `/hosting/experiences/{experienceId}/sessions/{sessionId}` | — | `204`. A session with bookings is cancelled instead, and every booking gets a full refund. |
| GET | `/hosting/experiences/bookings?experienceId=&page=&pageSize=` | — | Paged `ExperienceBookingResponse` list. |

* **`ExperienceRequest`**: `{ title (≤ 120), description (≤ 4,000), category, city, country, latitude, longitude, meetingPoint (≤ 500), durationMinutes (30–1,440), pricePerGuest (> 0), currency, maxGuests (1–50), languages: ["en", …] (1–10), included: string[] (≤ 20, each ≤ 100), imageIds (0–20, from POST /hosting/photos), isListed? }`
* **`CreateSessionsRequest`**: send exactly one of these two forms:
  * a single session: `{ startsAtUtc, capacity? }`
  * a recurring schedule: `{ recurring: { daysOfWeek: ["Monday", …], startTime: "HH:mm", timeZone: "<IANA id>", from, until }, capacity? }`, at most 200 sessions.

  `capacity` defaults to `maxGuests` and can't exceed it.

| Errors | |
| --- | --- |
| `403 User.EmailNotVerified`, `403 Experience.NotHost` | |
| `404 Experience.NotFound`, `404 ExperienceSession.NotFound`, `404 ExperienceBooking.NotFound` | |
| `400 ExperienceBooking.OwnExperience`, `ExperienceSession.Cancelled`, `ExperienceSession.AlreadyStarted` | Reserving. |
| `409 ExperienceSession.NotEnoughSpots` | |
| `400 ExperienceBooking.NotReserved` | Checkout of a booking that isn't `Reserved`. |
| `400 ExperienceBooking.NotCancellable` | The booking isn't `Reserved` or `Confirmed`, or the session has started. |
| `403 ExperienceBooking.NotCompleted`, `400 ExperienceBooking.AlreadyReviewed` | Reviewing. |
| `400 Rating.Invalid`, validation `Comment.Empty`, `Comment.TooLong` | |
| Validation: `ExperienceBooking.InvalidGuestCount`, `Experience.Invalid*` (`Title`, `Description`, `Category`, `Location`, `MeetingPoint`, `Duration`, `Price`, `Currency`, `MaxGuests`, `Languages`, `Included`, `Image`), `Experience.TooManyImages` | |
| Validation: `ExperienceSession.InvalidSchedule`, `InvalidRecurrence`, `InvalidTimeZone`, `TooManySessions`, `NoSessions`, `StartsInPast`, `InvalidCapacity` | |

## Files

| Method | Route | Auth | Notes |
| --- | --- | --- | --- |
| POST | `/files` | Self + `X-Idempotency-Key` | `multipart/form-data` with the fields `userId` and `file`. Like avatars, the file must be a JPEG, PNG or WebP image (declared content type and leading bytes), at most 5 MB; otherwise `400 UploadFile.InvalidContentType` or `UploadFile.FileTooLarge`. The detected content type is stored. Returns `200 "<fileId>"`. |
| GET | `/files/{fileId}` | — | Streams the blob with its stored content type. Avatars and listing photos are served from here. |
| DELETE | `/files/{fileId}/users/{userId}` | Self | `204`. Owner only. |

Errors: `403 User.InvalidPermissions`, `404 Blob.NotFound`; validation `UploadFile.*` (`FileIsRequired`, `FileTooLarge`, `InvalidContentType`, …).

## Health

| Route | Description |
| --- | --- |
| `GET /health` | Detailed JSON health report: PostgreSQL, Redis, blob storage, RabbitMQ, Hangfire. |
| `GET /health/live` | Liveness probe; runs no checks. |
| `GET /gateway/health` | The gateway's own probe, only available on port 8000. `/health` on the gateway is proxied to the API. |

## Enum reference

### Booking statuses

`BookingResponse.status` and the `status` query filters are integers:

| Value | Name | Meaning |
| --- | --- | --- |
| 1 | Reserved | The dates are held and the guest has to pay. |
| 2 | Confirmed | Paid. |
| 3 | Rejected | Declined by the host or rejected by an admin. |
| 4 | Cancelled | Cancelled by the guest or the host (see `cancelledByHost`). |
| 5 | Completed | The stay has ended. |
| 6 | PendingApproval | A request to book; the dates are held until the host responds. |
| 7 | Expired | The host didn't answer the request in time. |

Statuses 6, 1, 2 and 5 make nights unavailable to other guests.

### Refund statuses

`None`, `Pending`, `Succeeded`, `Failed`, `Manual`. See [Cancellations and refunds](#cancellations-and-refunds). Experience bookings use the same values.

### Payout statuses

| Payout `status` | Meaning |
| --- | --- |
| `Scheduled` | Waiting for its date. |
| `OnHold` | Due, but the host has no active payout account. |
| `Paid` | Transferred (a Stripe transfer, or `sim_tr_…`). |
| `Failed` | The transfer failed. |
| `Cancelled` | The booking was cancelled with a full refund. |

Payout account `status`: `NotConnected`, `Pending` (onboarding started), `Active`.

### Cancellation policies

`Flexible`, `Moderate`, `Strict`.

### Property types

`Apartment`, `House`, `Cabin`, `Villa`, `Cottage`, `Treehouse`, `Houseboat`, `Igloo`, `Other`.

### Amenities

* **Amenities:** `WiFi`, `AirConditioning`, `Parking`, `PetFriendly`, `SwimmingPool`, `Gym`, `Spa`, `Terrace`, `MountainView`, `GardenView`, `Lakefront`, `Kitchen`, `Washer`, `Heating`, `Workspace`, `HotTub`, `Fireplace`, `BeachAccess`, `EVCharger`, `SelfCheckIn`.
* **Category tags,** used by the category bar: `Cabins`, `Treehouses`, `Camping`, `Castles`, `Farms`, `Boats`, `Domes`, `Tropical`, `Mansions`, `Caves`, `Barns`, `TopCities`.

### Experience categories

`Food`, `Outdoors`, `Art`, `Culture`, `Sports`, `Wellness`, `Nightlife`, `Workshops`.

### System events

`MessageResponse.systemEvent` codes:

| Code | Posted when |
| --- | --- |
| `BookingRequested` | A guest sent a request to book (status 6). |
| `BookingReserved` | An instant booking was made (status 1). |
| `BookingAccepted` | The host accepted the request. |
| `BookingDeclined` | The host (or an admin) declined it. |
| `BookingExpired` | The request expired. |
| `BookingConfirmed` | The booking was paid. |
| `BookingDatesChanged` | The guest changed the dates. |
| `BookingCancelledByGuest` | The guest cancelled. |
| `BookingCancelledByHost` | The host cancelled. |

### Notification types

Every stay notification carries these **booking keys** in `data`: `bookingId`, `apartmentId`, `listingName`, `checkIn`, `checkOut` (`yyyy-MM-dd`), `guestCount`, `guestFirstName`, `hostFirstName`.

The links below use these paths:

* **Guest link:** `/bookings?bookingId={id}`
* **Host link:** `/hosting/reservations?bookingId={id}`

| `type` | Category | Recipient | Extra `data` keys | `link` |
| --- | --- | --- | --- | --- |
| `BookingRequested` | Reservations | Host | — | Host link |
| `BookingAccepted` | Reservations | Guest | — | Guest link |
| `BookingDeclined` | Reservations | Guest | — | Guest link |
| `BookingExpired` | Reservations | Guest | — | Guest link |
| `BookingConfirmed` | Reservations | Guest and host | `role` (`Guest` / `Host`) | Guest or host link |
| `BookingCancelled` | Reservations | The other party | `cancelledBy` (`Guest` / `Host`), `refundAmount\|null`, `currency` | Guest or host link |
| `BookingDatesChanged` | Reservations | Host | `previousCheckIn`, `previousCheckOut` | Host link |
| `RefundSucceeded` (stay) | Reservations | Guest | `amount`, `currency` | Guest link |
| `TripReminder` | Reminders | Guest, 2 days before check-in | — | Guest link |
| `ArrivalReminder` | Reminders | Host, the day before arrival | — | Host link |
| `ReviewReminder` | Reminders | Guest and host, the day after check-out | `role`, `reviewDeadline` | Guest or host link |

These types don't carry the booking keys:

| `type` | Category | Recipient | `data` keys | `link` |
| --- | --- | --- | --- | --- |
| `ReviewReceived` | Reviews | Host (listing review) or guest (guest review), when published | `reviewId`, `kind` (`Listing` / `Guest`), `rating`, `bookingId`, `apartmentId`, `listingName`, `reviewerFirstName` | Host: `/hosting/reviews`; guest: `/users/{guestId}` |
| `ReviewReplyReceived` | Reviews | Guest | `reviewId`, `bookingId`, `apartmentId`, `listingName`, `hostFirstName` | `/apartments/{apartmentId}#reviews` |
| `PayoutPaid` | Payouts | Host | `payoutId`, `amount`, `currency`, `bookingId\|null`, `experienceBookingId\|null`, `listingName` | `/hosting/payouts` |
| `RefundSucceeded` (experience) | Reservations | Guest | `experienceBookingId`, `experienceId`, `experienceTitle`, `amount`, `currency` | `/experience-bookings/{id}` |
| `ExperienceBooked` | Reservations | Guest and host | `experienceBookingId`, `experienceId`, `experienceTitle`, `startsAtUtc`, `guestCount`, `meetingPoint`, `guestFirstName`, `hostFirstName`, `role` | Guest: `/experience-bookings/{id}`; host: `/hosting/experiences/{experienceId}/bookings` |
| `ExperienceReminder` | Reminders | Guest, the day before | Same as `ExperienceBooked` (`role: Guest`) | `/experience-bookings/{id}` |

New-message e-mails link to `/messages/{conversationId}`.
