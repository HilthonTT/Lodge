import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";

import { HostExperienceBookings } from "@/features/experiences/components/hosting/host-experience-bookings";
import { isUuid } from "@/lib/uuid";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("experiences.hosting.bookings");
  return { title: t("meta"), robots: { index: false, follow: false } };
}

export default async function ExperienceBookingsPage({
  params,
  searchParams,
}: PageProps<"/hosting/experiences/[experienceId]/bookings">) {
  const [{ experienceId }, query] = await Promise.all([params, searchParams]);
  if (!isUuid(experienceId)) notFound();
  const raw = Number(Array.isArray(query.page) ? query.page[0] : query.page);
  const page = Number.isInteger(raw) && raw > 0 && raw < 10_000 ? raw : 1;
  return <HostExperienceBookings experienceId={experienceId} page={page} />;
}
