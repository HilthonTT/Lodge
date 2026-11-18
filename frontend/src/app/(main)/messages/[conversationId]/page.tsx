import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";

import { ThreadView } from "@/features/messaging/components/thread-view";
import { isUuid } from "@/lib/uuid";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("messaging");
  return { title: t("title") };
}

// E-mails link here, so it must work as a page of its own (and is the mobile thread view).
export default async function ConversationPage({ params }: PageProps<"/messages/[conversationId]">) {
  const { conversationId } = await params;
  if (!isUuid(conversationId)) notFound();
  return <ThreadView key={conversationId} conversationId={conversationId} />;
}
