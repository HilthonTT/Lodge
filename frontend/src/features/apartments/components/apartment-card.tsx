import { Award } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";

import { RemoteImage } from "@/components/shared/remote-image";
import { Skeleton } from "@/components/ui/skeleton";
import { Money } from "@/features/payments/components/money";
import { WishlistHeart } from "@/features/wishlists/components/wishlist-heart";
import { cn } from "@/lib/utils";
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
  /** Map view: reports hover/focus so the matching marker can be highlighted. */
  onHoverChange?: (hovered: boolean) => void;
  /** Map view: the matching marker is hovered. */
  highlighted?: boolean;
  /** Hide the heart (e.g. on the wishlist page, which has its own remove button). */
  hideWishlist?: boolean;
  sizes?: string;
};

export function ApartmentCard({
  apartment,
  query,
  priority,
  onHoverChange,
  highlighted,
  hideWishlist,
  sizes = "(min-width: 1280px) 25vw, (min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw",
}: ApartmentCardProps) {
  const t = useTranslations("common");
  const tListing = useTranslations("listing");
  const href = `/apartments/${apartment.id}${query ? `?${query}` : ""}`;
  const location = formatLocation(apartment.address);

  return (
    <article
      className="group relative flex flex-col gap-3"
      data-apartment-id={apartment.id}
      onMouseEnter={onHoverChange ? () => onHoverChange(true) : undefined}
      onMouseLeave={onHoverChange ? () => onHoverChange(false) : undefined}
      onFocus={onHoverChange ? () => onHoverChange(true) : undefined}
      onBlur={onHoverChange ? () => onHoverChange(false) : undefined}
    >
      <div
        className={cn(
          "relative aspect-[4/3] overflow-hidden rounded-2xl bg-muted transition-shadow",
          highlighted && "ring-2 ring-primary ring-offset-2 ring-offset-background",
        )}
      >
        <RemoteImage
          src={apartment.imageUrl}
          alt=""
          fill
          priority={priority}
          sizes={sizes}
          className="object-cover transition-transform duration-500 ease-out group-hover:scale-[1.04]"
        />
        <div className="pointer-events-none absolute inset-0 rounded-2xl ring-1 ring-black/5 ring-inset" />
        {apartment.host?.isSuperhost && (
          <span className="absolute top-3 left-3 inline-flex items-center gap-1 rounded-full bg-background/95 px-2.5 py-1 text-xs font-semibold shadow-card">
            <Award className="size-3.5 text-primary" aria-hidden="true" />
            {t("superhost")}
          </span>
        )}
        {!hideWishlist && (
          <WishlistHeart
            apartmentId={apartment.id}
            apartmentName={apartment.name}
            className="absolute top-2 right-2 z-10"
          />
        )}
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
        <p className="line-clamp-1 text-sm text-muted-foreground">
          {[
            apartment.propertyType ? tListing(`propertyTypes.${apartment.propertyType}`) : null,
            t("bedrooms", { count: apartment.bedrooms }),
            t("beds", { count: apartment.beds }),
          ]
            .filter(Boolean)
            .join(" · ")}
        </p>
        <p className="mt-1">
          <Money amount={apartment.price} currency={apartment.currency} compact className="font-semibold" />{" "}
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
