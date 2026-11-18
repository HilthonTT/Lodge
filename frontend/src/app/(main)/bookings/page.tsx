import { Compass, House } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { Suspense } from "react";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { BookingsSkeleton, BookingsView } from "@/features/bookings/components/bookings-view";
import { ExperienceTrips } from "@/features/experiences/components/experience-trips";
import { cn } from "@/lib/utils";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("bookings");
  return { title: t("title") };
}

const first = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);

// Auth is enforced by src/proxy.ts; data is fetched client-side with the user's token.
export default async function BookingsPage({ searchParams }: PageProps<"/bookings">) {
  const [query, t] = await Promise.all([searchParams, getTranslations("bookings")]);
  const experiences = first(query.tab) === "experiences";

  const tab = (active: boolean) =>
    cn(
      "inline-flex h-9 items-center gap-1.5 rounded-full px-4 text-sm font-medium transition-colors focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none",
      active ? "bg-background text-foreground shadow-xs" : "text-muted-foreground hover:text-foreground",
    );

  return (
    <Container className="grid max-w-5xl gap-8">
      <PageHeader title={t("title")} description={t("description")} />
      <nav aria-label={t("tabsLabel")} className="inline-flex items-center gap-0.5 justify-self-start rounded-full bg-muted p-1">
        <Link href="/bookings" aria-current={!experiences ? "page" : undefined} className={tab(!experiences)} scroll={false}>
          <House className="size-4" aria-hidden="true" /> {t("tabStays")}
        </Link>
        <Link
          href="/bookings?tab=experiences"
          aria-current={experiences ? "page" : undefined}
          className={tab(experiences)}
          scroll={false}
          data-testid="trips-tab-experiences"
        >
          <Compass className="size-4" aria-hidden="true" /> {t("tabExperiences")}
        </Link>
      </nav>
      {experiences ? (
        <ExperienceTrips highlight={first(query.highlight) ?? null} />
      ) : (
        <Suspense fallback={<BookingsSkeleton />}>
          <BookingsView />
        </Suspense>
      )}
    </Container>
  );
}
