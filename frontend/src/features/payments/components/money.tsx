"use client";

import { useLocale, useTranslations } from "next-intl";

import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { formatPrice, formatPriceCompact } from "@/lib/format";
import { cn } from "@/lib/utils";

import { useDisplayMoney } from "../currency-provider";

type MoneyProps = {
  amount: number;
  /** The currency the API returned the amount in. */
  currency: string;
  /** Whole units (cards, headlines). */
  compact?: boolean;
  /**
   * Explain converted amounts in a focusable tooltip ("≈ converted from USD").
   * Without it, the explanation is a native title plus screen-reader text.
   */
  tooltip?: boolean;
  className?: string;
};

/** A price in the visitor's display currency, marked when it was converted. */
export function Money({ amount, currency, compact, tooltip, className }: MoneyProps) {
  const t = useTranslations("currency");
  const locale = useLocale();
  const { convert } = useDisplayMoney();
  const money = convert(amount, currency);
  const fmt = compact ? formatPriceCompact : formatPrice;
  const text = fmt(money.amount, money.currency, locale);

  if (!money.converted) return <span className={className}>{text}</span>;

  const original = fmt(amount, currency, locale);
  const note = t("convertedFrom", { currency, amount: original });
  if (!tooltip) {
    return (
      <span className={className} title={note}>
        {`≈ ${text}`}
        <span className="sr-only"> ({note})</span>
      </span>
    );
  }
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <span
          tabIndex={0}
          className={cn(
            "cursor-help rounded-sm underline decoration-muted-foreground/50 decoration-dotted underline-offset-4 outline-none focus-visible:ring-[3px] focus-visible:ring-ring/40",
            className,
          )}
        >
          {`≈ ${text}`}
          <span className="sr-only"> ({note})</span>
        </span>
      </TooltipTrigger>
      <TooltipContent>{note}</TooltipContent>
    </Tooltip>
  );
}

/**
 * An amount that is (or will be) charged: always in the charged currency, with
 * the display-currency equivalent as a secondary line when they differ.
 */
export function ChargedAmount({
  amount,
  currency,
  className,
  secondaryClassName,
}: {
  amount: number;
  currency: string;
  className?: string;
  secondaryClassName?: string;
}) {
  const t = useTranslations("currency");
  const locale = useLocale();
  const { convert } = useDisplayMoney();
  const money = convert(amount, currency);
  return (
    <span className={cn("inline-grid justify-items-end", className)}>
      <span>{formatPrice(amount, currency, locale)}</span>
      {money.converted && (
        <span className={cn("text-xs font-normal text-muted-foreground", secondaryClassName)}>
          {t("approx", { amount: formatPrice(money.amount, money.currency, locale) })}
        </span>
      )}
    </span>
  );
}

/** "You'll be charged in EUR" — plus the converted note when the display currency differs. */
export function ChargedInNote({ currency, className }: { currency: string; className?: string }) {
  const t = useTranslations("currency");
  const { currency: display } = useDisplayMoney();
  return (
    <p className={cn("text-xs text-muted-foreground", className)}>
      {t("chargedIn", { currency })}
      {display !== currency && ` ${t("displayedIn", { currency: display })}`}
    </p>
  );
}
