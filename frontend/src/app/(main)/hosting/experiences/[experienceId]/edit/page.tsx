import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";

import { ExperienceWizard } from "@/features/experiences/components/hosting/experience-wizard";
import { isUuid } from "@/lib/uuid";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("experiences.hosting.wizard");
  return { title: t("editMeta"), robots: { index: false, follow: false } };
}

export default async function EditExperiencePage({ params }: PageProps<"/hosting/experiences/[experienceId]/edit">) {
  const { experienceId } = await params;
  if (!isUuid(experienceId)) notFound();
  return (
    <div className="mx-auto w-full max-w-4xl">
      <ExperienceWizard experienceId={experienceId} />
    </div>
  );
}
