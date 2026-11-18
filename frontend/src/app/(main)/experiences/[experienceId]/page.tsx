import { Award, CalendarCheck, Check, MapPin } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { ErrorState } from "@/components/shared/state";
import { UserAvatar } from "@/components/shared/user-avatar";
import { Badge } from "@/components/ui/badge";
import { Breadcrumbs } from "@/components/ui/breadcrumb";
import { Separator } from "@/components/ui/separator";
import { ApartmentMap } from "@/features/apartments/components/apartment-map";
import { PhotoGallery } from "@/features/apartments/components/photo-gallery";
import { RatingSummary } from "@/features/apartments/components/rating-summary";
import { ShareButton } from "@/features/apartments/components/share-button";
import { experiencesApi } from "@/features/experiences/api/experiences";
import { loadExperience } from "@/features/experiences/api/experiences.server";
import { ExperienceFacts, ExperienceHero } from "@/features/experiences/components/experience-facts";
import { ExperienceReviews } from "@/features/experiences/components/experience-reviews";
import { SessionPicker } from "@/features/experiences/components/session-picker";
import { apiErrorMessage } from "@/i18n/api-errors";
import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";

export async function generateMetadata({ params }: PageProps<"/experiences/[experienceId]">): Promise<Metadata> {
  const { experienceId } = await params;
  const result = await loadExperience(experienceId);
  if (!result.ok) {
    const t = await getTranslations("experiences");
    return { title: t("metaTitle") };
  }
  return { title: result.data.title, description: result.data.description.slice(0, 160) };
}

export default async function ExperiencePage({ params }: PageProps<"/experiences/[experienceId]">) {
  const [{ experienceId }, t, tExp] = await Promise.all([
    params,
    getTranslations("experiences.detail"),
    getTranslations("experiences"),
  ]);
  const result = await loadExperience(experienceId);

  if (!result.ok) {
    const tErrors = await getTranslations("errors.api");
    return (
      <Container>
        <ErrorState title={t("loadError")} description={apiErrorMessage(result.error, tErrors)} />
      </Container>
    );
  }

  const e = result.data;
  const http = await getServerHttp();
  const reviews = await attempt(() => experiencesApi.reviews(http, e.id, { page: 1, pageSize: 6 }));
  const location = [e.city, e.country].filter(Boolean).join(", ");
  const hasPin = Number.isFinite(e.latitude) && Number.isFinite(e.longitude) && (e.latitude !== 0 || e.longitude !== 0);

  return (
    <Container className="grid gap-8 pb-20 lg:pb-0">
      <div className="grid gap-3">
        <Breadcrumbs
          items={[
            { label: tExp("title"), href: "/experiences" },
            { label: tExp(`categories.${e.category}`), href: `/experiences?category=${e.category}` },
            { label: e.title },
          ]}
        />
        <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
          <div className="grid gap-1">
            <h1 className="text-2xl font-semibold sm:text-3xl">{e.title}</h1>
            <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
              <a href="#reviews-heading" className="rounded-sm underline-offset-4 hover:underline">
                <RatingSummary averageRating={e.averageRating} reviewCount={e.reviewCount} variant="full" className="text-base" />
              </a>
              {location && (
                <p className="inline-flex items-center gap-1.5 text-muted-foreground">
                  <MapPin className="size-4" aria-hidden="true" /> {location}
                </p>
              )}
            </div>
          </div>
          <div className="-ml-3 self-start sm:ml-0 sm:self-auto">
            <ShareButton title={e.title} />
          </div>
        </div>
      </div>

      {e.images.length > 0 ? (
        <PhotoGallery images={e.images} coverUrl={e.images[0]!.url} name={e.title} />
      ) : (
        <ExperienceHero category={e.category} />
      )}

      <div className="grid gap-12 lg:grid-cols-[minmax(0,1fr)_24rem] lg:gap-16">
        <div className="grid content-start gap-10">
          <section aria-label={t("hostLabel")} className="flex items-center gap-4">
            <Link href={`/users/${e.host.id}`} className="group flex items-center gap-4">
              <span className="relative shrink-0">
                <UserAvatar name={e.host.firstName} src={e.host.avatarUrl} className="size-14" fallbackClassName="text-lg" />
                {e.host.isSuperhost && (
                  <span className="absolute -right-1 -bottom-1 grid size-6 place-items-center rounded-full bg-primary text-primary-foreground ring-2 ring-background">
                    <Award className="size-3.5" aria-hidden="true" />
                  </span>
                )}
              </span>
              <span className="grid gap-1">
                <span className="font-semibold group-hover:underline">{tExp("hostedBy", { name: e.host.firstName })}</span>
                {e.host.isSuperhost && (
                  <Badge variant="success">
                    <Award aria-hidden="true" /> {t("superhost")}
                  </Badge>
                )}
              </span>
            </Link>
          </section>

          <ExperienceFacts experience={e} />

          <Separator />

          <section aria-labelledby="about-heading" className="grid gap-3">
            <h2 id="about-heading" className="text-xl font-semibold">
              {t("about")}
            </h2>
            <p className="leading-relaxed whitespace-pre-line text-muted-foreground">{e.description}</p>
          </section>

          {e.included.length > 0 && (
            <>
              <Separator />
              <section aria-labelledby="included-heading" className="grid gap-4">
                <h2 id="included-heading" className="text-xl font-semibold">
                  {t("included")}
                </h2>
                <ul className="grid gap-3 sm:grid-cols-2">
                  {e.included.map((item) => (
                    <li key={item} className="flex items-start gap-3">
                      <Check className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
                      {item}
                    </li>
                  ))}
                </ul>
              </section>
            </>
          )}

          <Separator />

          <section aria-labelledby="meeting-heading" className="grid gap-4">
            <div className="grid gap-1">
              <h2 id="meeting-heading" className="text-xl font-semibold">
                {t("meetingPoint")}
              </h2>
              <p className="flex items-start gap-1.5 text-muted-foreground">
                <MapPin className="mt-0.5 size-4 shrink-0" aria-hidden="true" /> {e.meetingPoint || location}
              </p>
            </div>
            {hasPin && <ApartmentMap center={[e.latitude, e.longitude]} label={e.meetingPoint || location} zoom={14} radius={250} />}
          </section>

          <Separator />

          <section className="flex items-start gap-4 rounded-2xl bg-muted/60 p-5" data-testid="free-cancellation">
            <CalendarCheck className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
            <p className="text-sm">
              <span className="font-medium">{t("freeCancellationTitle")}</span>{" "}
              <span className="text-muted-foreground">{t("freeCancellationText", { hours: e.freeCancellationHours || 24 })}</span>
            </p>
          </section>

          <Separator />

          <ExperienceReviews
            experienceId={e.id}
            averageRating={e.averageRating}
            reviewCount={e.reviewCount}
            initial={reviews.ok ? reviews.data : null}
          />
        </div>

        <aside aria-label={t("bookAside")} className="lg:sticky lg:top-28 lg:self-start">
          <SessionPicker experience={e} />
        </aside>
      </div>
    </Container>
  );
}

