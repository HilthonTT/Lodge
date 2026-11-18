import { useLocale, useTranslations } from "next-intl";

import { Separator } from "@/components/ui/separator";
import { ChargedAmount } from "@/features/payments/components/money";
import { nightsBetween, parseDateOnly } from "@/lib/dates";
import { formatNumber, formatPrice } from "@/lib/format";
import { cn } from "@/lib/utils";
import type { Booking, PriceDetails } from "@/types/api";

type PriceBreakdownProps = {
  price: PriceDetails;
  /** Fallback when the response has no `nights`. */
  nights?: number;
  /** Label of the total row (default "Total"). */
  totalLabel?: string;
  className?: string;
};

/**
 * Itemized stay price as returned by GET /apartments/{id}/price (or stored on a
 * booking), in the currency that is charged. The total also shows the
 * display-currency equivalent when the visitor picked another currency.
 */
export function PriceBreakdown({ price, nights: nightsProp, totalLabel, className }: PriceBreakdownProps) {
  const t = useTranslations();
  const locale = useLocale();
  const currency = price.currency;
  const nights = price.nights || nightsProp || 0;
  const nightly = price.averageNightlyPrice || (nights > 0 ? price.pricePerPeriod / nights : price.pricePerPeriod);
  const row = "flex items-center justify-between gap-4";

  return (
    <dl className={cn("grid gap-3 text-sm", className)}>
      <div className={row}>
        <dt className="text-muted-foreground">
          {formatPrice(nightly, currency, locale)} × {t("common.nights", { count: nights })}
        </dt>
        <dd className="tabular-nums">{formatPrice(price.pricePerPeriod, currency, locale)}</dd>
      </div>
      {price.discountAmount > 0 && (
        <div className={row}>
          <dt className="text-success">
            {price.discountType === "monthly"
              ? t("apartment.price.monthlyDiscount")
              : price.discountType === "weekly"
                ? t("apartment.price.weeklyDiscount")
                : t("apartment.price.discount")}
          </dt>
          <dd className="text-success tabular-nums">−{formatPrice(price.discountAmount, currency, locale)}</dd>
        </div>
      )}
      {price.cleaningFee > 0 && (
        <div className={row}>
          <dt className="text-muted-foreground">{t("apartment.price.cleaningFee")}</dt>
          <dd className="tabular-nums">{formatPrice(price.cleaningFee, currency, locale)}</dd>
        </div>
      )}
      {price.amenitiesUpCharge > 0 && (
        <div className={row}>
          <dt className="text-muted-foreground">{t("apartment.price.amenities")}</dt>
          <dd className="tabular-nums">{formatPrice(price.amenitiesUpCharge, currency, locale)}</dd>
        </div>
      )}
      {price.serviceFee > 0 && (
        <div className={row}>
          <dt className="text-muted-foreground">{t("apartment.price.serviceFee")}</dt>
          <dd className="tabular-nums">{formatPrice(price.serviceFee, currency, locale)}</dd>
        </div>
      )}
      {price.taxes > 0 && (
        <div className={row}>
          <dt className="text-muted-foreground">
            {t("apartment.price.taxes", { rate: formatNumber(price.taxRatePercent, locale) })}
          </dt>
          <dd className="tabular-nums">{formatPrice(price.taxes, currency, locale)}</dd>
        </div>
      )}
      <Separator className="my-1" />
      <div className="flex items-start justify-between gap-4 text-base font-semibold">
        <dt>{totalLabel ?? t("apartment.price.total")}</dt>
        <dd className="text-right tabular-nums">
          <ChargedAmount amount={price.totalPrice} currency={currency} />
        </dd>
      </div>
    </dl>
  );
}

/** A booking's stored amounts in the shape of a price quote, for `<PriceBreakdown>`. */
export function bookingPriceDetails(booking: Booking): PriceDetails {
  const start = parseDateOnly(booking.durationStart);
  const end = parseDateOnly(booking.durationEnd);
  const nights = start && end ? nightsBetween(start, end) : 0;
  return {
    currency: booking.totalPriceCurrency,
    nights,
    pricePerPeriod: booking.priceAmount,
    averageNightlyPrice: nights > 0 ? booking.priceAmount / nights : booking.priceAmount,
    discountAmount: booking.discountAmount,
    discountType: null,
    cleaningFee: booking.cleaningFeeAmount,
    amenitiesUpCharge: booking.amenitiesUpChargeAmount,
    serviceFee: booking.serviceFeeAmount ?? 0,
    taxes: booking.taxAmount ?? 0,
    taxRatePercent: booking.taxRatePercent ?? 0,
    totalPrice: booking.totalPriceAmount,
  };
}
