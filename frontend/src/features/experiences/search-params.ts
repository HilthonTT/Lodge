import { z } from "zod";

import { EXPERIENCE_CATEGORIES, type ExperienceCategory, type ExperienceSearchParams } from "@/types/api";

export const EXPERIENCES_PAGE_SIZE = 12;

const first = (v: unknown) => (Array.isArray(v) ? v[0] : v);
const optionalText = z.preprocess(first, z.string().trim().max(100).optional().catch(undefined));
const optionalNumber = (max: number) =>
  z.preprocess(
    (v) => (first(v) === "" ? undefined : first(v)),
    z.coerce.number().min(0).max(max).optional().catch(undefined),
  );

const schema = z.object({
  q: optionalText,
  category: z.preprocess(first, z.enum(EXPERIENCE_CATEGORIES).optional().catch(undefined)),
  date: z.preprocess(first, z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional().catch(undefined)),
  guests: z.preprocess(first, z.coerce.number().int().min(1).max(50).optional().catch(undefined)),
  minPrice: optionalNumber(100_000),
  maxPrice: optionalNumber(100_000),
  page: z.preprocess(first, z.coerce.number().int().min(1).max(1000).catch(1)).default(1),
  view: z.preprocess(first, z.enum(["list", "map"]).catch("list")).default("list"),
});

/** /experiences URL state. Prices are in USD, like the API compares them. */
export type ExperienceSearch = {
  q?: string;
  category?: ExperienceCategory;
  date?: string;
  guests?: number;
  minPrice?: number;
  maxPrice?: number;
  page: number;
  view: "list" | "map";
};

export function parseExperienceSearch(raw: Record<string, string | string[] | undefined>): ExperienceSearch {
  return schema.parse(raw);
}

export function experiencesHref(search: Partial<ExperienceSearch>): string {
  const params = new URLSearchParams();
  if (search.q) params.set("q", search.q);
  if (search.category) params.set("category", search.category);
  if (search.date) params.set("date", search.date);
  if (search.guests) params.set("guests", String(search.guests));
  if (search.minPrice !== undefined) params.set("minPrice", String(search.minPrice));
  if (search.maxPrice !== undefined) params.set("maxPrice", String(search.maxPrice));
  if (search.view === "map") params.set("view", "map");
  if (search.page && search.page > 1) params.set("page", String(search.page));
  const query = params.toString();
  return query ? `/experiences?${query}` : "/experiences";
}

/** The `q` box matches titles, cities and countries on the API. */
export function toExperienceApiParams(search: ExperienceSearch): ExperienceSearchParams {
  return {
    q: search.q,
    category: search.category,
    date: search.date,
    guests: search.guests,
    minPrice: search.minPrice,
    maxPrice: search.maxPrice,
    page: search.page,
    pageSize: EXPERIENCES_PAGE_SIZE,
  };
}
