import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { PageHeader } from "@/components/shared/page-header";
import { ReservationsView } from "@/features/hosting/components/reservations-view";
import { parseReservationsSearch } from "@/features/hosting/reservations-search-params";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("hosting.meta");
  return { title: t("reservations"), robots: { index: false, follow: false } };
}

export default async function HostReservationsPage({ searchParams }: PageProps<"/hosting/reservations">) {
  const [search, t] = await Promise.all([
    searchParams.then(parseReservationsSearch),
    getTranslations("hosting.reservations"),
  ]);
  return (
    <div className="grid gap-8">
      <PageHeader title={t("title")} description={t("description")} />
      <ReservationsView search={search} />
    </div>
  );
}
