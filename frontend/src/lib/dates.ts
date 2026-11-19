import { addDays, differenceInCalendarDays, format, isValid, type Locale as DateFnsLocale, startOfDay } from "date-fns";
import { enUS } from "date-fns/locale/en-US";
import { fr } from "date-fns/locale/fr";
import { nl } from "date-fns/locale/nl";

import { DEFAULT_LOCALE, isLocale, type Locale } from "@/i18n/config";

/** API date format for `DateOnly` parameters. */
export const API_DATE_FORMAT = "yyyy-MM-dd";

/**
 * Parses a calendar date ("2026-11-20" or "2026-11-20T00:00:00[Z]") as a *local*
 * date at midnight. Using `new Date("2026-11-20")` would parse as UTC midnight and
 * shift the day for users west of UTC.
 */
export function parseDateOnly(value: string | null | undefined): Date | undefined {
  if (!value) return undefined;
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  if (!match) return undefined;
  const [, y, m, d] = match;
  const date = new Date(Number(y), Number(m) - 1, Number(d));
  return isValid(date) ? date : undefined;
}

/** Formats a local date as an API `DateOnly` string (never localized). */
export function toDateOnly(date: Date): string {
  return format(date, API_DATE_FORMAT);
}

export function today(): Date {
  return startOfDay(new Date());
}

/** The API's "today" (it compares dates in UTC), as a local midnight Date. */
export function utcToday(): Date {
  const now = new Date();
  return new Date(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
}

/**
 * Earliest check-in the API accepts and the visitor still sees as today or later: the later of the local and
 * UTC dates. West of UTC in the evening, the local "today" is already in the past for the API.
 */
export function earliestCheckIn(): Date {
  const local = today();
  const utc = utcToday();
  return utc > local ? utc : local;
}

export function nightsBetween(start: Date, end: Date): number {
  return Math.max(0, differenceInCalendarDays(end, start));
}

const DATE_FNS_LOCALES: Record<Locale, DateFnsLocale> = { en: enUS, nl, fr };

/** date-fns locale for a UI locale (month names, week start). */
export function getDateFnsLocale(locale: string): DateFnsLocale {
  return DATE_FNS_LOCALES[isLocale(locale) ? locale : DEFAULT_LOCALE];
}

/**
 * Per-locale patterns. English puts the month first ("Nov 20 – 25, 2026"), Dutch
 * and French put the day first ("20 – 25 nov. 2026").
 */
const PATTERNS = {
  monthFirst: {
    short: "MMM d",
    full: "MMM d, yyyy",
    sameMonth: ["MMM d", "d, yyyy"],
    sameYear: ["MMM d", "MMM d, yyyy"],
  },
  dayFirst: {
    short: "d MMM",
    full: "d MMM yyyy",
    sameMonth: ["d", "d MMM yyyy"],
    sameYear: ["d MMM", "d MMM yyyy"],
  },
} as const;

function patternsFor(locale: string) {
  return locale === "nl" || locale === "fr" ? PATTERNS.dayFirst : PATTERNS.monthFirst;
}

/** "Nov 20 – 25, 2026" / "20 – 25 nov. 2026" */
export function formatDateRange(start: Date, end: Date, locale: string): string {
  const options = { locale: getDateFnsLocale(locale) };
  const patterns = patternsFor(locale);
  const sameYear = start.getFullYear() === end.getFullYear();
  const sameMonth = sameYear && start.getMonth() === end.getMonth();
  const [from, to] = sameMonth ? patterns.sameMonth : sameYear ? patterns.sameYear : [patterns.full, patterns.full];
  return `${format(start, from, options)} – ${format(end, to, options)}`;
}

/** "Nov 20" / "20 nov." */
export function formatShortDate(date: Date, locale: string): string {
  return format(date, patternsFor(locale).short, { locale: getDateFnsLocale(locale) });
}

/** "Nov 20, 2026" / "20 nov. 2026" */
export function formatMediumDate(date: Date, locale: string): string {
  return format(date, patternsFor(locale).full, { locale: getDateFnsLocale(locale) });
}

/** "November 2026" / "november 2026" / "novembre 2026" */
export function formatMonthYear(date: Date, locale: string): string {
  return format(date, "LLLL yyyy", { locale: getDateFnsLocale(locale) });
}

/** Long date and time for tooltips, e.g. "Nov 20, 2026, 3:04:05 PM". */
export function formatDateTime(date: Date, locale: string): string {
  return format(date, "PPpp", { locale: getDateFnsLocale(locale) });
}

/** Hours and minutes, e.g. "3:04 PM" / "15:04". */
export function formatTime(date: Date, locale: string): string {
  return format(date, "p", { locale: getDateFnsLocale(locale) });
}

export type StayRange = { from: Date; to: Date };

/**
 * Reads a valid stay range from `startDate`/`endDate` search params. Returns
 * undefined unless both are valid, check-in is today or later and check-out is
 * after check-in.
 */
export function parseStayRange(
  startDate: string | null | undefined,
  endDate: string | null | undefined,
): StayRange | undefined {
  const from = parseDateOnly(startDate);
  const to = parseDateOnly(endDate);
  if (!from || !to) return undefined;
  if (from < earliestCheckIn() || to <= from) return undefined;
  return { from, to };
}

export { addDays };

const relativeFormatters = new Map<string, Intl.RelativeTimeFormat>();

/** "just now", "5 min. ago", "yesterday", "3 days ago"; older than a week falls back to the date. */
export function formatRelativeTime(date: Date, locale: string, now = Date.now()): string {
  let formatter = relativeFormatters.get(locale);
  if (!formatter) {
    formatter = new Intl.RelativeTimeFormat(locale, { numeric: "auto", style: "short" });
    relativeFormatters.set(locale, formatter);
  }
  const seconds = Math.round((date.getTime() - now) / 1000);
  const abs = Math.abs(seconds);
  if (abs < 45) return formatter.format(0, "second");
  if (abs < 3_600) return formatter.format(Math.round(seconds / 60), "minute");
  if (abs < 86_400) return formatter.format(Math.round(seconds / 3_600), "hour");
  if (abs < 7 * 86_400) return formatter.format(Math.round(seconds / 86_400), "day");
  return formatMediumDate(date, locale);
}

/** Weekday, date and time of an instant in the visitor's time zone, e.g. "Sat, Nov 21 · 10:00 AM". */
export function formatSessionDateTime(date: Date, locale: string): string {
  const options = { locale: getDateFnsLocale(locale) };
  const day = locale === "nl" || locale === "fr" ? "EEE d MMM" : "EEE, MMM d";
  return `${format(date, day, options)} · ${format(date, "p", options)}`;
}

/** "Saturday, November 21" / "zaterdag 21 november". */
export function formatLongDay(date: Date, locale: string): string {
  const pattern = locale === "nl" || locale === "fr" ? "EEEE d MMMM" : "EEEE, MMMM d";
  return format(date, pattern, { locale: getDateFnsLocale(locale) });
}
