"use client";

import { useQuery } from "@tanstack/react-query";

import { getBrowserHttp } from "@/lib/api/browser";
import { queryKeys } from "@/lib/query-keys";

import { apartmentsApi } from "../api/apartments";

export function useApartment(apartmentId: string, enabled = true) {
  return useQuery({
    queryKey: queryKeys.apartments.detail(apartmentId),
    queryFn: () => apartmentsApi.get(getBrowserHttp(), apartmentId),
    enabled,
    staleTime: 5 * 60_000,
  });
}

/** Unavailable nights (bookings, pending requests, blocked nights). */
export function useBookedDates(apartmentId: string, enabled = true) {
  return useQuery({
    queryKey: queryKeys.apartments.bookedDates(apartmentId),
    queryFn: () => apartmentsApi.bookedDates(getBrowserHttp(), apartmentId),
    enabled,
    staleTime: 30_000,
  });
}
