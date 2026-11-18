import { BedDouble, Users } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { RemoteImage } from "@/components/shared/remote-image";
import { Skeleton } from "@/components/ui/skeleton";
import { formatPriceCompact } from "@/lib/format";
import type { Apartment } from "@/types/api";

import { RatingSummary } from "./rating-summary";

export function formatLocation(address: Apartment["address"] | undefined) {
  if (!address) return "";
  return [address.city, address.country].filter(Boolean).join(", ");
}

type ApartmentCardProps = {
  apartment: Apartment;
  /** Query string forwarded to the detail page (dates/guests from a search). */
  query?: string;
  priority?: boolean;
};

export function ApartmentCard({ apartment, query, priority }: ApartmentCardProps) {
  const t = useTranslations("common");
  const locale = useLocale();
  const href = `/apartments/${apartment.id}${query ? `?${query}` : ""}`;
  const location = formatLocation(apartment.address);

  return (
    <article className="group relative flex flex-col gap-3">
      <div className="relative aspect-[4/3] overflow-hidden rounded-2xl bg-muted">
        <RemoteImage
          src={apartment.imageUrl}
          alt=""
          fill
          priority={priority}
          sizes="(min-width: 1280px) 25vw, (min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw"
          className="object-cover transition-transform duration-500 ease-out group-hover:scale-[1.04]"
        />
        <div className="pointer-events-none absolute inset-0 rounded-2xl ring-1 ring-black/5 ring-inset" />
      </div>

      <div className="grid gap-1 px-0.5">
        <div className="flex items-start justify-between gap-3">
          <h3 className="line-clamp-1 font-semibold">
            <Link
              href={href}
              className="rounded-sm outline-none after:absolute after:inset-0 after:rounded-2xl focus-visible:after:ring-[3px] focus-visible:after:ring-ring/50"
            >
              {apartment.name}
            </Link>
          </h3>
          <RatingSummary
            averageRating={apartment.averageRating}
            reviewCount={apartment.reviewCount}
            className="shrink-0"
          />
        </div>
        {location && <p className="line-clamp-1 text-sm text-muted-foreground">{location}</p>}
        {apartment.maximumGuestCount > 0 && (
          <p className="flex items-center gap-3 text-sm text-muted-foreground">
            <span className="inline-flex items-center gap-1">
              <Users className="size-3.5" aria-hidden="true" />
              {t("guests", { count: apartment.maximumGuestCount })}
            </span>
            {apartment.maximumRoomCount > 0 && (
              <span className="inline-flex items-center gap-1">
                <BedDouble className="size-3.5" aria-hidden="true" />
                {t("rooms", { count: apartment.maximumRoomCount })}
              </span>
            )}
          </p>
        )}
        <p className="mt-1">
          <span className="font-semibold">{formatPriceCompact(apartment.price, apartment.currency, locale)}</span>{" "}
          <span className="text-sm text-muted-foreground">{t("perNight")}</span>
        </p>
      </div>
    </article>
  );
}

export function ApartmentCardSkeleton() {
  return (
    <div className="flex flex-col gap-3">
      <Skeleton className="aspect-[4/3] rounded-2xl" />
      <div className="grid gap-2 px-0.5">
        <Skeleton className="h-4 w-3/4" />
        <Skeleton className="h-3.5 w-1/2" />
        <Skeleton className="h-4 w-1/4" />
      </div>
    </div>
  );
}
