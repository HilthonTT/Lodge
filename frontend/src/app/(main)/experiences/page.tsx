import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { Suspense } from "react";

import { Container } from "@/components/shared/container";
import { Skeleton } from "@/components/ui/skeleton";
import { ExperiencesExplore } from "@/features/experiences/components/experiences-explore";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("experiences");
  return { title: t("metaTitle"), description: t("subtitle") };
}

// Results are fetched client-side from the URL (search, category, price, map toggle).
export default function ExperiencesPage() {
  return (
    <Container>
      <Suspense fallback={<Skeleton className="h-96 rounded-2xl" />}>
        <ExperiencesExplore />
      </Suspense>
    </Container>
  );
}
