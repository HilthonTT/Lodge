import type { Http } from "@/lib/api/client";
import type {
  Booking,
  BookingStatus,
  CancellationQuote,
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

  /** Guest only, for PendingApproval and Reserved bookings. */
  changeDates: (http: Http, bookingId: string, startDate: string, endDate: string) =>
    http
      .put<void>(`/bookings/${encodeURIComponent(bookingId)}/dates`, { startDate, endDate })
      .then(() => undefined),

  get: (http: Http, bookingId: string) =>
    http.get<Booking>(`/bookings/${encodeURIComponent(bookingId)}`).then((r) => r.data),

  cancel: (http: Http, bookingId: string, userId: string) =>
    http.put<void>(`/bookings/${encodeURIComponent(bookingId)}/cancel`, { userId }).then(() => undefined),

  /** What cancelling now would refund, with the policy's refund timeline. */
  cancellationQuote: (http: Http, bookingId: string) =>
    http
      .get<CancellationQuote>(`/bookings/${encodeURIComponent(bookingId)}/cancellation-quote`)
      .then((r) => r.data),

  /**
   * Starts payment and returns the page to send the guest to: Stripe Checkout, or
   * `/checkout/simulated?bookingId=…` on this site in Simulated mode.
   */
  checkout: (http: Http, bookingId: string, userId: string) =>
    http.post<{ url: string }>("/stripe/checkout", { userId, bookingId }).then((r) => r.data.url),

  /** Admin only: every booking across all users, newest first. */
  list: (http: Http, { status, ...params }: AdminBookingsParams) =>
    http
      .get<PagedList<Booking>>("/bookings", { params: status ? { ...params, status } : params })
      .then((r) => r.data),

  /** Admin only: rejects a reserved (unpaid) or pending booking. */
  reject: (http: Http, bookingId: string) =>
    http.put<void>(`/bookings/${encodeURIComponent(bookingId)}/reject`).then(() => undefined),

  /** Admin only: marks a confirmed stay whose end date has passed as completed. */
  complete: (http: Http, bookingId: string) =>
    http.put<void>(`/bookings/${encodeURIComponent(bookingId)}/complete`).then(() => undefined),
};
