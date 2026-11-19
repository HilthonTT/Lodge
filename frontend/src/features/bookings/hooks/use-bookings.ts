"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";

import { useUserId } from "@/features/auth/session-provider";
import { getBrowserHttp } from "@/lib/api/browser";
import { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import type { Booking, CancellationQuote, ReserveBookingRequest } from "@/types/api";

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
    // Refunds settle asynchronously (Pending → Succeeded within seconds): poll until they do.
    refetchInterval: (query) =>
      query.state.data?.some((b) => b.refundStatus === "Pending") ? 3_000 : false,
  });
}

/** A single booking (guest or listing host). */
export function useBooking(bookingId: string | undefined) {
  return useQuery<Booking, ApiError>({
    queryKey: queryKeys.bookings.detail(bookingId ?? ""),
    queryFn: () => bookingsApi.get(getBrowserHttp(), bookingId!),
    enabled: Boolean(bookingId),
    staleTime: 5_000,
  });
}

/** What cancelling right now would refund. Always fresh: the tier depends on today's date. */
export function useCancellationQuote(bookingId: string, enabled: boolean) {
  return useQuery<CancellationQuote, ApiError>({
    queryKey: queryKeys.bookings.cancellationQuote(bookingId),
    queryFn: () => bookingsApi.cancellationQuote(getBrowserHttp(), bookingId),
    enabled,
    staleTime: 0,
    gcTime: 0,
  });
}

export function useReserveBooking() {
  const queryClient = useQueryClient();
  return useMutation<string, ApiError, { body: ReserveBookingRequest; idempotencyKey: string }>({
    mutationFn: ({ body, idempotencyKey }) => bookingsApi.reserve(getBrowserHttp(), body, idempotencyKey),
    onSuccess: (_id, { body }) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookings.byUser(body.userId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.apartments.bookedDates(body.apartmentId) });
      void queryClient.invalidateQueries({ queryKey: ["apartments", body.apartmentId, "calendar"] });
    },
  });
}

export function useCancelBooking() {
  const userId = useUserId();
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, string>({
    mutationFn: (bookingId) => bookingsApi.cancel(getBrowserHttp(), bookingId, requireUser(userId)),
    onSettled: (_data, _error, bookingId) => {
      queryClient.removeQueries({ queryKey: queryKeys.bookings.cancellationQuote(bookingId) });
      // Freed nights must show as available again on the listing.
      void queryClient.invalidateQueries({ queryKey: queryKeys.apartments.all });
      return queryClient.invalidateQueries({
        queryKey: queryKeys.bookings.all,
        predicate: (query) => query.queryKey[3] !== "cancellation-quote",
      });
    },
  });
}

export function useChangeBookingDates() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, { bookingId: string; apartmentId: string; startDate: string; endDate: string }>({
    mutationFn: ({ bookingId, startDate, endDate }) =>
      bookingsApi.changeDates(getBrowserHttp(), bookingId, startDate, endDate),
    onSuccess: (_data, { apartmentId }) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all });
      void queryClient.invalidateQueries({ queryKey: queryKeys.apartments.bookedDates(apartmentId) });
      void queryClient.invalidateQueries({ queryKey: ["apartments", apartmentId, "calendar"] });
    },
  });
}

/**
 * Accepts Stripe's https checkout links and, in Simulated mode, links back to
 * this site. Returns the parsed URL, or null for anything else.
 */
export function parseCheckoutUrl(value: unknown): URL | null {
  if (typeof value !== "string") return null;
  try {
    const url = new URL(value);
    if (url.protocol === "https:") return url;
    return url.origin === window.location.origin ? url : null;
  } catch {
    return null;
  }
}

export function useCheckout() {
  const userId = useUserId();
  const router = useRouter();
  return useMutation<URL, ApiError, string>({
    mutationFn: async (bookingId) => {
      const url = parseCheckoutUrl(await bookingsApi.checkout(getBrowserHttp(), bookingId, requireUser(userId)));
      if (!url) {
        throw new ApiError("The payment provider returned an invalid checkout link.", {
          messageKey: "invalidCheckoutLink",
        });
      }
      return url;
    },
    // Our own simulated checkout is a client-side navigation; Stripe is a full page load
    // and returns to /bookings when done.
    onSuccess: (url) => {
      if (url.origin === window.location.origin) router.push(`${url.pathname}${url.search}`);
      else window.location.assign(url.href);
    },
  });
}
