import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { Suspense } from "react";

import { VerifyEmailView } from "@/features/auth/components/verify-email-view";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("auth.verifyEmail");
  return { title: t("metaTitle"), robots: { index: false } };
}

export default function VerifyEmailPage() {
  return (
    <Suspense>
      <VerifyEmailView />
    </Suspense>
  );
}
