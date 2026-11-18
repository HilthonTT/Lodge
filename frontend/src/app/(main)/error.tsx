"use client";

import { useTranslations } from "next-intl";
import { useEffect } from "react";

import { Container } from "@/components/shared/container";
import { ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";

export default function MainError({
  error,
  retry,
}: {
  error: Error & { digest?: string };
  /** Re-fetches and re-renders the segment (stable since Next 16.3). */
  retry: () => void;
}) {
  const t = useTranslations();

  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <Container className="py-10">
      <ErrorState
        title={t("errors.page.title")}
        description={t("errors.page.description")}
        action={<Button onClick={() => retry()}>{t("common.tryAgain")}</Button>}
      />
    </Container>
  );
}
