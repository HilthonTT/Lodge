import { z } from "zod";

import { BookingStatus } from "@/types/api";

export const ADMIN_PAGE_SIZE = 20;

/** Status filter chips for /admin/bookings, keyed by their URL slug (`?status=reserved`). */
export const ADMIN_STATUS_FILTERS = {
  all: { label: "All", status: undefined },
  reserved: { label: "Awaiting payment", status: BookingStatus.Reserved },
  confirmed: { label: "Confirmed", status: BookingStatus.Confirmed },
  completed: { label: "Completed", status: BookingStatus.Completed },
  cancelled: { label: "Cancelled", status: BookingStatus.Cancelled },
  rejected: { label: "Rejected", status: BookingStatus.Rejected },
} as const satisfies Record<string, { label: string; status: BookingStatus | undefined }>;

export type AdminStatusFilter = keyof typeof ADMIN_STATUS_FILTERS;

const first = (v: unknown) => (Array.isArray(v) ? v[0] : v);

const schema = z.object({
  page: z.preprocess(first, z.coerce.number().int().min(1).max(10_000).catch(1)).default(1),
  status: z
    .preprocess(
      first,
      z.enum(Object.keys(ADMIN_STATUS_FILTERS) as [AdminStatusFilter, ...AdminStatusFilter[]]).catch("all"),
    )
    .default("all"),
});

export type AdminBookingsSearch = { page: number; status: AdminStatusFilter };

/** Parses (and sanitizes) the search params of /admin/bookings. Never throws. */
export function parseAdminBookingsSearch(raw: Record<string, string | string[] | undefined>): AdminBookingsSearch {
  return schema.parse(raw);
}

/** URL params for a search, omitting defaults. */
export function toAdminSearchParams(search: AdminBookingsSearch): Record<string, string | undefined> {
  return {
    status: search.status !== "all" ? search.status : undefined,
    page: search.page > 1 ? String(search.page) : undefined,
  };
}
