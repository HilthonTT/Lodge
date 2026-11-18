import "server-only";

import { cookies } from "next/headers";
import { getLocale } from "next-intl/server";
import { cache } from "react";

import { SERVER_API_URL } from "@/lib/env.server";
import type { ExchangeRates } from "@/types/api";

import { CURRENCY_COOKIE, type DisplayCurrency, resolveCurrency } from "./currency";

/** The visitor's display currency: the cookie, or the locale's default. */
export const getServerCurrency = cache(async (): Promise<DisplayCurrency> => {
  const [store, locale] = await Promise.all([cookies(), getLocale()]);
  return resolveCurrency(store.get(CURRENCY_COOKIE)?.value, locale);
});

/**
 * Exchange rates for server-rendered prices, cached by Next for an hour so
 * pages don't hit the API on every request. Null when the API is unreachable.
 */
export const loadExchangeRates = cache(async (): Promise<ExchangeRates | null> => {
  try {
    const response = await fetch(`${SERVER_API_URL}/currencies`, {
      headers: { Accept: "application/json" },
      next: { revalidate: 3600 },
      signal: AbortSignal.timeout(3_000),
    });
    if (!response.ok) return null;
    const data = (await response.json()) as ExchangeRates;
    return data && typeof data.rates === "object" ? data : null;
  } catch {
    return null;
  }
});
