import { z } from "zod";

import { parseStayRange, toDateOnly } from "@/lib/dates";
import {
  AMENITY_VALUES,
  type Amenity,
  type ApartmentSortColumn,
  type FlexibleStay,
  PROPERTY_TYPES,
  type PropertyType,
  type SortOrder,
} from "@/types/api";

import type { SearchApartmentsParams } from "./api/apartments";
import { REAL_AMENITIES } from "./listing-options";

export const PAGE_SIZE = 24;
export const MAX_GUESTS = 16;
export const MAX_ROOMS = 10;
export const MAX_BEDS = 16;
export const MAX_BATHS = 8;

/** Sort options keyed by URL slug; `labelKey` lives under `search.sort` in messages/*.json. */
export const SORT_OPTIONS = {
  recommended: { labelKey: "recommended" },
  "top-rated": { labelKey: "topRated", column: "rating", order: "desc" },
  "price-asc": { labelKey: "priceAsc", column: "price_amount", order: "asc" },
  "price-desc": { labelKey: "priceDesc", column: "price_amount", order: "desc" },
  newest: { labelKey: "newest", column: "newest", order: "desc" },
  name: { labelKey: "name", column: "name", order: "asc" },
} as const satisfies Record<
  string,
  {
    labelKey: "recommended" | "topRated" | "priceAsc" | "priceDesc" | "newest" | "name";
    column?: ApartmentSortColumn;
    order?: SortOrder;
  }
>;

export type SortKey = keyof typeof SORT_OPTIONS;

export const FLEXIBLE_STAYS = ["weekend", "week", "month"] as const satisfies readonly FlexibleStay[];

type RawSearchParams = Record<string, string | string[] | undefined>;

const first = (v: unknown) => (Array.isArray(v) ? v[0] : v);

/** Repeated (`?a=x&a=y`) or comma-separated (`?a=x,y`) list. */
const list = (v: unknown): string[] =>
  (Array.isArray(v) ? v : typeof v === "string" ? [v] : [])
    .flatMap((s) => String(s).split(","))
    .map((s) => s.trim())
    .filter(Boolean);

const positiveInt = (max: number) =>
  z.preprocess(first, z.coerce.number().int().min(1).max(max).optional().catch(undefined));

const nonNegative = z.preprocess(first, z.coerce.number().min(0).max(1_000_000).optional().catch(undefined));

const coordinate = (limit: number) =>
  z.preprocess(first, z.coerce.number().min(-limit).max(limit).optional().catch(undefined));

const flag = z.preprocess(first, z.enum(["true", "1"]).optional().catch(undefined));

const schema = z.object({
  page: z.preprocess(first, z.coerce.number().int().min(1).max(10_000).catch(1)).default(1),
  q: z.preprocess(first, z.string().trim().max(100).optional().catch(undefined)),
  startDate: z.preprocess(first, z.string().optional().catch(undefined)),
  endDate: z.preprocess(first, z.string().optional().catch(undefined)),
  guests: positiveInt(MAX_GUESTS),
  bedrooms: positiveInt(MAX_ROOMS),
  beds: positiveInt(MAX_BEDS),
  baths: positiveInt(MAX_BATHS),
  minPrice: nonNegative,
  maxPrice: nonNegative,
  instant: flag,
  superhost: flag,
  minLat: coordinate(90),
  maxLat: coordinate(90),
  minLng: coordinate(180),
  maxLng: coordinate(180),
  flexible: z.preprocess(first, z.enum(FLEXIBLE_STAYS).optional().catch(undefined)),
  sort: z
    .preprocess(first, z.enum(Object.keys(SORT_OPTIONS) as [SortKey, ...SortKey[]]).catch("recommended"))
    .default("recommended"),
});

export type MapBounds = { minLat: number; maxLat: number; minLng: number; maxLng: number };

export type ApartmentSearch = {
  page: number;
  q?: string;
  guests?: number;
  bedrooms?: number;
  beds?: number;
  baths?: number;
  minPrice?: number;
  maxPrice?: number;
  types: PropertyType[];
  /** Real amenities and category tags (the API requires all of them). */
  amenities: Amenity[];
  instant: boolean;
  superhost: boolean;
  /** Only set while "Search as I move the map" is on. */
  bounds?: MapBounds;
  /** Flexible dates; never combined with `range`. */
  flexible?: FlexibleStay;
  /** "yyyy-MM" months for a flexible search. */
  months: string[];
  sort: SortKey;
  /** Only present when both dates are valid and in the future. */
  range?: { from: Date; to: Date };
};

export const EMPTY_SEARCH: ApartmentSearch = {
  page: 1,
  types: [],
  amenities: [],
  instant: false,
  superhost: false,
  months: [],
  sort: "recommended",
};

const MONTH = /^\d{4}-(0[1-9]|1[0-2])$/;

