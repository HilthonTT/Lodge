"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";

import { getBrowserHttp } from "@/lib/api/browser";
import { queryKeys } from "@/lib/query-keys";

import { apartmentsApi, type SearchApartmentsParams } from "../api/apartments";
import { type ApartmentSearch, toApiParams } from "../search-params";

/** `value`, but only after it stopped changing for `delay` ms. */
export function useDebouncedValue<T>(value: T, delay = 250): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delay);
    return () => window.clearTimeout(timer);
  }, [value, delay]);
  return debounced;
}

/** Cheapest and most expensive nightly price of listed stays, for the price slider. */
export function usePriceRange(enabled = true) {
  return useQuery({
    queryKey: queryKeys.apartments.priceRange,
    queryFn: () => apartmentsApi.priceRange(getBrowserHttp()),
    staleTime: 10 * 60_000,
    enabled,
  });
}

/** Live "Show N stays" count for a draft search (debounced, pageSize 1). */
export function useSearchCount(search: ApartmentSearch, enabled = true) {
  const key = JSON.stringify({ ...toApiParams({ ...search, page: 1 }, 1), sortColumn: undefined, sortOrder: undefined });
  // Debounce the serialized params: a fresh object every render would never settle.
  const debouncedKey = useDebouncedValue(key, 300);
  const params = useMemo(() => JSON.parse(debouncedKey) as SearchApartmentsParams, [debouncedKey]);
  return useQuery({
    queryKey: queryKeys.apartments.search(params),
    queryFn: () => apartmentsApi.search(getBrowserHttp(), params).then((r) => r.totalCount),
    placeholderData: keepPreviousData,
    staleTime: 60_000,
    enabled,
  });
}

/** Destination autocomplete (2+ characters, debounced). */
export function useLocationSuggestions(query: string) {
  const q = useDebouncedValue(query.trim(), 250);
  const enabled = q.length >= 2 && q.length <= 100;
  const result = useQuery({
    queryKey: queryKeys.apartments.suggest(q.toLowerCase()),
    queryFn: ({ signal }) => apartmentsApi.suggestLocations(getBrowserHttp(), q, signal),
    enabled,
    staleTime: 5 * 60_000,
    placeholderData: keepPreviousData,
  });
  return { data: result.data, isFetching: result.isFetching, enabled, settled: q === query.trim() };
}
