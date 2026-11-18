/**
 * Locale settings shared by the server (request config, proxy) and the client
 * (language switcher, formatting). No URL prefixes: the active locale lives in
 * the `NEXT_LOCALE` cookie, with `Accept-Language` as the fallback.
 */
export const LOCALES = ["en", "nl", "fr"] as const;

export type Locale = (typeof LOCALES)[number];

export const DEFAULT_LOCALE: Locale = "en";

/** Cookie read by `src/i18n/request.ts` and written by the language switcher. */
export const LOCALE_COOKIE = "NEXT_LOCALE";

/** One year, like Airbnb's language preference. */
export const LOCALE_COOKIE_MAX_AGE = 60 * 60 * 24 * 365;

/** Native names, shown as-is in every UI language. */
export const LOCALE_NAMES: Record<Locale, string> = {
  en: "English",
  nl: "Nederlands",
  fr: "Français",
};

/** BCP 47 tags for `Intl` APIs (region picks separators and currency placement). */
export const INTL_LOCALES: Record<Locale, string> = {
  en: "en-US",
  nl: "nl-NL",
  fr: "fr-FR",
};

export function isLocale(value: unknown): value is Locale {
  return typeof value === "string" && (LOCALES as readonly string[]).includes(value);
}

/**
 * Picks the best supported locale from an `Accept-Language` header, e.g.
 * "fr-BE,fr;q=0.9,en;q=0.8" -> "fr". Falls back to the default locale.
 */
export function negotiateLocale(header: string | null | undefined): Locale {
  if (!header) return DEFAULT_LOCALE;
  const ranked = header
    .split(",")
    .map((part, index) => {
      const [tag = "", ...params] = part.trim().split(";");
      const q = params.map((p) => p.trim()).find((p) => p.startsWith("q="));
      const quality = q ? Number(q.slice(2)) : 1;
      return { tag: tag.toLowerCase(), quality: Number.isFinite(quality) ? quality : 0, index };
    })
    .filter((entry) => entry.tag && entry.quality > 0)
    .sort((a, b) => b.quality - a.quality || a.index - b.index);

  for (const { tag } of ranked) {
    const base = tag.split("-")[0];
    if (isLocale(base)) return base;
  }
  return DEFAULT_LOCALE;
}
