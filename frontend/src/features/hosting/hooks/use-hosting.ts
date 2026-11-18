"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { useSession } from "@/features/auth/session-provider";
import { getBrowserHttp } from "@/lib/api/browser";
import type { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import {
  type Booking,
  BookingStatus,
  type HostListingSummary,
  type ListingRequest,
  type PagedList,
  type Payout,
  type PayoutAccount,
  type UpdateCalendarRequest,
} from "@/types/api";

import { type HostBookingsParams, hostingApi, type PayoutsParams } from "../api/hosting";

const noRetryOn403 = (count: number, error: ApiError) => error.status !== 403 && error.status !== 404 && count < 2;

function useVerified() {
  return Boolean(useSession().session?.emailVerified);
}

export function useHostListings(params: { page: number; pageSize: number }) {
  const verified = useVerified();
  return useQuery<PagedList<HostListingSummary>, ApiError>({
    queryKey: queryKeys.hosting.listingsPage(params),
    queryFn: () => hostingApi.listings(getBrowserHttp(), params),
    enabled: verified,
    placeholderData: keepPreviousData,
    staleTime: 15_000,
    retry: noRetryOn403,
  });
}

export function useHostListing(id: string | undefined) {
  const verified = useVerified();
  return useQuery({
    queryKey: queryKeys.hosting.listing(id ?? ""),
    queryFn: () => hostingApi.listing(getBrowserHttp(), id!),
    enabled: verified && Boolean(id),
    staleTime: 15_000,
    retry: noRetryOn403,
  });
}

export function useHostBookings(params: HostBookingsParams) {
  const verified = useVerified();
  return useQuery<PagedList<Booking>, ApiError>({
    queryKey: queryKeys.hosting.bookingsList(params),
    queryFn: () => hostingApi.bookings(getBrowserHttp(), params),
    enabled: verified,
    placeholderData: keepPreviousData,
    staleTime: 15_000,
    retry: noRetryOn403,
  });
}

export function useHostCalendar(id: string, from: string, to: string) {
  const verified = useVerified();
  return useQuery({
    queryKey: queryKeys.hosting.calendar(id, from, to),
    queryFn: () => hostingApi.calendar(getBrowserHttp(), id, { from, to }),
    enabled: verified,
    placeholderData: keepPreviousData,
    staleTime: 15_000,
    retry: noRetryOn403,
  });
}

/** Invalidates everything a listing change can affect (host views and public search). */
function useInvalidateListings() {
  const queryClient = useQueryClient();
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.hosting.all }),
      queryClient.invalidateQueries({ queryKey: queryKeys.apartments.all }),
    ]);
}

export function useSaveListing(id?: string) {
  const invalidate = useInvalidateListings();
  return useMutation<string, ApiError, ListingRequest>({
    mutationFn: async (body) => {
      const http = getBrowserHttp();
      if (id) {
        await hostingApi.update(http, id, body);
        return id;
      }
      return hostingApi.create(http, body);
    },
    onSuccess: () => invalidate(),
  });
}

/** Lists or unlists, optimistically in every cached listings page. */
export function useSetListed() {
  const queryClient = useQueryClient();
  const invalidate = useInvalidateListings();
  return useMutation<
    void,
    ApiError,
    { id: string; isListed: boolean },
    { previous: [readonly unknown[], PagedList<HostListingSummary> | undefined][] }
  >({
    mutationFn: ({ id, isListed }) => hostingApi.setListed(getBrowserHttp(), id, isListed),
    onMutate: async ({ id, isListed }) => {
      const filter = { queryKey: [...queryKeys.hosting.listings, "page"] };
      await queryClient.cancelQueries(filter);
      const previous = queryClient.getQueriesData<PagedList<HostListingSummary>>(filter);
      queryClient.setQueriesData<PagedList<HostListingSummary>>(filter, (page) =>
        page ? { ...page, items: page.items.map((l) => (l.id === id ? { ...l, isListed } : l)) } : page,
      );
      return { previous };
    },
    onError: (_e, _v, context) => context?.previous.forEach(([key, data]) => queryClient.setQueryData(key, data)),
    onSettled: () => invalidate(),
  });
}

