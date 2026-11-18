"use client";

import { Check } from "lucide-react";
import { useTranslations } from "next-intl";

import { POLICY_TIERS } from "@/lib/cancellation";
import { cn } from "@/lib/utils";
import { CANCELLATION_POLICIES, type CancellationPolicy } from "@/types/api";

type Props = {
  value: CancellationPolicy;
  onChange: (value: CancellationPolicy) => void;
  error?: string;
};

/** Flexible / Moderate / Strict as radio cards, each explaining its refund rules. */
export function CancellationPolicyPicker({ value, onChange, error }: Props) {
  const t = useTranslations("hosting.wizard");
  const tPolicy = useTranslations("listing.cancellationPolicies");
  const tCancel = useTranslations("cancellation");

  const rule = (policy: CancellationPolicy) =>
    POLICY_TIERS[policy]
      .map((tier, i, tiers) => {
        const refund =
          tier.refundPercent >= 100
            ? tCancel("fullRefund")
            : tier.refundPercent <= 0
              ? tCancel("noRefund")
              : tCancel("partialRefund", { percent: String(tier.refundPercent) });
        const previous = tiers[i - 1];
        const when =
          tier.daysBefore !== null
            ? previous?.daysBefore != null
              ? tCancel("daysRange", { from: tier.daysBefore, to: previous.daysBefore - 1 })
              : tCancel("atLeastDays", { count: tier.daysBefore })
            : tCancel("lessThanDays", { count: previous?.daysBefore ?? 0 });
        return `${when}: ${refund.toLowerCase()}`;
      })
      .join(" · ");

  return (
    <fieldset className="grid gap-3" aria-describedby={error ? "policy-error" : "policy-hint"}>
      <legend className="mb-1 font-medium">{t("fields.cancellationPolicy")}</legend>
      <p id="policy-hint" className="-mt-1 text-sm text-muted-foreground">
        {t("fields.cancellationPolicyHint")}
      </p>
      <div className="grid gap-3 md:grid-cols-3">
        {CANCELLATION_POLICIES.map((policy) => {
          const selected = value === policy;
          return (
            <label
              key={policy}
              className={cn(
                "relative grid cursor-pointer content-start gap-2 rounded-xl border p-4 text-sm transition-colors has-[:focus-visible]:ring-[3px] has-[:focus-visible]:ring-ring/40",
                selected ? "border-foreground bg-muted/40" : "hover:bg-muted/40",
              )}
            >
              <input
                type="radio"
                name="cancellationPolicy"
                value={policy}
                checked={selected}
                onChange={() => onChange(policy)}
                className="sr-only"
              />
              <span className="flex items-center justify-between gap-2 font-semibold">
                {tPolicy(`${policy}.name`)}
                {selected && <Check className="size-4" aria-hidden="true" />}
              </span>
              <span className="text-muted-foreground">{tPolicy(`${policy}.description`)}</span>
              <span className="text-xs text-muted-foreground">{rule(policy)}</span>
            </label>
          );
        })}
      </div>
      {error && (
        <p id="policy-error" className="text-xs font-medium text-destructive">
          {error}
        </p>
      )}
    </fieldset>
  );
}
