import type { Http } from "@/lib/api/client";
import type {
  CreateSessionsRequest,
  Experience,
  ExperienceBooking,
  ExperienceRequest,
  ExperienceReview,
  ExperienceSearchParams,
  ExperienceSession,
  ExperienceSummary,
  HostExperienceSession,
  HostExperienceSummary,
  PagedList,
} from "@/types/api";

const experiencePath = (id: string) => `/experiences/${encodeURIComponent(id)}`;
const bookingPath = (id: string) => `/experience-bookings/${encodeURIComponent(id)}`;
const hostPath = (id: string) => `/hosting/experiences/${encodeURIComponent(id)}`;

/** Drops empty values so they don't reach the query string. */
function clean<T extends object>(params: T): Partial<T> {
  return Object.fromEntries(
    Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== ""),
  ) as Partial<T>;
}

/** Public (anonymous) endpoints. */
export const experiencesApi = {
  search: (http: Http, params: ExperienceSearchParams) =>
    http.get<PagedList<ExperienceSummary>>("/experiences", { params: clean(params) }).then((r) => r.data),

  get: (http: Http, id: string) => http.get<Experience>(experiencePath(id)).then((r) => r.data),

  /** Defaults to now through +60 days. */
  sessions: (http: Http, id: string, params: { from?: string; to?: string } = {}) =>
    http.get<ExperienceSession[]>(`${experiencePath(id)}/sessions`, { params: clean(params) }).then((r) => r.data),

  reviews: (http: Http, id: string, params: { page: number; pageSize: number }) =>
    http.get<PagedList<ExperienceReview>>(`${experiencePath(id)}/reviews`, { params }).then((r) => r.data),
};

/** Guest endpoints (Bearer token). */
export const experienceBookingsApi = {
  /** Holds the spots for 60 minutes until paid. Returns the booking id. */
  create: (http: Http, body: { sessionId: string; guestCount: number }, idempotencyKey: string) =>
    http
      .post<{ id: string }>("/experience-bookings", body, { headers: { "X-Idempotency-Key": idempotencyKey } })
      .then((r) => r.data.id),

  /** Stripe Checkout, or `/checkout/simulated?experienceBookingId=…` in Simulated mode. */
  checkout: (http: Http, id: string) => http.post<{ url: string }>(`${bookingPath(id)}/checkout`).then((r) => r.data.url),

  completeSimulated: (http: Http, id: string) =>
    http
      .post<void>(`/payments/simulated/experience-bookings/${encodeURIComponent(id)}/complete`)
      .then(() => undefined),

  get: (http: Http, id: string) => http.get<ExperienceBooking>(bookingPath(id)).then((r) => r.data),

  byUser: (http: Http, userId: string) =>
    http.get<ExperienceBooking[]>(`/users/${encodeURIComponent(userId)}/experience-bookings`).then((r) => r.data),

  /** Full refund at least 24 hours before the start, otherwise none. */
  cancel: (http: Http, id: string) => http.put<void>(`${bookingPath(id)}/cancel`).then(() => undefined),

  review: (http: Http, id: string, body: { rating: number; comment: string }) =>
    http.post<{ id: string }>(`${bookingPath(id)}/review`, body).then((r) => r.data.id),
};

/** Host endpoints (Bearer token, verified e-mail). */
export const hostExperiencesApi = {
  list: (http: Http, params: { page: number; pageSize: number }) =>
    http.get<PagedList<HostExperienceSummary>>("/hosting/experiences", { params }).then((r) => r.data),

  get: (http: Http, id: string) => http.get<Experience>(hostPath(id)).then((r) => r.data),

  create: (http: Http, body: ExperienceRequest) =>
    http.post<{ id: string }>("/hosting/experiences", body).then((r) => r.data.id),

  update: (http: Http, id: string, body: ExperienceRequest) => http.put<void>(hostPath(id), body).then(() => undefined),

  setListed: (http: Http, id: string, isListed: boolean) =>
    http.put<void>(`${hostPath(id)}/status`, { isListed }).then(() => undefined),

  /** 409 `Experience.HasUpcomingBookings`. */
  remove: (http: Http, id: string) => http.delete<void>(hostPath(id)).then(() => undefined),

  sessions: (http: Http, id: string, params: { from?: string; to?: string } = {}) =>
    http.get<HostExperienceSession[]>(`${hostPath(id)}/sessions`, { params: clean(params) }).then((r) => r.data),

  addSessions: (http: Http, id: string, body: CreateSessionsRequest) =>
    http.post<{ ids: string[] }>(`${hostPath(id)}/sessions`, body).then((r) => r.data.ids),

  /** Sessions with bookings are cancelled and every booking is refunded in full. */
  cancelSession: (http: Http, id: string, sessionId: string) =>
    http.delete<void>(`${hostPath(id)}/sessions/${encodeURIComponent(sessionId)}`).then(() => undefined),

  bookings: (http: Http, params: { page: number; pageSize: number; experienceId?: string }) =>
    http
      .get<PagedList<ExperienceBooking>>("/hosting/experiences/bookings", { params: clean(params) })
      .then((r) => r.data),
};
