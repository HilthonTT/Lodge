import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";

import { ListingWizard } from "@/features/hosting/components/wizard/listing-wizard";
import { isUuid } from "@/lib/uuid";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("hosting.meta");
  return { title: t("editListing"), robots: { index: false, follow: false } };
}

export default async function EditListingPage({ params }: PageProps<"/hosting/listings/[listingId]/edit">) {
  const { listingId } = await params;
  if (!isUuid(listingId)) notFound();
  return (
    <div className="mx-auto w-full max-w-4xl">
      <ListingWizard listingId={listingId} />
    </div>
  );
}
