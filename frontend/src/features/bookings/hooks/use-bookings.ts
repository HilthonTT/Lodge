"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { useUserId } from "@/features/auth/session-provider";
import { getBrowserHttp } from "@/lib/api/browser";
import { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import type { ReserveBookingRequest } from "@/types/api";

import { bookingsApi } from "../api/bookings";

function requireUser(userId: string | null): string {
  if (!userId) throw new ApiError("Please sign in again to continue.", { status: 401, messageKey: "signInAgain" });
  return userId;
}

export function useUserBookings() {
  const userId = useUserId();
  return useQuery({
    queryKey: queryKeys.bookings.byUser(userId ?? ""),
    queryFn: () => bookingsApi.byUser(getBrowserHttp(), userId!),
    enabled: userId !== null,
    // Payment status changes via a Stripe webhook, so refresh when the user returns.
    refetchOnWindowFocus: true,
    staleTime: 15_000,
  });
}

export function useReserveBooking() {
  const queryClient = useQueryClient();
  return useMutation<string, ApiError, { body: ReserveBookingRequest; idempotencyKey: string }>({
    mutationFn: ({ body, idempotencyKey }) => bookingsApi.reserve(getBrowserHttp(), body, idempotencyKey),
    onSuccess: (_id, { body }) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookings.byUser(body.userId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.apartments.bookedDates(body.apartmentId) });
    },
  });
}

export function useCancelBooking() {
  const userId = useUserId();
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, string>({
    mutationFn: (bookingId) => bookingsApi.cancel(getBrowserHttp(), bookingId, requireUser(userId)),
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all }),
  });
}

export function useCheckout() {
  const userId = useUserId();
  return useMutation<string, ApiError, string>({
    mutationFn: async (bookingId) => {
      const url = await bookingsApi.checkout(getBrowserHttp(), bookingId, requireUser(userId));
      if (typeof url !== "string" || !/^https:\/\//i.test(url)) {
        throw new ApiError("The payment provider returned an invalid checkout link.", {
          messageKey: "invalidCheckoutLink",
        });
      }
      return url;
    },
    // Hand off to Stripe; it returns to /bookings when done.
    onSuccess: (url) => window.location.assign(url),
  });
}

export function useCreateReview() {
  const userId = useUserId();
  const queryClient = useQueryClient();
  return useMutation<
    string,
    ApiError,
    { bookingId: string; rating: number; comment: string; idempotencyKey: string }
  >({
    mutationFn: ({ idempotencyKey, ...body }) =>
      bookingsApi.review(getBrowserHttp(), { ...body, userId: requireUser(userId) }, idempotencyKey),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all }),
  });
}
