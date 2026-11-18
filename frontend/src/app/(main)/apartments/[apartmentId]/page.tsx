import { BedDouble, CalendarCheck, MapPin, TentTree, Users } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { RemoteImage } from "@/components/shared/remote-image";
import { ErrorState } from "@/components/shared/state";
import { Breadcrumbs } from "@/components/ui/breadcrumb";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { apartmentsApi } from "@/features/apartments/api/apartments";
import { loadApartment } from "@/features/apartments/api/apartments.server";
import { formatLocation } from "@/features/apartments/components/apartment-card";
import { ApartmentMap } from "@/features/apartments/components/apartment-map";
import { BookingPanel } from "@/features/apartments/components/booking-panel";
import { RatingSummary } from "@/features/apartments/components/rating-summary";
import { ReviewsSection } from "@/features/apartments/components/reviews-section";
import { ShareButton } from "@/features/apartments/components/share-button";
import { apiErrorMessage } from "@/i18n/api-errors";
import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";
import { findCountry } from "@/lib/countries.server";

export async function generateMetadata({
  params,
}: PageProps<"/apartments/[apartmentId]">): Promise<Metadata> {
  const { apartmentId } = await params;
  const result = await loadApartment(apartmentId);
  if (!result.ok) {
    const t = await getTranslations("apartment");
    return { title: t("metaFallbackTitle") };
  }

  const apartment = result.data;
  return {
    title: apartment.name,
    description: apartment.description.slice(0, 160),
    openGraph: { title: apartment.name, images: apartment.imageUrl ? [apartment.imageUrl] : undefined },
  };
}

const first = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);

