"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";

import { getBrowserHttp } from "@/lib/api/browser";
import { toDateOnly, type StayRange } from "@/lib/dates";
import { queryKeys } from "@/lib/query-keys";

import { apartmentsApi } from "../api/apartments";

/** Live price breakdown for a stay; disabled until a valid range is picked. */
export function useApartmentPrice(apartmentId: string, range: StayRange | undefined) {
  const start = range ? toDateOnly(range.from) : "";
  const end = range ? toDateOnly(range.to) : "";

  return useQuery({
    queryKey: queryKeys.apartments.price(apartmentId, start, end),
    queryFn: () => apartmentsApi.price(getBrowserHttp(), apartmentId, start, end),
    enabled: Boolean(range),
    placeholderData: keepPreviousData,
    staleTime: 5 * 60_000,
  });
}
