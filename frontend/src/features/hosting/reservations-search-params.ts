import { z } from "zod";

import { BookingStatus } from "@/types/api";

export const RESERVATIONS_PAGE_SIZE = 10;

/**
 * Status chips for /hosting/reservations, keyed by URL slug (`?status=pending`).
 * Labels come from `bookings.status.<labelKey>` (or `hosting.reservations.all`).
 */
export const RESERVATION_FILTERS = {
  all: { status: undefined, labelKey: undefined },
  pending: { status: BookingStatus.PendingApproval, labelKey: "pendingApproval" },
  reserved: { status: BookingStatus.Reserved, labelKey: "reserved" },
  confirmed: { status: BookingStatus.Confirmed, labelKey: "confirmed" },
  completed: { status: BookingStatus.Completed, labelKey: "completed" },
  cancelled: { status: BookingStatus.Cancelled, labelKey: "cancelled" },
  rejected: { status: BookingStatus.Rejected, labelKey: "rejected" },
  expired: { status: BookingStatus.Expired, labelKey: "expired" },
} as const;

export type ReservationFilter = keyof typeof RESERVATION_FILTERS;

const first = (v: unknown) => (Array.isArray(v) ? v[0] : v);
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const schema = z.object({
  page: z.preprocess(first, z.coerce.number().int().min(1).max(10_000).catch(1)).default(1),
  status: z
    .preprocess(
      first,
      z.enum(Object.keys(RESERVATION_FILTERS) as [ReservationFilter, ...ReservationFilter[]]).catch("all"),
    )
    .default("all"),
  listing: z.preprocess(first, z.string().regex(UUID).optional().catch(undefined)),
});

export type ReservationsSearch = { page: number; status: ReservationFilter; listing?: string };

export function parseReservationsSearch(raw: Record<string, string | string[] | undefined>): ReservationsSearch {
  return schema.parse(raw);
}

export function reservationsHref(search: ReservationsSearch) {
  const params = new URLSearchParams();
  if (search.status !== "all") params.set("status", search.status);
  if (search.listing) params.set("listing", search.listing);
  if (search.page > 1) params.set("page", String(search.page));
  const query = params.toString();
  return query ? `/hosting/reservations?${query}` : "/hosting/reservations";
}
