import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { AuthCallback } from "@/features/auth/components/auth-callback";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("auth.callback");
  return { title: t("metaTitle"), robots: { index: false } };
}

/** Social login lands here with `#token=…`, `#twoFactorToken=…` or `#error=…` in the fragment. */
export default function AuthCallbackPage() {
  return <AuthCallback />;
}
