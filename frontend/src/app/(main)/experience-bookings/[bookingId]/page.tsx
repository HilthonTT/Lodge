import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { Breadcrumbs } from "@/components/ui/breadcrumb";
import { ExperienceBookingDetail } from "@/features/experiences/components/experience-booking-detail";
import { isUuid } from "@/lib/uuid";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("experiences.trips");
  return { title: t("detailTitle") };
}

// Auth is enforced by src/proxy.ts. Notifications about experience bookings link here.
export default async function ExperienceBookingPage({ params }: PageProps<"/experience-bookings/[bookingId]">) {
  const [{ bookingId }, t, tBookings] = await Promise.all([
    params,
    getTranslations("experiences.trips"),
    getTranslations("bookings"),
  ]);
  if (!isUuid(bookingId)) notFound();
  return (
    <Container className="grid max-w-6xl gap-6">
      <Breadcrumbs items={[{ label: tBookings("title"), href: "/bookings?tab=experiences" }, { label: t("detailTitle") }]} />
      <ExperienceBookingDetail bookingId={bookingId} />
    </Container>
  );
}
