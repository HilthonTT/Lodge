import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { ExperienceWizard } from "@/features/experiences/components/hosting/experience-wizard";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("experiences.hosting.wizard");
  return { title: t("newTitle"), robots: { index: false, follow: false } };
}

export default function NewExperiencePage() {
  return (
    <div className="mx-auto w-full max-w-4xl">
      <ExperienceWizard />
    </div>
  );
}
