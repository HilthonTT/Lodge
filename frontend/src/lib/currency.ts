import { DEFAULT_LOCALE, isLocale, type Locale } from "@/i18n/config";

/** Display currencies offered in the globe dialog. */
export const CURRENCIES = ["USD", "EUR", "GBP", "JPY", "CHF", "CAD", "AUD"] as const;
export type DisplayCurrency = (typeof CURRENCIES)[number];

/** Cookie holding the chosen display currency (set by the currency picker). */
export const CURRENCY_COOKIE = "LODGE_CURRENCY";
export const CURRENCY_COOKIE_MAX_AGE = 60 * 60 * 24 * 365;

/** The API's base currency: price filters and the price range are in USD. */
export const BASE_CURRENCY = "USD";

const LOCALE_DEFAULTS: Record<Locale, DisplayCurrency> = { en: "USD", nl: "EUR", fr: "EUR" };

export function isDisplayCurrency(value: unknown): value is DisplayCurrency {
  return typeof value === "string" && (CURRENCIES as readonly string[]).includes(value);
}

export function defaultCurrencyFor(locale: string): DisplayCurrency {
  return LOCALE_DEFAULTS[isLocale(locale) ? locale : DEFAULT_LOCALE];
}

/** The cookie's currency when valid, otherwise the locale's default. */
export function resolveCurrency(cookieValue: string | undefined, locale: string): DisplayCurrency {
  return isDisplayCurrency(cookieValue) ? cookieValue : defaultCurrencyFor(locale);
}

/**
 * Converts with rates relative to a common base (`amount ÷ rate[from] × rate[to]`).
 * Returns null when either rate is unknown.
 */
export function convertAmount(
  amount: number,
  from: string,
  to: string,
  rates: Record<string, number> | undefined,
): number | null {
  if (from === to) return amount;
  const fromRate = rates?.[from];
  const toRate = rates?.[to];
  if (!fromRate || !toRate) return null;
  return (amount / fromRate) * toRate;
}
