"use client";

import { CalendarClock, Clock } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { Skeleton } from "@/components/ui/skeleton";
import { RatingSummary } from "@/features/apartments/components/rating-summary";
import { Money } from "@/features/payments/components/money";
import { formatSessionDateTime } from "@/lib/dates";
import { cn } from "@/lib/utils";
import type { ExperienceSummary } from "@/types/api";

import { CATEGORY_THEME, ExperienceImage, useFormatDuration } from "../experience-options";

export function ExperienceCard({
  experience: e,
  priority,
  highlighted,
  onHoverChange,
}: {
  experience: ExperienceSummary;
  priority?: boolean;
  highlighted?: boolean;
  onHoverChange?: (hovered: boolean) => void;
}) {
  const t = useTranslations("experiences");
  const locale = useLocale();
  const duration = useFormatDuration();
  const Icon = CATEGORY_THEME[e.category]?.icon;

  return (
    <article
      className="group relative flex flex-col gap-3"
      data-experience-id={e.id}
      data-testid="experience-card"
      onMouseEnter={onHoverChange ? () => onHoverChange(true) : undefined}
      onMouseLeave={onHoverChange ? () => onHoverChange(false) : undefined}
    >
      <div
        className={cn(
          "relative aspect-[4/5] overflow-hidden rounded-2xl bg-muted",
          highlighted && "ring-2 ring-primary ring-offset-2 ring-offset-background",
        )}
      >
        <ExperienceImage
          src={e.imageUrl}
          category={e.category}
          priority={priority}
          sizes="(min-width: 1280px) 25vw, (min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw"
          className="transition-transform duration-500 ease-out group-hover:scale-[1.04]"
        />
        <span className="absolute top-3 left-3 inline-flex items-center gap-1.5 rounded-full bg-background/95 px-2.5 py-1 text-xs font-semibold shadow-card">
          {Icon && <Icon className="size-3.5 text-primary" aria-hidden="true" />}
          {t(`categories.${e.category}`)}
        </span>
      </div>
      <div className="grid gap-1 px-0.5">
        <div className="flex items-start justify-between gap-3">
          <h3 className="line-clamp-2 font-semibold">
            <Link
              href={`/experiences/${e.id}`}
              className="rounded-sm outline-none after:absolute after:inset-0 after:rounded-2xl focus-visible:after:ring-[3px] focus-visible:after:ring-ring/50"
            >
              {e.title}
            </Link>
          </h3>
          <RatingSummary averageRating={e.averageRating} reviewCount={e.reviewCount} className="shrink-0" />
        </div>
        <p className="line-clamp-1 text-sm text-muted-foreground">
          {[e.city, e.country].filter(Boolean).join(", ")} · {t("hostedBy", { name: e.hostFirstName })}
        </p>
        <p className="flex flex-wrap items-center gap-x-3 gap-y-0.5 text-sm text-muted-foreground">
          <span className="inline-flex items-center gap-1">
            <Clock className="size-3.5" aria-hidden="true" /> {duration(e.durationMinutes)}
          </span>
          {e.nextSessionStartsAtUtc && (
            <span className="inline-flex items-center gap-1">
              <CalendarClock className="size-3.5" aria-hidden="true" />
              {t("nextSession", { date: formatSessionDateTime(new Date(e.nextSessionStartsAtUtc), locale) })}
            </span>
          )}
        </p>
        <p className="mt-1">
          <span className="text-sm text-muted-foreground">{t("from")} </span>
          <Money amount={e.pricePerGuest} currency={e.currency} compact className="font-semibold" />{" "}
          <span className="text-sm text-muted-foreground">{t("perGuest")}</span>
        </p>
      </div>
    </article>
  );
}

export function ExperienceCardSkeleton() {
  return (
    <div className="flex flex-col gap-3">
      <Skeleton className="aspect-[4/5] rounded-2xl" />
      <div className="grid gap-2 px-0.5">
        <Skeleton className="h-4 w-3/4" />
        <Skeleton className="h-3.5 w-1/2" />
        <Skeleton className="h-4 w-1/4" />
      </div>
    </div>
  );
}
