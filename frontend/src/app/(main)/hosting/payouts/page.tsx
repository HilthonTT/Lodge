import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { PageHeader } from "@/components/shared/page-header";
import { PayoutsView } from "@/features/hosting/components/payouts-view";
import { parsePayoutsSearch } from "@/features/hosting/payouts-search-params";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("hosting.meta");
  return { title: t("payouts"), robots: { index: false, follow: false } };
}

export default async function HostPayoutsPage({ searchParams }: PageProps<"/hosting/payouts">) {
  const [search, t] = await Promise.all([searchParams.then(parsePayoutsSearch), getTranslations("hosting.payouts")]);
  return (
    <div className="grid gap-8">
      <PageHeader title={t("title")} description={t("description")} />
      <PayoutsView search={search} />
    </div>
  );
}