/** Parses (and sanitizes) the search params of `/` and `/search`. Never throws. */
export function parseApartmentSearch(raw: RawSearchParams): ApartmentSearch {
  const parsed = schema.parse(raw);
  const range = parseStayRange(parsed.startDate, parsed.endDate);
  const { minLat, maxLat, minLng, maxLng } = parsed;
  const bounds =
    minLat !== undefined && maxLat !== undefined && minLng !== undefined && maxLng !== undefined && minLat < maxLat
      ? { minLat, maxLat, minLng, maxLng }
      : undefined;
  let { minPrice, maxPrice } = parsed;
  if (minPrice !== undefined && maxPrice !== undefined && minPrice > maxPrice) [minPrice, maxPrice] = [maxPrice, minPrice];
  const months = [...new Set(list(raw.months).filter((m) => MONTH.test(m)))].sort().slice(0, 12);
  const flexible = range ? undefined : parsed.flexible;

  return {
    page: parsed.page,
    q: parsed.q || undefined,
    guests: parsed.guests,
    bedrooms: parsed.bedrooms,
    beds: parsed.beds,
    baths: parsed.baths,
    minPrice,
    maxPrice,
    types: [...new Set(list(raw.types))].filter((t): t is PropertyType =>
      (PROPERTY_TYPES as readonly string[]).includes(t),
    ),
    amenities: [...new Set(list(raw.amenities))].filter((a): a is Amenity =>
      (AMENITY_VALUES as readonly string[]).includes(a),
    ),
    instant: parsed.instant !== undefined,
    superhost: parsed.superhost !== undefined,
    bounds,
    flexible,
    months: flexible ? months : [],
    sort: parsed.sort,
    range,
  };
}

const round = (value: number) => String(Math.round(value * 10_000) / 10_000);

/** Serializes a search back to URL params (omitting defaults). */
export function toSearchParams(search: Partial<ApartmentSearch>): Record<string, string | undefined> {
  const flexible = search.range ? undefined : search.flexible;
  return {
    q: search.q || undefined,
    startDate: search.range ? toDateOnly(search.range.from) : undefined,
    endDate: search.range ? toDateOnly(search.range.to) : undefined,
    flexible,
    months: flexible && search.months?.length ? search.months.join(",") : undefined,
    guests: search.guests && search.guests > 1 ? String(search.guests) : undefined,
    bedrooms: search.bedrooms ? String(search.bedrooms) : undefined,
    beds: search.beds ? String(search.beds) : undefined,
    baths: search.baths ? String(search.baths) : undefined,
    minPrice: search.minPrice !== undefined ? String(search.minPrice) : undefined,
    maxPrice: search.maxPrice !== undefined ? String(search.maxPrice) : undefined,
    types: search.types?.length ? search.types.join(",") : undefined,
    amenities: search.amenities?.length ? search.amenities.join(",") : undefined,
    instant: search.instant ? "true" : undefined,
    superhost: search.superhost ? "true" : undefined,
    minLat: search.bounds ? round(search.bounds.minLat) : undefined,
    maxLat: search.bounds ? round(search.bounds.maxLat) : undefined,
    minLng: search.bounds ? round(search.bounds.minLng) : undefined,
    maxLng: search.bounds ? round(search.bounds.maxLng) : undefined,
    sort: search.sort && search.sort !== "recommended" ? search.sort : undefined,
    page: search.page && search.page > 1 ? String(search.page) : undefined,
  };
}

/** Months to search when "flexible" is picked without any month: this one and the next two. */
export function defaultFlexibleMonths(now = new Date()): string[] {
  return [0, 1, 2].map((offset) => {
    const d = new Date(now.getFullYear(), now.getMonth() + offset, 1);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
  });
}

/** The API query for a search (`page`/`pageSize` included). */
export function toApiParams(search: ApartmentSearch, pageSize = PAGE_SIZE): SearchApartmentsParams {
  const sort = SORT_OPTIONS[search.sort];
  const flexible = search.range ? undefined : search.flexible;
  return {
    page: search.page,
    pageSize,
    searchTerm: search.q,
    guestCount: search.guests,
    bedrooms: search.bedrooms,
    beds: search.beds,
    bathrooms: search.baths,
    minPrice: search.minPrice,
    maxPrice: search.maxPrice,
    propertyTypes: search.types,
    amenities: search.amenities,
    instantBook: search.instant || undefined,
    superhost: search.superhost || undefined,
    ...(search.bounds ?? {}),
    startDate: search.range ? toDateOnly(search.range.from) : undefined,
    endDate: search.range ? toDateOnly(search.range.to) : undefined,
    flexible,
    months: flexible ? (search.months.length ? search.months : defaultFlexibleMonths()) : undefined,
    sortColumn: "column" in sort ? sort.column : undefined,
    sortOrder: "order" in sort ? sort.order : undefined,
  };
}

/** Number of filters set in the Filters dialog (category tags don't count; they have their own bar). */
export function countActiveFilters(search: ApartmentSearch): number {
  return (
    (search.minPrice !== undefined || search.maxPrice !== undefined ? 1 : 0) +
    search.types.length +
    (search.bedrooms ? 1 : 0) +
    (search.beds ? 1 : 0) +
    (search.baths ? 1 : 0) +
    search.amenities.filter((a) => REAL_AMENITIES.includes(a)).length +
    (search.instant ? 1 : 0) +
    (search.superhost ? 1 : 0)
  );
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
