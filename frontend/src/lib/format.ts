const formatterCache = new Map<string, Intl.NumberFormat>();

function getCurrencyFormatter(currency: string, fractionDigits: number) {
  const key = `${currency}:${fractionDigits}`;
  let formatter = formatterCache.get(key);
  if (!formatter) {
    try {
      formatter = new Intl.NumberFormat("en-US", {
        style: "currency",
        currency,
        minimumFractionDigits: fractionDigits,
        maximumFractionDigits: fractionDigits,
      });
    } catch {
      // Unknown/empty ISO code from the API: fall back to a plain number.
      formatter = new Intl.NumberFormat("en-US", {
        minimumFractionDigits: fractionDigits,
        maximumFractionDigits: fractionDigits,
      });
    }
    formatterCache.set(key, formatter);
  }
  return formatter;
}

/**
 * Formats a money amount exactly: whole amounts drop the cents, anything else
 * keeps 2 decimals (the API returns decimals such as 412.37).
 */
export function formatPrice(amount: number, currency = "USD") {
  const value = Number.isFinite(amount) ? Math.round(amount * 100) / 100 : 0;
  const digits = Number.isInteger(value) ? 0 : 2;
  return getCurrencyFormatter(currency || "USD", digits).format(value);
}

/** Price rounded to whole units, for compact display (cards, headlines). */
export function formatPriceCompact(amount: number, currency = "USD") {
  return getCurrencyFormatter(currency || "USD", 0).format(Math.round(amount));
}

export function pluralize(count: number, singular: string, plural = `${singular}s`) {
  return `${count} ${count === 1 ? singular : plural}`;
}
