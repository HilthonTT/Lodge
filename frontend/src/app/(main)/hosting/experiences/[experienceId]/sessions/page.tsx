import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";

import { SessionsManager } from "@/features/experiences/components/hosting/sessions-manager";
import { isUuid } from "@/lib/uuid";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("experiences.hosting.sessions");
  return { title: t("meta"), robots: { index: false, follow: false } };
}

export default async function ExperienceSessionsPage({ params }: PageProps<"/hosting/experiences/[experienceId]/sessions">) {
  const { experienceId } = await params;
  if (!isUuid(experienceId)) notFound();
  return <SessionsManager experienceId={experienceId} />;
}
