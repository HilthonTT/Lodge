"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { getBrowserHttp } from "@/lib/api/browser";
import type { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import { type Booking, BookingStatus, type PagedList } from "@/types/api";

import { type AdminBookingsParams, bookingsApi } from "../api/bookings";

/** Admin: one page of every booking, optionally filtered by status. */
export function useAdminBookings(params: AdminBookingsParams) {
  return useQuery({
    queryKey: queryKeys.bookings.adminList(params),
    queryFn: () => bookingsApi.list(getBrowserHttp(), params),
    // Keep the current page on screen while the next filter/page loads.
    placeholderData: keepPreviousData,
    staleTime: 15_000,
  });
}

type AdminAction = "reject" | "complete";

const NEXT_STATUS: Record<AdminAction, BookingStatus> = {
  reject: BookingStatus.Rejected,
  complete: BookingStatus.Completed,
};

/**
 * Admin: reject a reserved booking or complete a finished stay. The status is
 * updated optimistically in every cached admin page and rolled back on error.
 */
export function useAdminBookingAction(action: AdminAction) {
  const queryClient = useQueryClient();

  return useMutation<void, ApiError, string, { previous: [readonly unknown[], PagedList<Booking> | undefined][] }>({
    mutationFn: (bookingId) =>
      action === "reject"
        ? bookingsApi.reject(getBrowserHttp(), bookingId)
        : bookingsApi.complete(getBrowserHttp(), bookingId),
    onMutate: async (bookingId) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.bookings.admin });
      const previous = queryClient.getQueriesData<PagedList<Booking>>({ queryKey: queryKeys.bookings.admin });
      queryClient.setQueriesData<PagedList<Booking>>({ queryKey: queryKeys.bookings.admin }, (page) =>
        page && {
          ...page,
          items: page.items.map((b) => (b.id === bookingId ? { ...b, status: NEXT_STATUS[action] } : b)),
        },
      );
      return { previous };
    },
    onError: (_error, _bookingId, context) => {
      for (const [key, data] of context?.previous ?? []) queryClient.setQueryData(key, data);
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all }),
  });
}
