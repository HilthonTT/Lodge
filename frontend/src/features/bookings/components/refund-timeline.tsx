import { useLocale, useTranslations } from "next-intl";

import { formatMediumDate } from "@/lib/dates";
import type { TimelineTier } from "@/lib/cancellation";
import { cn } from "@/lib/utils";

type RefundTimelineProps = {
  tiers: TimelineTier[];
  /** Index of the tier that applies today, highlighted as "Now". */
  currentIndex?: number;
  className?: string;
};

function tone(percent: number) {
  if (percent >= 100) return { dot: "bg-success", text: "text-success" };
  if (percent > 0) return { dot: "bg-warning", text: "text-[color-mix(in_oklch,var(--warning),black_35%)] dark:text-warning" };
  return { dot: "bg-muted-foreground/50", text: "text-muted-foreground" };
}

/**
 * Readable refund timeline built from a cancellation policy's tiers: concrete
 * dates when the check-in is known ("Cancel by 4 Dec: full refund"), otherwise
 * days before check-in ("At least 14 days before check-in: full refund").
 */
export function RefundTimeline({ tiers, currentIndex, className }: RefundTimelineProps) {
  const t = useTranslations("cancellation");
  const locale = useLocale();

  const when = (index: number) => {
    const tier = tiers[index]!;
    const previous = index > 0 ? tiers[index - 1] : undefined;
    if (tier.until) return t("cancelBy", { date: formatMediumDate(tier.until, locale) });
    if (previous?.until) return t("after", { date: formatMediumDate(previous.until, locale) });
    if (tier.daysBefore !== null) {
      return previous?.daysBefore != null
        ? t("daysRange", { from: tier.daysBefore, to: previous.daysBefore - 1 })
        : t("atLeastDays", { count: tier.daysBefore });
    }
    return previous?.daysBefore != null ? t("lessThanDays", { count: previous.daysBefore }) : t("anyTime");
  };

  const refund = (percent: number) =>
    percent >= 100 ? t("fullRefund") : percent <= 0 ? t("noRefund") : t("partialRefund", { percent: String(percent) });

  return (
    <ol className={cn("grid gap-0", className)}>
      {tiers.map((tier, index) => {
        const colors = tone(tier.refundPercent);
        const current = index === currentIndex;
        const last = index === tiers.length - 1;
        return (
          <li key={index} className="grid grid-cols-[1rem_minmax(0,1fr)] gap-3" aria-current={current ? "step" : undefined}>
            <span className="relative flex justify-center" aria-hidden="true">
              <span className={cn("relative z-10 mt-1.5 size-3 rounded-full ring-4 ring-card", colors.dot)} />
              {!last && <span className="absolute top-3 bottom-0 w-px bg-border" />}
            </span>
            <div className={cn("grid gap-0.5", !last && "pb-4")}>
              <p className="flex flex-wrap items-center gap-2 text-sm font-medium">
                {when(index)}
                {current && (
                  <span className="rounded-full bg-foreground px-2 py-0.5 text-[0.65rem] font-semibold tracking-wide text-background uppercase">
                    {t("now")}
                  </span>
                )}
              </p>
              <p className={cn("text-sm", colors.text)}>{refund(tier.refundPercent)}</p>
            </div>
          </li>
        );
      })}
    </ol>
  );
}

/** Tiers from a cancellation quote (`until` as yyyy-MM-dd). */
export function quoteTiers(timeline: { until: string | null; refundPercent: number }[]): TimelineTier[] {
  return timeline.map((tier) => {
    const [y, m, d] = (tier.until ?? "").split("-").map(Number);
    return {
      until: tier.until && y && m && d ? new Date(y, m - 1, d) : null,
      refundPercent: tier.refundPercent,
      daysBefore: null,
    };
  });
}
