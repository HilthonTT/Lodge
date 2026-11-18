import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { NotificationsView } from "@/features/notifications/components/notifications-view";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("notifications");
  return { title: t("title") };
}

const first = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);

// Auth is enforced by src/proxy.ts; data is fetched client-side with the user's token.
export default async function NotificationsPage({ searchParams }: PageProps<"/notifications">) {
  const [query, t] = await Promise.all([searchParams, getTranslations("notifications")]);
  const page = Math.min(1000, Math.max(1, Math.floor(Number(first(query.page))) || 1));
  const unreadOnly = first(query.unread) === "1";

  return (
    <Container className="grid max-w-3xl gap-8">
      <PageHeader title={t("title")} description={t("description")} />
      <NotificationsView page={page} unreadOnly={unreadOnly} />
    </Container>
  );
}
