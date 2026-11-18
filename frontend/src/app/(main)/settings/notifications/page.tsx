import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { Breadcrumbs } from "@/components/ui/breadcrumb";
import { NotificationPreferencesForm } from "@/features/notifications/components/notification-preferences";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("notifications.settings");
  return { title: t("title") };
}

export default async function NotificationSettingsPage() {
  const [t, tSettings] = await Promise.all([getTranslations("notifications.settings"), getTranslations("settings")]);

  return (
    <Container className="grid max-w-3xl gap-8">
      <div className="grid gap-3">
        <Breadcrumbs items={[{ label: tSettings("title"), href: "/settings" }, { label: t("title") }]} />
        <PageHeader title={t("title")} description={t("description")} />
      </div>
      <NotificationPreferencesForm />
    </Container>
  );
}
