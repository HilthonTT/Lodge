import { Award, BadgeCheck, CalendarDays, House, MessageSquareText, Star } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { getLocale, getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { ErrorState } from "@/components/shared/state";
import { UserAvatar } from "@/components/shared/user-avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ApartmentCard } from "@/features/apartments/components/apartment-card";
import { StarRating } from "@/features/reviews/components/review-item";
import { loadPublicProfile } from "@/features/users/api/users.server";
import { apiErrorMessage } from "@/i18n/api-errors";
import { formatMonthYear } from "@/lib/dates";
import { formatRating } from "@/lib/format";
import type { PublicProfile } from "@/types/api";

/** "Jamie L." — the API only exposes the first name and last initial. */
const displayName = (profile: PublicProfile) =>
  profile.lastInitial ? `${profile.firstName} ${profile.lastInitial}.` : profile.firstName;

export async function generateMetadata({ params }: PageProps<"/users/[userId]">): Promise<Metadata> {
  const { userId } = await params;
  const [result, t, locale] = await Promise.all([loadPublicProfile(userId), getTranslations("profile"), getLocale()]);
  if (!result.ok) return { title: t("metaFallbackTitle") };

  const profile = result.data;
  const name = displayName(profile);
  const description =
    profile.bio?.slice(0, 160) ||
    t("metaDescription", { name, date: formatMonthYear(new Date(profile.joinedOnUtc), locale) });
  return {
    title: name,
    description,
    openGraph: {
      title: name,
      description,
      type: "profile",
      images: profile.avatarUrl ? [profile.avatarUrl] : undefined,
    },
  };
}

export default async function PublicProfilePage({ params }: PageProps<"/users/[userId]">) {
  const { userId } = await params;
  const [result, t, tCommon, locale] = await Promise.all([
    loadPublicProfile(userId),
    getTranslations("profile"),
    getTranslations("common"),
    getLocale(),
  ]);

  if (!result.ok) {
    const tErrors = await getTranslations("errors.api");
    return (
      <Container>
        <ErrorState
          title={t("loadError")}
          description={apiErrorMessage(result.error, tErrors)}
          action={
            <Button asChild variant="outline">
              <Link href={`/users/${userId}`} prefetch={false}>
                {tCommon("tryAgain")}
              </Link>
            </Button>
          }
        />
      </Container>
    );
  }

  const profile = result.data;
  const name = displayName(profile);
  const joined = new Date(profile.joinedOnUtc);
  const listings = profile.listings ?? [];
  const isHost = Boolean(profile.hostingSince) || listings.length > 0;

  return (
    <Container className="grid max-w-5xl gap-10 md:grid-cols-[18rem_1fr] md:items-start">
      <aside className="grid justify-items-center gap-5 rounded-3xl border bg-card p-8 text-center shadow-card">
        <UserAvatar
          name={`${profile.firstName} ${profile.lastInitial}`}
          src={profile.avatarUrl}
          className="size-28"
          fallbackClassName="text-3xl"
        />
        <div className="grid justify-items-center gap-1">
          <h1 className="text-2xl font-semibold">{name}</h1>
          {profile.isSuperhost && (
            <Badge variant="success" className="mb-1">
              <Award aria-hidden="true" /> {tCommon("superhost")}
            </Badge>
          )}
          <p className="flex items-center justify-center gap-1.5 text-sm text-muted-foreground">
            <CalendarDays className="size-4" aria-hidden="true" />
            <time dateTime={profile.joinedOnUtc}>{t("joined", { date: formatMonthYear(joined, locale) })}</time>
          </p>
        </div>
        <ul className="grid w-full gap-3 border-t pt-5 text-left text-sm">
          {profile.emailVerified && (
            <li className="flex items-center gap-2.5">
              <BadgeCheck className="size-5 text-primary" aria-hidden="true" />
              <span>
                <span className="font-medium">{t("verified")}</span>
                <span className="block text-xs text-muted-foreground">{t("verifiedDescription")}</span>
              </span>
            </li>
          )}
          {isHost && profile.hostingSince && (
            <li className="flex items-center gap-2.5">
              <House className="size-5 text-muted-foreground" aria-hidden="true" />
              <span className="font-medium">
                {t("hostingSince", { date: formatMonthYear(new Date(profile.hostingSince), locale) })}
              </span>
            </li>
          )}
          {isHost && (
            <li className="flex items-center gap-2.5">
              <Star className="size-5 fill-star text-star" aria-hidden="true" />
              <span>
                <span className="font-medium">
                  {profile.hostAverageRating !== null
                    ? t("hostRating", { rating: formatRating(profile.hostAverageRating, locale) })
                    : t("hostNoRating")}
                </span>
                <span className="block text-xs text-muted-foreground">
                  {t("hostReviews", { count: profile.hostReviewCount })}
                </span>
              </span>
            </li>
          )}
          {(profile.guestReviewCount ?? 0) > 0 && (
            <li className="flex items-center gap-2.5">
              <Star className="size-5 text-muted-foreground" aria-hidden="true" />
              <span>
                <span className="font-medium">
                  {profile.guestAverageRating !== null
                    ? t("guestRating", { rating: formatRating(profile.guestAverageRating, locale) })
                    : t("guestNoRating")}
                </span>
                <span className="block text-xs text-muted-foreground">
                  {t("guestReviews", { count: profile.guestReviewCount })}
                </span>
              </span>
            </li>
          )}
          <li className="flex items-center gap-2.5">
            <MessageSquareText className="size-5 text-muted-foreground" aria-hidden="true" />
            <span className="font-medium">{t("reviewsWritten", { count: profile.reviewsWrittenCount })}</span>
          </li>
        </ul>
      </aside>

      <div className="grid content-start gap-10">
        <section className="grid gap-4" aria-labelledby="about-heading">
          <h2 id="about-heading" className="text-xl font-semibold">
            {t("about", { name: profile.firstName })}
          </h2>
          {profile.bio ? (
            <p className="leading-relaxed whitespace-pre-line">{profile.bio}</p>
          ) : (
            <p className="text-muted-foreground">{t("noBio", { name: profile.firstName })}</p>
          )}
        </section>

        {(profile.guestReviewCount ?? 0) > 0 && (
          <section className="grid gap-5 border-t pt-8" aria-labelledby="guest-reviews-heading" data-testid="guest-reviews">
            <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
              <h2 id="guest-reviews-heading" className="text-xl font-semibold">
                {t("reviewsFromHosts")}
              </h2>
              <span className="inline-flex items-center gap-1 text-sm">
                <Star className="size-3.5 fill-star text-star" aria-hidden="true" />
                {profile.guestAverageRating !== null && (
                  <span className="font-semibold tabular-nums">{formatRating(profile.guestAverageRating, locale)}</span>
                )}
                <span className="text-muted-foreground">· {tCommon("reviews", { count: profile.guestReviewCount })}</span>
              </span>
            </div>
            <ul className="grid gap-4 sm:grid-cols-2">
              {(profile.guestReviews ?? []).map((review) => (
                <li key={review.id} className="grid content-start gap-3 rounded-2xl border bg-card p-5">
                  <div className="flex items-center gap-3">
                    <UserAvatar name={review.hostFirstName} src={review.hostAvatarUrl} className="size-10" />
                    <div className="grid min-w-0 gap-0.5 text-sm">
                      <p className="truncate font-semibold">{review.hostFirstName}</p>
                      <p className="truncate text-xs text-muted-foreground">
                        {t("hostOf", { listing: review.listingName })} ·{" "}
                        <time dateTime={review.createdOnUtc}>{formatMonthYear(new Date(review.createdOnUtc), locale)}</time>
                      </p>
                    </div>
                  </div>
                  <StarRating value={review.rating} />
                  <p className="text-sm leading-relaxed whitespace-pre-line">{review.comment}</p>
                </li>
              ))}
            </ul>
          </section>
        )}

        {listings.length > 0 && (
          <section className="grid gap-5 border-t pt-8" aria-labelledby="listings-heading">
            <h2 id="listings-heading" className="text-xl font-semibold">
              {t("listings", { name: profile.firstName })}
            </h2>
            <ul className="grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-2">
              {listings.map((apartment, index) => (
                <li key={apartment.id}>
                  <ApartmentCard
                    apartment={apartment}
                    priority={index < 2}
                    sizes="(min-width: 768px) 320px, (min-width: 640px) 50vw, 100vw"
                  />
                </li>
              ))}
            </ul>
          </section>
        )}
      </div>
    </Container>
  );
}
