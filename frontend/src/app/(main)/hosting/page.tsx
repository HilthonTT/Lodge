import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { HostingDashboard } from "@/features/hosting/components/hosting-dashboard";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("hosting.meta");
  return { title: t("dashboard"), robots: { index: false, follow: false } };
}

export default function HostingPage() {
  return <HostingDashboard />;
}
