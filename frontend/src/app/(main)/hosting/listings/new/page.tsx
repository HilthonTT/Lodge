import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { ListingWizard } from "@/features/hosting/components/wizard/listing-wizard";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("hosting.meta");
  return { title: t("newListing"), robots: { index: false, follow: false } };
}

export default function NewListingPage() {
  return (
    <div className="mx-auto w-full max-w-4xl">
      <ListingWizard />
    </div>
  );
}
