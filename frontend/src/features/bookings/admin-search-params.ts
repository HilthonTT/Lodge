import { z } from "zod";

import { BookingStatus } from "@/types/api";

export const ADMIN_PAGE_SIZE = 20;

/**
 * Status filter chips for /admin/bookings, keyed by their URL slug (`?status=reserved`).
 * Labels: `admin.filterAll` and `bookings.status.<slug>` in messages/*.json.
 */
export const ADMIN_STATUS_FILTERS = {
  all: { status: undefined },
  pendingApproval: { status: BookingStatus.PendingApproval },
  reserved: { status: BookingStatus.Reserved },
  confirmed: { status: BookingStatus.Confirmed },
  completed: { status: BookingStatus.Completed },
  cancelled: { status: BookingStatus.Cancelled },
  rejected: { status: BookingStatus.Rejected },
  expired: { status: BookingStatus.Expired },
} as const satisfies Record<string, { status: BookingStatus | undefined }>;

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
