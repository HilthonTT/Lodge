import { DEFAULT_LOCALE, INTL_LOCALES, isLocale } from "@/i18n/config";

const formatterCache = new Map<string, Intl.NumberFormat>();

function intlLocale(locale: string) {
  return INTL_LOCALES[isLocale(locale) ? locale : DEFAULT_LOCALE];
}

function getFormatter(locale: string, key: string, options: Intl.NumberFormatOptions, fallback?: Intl.NumberFormatOptions) {
  const cacheKey = `${locale}:${key}`;
  let formatter = formatterCache.get(cacheKey);
  if (!formatter) {
    try {
      formatter = new Intl.NumberFormat(intlLocale(locale), options);
    } catch {
      formatter = new Intl.NumberFormat(intlLocale(locale), fallback);
    }
    formatterCache.set(cacheKey, formatter);
  }
  return formatter;
}

function getCurrencyFormatter(locale: string, currency: string, fractionDigits: number) {
  const digits = { minimumFractionDigits: fractionDigits, maximumFractionDigits: fractionDigits };
  // Unknown/empty ISO code from the API: fall back to a plain number.
  return getFormatter(locale, `cur:${currency}:${fractionDigits}`, { style: "currency", currency, ...digits }, digits);
}

/**
 * Formats a money amount exactly, in the API's currency and the UI locale's
 * conventions: whole amounts drop the cents, anything else keeps 2 decimals
 * (the API returns decimals such as 412.37). `$1,234.50` / `€ 1.234,50` / `1 234,50 €`.
 */
export function formatPrice(amount: number, currency: string, locale: string) {
  const value = Number.isFinite(amount) ? Math.round(amount * 100) / 100 : 0;
  const code = currency || "USD";
  const digits = Number.isInteger(value) ? 0 : Math.min(2, currencyMinorDigits(locale, code));
  return getCurrencyFormatter(locale, code, digits).format(value);
}

/** Decimal places the currency uses (2 for USD/EUR, 0 for JPY); 2 for an unknown code. */
function currencyMinorDigits(locale: string, currency: string): number {
  return (
    getFormatter(locale, `cur:${currency}:default`, { style: "currency", currency }, { maximumFractionDigits: 2 })
      .resolvedOptions().maximumFractionDigits ?? 2
  );
}

/** Price rounded to whole units, for compact display (cards, headlines). */
export function formatPriceCompact(amount: number, currency: string, locale: string) {
  return getCurrencyFormatter(locale, currency || "USD", 0).format(Math.round(amount));
}

/** Plain number with locale grouping, e.g. `1,234` / `1.234` / `1 234`. */
export function formatNumber(value: number, locale: string) {
  return getFormatter(locale, "int", {}).format(value);
}

/** Average rating with one decimal: `4.8` / `4,8`. */
export function formatRating(value: number, locale: string) {
  return getFormatter(locale, "rating", { minimumFractionDigits: 1, maximumFractionDigits: 1 }).format(value);
}
