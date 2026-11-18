import { z } from "zod";

import type { PayoutStatus } from "@/types/api";

export const PAYOUTS_PAGE_SIZE = 10;

/** Status chips for /hosting/payouts, keyed by URL slug (`?status=on-hold`). */
export const PAYOUT_FILTERS = {
  all: undefined,
  scheduled: "Scheduled",
  "on-hold": "OnHold",
  paid: "Paid",
  failed: "Failed",
  cancelled: "Cancelled",
} as const satisfies Record<string, PayoutStatus | undefined>;

export type PayoutFilter = keyof typeof PAYOUT_FILTERS;

const first = (v: unknown) => (Array.isArray(v) ? v[0] : v);

const schema = z.object({
  page: z.preprocess(first, z.coerce.number().int().min(1).max(10_000).catch(1)).default(1),
  status: z
    .preprocess(first, z.enum(Object.keys(PAYOUT_FILTERS) as [PayoutFilter, ...PayoutFilter[]]).catch("all"))
    .default("all"),
  onboarding: z.preprocess(first, z.enum(["complete", "refresh"]).optional().catch(undefined)),
});

export type PayoutsSearch = { page: number; status: PayoutFilter; onboarding?: "complete" | "refresh" };

export function parsePayoutsSearch(raw: Record<string, string | string[] | undefined>): PayoutsSearch {
  return schema.parse(raw);
}

export function payoutsHref(search: Pick<PayoutsSearch, "page" | "status">) {
  const params = new URLSearchParams();
  if (search.status !== "all") params.set("status", search.status);
  if (search.page > 1) params.set("page", String(search.page));
  const query = params.toString();
  return query ? `/hosting/payouts?${query}` : "/hosting/payouts";
}
