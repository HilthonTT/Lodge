import { useLocale, useTranslations } from "next-intl";

import { Separator } from "@/components/ui/separator";
import { formatPrice } from "@/lib/format";
import { cn } from "@/lib/utils";
import type { PriceDetails } from "@/types/api";

type PriceBreakdownProps = {
  price: PriceDetails;
  nights: number;
  className?: string;
};

/** Itemized stay price as returned by GET /apartments/{id}/price. */
export function PriceBreakdown({ price, nights, className }: PriceBreakdownProps) {
  const t = useTranslations();
  const locale = useLocale();
  const currency = price.currency;
  const nightly = nights > 0 ? price.pricePerPeriod / nights : price.pricePerPeriod;

  return (
    <dl className={cn("grid gap-3 text-sm", className)}>
      <div className="flex items-center justify-between gap-4">
        <dt className="text-muted-foreground">
          {formatPrice(nightly, currency, locale)} × {t("common.nights", { count: nights })}
        </dt>
        <dd className="tabular-nums">{formatPrice(price.pricePerPeriod, currency, locale)}</dd>
      </div>
      <div className="flex items-center justify-between gap-4">
        <dt className="text-muted-foreground">{t("apartment.price.cleaningFee")}</dt>
        <dd className="tabular-nums">{formatPrice(price.cleaningFee, currency, locale)}</dd>
      </div>
      {price.amenitiesUpCharge > 0 && (
        <div className="flex items-center justify-between gap-4">
          <dt className="text-muted-foreground">{t("apartment.price.amenities")}</dt>
          <dd className="tabular-nums">{formatPrice(price.amenitiesUpCharge, currency, locale)}</dd>
        </div>
      )}
      <Separator className="my-1" />
      <div className="flex items-center justify-between gap-4 text-base font-semibold">
        <dt>{t("apartment.price.total")}</dt>
        <dd className="tabular-nums">{formatPrice(price.totalPrice, currency, locale)}</dd>
      </div>
    </dl>
  );
}