export function useDeleteListing() {
  const invalidate = useInvalidateListings();
  return useMutation<void, ApiError, string>({
    mutationFn: (id) => hostingApi.remove(getBrowserHttp(), id),
    onSuccess: () => invalidate(),
  });
}

export function useUpdateCalendar(id: string) {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, UpdateCalendarRequest>({
    mutationFn: (body) => hostingApi.updateCalendar(getBrowserHttp(), id, body),
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.hosting.calendarAll(id) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.apartments.all }),
      ]),
  });
}

/**
 * Accept or decline a pending request; the request disappears optimistically from pending lists.
 * Feedback callbacks live on the mutation (not on `mutate`) so they still run after the card unmounts.
 */
export function useAnswerRequest(
  action: "accept" | "decline",
  callbacks: { onSuccess?: () => void; onError?: (error: ApiError) => void } = {},
) {
  const queryClient = useQueryClient();
  return useMutation<
    void,
    ApiError,
    string,
    { previous: [readonly unknown[], PagedList<Booking> | undefined][] }
  >({
    mutationFn: (bookingId) =>
      action === "accept"
        ? hostingApi.accept(getBrowserHttp(), bookingId)
        : hostingApi.decline(getBrowserHttp(), bookingId),
    onMutate: async (bookingId) => {
      const filter = { queryKey: queryKeys.hosting.bookings };
      await queryClient.cancelQueries(filter);
      const previous = queryClient.getQueriesData<PagedList<Booking>>(filter);
      const status = action === "accept" ? BookingStatus.Reserved : BookingStatus.Rejected;
      queryClient.setQueriesData<PagedList<Booking>>(filter, (page) =>
        page ? { ...page, items: page.items.map((b) => (b.id === bookingId ? { ...b, status } : b)) } : page,
      );
      return { previous };
    },
    onSuccess: () => callbacks.onSuccess?.(),
    onError: (error, _v, context) => {
      context?.previous.forEach(([key, data]) => queryClient.setQueryData(key, data));
      callbacks.onError?.(error);
    },
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.hosting.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all }),
      ]),
  });
}

/** Host cancellation of a confirmed stay (full refund for the guest). */
export function useHostCancelBooking() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, string>({
    mutationFn: (bookingId) => hostingApi.cancelBooking(getBrowserHttp(), bookingId),
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.hosting.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.apartments.all }),
      ]),
  });
}

export function usePayoutAccount() {
  const verified = useVerified();
  return useQuery<PayoutAccount, ApiError>({
    queryKey: queryKeys.hosting.payoutAccount,
    queryFn: () => hostingApi.payoutAccount(getBrowserHttp()),
    enabled: verified,
    staleTime: 15_000,
    retry: noRetryOn403,
  });
}

/**
 * Payouts page. While any payout is on hold for an active account, poll: the API
 * releases them in the background once onboarding is complete.
 */
export function usePayouts(params: PayoutsParams, options: { pollOnHold?: boolean } = {}) {
  const verified = useVerified();
  return useQuery<PagedList<Payout>, ApiError>({
    queryKey: queryKeys.hosting.payoutsList(params),
    queryFn: () => hostingApi.payouts(getBrowserHttp(), params),
    enabled: verified,
    placeholderData: keepPreviousData,
    staleTime: 10_000,
    retry: noRetryOn403,
    refetchInterval: (query) =>
      options.pollOnHold && query.state.data?.items.some((p) => p.status === "OnHold") ? 4_000 : false,
  });
}

export function useStartOnboarding() {
  return useMutation<string, ApiError, void>({
    mutationFn: () => hostingApi.startOnboarding(getBrowserHttp()),
  });
}
