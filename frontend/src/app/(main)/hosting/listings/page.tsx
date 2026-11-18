import { Plus } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { getTranslations } from "next-intl/server";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { HostListingsView } from "@/features/hosting/components/host-listings-view";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("hosting.meta");
  return { title: t("listings"), robots: { index: false, follow: false } };
}

export default async function HostListingsPage({ searchParams }: PageProps<"/hosting/listings">) {
  const [query, t] = await Promise.all([searchParams, getTranslations("hosting.listings")]);
  const raw = Number(Array.isArray(query.page) ? query.page[0] : query.page);
  const page = Number.isInteger(raw) && raw > 0 && raw < 10_000 ? raw : 1;

  return (
    <div className="grid gap-8">
      <PageHeader
        title={t("title")}
        description={t("description")}
        actions={
          <Button asChild className="rounded-full">
            <Link href="/hosting/listings/new">
              <Plus aria-hidden="true" /> {t("create")}
            </Link>
          </Button>
        }
      />
      <HostListingsView page={page} />
    </div>
  );
}
