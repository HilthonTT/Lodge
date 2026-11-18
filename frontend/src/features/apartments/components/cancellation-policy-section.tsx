import { ShieldCheck } from "lucide-react";
import { useTranslations } from "next-intl";

import { RefundTimeline } from "@/features/bookings/components/refund-timeline";
import { policyTimeline } from "@/lib/cancellation";
import { parseDateOnly, today } from "@/lib/dates";
import type { CancellationPolicy } from "@/types/api";

/**
 * The listing's cancellation policy as a refund timeline: with the dates from
 * the URL when a stay is selected, otherwise in days before check-in.
 */
export function CancellationPolicySection({ policy, checkIn }: { policy: CancellationPolicy; checkIn?: string }) {
  const t = useTranslations("cancellation");
  const tPolicy = useTranslations("listing.cancellationPolicies");
  const start = parseDateOnly(checkIn);
  const date = start && start > today() ? start : undefined;
  const tiers = policyTimeline(policy, date);
  // Drop tiers that already ended (e.g. a stay starting in 3 days under Strict).
  const now = today();
  const visible = date ? tiers.filter((tier, i) => !tier.until || tier.until >= now || i === tiers.length - 1) : tiers;

  return (
    <section aria-labelledby="cancellation-heading" className="grid gap-4" data-testid="cancellation-policy">
      <div className="grid gap-1">
        <h2 id="cancellation-heading" className="text-xl font-semibold">
          {t("heading")}
        </h2>
        <p className="flex items-center gap-2 text-sm">
          <ShieldCheck className="size-4 text-primary" aria-hidden="true" />
          <span className="font-medium">{tPolicy(`${policy}.name`)}</span>
          <span className="text-muted-foreground">· {tPolicy(`${policy}.summary`)}</span>
        </p>
      </div>
      <div className="grid gap-4 rounded-2xl border bg-card p-5">
        <RefundTimeline tiers={visible} currentIndex={date ? 0 : undefined} />
        <p className="text-xs text-muted-foreground">{date ? t("datesNote") : t("addDatesNote")}</p>
        <p className="text-xs text-muted-foreground">{t("feesNote")}</p>
      </div>
    </section>
  );
}
