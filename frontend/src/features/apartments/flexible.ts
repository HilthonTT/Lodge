import { format } from "date-fns";

import { getDateFnsLocale } from "@/lib/dates";

/** "2026-12" -> Date (1st of that month, local). */
export function monthToDate(month: string): Date {
  const [y = 1970, m = 1] = month.split("-").map(Number);
  return new Date(y, m - 1, 1);
}

/** "2026-12" for a date. */
export function toMonthKey(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`;
}

/** The next `count` months, starting with the current one. */
export function upcomingMonths(count = 12, now = new Date()): string[] {
  return Array.from({ length: count }, (_, i) => toMonthKey(new Date(now.getFullYear(), now.getMonth() + i, 1)));
}

/** "Dec, Jan" / "déc., janv." */
export function formatMonthList(months: string[], locale: string): string {
  return months.map((m) => format(monthToDate(m), "MMM", { locale: getDateFnsLocale(locale) })).join(", ");
}
