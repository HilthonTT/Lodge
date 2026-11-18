import { z } from "zod";

import { parseStayRange, toDateOnly } from "@/lib/dates";
import type { ApartmentSortColumn, SortOrder } from "@/types/api";

export const PAGE_SIZE = 24;
export const MAX_GUESTS = 16;
export const MAX_ROOMS = 10;

/** Sort options keyed by URL slug; `labelKey` lives under `search.sort` in messages/*.json. */
export const SORT_OPTIONS = {
  recommended: { labelKey: "recommended" },
  "top-rated": { labelKey: "topRated", column: "rating", order: "desc" },
  "price-asc": { labelKey: "priceAsc", column: "price_amount", order: "asc" },
  "price-desc": { labelKey: "priceDesc", column: "price_amount", order: "desc" },
  name: { labelKey: "name", column: "name", order: "asc" },
} as const satisfies Record<
  string,
  { labelKey: "recommended" | "topRated" | "priceAsc" | "priceDesc" | "name"; column?: ApartmentSortColumn; order?: SortOrder }
>;

export type SortKey = keyof typeof SORT_OPTIONS;

type RawSearchParams = Record<string, string | string[] | undefined>;

const first = (v: unknown) => (Array.isArray(v) ? v[0] : v);

const positiveInt = (max: number) =>
  z.preprocess(first, z.coerce.number().int().min(1).max(max).optional().catch(undefined));

const schema = z.object({
  page: z.preprocess(first, z.coerce.number().int().min(1).max(10_000).catch(1)).default(1),
  q: z.preprocess(first, z.string().trim().max(100).optional().catch(undefined)),
  startDate: z.preprocess(first, z.string().optional().catch(undefined)),
  endDate: z.preprocess(first, z.string().optional().catch(undefined)),
  guests: positiveInt(MAX_GUESTS),
  rooms: positiveInt(MAX_ROOMS),
  sort: z.preprocess(
    first,
    z.enum(Object.keys(SORT_OPTIONS) as [SortKey, ...SortKey[]]).catch("recommended"),
  ).default("recommended"),
});

export type ApartmentSearch = {
  page: number;
  q?: string;
  guests?: number;
  rooms?: number;
  sort: SortKey;
  /** Only present when both dates are valid and in the future. */
  range?: { from: Date; to: Date };
};

/** Parses (and sanitizes) the search params of `/` and `/search`. Never throws. */
export function parseApartmentSearch(raw: RawSearchParams): ApartmentSearch {
  const parsed = schema.parse(raw);
  return {
    page: parsed.page,
    q: parsed.q || undefined,
    guests: parsed.guests,
    rooms: parsed.rooms,
    sort: parsed.sort,
    range: parseStayRange(parsed.startDate, parsed.endDate),
  };
}

/** Serializes a search back to URL params (omitting defaults). */
export function toSearchParams(search: Partial<ApartmentSearch>): Record<string, string | undefined> {
  return {
    q: search.q || undefined,
    startDate: search.range ? toDateOnly(search.range.from) : undefined,
    endDate: search.range ? toDateOnly(search.range.to) : undefined,
    guests: search.guests && search.guests > 1 ? String(search.guests) : undefined,
    rooms: search.rooms && search.rooms > 1 ? String(search.rooms) : undefined,
    sort: search.sort && search.sort !== "recommended" ? search.sort : undefined,
    page: search.page && search.page > 1 ? String(search.page) : undefined,
  };
}

export function buildUrl(pathname: string, params: Record<string, string | undefined>) {
  const query = new URLSearchParams(
    Object.entries(params).filter((e): e is [string, string] => Boolean(e[1])),
  ).toString();
  return query ? `${pathname}?${query}` : pathname;
}

export function totalPages(totalCount: number, pageSize: number) {
  if (!totalCount || !pageSize) return 0;
  return Math.ceil(totalCount / pageSize);
}
