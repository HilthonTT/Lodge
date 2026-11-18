"use client";

import { useQuery } from "@tanstack/react-query";

import { getBrowserHttp } from "@/lib/api/browser";
import { addDays, toDateOnly, today } from "@/lib/dates";
import { queryKeys } from "@/lib/query-keys";

import { apartmentsApi } from "../api/apartments";

/** Nightly prices and availability for the next year (the API allows up to 366 days). */
export function useApartmentCalendar(apartmentId: string, enabled = true) {
  const from = toDateOnly(today());
  const to = toDateOnly(addDays(today(), 365));
  return useQuery({
    queryKey: queryKeys.apartments.calendar(apartmentId, from, to),
    queryFn: () => apartmentsApi.calendar(getBrowserHttp(), apartmentId, { from, to }),
    enabled,
    staleTime: 60_000,
    select: (days) => new Map(days.map((day) => [day.date, day])),
  });
}