export default async function ApartmentPage({
  params,
  searchParams,
}: PageProps<"/apartments/[apartmentId]">) {
  const [{ apartmentId }, query, t, tCommon] = await Promise.all([
    params,
    searchParams,
    getTranslations("apartment"),
    getTranslations("common"),
  ]);
  const result = await loadApartment(apartmentId);

  if (!result.ok) {
    const tErrors = await getTranslations("errors.api");
    return (
      <Container>
        <ErrorState
          title={t("loadError")}
          description={apiErrorMessage(result.error, tErrors)}
          action={
            <Button asChild variant="outline">
              <Link href={`/apartments/${apartmentId}`} prefetch={false}>
                {tCommon("tryAgain")}
              </Link>
            </Button>
          }
        />
      </Container>
    );
  }

  const apartment = result.data;
  const http = await getServerHttp();
  const [booked, reviews] = await Promise.all([
    attempt(() => apartmentsApi.bookedDates(http, apartment.id)),
    attempt(() => apartmentsApi.reviews(http, apartment.id, { page: 1, pageSize: 6 })),
  ]);
  const country = findCountry(apartment.address.country);
  const location = formatLocation(apartment.address);
  const guestsParam = Number(first(query.guests));

  return (
    <Container className="grid gap-8 pb-20 lg:pb-0">
      <div className="grid gap-3">
        <Breadcrumbs
          items={[
            { label: t("breadcrumbStays"), href: "/" },
            ...(apartment.address.country
              ? [{ label: apartment.address.country, href: `/search?q=${encodeURIComponent(apartment.address.country)}` }]
              : []),
            { label: apartment.name },
          ]}
        />
        <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
          <div className="grid gap-1">
            <h1 className="text-2xl font-semibold sm:text-3xl">{apartment.name}</h1>
            <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
              <a
                href="#reviews-heading"
                className="rounded-sm underline-offset-4 hover:underline focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none"
              >
                <RatingSummary
                  averageRating={apartment.averageRating}
                  reviewCount={apartment.reviewCount}
                  variant="full"
                  className="text-base"
                />
              </a>
              {location && (
                <p className="inline-flex items-center gap-1.5 text-muted-foreground">
                  <MapPin className="size-4" aria-hidden="true" /> {location}
                </p>
              )}
            </div>
          </div>
          <div className="-ml-3 self-start sm:ml-0 sm:self-auto">
            <ShareButton title={apartment.name} />
          </div>
        </div>
      </div>

      <div className="relative aspect-[16/10] max-h-[30rem] w-full overflow-hidden rounded-3xl bg-muted sm:aspect-[21/9]">
        <RemoteImage
          src={apartment.imageUrl}
          alt={t("photoAlt", { name: apartment.name })}
          fill
          priority
          sizes="(min-width: 1280px) 1216px, 100vw"
          className="object-cover"
        />
      </div>

      <div className="grid gap-12 lg:grid-cols-[minmax(0,1fr)_24rem] lg:gap-16">
        <div className="grid content-start gap-10">
          <section className="grid gap-4">
            <h2 className="text-xl font-semibold">
              {apartment.address.city ? t("entirePlaceIn", { city: apartment.address.city }) : t("entirePlace")}
            </h2>
            <ul className="flex flex-wrap gap-2 text-sm">
              <li className="inline-flex items-center gap-2 rounded-full border px-3 py-1.5">
                <Users className="size-4 text-muted-foreground" aria-hidden="true" />
                {t("upToGuests", { count: apartment.maximumGuestCount })}
              </li>
              <li className="inline-flex items-center gap-2 rounded-full border px-3 py-1.5">
                <BedDouble className="size-4 text-muted-foreground" aria-hidden="true" />
                {tCommon("rooms", { count: apartment.maximumRoomCount })}
              </li>
            </ul>
          </section>

          <Separator />

          <section className="flex items-start gap-4">
            <span className="grid size-12 shrink-0 place-items-center rounded-2xl bg-accent text-accent-foreground">
              <TentTree className="size-6" aria-hidden="true" />
            </span>
            <div className="grid gap-1">
              <h2 className="font-semibold">{t("hostedBy")}</h2>
              <p className="text-sm text-muted-foreground">{t("hostedByDescription")}</p>
            </div>
          </section>

          <Separator />

          <section aria-labelledby="about-heading" className="grid gap-3">
            <h2 id="about-heading" className="text-xl font-semibold">
              {t("about")}
            </h2>
            <p className="leading-relaxed whitespace-pre-line text-muted-foreground">
              {apartment.description || t("noDescription")}
            </p>
          </section>

          <section className="flex items-start gap-4 rounded-2xl bg-muted/60 p-5">
            <CalendarCheck className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
            <p className="text-sm">
              <span className="font-medium">{t("payLaterTitle")}</span>{" "}
              <span className="text-muted-foreground">{t("payLaterDescription")}</span>
            </p>
          </section>

          <Separator />

          {country && (
            <>
              <section aria-labelledby="location-heading" className="grid gap-4">
                <div className="grid gap-1">
                  <h2 id="location-heading" className="text-xl font-semibold">
                    {t("whereYoullBe")}
                  </h2>
                  <p className="text-sm text-muted-foreground">
                    {country.flag} {location || country.name} · {t("exactAddressLater")}
                  </p>
                </div>
                <ApartmentMap center={country.latlng} label={location || country.name} />
              </section>
              <Separator />
            </>
          )}

          <ReviewsSection
            reviews={reviews.ok ? reviews.data : null}
            averageRating={apartment.averageRating}
            reviewCount={apartment.reviewCount}
          />
        </div>

        <aside aria-label={t("reserveAside")} className="lg:sticky lg:top-28 lg:self-start">
          <BookingPanel
            apartment={apartment}
            bookedDates={booked.ok ? booked.data : []}
            bookedDatesUnavailable={!booked.ok}
            initialStartDate={first(query.startDate)}
            initialEndDate={first(query.endDate)}
            initialGuests={Number.isFinite(guestsParam) && guestsParam > 0 ? guestsParam : undefined}
          />
        </aside>
      </div>
    </Container>
  );
}
