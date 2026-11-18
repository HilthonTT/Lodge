import { BadgeCheck, CalendarDays, MessageSquareText } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { getLocale, getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { ErrorState } from "@/components/shared/state";
import { UserAvatar } from "@/components/shared/user-avatar";
import { Button } from "@/components/ui/button";
import { loadPublicProfile } from "@/features/users/api/users.server";
import { apiErrorMessage } from "@/i18n/api-errors";
import { formatMonthYear } from "@/lib/dates";
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

  return (
    <Container className="grid max-w-4xl gap-10 md:grid-cols-[18rem_1fr] md:items-start">
      <aside className="grid justify-items-center gap-5 rounded-3xl border bg-card p-8 text-center shadow-card">
        <UserAvatar
          name={`${profile.firstName} ${profile.lastInitial}`}
          src={profile.avatarUrl}
          className="size-28"
          fallbackClassName="text-3xl"
        />
        <div className="grid gap-1">
          <h1 className="text-2xl font-semibold">{name}</h1>
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
          <li className="flex items-center gap-2.5">
            <MessageSquareText className="size-5 text-muted-foreground" aria-hidden="true" />
            <span className="font-medium">{t("reviewsWritten", { count: profile.reviewsWrittenCount })}</span>
          </li>
        </ul>
      </aside>

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
    </Container>
  );
}
