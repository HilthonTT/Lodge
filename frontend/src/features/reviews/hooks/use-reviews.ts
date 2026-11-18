"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { useSession, useUserId } from "@/features/auth/session-provider";
import { getBrowserHttp } from "@/lib/api/browser";
import { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import type { CategoryRatings, GuestReviewRequest, PagedList, Review } from "@/types/api";

import { type HostReviewsParams, reviewsApi } from "../api/reviews";

function requireUser(userId: string | null): string {
  if (!userId) throw new ApiError("Please sign in again to continue.", { status: 401, messageKey: "signInAgain" });
  return userId;
}

/**
 * The signed-in guest's own review of a booking, found among the listing's
 * reviews (the API includes the caller's unpublished one).
 */
export function useOwnReview(apartmentId: string, bookingId: string, enabled: boolean) {
  const userId = useUserId();
  return useQuery<PagedList<Review>, ApiError, Review | null>({
    queryKey: queryKeys.reviews.apartment(apartmentId),
    queryFn: () => reviewsApi.forApartment(getBrowserHttp(), apartmentId, { page: 1, pageSize: 50 }),
    select: (page) => page.items.find((r) => r.bookingId === bookingId && r.userId === userId) ?? null,
    enabled: enabled && userId !== null,
    staleTime: 30_000,
  });
}

export type ReviewInput = { rating: number; comment: string; categories: CategoryRatings };

function useInvalidateReviews() {
  const queryClient = useQueryClient();
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.reviews.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.hosting.all }),
    ]);
}

export function useCreateReview() {
  const userId = useUserId();
  const invalidate = useInvalidateReviews();
  return useMutation<string, ApiError, ReviewInput & { bookingId: string; idempotencyKey: string }>({
    mutationFn: ({ idempotencyKey, ...body }) =>
      reviewsApi.create(getBrowserHttp(), { ...body, userId: requireUser(userId) }, idempotencyKey),
    onSuccess: () => invalidate(),
  });
}

export function useUpdateReview() {
  const userId = useUserId();
  const invalidate = useInvalidateReviews();
  return useMutation<void, ApiError, ReviewInput & { reviewId: string }>({
    mutationFn: ({ reviewId, ...body }) =>
      reviewsApi.update(getBrowserHttp(), reviewId, { ...body, userId: requireUser(userId) }),
    onSuccess: () => invalidate(),
  });
}

export function useHostReviews(params: HostReviewsParams) {
  const verified = Boolean(useSession().session?.emailVerified);
  return useQuery<PagedList<Review>, ApiError>({
    queryKey: queryKeys.hosting.reviewsList(params),
    queryFn: () => reviewsApi.hostReviews(getBrowserHttp(), params),
    enabled: verified,
    placeholderData: keepPreviousData,
    staleTime: 15_000,
    retry: (count, error) => error.status !== 403 && count < 2,
  });
}

export function useReplyToReview() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, { reviewId: string; text: string | null }>({
    mutationFn: ({ reviewId, text }) =>
      text === null
        ? reviewsApi.deleteReply(getBrowserHttp(), reviewId)
        : reviewsApi.reply(getBrowserHttp(), reviewId, text),
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.hosting.reviews }),
        queryClient.invalidateQueries({ queryKey: queryKeys.reviews.all }),
      ]),
  });
}

export function useReviewGuest() {
  const queryClient = useQueryClient();
  return useMutation<string, ApiError, GuestReviewRequest & { bookingId: string }>({
    mutationFn: ({ bookingId, ...body }) => reviewsApi.reviewGuest(getBrowserHttp(), bookingId, body),
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.hosting.all }),
  });
}
