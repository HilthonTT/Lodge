import { CalendarCheck, Hourglass, MapPin, Zap } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { ErrorState } from "@/components/shared/state";
import { Breadcrumbs } from "@/components/ui/breadcrumb";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { apartmentsApi } from "@/features/apartments/api/apartments";
import { loadApartment } from "@/features/apartments/api/apartments.server";
import { formatLocation } from "@/features/apartments/components/apartment-card";
import { ApartmentMap } from "@/features/apartments/components/apartment-map";
import { BookingPanel } from "@/features/apartments/components/booking-panel";
import { CancellationPolicySection } from "@/features/apartments/components/cancellation-policy-section";
import { AmenitiesList, HostCard, HouseRules, ListingFacts } from "@/features/apartments/components/listing-details";
import { PhotoGallery } from "@/features/apartments/components/photo-gallery";
import { RatingSummary } from "@/features/apartments/components/rating-summary";
import { ReviewsSection } from "@/features/apartments/components/reviews-section";
import { ShareButton } from "@/features/apartments/components/share-button";
import { ContactHostButton } from "@/features/messaging/components/contact-host-dialog";
import { WishlistHeart } from "@/features/wishlists/components/wishlist-heart";
import { apiErrorMessage } from "@/i18n/api-errors";
import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";
import { getServerSession } from "@/lib/auth/session.server";
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
  const [{ apartmentId }, query, t, tCommon, tListing, session] = await Promise.all([
    params,
    searchParams,
    getTranslations("apartment"),
    getTranslations("common"),
    getTranslations("listing"),
    getServerSession(),
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
  // With the visitor's token, so their own unpublished review is included.
  const [http, authHttp] = await Promise.all([getServerHttp(), getServerHttp({ auth: true })]);
  const [booked, reviews] = await Promise.all([
    attempt(() => apartmentsApi.bookedDates(http, apartment.id)),
    attempt(() => apartmentsApi.reviews(authHttp, apartment.id, { page: 1, pageSize: 6 })),
  ]);
  const country = findCountry(apartment.address.country);
  const location = formatLocation(apartment.address);
  const hasCoordinates = Number.isFinite(apartment.latitude) && Number.isFinite(apartment.longitude) && (apartment.latitude !== 0 || apartment.longitude !== 0);
  const mapCenter: [number, number] | undefined = hasCoordinates
    ? [apartment.latitude, apartment.longitude]
    : country?.latlng;
  const isOwnListing = session?.userId === apartment.host?.id;
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
                href="#reviews"
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
          <div className="-ml-3 flex items-center gap-1 self-start sm:ml-0 sm:self-auto">
            <ShareButton title={apartment.name} />
            <WishlistHeart apartmentId={apartment.id} apartmentName={apartment.name} variant="button" />
          </div>
        </div>
      </div>

      <PhotoGallery images={apartment.images ?? []} coverUrl={apartment.imageUrl} name={apartment.name} />

      <div className="grid gap-12 lg:grid-cols-[minmax(0,1fr)_24rem] lg:gap-16">
        <div className="grid content-start gap-10">
          <section className="grid gap-4">
            <h2 className="text-xl font-semibold">
              {apartment.address.city
                ? t("typeIn", { type: tListing(`propertyTypes.${apartment.propertyType}`), city: apartment.address.city })
                : tListing(`propertyTypes.${apartment.propertyType}`)}
            </h2>
            <ListingFacts apartment={apartment} />
          </section>

          <Separator />

          {apartment.host && (
            <>
              <section aria-label={t("host.label")} className="flex flex-wrap items-center justify-between gap-4">
                <HostCard host={apartment.host} />
                <ContactHostButton
                  apartmentId={apartment.id}
                  listingName={apartment.name}
                  host={apartment.host}
                  className="rounded-full"
                />
              </section>
              <Separator />
            </>
          )}

          <ul className="grid gap-4 text-sm">
            <li className="flex items-start gap-4">
              {apartment.instantBook ? (
                <Zap className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
              ) : (
                <Hourglass className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
              )}
              <span>
                <span className="block font-medium">
                  {apartment.instantBook ? t("instantBookTitle") : t("requestTitle")}
                </span>
                <span className="text-muted-foreground">
                  {apartment.instantBook ? t("instantBookDescription") : t("requestDescription", { name: apartment.host?.firstName ?? "" })}
                </span>
              </span>
            </li>
          </ul>

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

          <AmenitiesList amenities={apartment.amenities ?? []} />

          <Separator />

          <HouseRules apartment={apartment} />

          <Separator />

          <CancellationPolicySection
            policy={apartment.cancellationPolicy ?? "Moderate"}
            checkIn={first(query.startDate)}
          />

          <Separator />

          {mapCenter && (
            <>
              <section aria-labelledby="location-heading" className="grid gap-4">
                <div className="grid gap-1">
                  <h2 id="location-heading" className="text-xl font-semibold">
                    {t("whereYoullBe")}
                  </h2>
                  <p className="text-sm text-muted-foreground">
                    {country?.flag} {location || country?.name} · {t("exactAddressLater")}
                  </p>
                </div>
                <ApartmentMap
                  center={mapCenter}
                  label={location || country?.name || apartment.name}
                  zoom={hasCoordinates ? 12 : 4}
                  radius={hasCoordinates ? 1_200 : 180_000}
                />
              </section>
              <Separator />
            </>
          )}

          <ReviewsSection
            reviews={reviews.ok ? reviews.data : null}
            averageRating={apartment.averageRating}
            reviewCount={apartment.reviewCount}
            ratingBreakdown={apartment.ratingBreakdown}
            hostName={apartment.host?.firstName}
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
            isOwnListing={isOwnListing}
          />
        </aside>
      </div>
    </Container>
  );
}
