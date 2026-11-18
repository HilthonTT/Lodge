"use client";

import { useQueryClient } from "@tanstack/react-query";
import Cookies from "js-cookie";
import { useLocale } from "next-intl";
import { createContext, use, useCallback, useMemo, useState } from "react";

import {
  convertAmount,
  CURRENCY_COOKIE,
  CURRENCY_COOKIE_MAX_AGE,
  type DisplayCurrency,
  isDisplayCurrency,
} from "@/lib/currency";
import { formatPrice, formatPriceCompact } from "@/lib/format";
import { queryKeys } from "@/lib/query-keys";
import type { ExchangeRates } from "@/types/api";

import { useExchangeRates } from "./hooks/use-payments";

type CurrencyContextValue = {
  currency: DisplayCurrency;
  setCurrency: (currency: DisplayCurrency) => void;
};

const CurrencyContext = createContext<CurrencyContextValue | null>(null);

/**
 * Display currency for prices. The server resolves the initial value from the
 * `LODGE_CURRENCY` cookie (or the locale's default); picking one stores it in
 * the cookie for a year. Charged amounts always stay in the listing's currency.
 */
export function CurrencyProvider({
  initialCurrency,
  initialRates,
  children,
}: {
  initialCurrency: DisplayCurrency;
  /** Rates fetched by the server, so converted prices render on first paint. */
  initialRates?: ExchangeRates | null;
  children: React.ReactNode;
}) {
  const queryClient = useQueryClient();
  // Seed the cache once, before any price renders (same pattern as HydrationBoundary).
  useState(() => {
    if (initialRates && !queryClient.getQueryData(queryKeys.payments.currencies)) {
      queryClient.setQueryData(queryKeys.payments.currencies, initialRates);
    }
    return null;
  });
  // Until the user picks one, follow the server's value (it changes with the locale).
  const [chosen, setChosen] = useState<DisplayCurrency | null>(null);
  const currency = chosen ?? initialCurrency;

  const setCurrency = useCallback((next: DisplayCurrency) => {
    if (!isDisplayCurrency(next)) return;
    Cookies.set(CURRENCY_COOKIE, next, {
      path: "/",
      expires: CURRENCY_COOKIE_MAX_AGE / 86_400,
      sameSite: "lax",
      secure: window.location.protocol === "https:",
    });
    setChosen(next);
  }, []);

  const value = useMemo(() => ({ currency, setCurrency }), [currency, setCurrency]);
  return <CurrencyContext value={value}>{children}</CurrencyContext>;
}

export function useCurrency(): CurrencyContextValue {
  const context = use(CurrencyContext);
  if (!context) throw new Error("useCurrency must be used inside <CurrencyProvider>");
  return context;
}

export type DisplayMoney = {
  amount: number;
  currency: string;
  /** True when the amount was converted from another currency. */
  converted: boolean;
  /** The original currency. */
  source: string;
};

/**
 * Converts API amounts to the visitor's display currency with the cached
 * `/currencies` rates. Falls back to the original currency while rates load.
 */
export function useDisplayMoney() {
  const { currency } = useCurrency();
  const locale = useLocale();
  const rates = useExchangeRates();
  const table = rates.data?.rates;

  return useMemo(() => {
    const convert = (amount: number, from: string): DisplayMoney => {
      const value = from === currency ? amount : convertAmount(amount, from, currency, table);
      return value === null
        ? { amount, currency: from, converted: false, source: from }
        : { amount: value, currency, converted: from !== currency, source: from };
    };
    /** Converts a display-currency amount back to `to` (e.g. USD for the API). */
    const convertBack = (amount: number, to: string): number | null => convertAmount(amount, currency, to, table);
    const format = (amount: number, from: string, compact = false) => {
      const money = convert(amount, from);
      return (compact ? formatPriceCompact : formatPrice)(money.amount, money.currency, locale);
    };
    return { currency, convert, convertBack, format, ready: Boolean(table) };
  }, [currency, table, locale]);
}
