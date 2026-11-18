import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { Container } from "@/components/shared/container";
import { PageHeader } from "@/components/shared/page-header";
import { Breadcrumbs } from "@/components/ui/breadcrumb";
import { LoginSecurity } from "@/features/users/components/login-security";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("settings.security");
  return { title: t("title") };
}

export default async function LoginAndSecurityPage() {
  const t = await getTranslations("settings");

  return (
    <Container className="grid max-w-3xl gap-8">
      <div className="grid gap-3">
        <Breadcrumbs items={[{ label: t("title"), href: "/settings" }, { label: t("security.title") }]} />
        <PageHeader title={t("security.title")} />
      </div>
      <LoginSecurity />
    </Container>
  );
}
