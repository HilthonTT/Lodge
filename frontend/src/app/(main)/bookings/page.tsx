import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { Suspense } from "react";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { BookingsSkeleton, BookingsView } from "@/features/bookings/components/bookings-view";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("bookings");
  return { title: t("title") };
}

// Auth is enforced by src/proxy.ts; data is fetched client-side with the user's token.
export default async function BookingsPage() {
  const t = await getTranslations("bookings");

  return (
    <Container className="grid max-w-5xl gap-8">
      <PageHeader title={t("title")} description={t("description")} />
      <Suspense fallback={<BookingsSkeleton />}>
        <BookingsView />
      </Suspense>
    </Container>
  );
}
