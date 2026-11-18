import type { Http } from "@/lib/api/client";
import type {
  CreateReviewRequest,
  GuestReviewRequest,
  PagedList,
  Review,
  UpdateReviewRequest,
} from "@/types/api";

export type HostReviewsParams = { page: number; pageSize: number; listingId?: string };

export const reviewsApi = {
  /** Published reviews plus the caller's own unpublished one (token optional). */
  forApartment: (http: Http, apartmentId: string, params: { page: number; pageSize: number }) =>
    http
      .get<PagedList<Review>>(`/apartments/${encodeURIComponent(apartmentId)}/reviews`, { params })
      .then((r) => r.data),

  /** Returns the new review id. */
  create: (http: Http, body: CreateReviewRequest, idempotencyKey: string) =>
    http
      .post<string>("/reviews", body, { headers: { "X-Idempotency-Key": idempotencyKey } })
      .then((r) => r.data),

  /** Only while unpublished; 409 `Review.AlreadyPublished` afterwards. */
  update: (http: Http, reviewId: string, body: UpdateReviewRequest) =>
    http.put<void>(`/reviews/${encodeURIComponent(reviewId)}`, body).then(() => undefined),

  /** Published reviews of the host's listings. */
  hostReviews: (http: Http, { listingId, ...params }: HostReviewsParams) =>
    http
      .get<PagedList<Review>>("/hosting/reviews", { params: listingId ? { ...params, listingId } : params })
      .then((r) => r.data),

  /** Creates or edits the host's public reply. */
  reply: (http: Http, reviewId: string, text: string) =>
    http.put<void>(`/hosting/reviews/${encodeURIComponent(reviewId)}/reply`, { text }).then(() => undefined),

  deleteReply: (http: Http, reviewId: string) =>
    http.delete<void>(`/hosting/reviews/${encodeURIComponent(reviewId)}/reply`).then(() => undefined),

  /** The host reviews the guest of a completed stay. */
  reviewGuest: (http: Http, bookingId: string, body: GuestReviewRequest) =>
    http
      .post<{ id: string }>(`/hosting/bookings/${encodeURIComponent(bookingId)}/guest-review`, body)
      .then((r) => r.data.id),
};
