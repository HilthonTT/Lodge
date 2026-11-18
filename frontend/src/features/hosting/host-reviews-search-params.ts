import { z } from "zod";

export const HOST_REVIEWS_PAGE_SIZE = 10;

const first = (v: unknown) => (Array.isArray(v) ? v[0] : v);
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const schema = z.object({
  page: z.preprocess(first, z.coerce.number().int().min(1).max(10_000).catch(1)).default(1),
  listing: z.preprocess(first, z.string().regex(UUID).optional().catch(undefined)),
});

export type HostReviewsSearch = { page: number; listing?: string };

export function parseHostReviewsSearch(raw: Record<string, string | string[] | undefined>): HostReviewsSearch {
  return schema.parse(raw);
}

export function hostReviewsHref(search: HostReviewsSearch) {
  const params = new URLSearchParams();
  if (search.listing) params.set("listing", search.listing);
  if (search.page > 1) params.set("page", String(search.page));
  const query = params.toString();
  return query ? `/hosting/reviews?${query}` : "/hosting/reviews";
}
