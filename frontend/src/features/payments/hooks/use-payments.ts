"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { getBrowserHttp } from "@/lib/api/browser";
import type { ApiError } from "@/lib/api/client";
import { queryKeys } from "@/lib/query-keys";
import type { ExchangeRates, PaymentsConfig } from "@/types/api";

import { paymentsApi } from "../api/payments";

const HOUR = 60 * 60_000;

export function usePaymentsConfig() {
  return useQuery<PaymentsConfig, ApiError>({
    queryKey: queryKeys.payments.config,
    queryFn: () => paymentsApi.config(getBrowserHttp()),
    staleTime: HOUR,
  });
}

/** Rates change at most daily: cache them for the whole session. */
export function useExchangeRates(enabled = true) {
  return useQuery<ExchangeRates, ApiError>({
    queryKey: queryKeys.payments.currencies,
    queryFn: () => paymentsApi.currencies(getBrowserHttp()),
    staleTime: 6 * HOUR,
    gcTime: 24 * HOUR,
    enabled,
  });
}

export function useCompleteSimulatedPayment() {
  const queryClient = useQueryClient();
  return useMutation<void, ApiError, string>({
    mutationFn: (bookingId) => paymentsApi.completeSimulated(getBrowserHttp(), bookingId),
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all }),
        queryClient.invalidateQueries({ queryKey: queryKeys.hosting.all }),
      ]),
  });
}
