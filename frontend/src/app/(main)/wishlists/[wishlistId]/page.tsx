import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { WishlistDetailView } from "@/features/wishlists/components/wishlist-detail-view";
import { isUuid } from "@/lib/uuid";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("wishlists");
  return { title: t("metaTitle") };
}

// Auth is enforced by src/proxy.ts; data is fetched client-side with the user's token.
export default async function WishlistPage({ params }: PageProps<"/wishlists/[wishlistId]">) {
  const { wishlistId } = await params;
  if (!isUuid(wishlistId)) notFound();
  return (
    <Container className="grid gap-8">
      <WishlistDetailView wishlistId={wishlistId} />
    </Container>
  );
}
