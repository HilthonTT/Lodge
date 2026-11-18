import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { WishlistsView } from "@/features/wishlists/components/wishlists-view";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("wishlists");
  return { title: t("metaTitle") };
}

// Auth is enforced by src/proxy.ts; data is fetched client-side with the user's token.
export default async function WishlistsPage() {
  const t = await getTranslations("wishlists");
  return (
    <Container className="grid gap-8">
      <PageHeader title={t("title")} description={t("description")} />
      <WishlistsView />
    </Container>
  );
}
