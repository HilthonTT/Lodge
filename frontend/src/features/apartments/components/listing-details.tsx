import {
  Award,
  Bath,
  BedDouble,
  BedSingle,
  CalendarRange,
  Cigarette,
  CigaretteOff,
  Clock,
  DoorOpen,
  PartyPopper,
  PawPrint,
  ShieldCheck,
  Users,
} from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { UserAvatar } from "@/components/shared/user-avatar";
import { Badge } from "@/components/ui/badge";
import { formatMonthYear, formatTime } from "@/lib/dates";
import { cn } from "@/lib/utils";
import type { Amenity, Apartment } from "@/types/api";

import { AMENITY_GROUPS, AMENITY_ICONS } from "../listing-options";

/** "Guests · bedrooms · beds · baths" chips under the listing title. */
export function ListingFacts({ apartment }: { apartment: Apartment }) {
  const t = useTranslations("common");
  const tApartment = useTranslations("apartment");
  const facts = [
    { icon: Users, label: tApartment("upToGuests", { count: apartment.maximumGuestCount }) },
    { icon: BedDouble, label: t("bedrooms", { count: apartment.bedrooms }) },
    { icon: BedSingle, label: t("beds", { count: apartment.beds }) },
    { icon: Bath, label: t("baths", { count: apartment.bathrooms }) },
  ];
  return (
    <ul className="flex flex-wrap gap-2 text-sm" aria-label={tApartment("factsLabel")}>
      {facts.map(({ icon: Icon, label }) => (
        <li key={label} className="inline-flex items-center gap-2 rounded-full border px-3 py-1.5">
          <Icon className="size-4 text-muted-foreground" aria-hidden="true" />
          {label}
        </li>
      ))}
    </ul>
  );
}

/** Host avatar, name, Superhost badge and a link to the public profile. */
export function HostCard({ host }: { host: Apartment["host"] }) {
  const t = useTranslations("apartment.host");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  return (
    <Link
      href={`/users/${host.id}`}
      className="group flex items-center gap-4 rounded-2xl outline-none focus-visible:ring-[3px] focus-visible:ring-ring/40"
    >
      <span className="relative shrink-0">
        <UserAvatar name={host.firstName} src={host.avatarUrl} className="size-14" fallbackClassName="text-lg" />
        {host.isSuperhost && (
          <span className="absolute -right-1 -bottom-1 grid size-6 place-items-center rounded-full bg-primary text-primary-foreground ring-2 ring-background">
            <Award className="size-3.5" aria-hidden="true" />
          </span>
        )}
      </span>
      <span className="grid gap-1">
        <span className="flex flex-wrap items-center gap-2">
          <span className="font-semibold group-hover:underline">{t("hostedBy", { name: host.firstName })}</span>
          {host.isSuperhost && (
            <Badge variant="success">
              <Award aria-hidden="true" /> {tCommon("superhost")}
            </Badge>
          )}
        </span>
        <span className="text-sm text-muted-foreground">
          {t("hostingSince", { date: formatMonthYear(new Date(host.joinedOnUtc), locale) })}
        </span>
      </span>
    </Link>
  );
}

/** "What this place offers": the real amenities, grouped, with icons. */
export function AmenitiesList({ amenities }: { amenities: Amenity[] }) {
  const t = useTranslations("listing");
  const tApartment = useTranslations("apartment");
  const owned = new Set(amenities);
  const groups = AMENITY_GROUPS.map((group) => ({
    key: group.key,
    items: group.amenities.filter((a) => owned.has(a)),
  })).filter((group) => group.items.length > 0);

  return (
    <section aria-labelledby="amenities-heading" className="grid gap-5">
      <h2 id="amenities-heading" className="text-xl font-semibold">
        {tApartment("amenitiesHeading")}
      </h2>
      {groups.length === 0 ? (
        <p className="text-sm text-muted-foreground">{tApartment("noAmenities")}</p>
      ) : (
        <div className="grid gap-6">
          {groups.map((group) => (
            <div key={group.key} className="grid gap-3">
              <h3 className="text-sm font-semibold text-muted-foreground">{t(`amenityGroups.${group.key}`)}</h3>
              <ul className="grid gap-3 sm:grid-cols-2">
                {group.items.map((amenity) => {
                  const Icon = AMENITY_ICONS[amenity];
                  return (
                    <li key={amenity} className="flex items-center gap-3">
                      <Icon className="size-5 shrink-0" aria-hidden="true" />
                      {t(`amenities.${amenity}`)}
                    </li>
                  );
                })}
              </ul>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

/** Check-in/out times, what's allowed, the stay limits and the host's own rules. */
/** "15:00" -> "3:00 PM" (en) / "15:00" (nl, fr). */
function localTime(value: string, locale: string) {
  const [h, m] = value.split(":").map(Number);
  if (!Number.isFinite(h) || !Number.isFinite(m)) return value;
  return formatTime(new Date(2000, 0, 1, h, m), locale);
}

export function HouseRules({ apartment }: { apartment: Apartment }) {
  const t = useTranslations("apartment.rules");
  const locale = useLocale();
  const rows = [
    { icon: Clock, label: t("checkIn", { time: localTime(apartment.checkInTime, locale) }) },
    { icon: DoorOpen, label: t("checkOut", { time: localTime(apartment.checkOutTime, locale) }) },
    { icon: Users, label: t("maxGuests", { count: apartment.maximumGuestCount }) },
    {
      icon: CalendarRange,
      label:
        apartment.minNights > 1
          ? t("stayLimits", { min: apartment.minNights, max: apartment.maxNights })
          : t("maxStay", { count: apartment.maxNights }),
    },
    { icon: PawPrint, label: apartment.petsAllowed ? t("petsAllowed") : t("noPets"), muted: !apartment.petsAllowed },
    {
      icon: apartment.smokingAllowed ? Cigarette : CigaretteOff,
      label: apartment.smokingAllowed ? t("smokingAllowed") : t("noSmoking"),
      muted: !apartment.smokingAllowed,
    },
    {
      icon: PartyPopper,
      label: apartment.partiesAllowed ? t("partiesAllowed") : t("noParties"),
      muted: !apartment.partiesAllowed,
    },
  ];

  return (
    <section aria-labelledby="rules-heading" className="grid gap-5">
      <h2 id="rules-heading" className="text-xl font-semibold">
        {t("heading")}
      </h2>
      <ul className="grid gap-3 sm:grid-cols-2">
        {rows.map(({ icon: Icon, label, muted }) => (
          <li key={label} className={cn("flex items-center gap-3", muted && "text-muted-foreground")}>
            <Icon className="size-5 shrink-0" aria-hidden="true" />
            {label}
          </li>
        ))}
      </ul>
      {apartment.houseRules && (
        <div className="flex items-start gap-3 rounded-2xl bg-muted/60 p-4 text-sm">
          <ShieldCheck className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
          <div className="grid gap-1">
            <p className="font-medium">{t("fromHost")}</p>
            <p className="whitespace-pre-line text-muted-foreground">{apartment.houseRules}</p>
          </div>
        </div>
      )}
    </section>
  );
}
