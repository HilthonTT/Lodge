import type { Http } from "@/lib/api/client";
import type {
  Booking,
  BookingStatus,
  CreateReviewRequest,
  PagedList,
  ReserveBookingRequest,
} from "@/types/api";

export type AdminBookingsParams = {
  page: number;
  pageSize: number;
  status?: BookingStatus;
};

export const bookingsApi = {
  byUser: (http: Http, userId: string) =>
    http.get<Booking[]>(`/users/${encodeURIComponent(userId)}/bookings`).then((r) => r.data),

  /** Returns the new booking id. The idempotency key makes retries safe. */
  reserve: (http: Http, body: ReserveBookingRequest, idempotencyKey: string) =>
    http
      .post<string>("/bookings", body, { headers: { "X-Idempotency-Key": idempotencyKey } })
      .then((r) => r.data),

  cancel: (http: Http, bookingId: string, userId: string) =>
    http.put<void>(`/bookings/${encodeURIComponent(bookingId)}/cancel`, { userId }).then(() => undefined),

  /** Creates a Stripe checkout session and returns its URL. */
  checkout: (http: Http, bookingId: string, userId: string) =>
    http.post<string>("/stripe/checkout", { userId, bookingId }).then((r) => r.data),

  /** Admin only: every booking across all users, newest first. */
  list: (http: Http, { status, ...params }: AdminBookingsParams) =>
    http
      .get<PagedList<Booking>>("/bookings", { params: status ? { ...params, status } : params })
      .then((r) => r.data),

  /** Admin only: rejects a reserved (unpaid) booking. */
  reject: (http: Http, bookingId: string) =>
    http.put<void>(`/bookings/${encodeURIComponent(bookingId)}/reject`).then(() => undefined),

  /** Admin only: marks a confirmed stay whose end date has passed as completed. */
  complete: (http: Http, bookingId: string) =>
    http.put<void>(`/bookings/${encodeURIComponent(bookingId)}/complete`).then(() => undefined),

  review: (http: Http, body: CreateReviewRequest, idempotencyKey: string) =>
    http
      .post<string>("/reviews", body, { headers: { "X-Idempotency-Key": idempotencyKey } })
      .then((r) => r.data),
};
