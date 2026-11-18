import { useTranslations } from "next-intl";

import type { Apartment } from "@/types/api";

import { ApartmentCard, ApartmentCardSkeleton } from "./apartment-card";

const gridClass = "grid grid-cols-1 gap-x-6 gap-y-10 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4";

export function ApartmentGrid({ apartments, query }: { apartments: Apartment[]; query?: string }) {
  return (
    <ul className={gridClass}>
      {apartments.map((apartment, index) => (
        <li key={apartment.id}>
          <ApartmentCard apartment={apartment} query={query} priority={index < 4} />
        </li>
      ))}
    </ul>
  );
}

export function ApartmentGridSkeleton({ count = 8 }: { count?: number }) {
  const t = useTranslations("search");
  return (
    <div className={gridClass} role="status" aria-label={t("loading")}>
      {Array.from({ length: count }, (_, i) => (
        <ApartmentCardSkeleton key={i} />
      ))}
    </div>
  );
}
