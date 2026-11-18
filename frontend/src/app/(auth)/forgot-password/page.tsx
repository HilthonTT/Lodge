import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { Suspense } from "react";

import { ForgotPasswordForm } from "@/features/auth/components/forgot-password-form";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("auth.forgotPassword");
  return { title: t("metaTitle"), robots: { index: false } };
}

export default async function ForgotPasswordPage() {
  const t = await getTranslations("auth.forgotPassword");

  return (
    <div className="grid gap-8">
      <div className="grid gap-2">
        <h1 className="text-3xl font-semibold">{t("title")}</h1>
        <p className="text-muted-foreground">{t("subtitle")}</p>
      </div>
      <Suspense>
        <ForgotPasswordForm />
      </Suspense>
    </div>
  );
}
