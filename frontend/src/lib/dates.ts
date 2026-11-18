import { addDays, differenceInCalendarDays, format, isValid, startOfDay } from "date-fns";

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

/** Formats a local date as an API `DateOnly` string. */
export function toDateOnly(date: Date): string {
  return format(date, API_DATE_FORMAT);
}

export function today(): Date {
  return startOfDay(new Date());
}

export function nightsBetween(start: Date, end: Date): number {
  return Math.max(0, differenceInCalendarDays(end, start));
}

export function formatDateRange(start: Date, end: Date): string {
  const sameYear = start.getFullYear() === end.getFullYear();
  const sameMonth = sameYear && start.getMonth() === end.getMonth();
  if (sameMonth) return `${format(start, "MMM d")} – ${format(end, "d, yyyy")}`;
  if (sameYear) return `${format(start, "MMM d")} – ${format(end, "MMM d, yyyy")}`;
  return `${format(start, "MMM d, yyyy")} – ${format(end, "MMM d, yyyy")}`;
}

export function formatShortDate(date: Date): string {
  return format(date, "MMM d");
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
  if (from < today() || to <= from) return undefined;
  return { from, to };
}

export { addDays };
