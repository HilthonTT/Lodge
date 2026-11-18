import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";

import { HostCalendar } from "@/features/hosting/components/host-calendar";
import { isUuid } from "@/lib/uuid";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("hosting.meta");
  return { title: t("calendar"), robots: { index: false, follow: false } };
}

export default async function HostCalendarPage({ params }: PageProps<"/hosting/listings/[listingId]/calendar">) {
  const { listingId } = await params;
  if (!isUuid(listingId)) notFound();
  return <HostCalendar listingId={listingId} />;
}
