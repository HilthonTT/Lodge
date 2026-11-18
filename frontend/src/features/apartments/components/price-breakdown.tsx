import { Separator } from "@/components/ui/separator";
import { formatPrice, pluralize } from "@/lib/format";
import { cn } from "@/lib/utils";
import type { PriceDetails } from "@/types/api";

type PriceBreakdownProps = {
  price: PriceDetails;
  nights: number;
  className?: string;
};

/** Itemized stay price as returned by GET /apartments/{id}/price. */
export function PriceBreakdown({ price, nights, className }: PriceBreakdownProps) {
  const currency = price.currency;
  const nightly = nights > 0 ? price.pricePerPeriod / nights : price.pricePerPeriod;

  return (
    <dl className={cn("grid gap-3 text-sm", className)}>
      <div className="flex items-center justify-between gap-4">
        <dt className="text-muted-foreground">
          {formatPrice(nightly, currency)} × {pluralize(nights, "night")}
        </dt>
        <dd className="tabular-nums">{formatPrice(price.pricePerPeriod, currency)}</dd>
      </div>
      <div className="flex items-center justify-between gap-4">
        <dt className="text-muted-foreground">Cleaning fee</dt>
        <dd className="tabular-nums">{formatPrice(price.cleaningFee, currency)}</dd>
      </div>
      {price.amenitiesUpCharge > 0 && (
        <div className="flex items-center justify-between gap-4">
          <dt className="text-muted-foreground">Amenities up-charge</dt>
          <dd className="tabular-nums">{formatPrice(price.amenitiesUpCharge, currency)}</dd>
        </div>
      )}
      <Separator className="my-1" />
      <div className="flex items-center justify-between gap-4 text-base font-semibold">
        <dt>Total</dt>
        <dd className="tabular-nums">{formatPrice(price.totalPrice, currency)}</dd>
      </div>
    </dl>
  );
}
