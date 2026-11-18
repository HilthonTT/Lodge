import type { AxiosProgressEvent } from "axios";

import type { Http } from "@/lib/api/client";
import type {
  Booking,
  BookingStatus,
  HostCalendarDay,
  HostingSummary,
  HostListing,
  HostListingSummary,
  ListingRequest,
  PagedList,
  Payout,
  PayoutAccount,
  PayoutStatus,
  UpdateCalendarRequest,
  UploadedPhoto,
} from "@/types/api";

const listingPath = (id: string) => `/hosting/listings/${encodeURIComponent(id)}`;

export type PayoutsParams = { page: number; pageSize: number; status?: PayoutStatus };

export type HostBookingsParams = {
  page: number;
  pageSize: number;
  status?: BookingStatus;
  listingId?: string;
};

/** Every endpoint under /hosting (Bearer token; 403 `User.EmailNotVerified` for unverified accounts). */
export const hostingApi = {
  summary: (http: Http) => http.get<HostingSummary>("/hosting/summary").then((r) => r.data),

  listings: (http: Http, params: { page: number; pageSize: number }) =>
    http.get<PagedList<HostListingSummary>>("/hosting/listings", { params }).then((r) => r.data),

  listing: (http: Http, id: string) => http.get<HostListing>(listingPath(id)).then((r) => r.data),

  /** Returns the new listing id. */
  create: (http: Http, body: ListingRequest) =>
    http.post<{ id: string }>("/hosting/listings", body).then((r) => r.data.id),

  update: (http: Http, id: string, body: ListingRequest) =>
    http.put<void>(listingPath(id), body).then(() => undefined),

  setListed: (http: Http, id: string, isListed: boolean) =>
    http.put<void>(`${listingPath(id)}/status`, { isListed }).then(() => undefined),

  /** Soft delete; 409 `Apartment.HasUpcomingBookings`. */
  remove: (http: Http, id: string) => http.delete<void>(listingPath(id)).then(() => undefined),

  calendar: (http: Http, id: string, params: { from?: string; to?: string } = {}) =>
    http.get<HostCalendarDay[]>(`${listingPath(id)}/calendar`, { params }).then((r) => r.data),

  updateCalendar: (http: Http, id: string, body: UpdateCalendarRequest) =>
    http.put<void>(`${listingPath(id)}/calendar`, body).then(() => undefined),

  /** JPEG/PNG/WebP up to 10 MB. */
  uploadPhoto: (http: Http, file: File, onProgress?: (fraction: number) => void, signal?: AbortSignal) => {
    const form = new FormData();
    form.append("file", file);
    return http
      .post<UploadedPhoto>("/hosting/photos", form, {
        signal,
        timeout: 120_000,
        onUploadProgress: (event: AxiosProgressEvent) => {
          if (onProgress && event.total) onProgress(event.loaded / event.total);
        },
      })
      .then((r) => r.data);
  },

  bookings: (http: Http, { status, listingId, ...params }: HostBookingsParams) =>
    http
      .get<PagedList<Booking>>("/hosting/bookings", {
        params: { ...params, ...(status ? { status } : {}), ...(listingId ? { listingId } : {}) },
      })
      .then((r) => r.data),

  accept: (http: Http, bookingId: string) =>
    http.put<void>(`/hosting/bookings/${encodeURIComponent(bookingId)}/accept`).then(() => undefined),

  decline: (http: Http, bookingId: string) =>
    http.put<void>(`/hosting/bookings/${encodeURIComponent(bookingId)}/decline`).then(() => undefined),

  /** Confirmed bookings before check-in only; the guest gets a full refund. */
  cancelBooking: (http: Http, bookingId: string) =>
    http.put<void>(`/hosting/bookings/${encodeURIComponent(bookingId)}/cancel`).then(() => undefined),

  payoutAccount: (http: Http) => http.get<PayoutAccount>("/hosting/payouts/account").then((r) => r.data),

  /** Returns the onboarding URL (in Simulated mode the account is activated immediately). */
  startOnboarding: (http: Http) =>
    http.post<{ url: string }>("/hosting/payouts/onboarding").then((r) => r.data.url),

  payouts: (http: Http, { status, ...params }: PayoutsParams) =>
    http
      .get<PagedList<Payout>>("/hosting/payouts", { params: status ? { ...params, status } : params })
      .then((r) => r.data),
};
