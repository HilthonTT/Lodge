import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { PageHeader } from "@/components/shared/page-header";
import { HostReviewsView } from "@/features/hosting/components/host-reviews-view";
import { parseHostReviewsSearch } from "@/features/hosting/host-reviews-search-params";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("hosting.meta");
  return { title: t("reviews"), robots: { index: false, follow: false } };
}

export default async function HostReviewsPage({ searchParams }: PageProps<"/hosting/reviews">) {
  const [search, t] = await Promise.all([
    searchParams.then(parseHostReviewsSearch),
    getTranslations("hosting.reviews"),
  ]);
  return (
    <div className="grid gap-8">
      <PageHeader title={t("title")} description={t("description")} />
      <HostReviewsView search={search} />
    </div>
  );
}
