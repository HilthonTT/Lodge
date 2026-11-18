"use client";

import { Loader2, ShieldAlert } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";
import { toast } from "sonner";

import { StatePanel } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { loginUrl } from "@/lib/auth/routes";
import { safeRedirectPath } from "@/lib/auth/token";

import { useSession } from "../session-provider";
import { TwoFactorForm } from "./two-factor-form";

/** Codes the API can put in `#error=` after a social login. */
const EXTERNAL_ERROR_KEYS = {
  "External.ProviderNotConfigured": "providerNotConfigured",
  "External.InvalidReturnUrl": "invalidReturnUrl",
  "External.InvalidState": "invalidState",
  "External.AccessDenied": "accessDenied",
  "External.ProviderError": "providerError",
  "External.EmailNotVerified": "emailNotVerified",
  "External.EmailInUse": "emailInUse",
  "User.NotFound": "userNotFound",
} as const;

type ExternalErrorKey = (typeof EXTERNAL_ERROR_KEYS)[keyof typeof EXTERNAL_ERROR_KEYS] | "unknown";

type CallbackResult =
  | { kind: "token"; token: string; returnUrl: string }
  | { kind: "twoFactor"; twoFactorToken: string; returnUrl: string }
  | { kind: "error"; errorKey: ExternalErrorKey };

/** Reads `#token=…` / `#twoFactorToken=…` / `#error=…` (plus `returnUrl`) from the URL fragment. */
function parseFragment(hash: string): CallbackResult {
  const params = new URLSearchParams(hash.replace(/^#/, ""));
  const returnUrl = safeRedirectPath(params.get("returnUrl"));
  const token = params.get("token");
  if (token) return { kind: "token", token, returnUrl };
  const twoFactorToken = params.get("twoFactorToken");
  if (twoFactorToken) return { kind: "twoFactor", twoFactorToken, returnUrl };
  const error = params.get("error") ?? "";
  const errorKey = Object.hasOwn(EXTERNAL_ERROR_KEYS, error)
    ? EXTERNAL_ERROR_KEYS[error as keyof typeof EXTERNAL_ERROR_KEYS]
    : "unknown";
  return { kind: "error", errorKey };
}

/**
 * Landing page for the social login redirect. Client-only (the fragment never
 * reaches the server); the fragment is removed from history straight away so
 * the token doesn't linger in the address bar or the back stack.
 */
export function AuthCallbackView() {
  const t = useTranslations("auth.callback");
  const tLogin = useTranslations("auth.login");
  const router = useRouter();
  const { signIn } = useSession();
  // Rendered with ssr: false, so reading `window` during the first render is safe.
  const [result] = useState(() => parseFragment(window.location.hash));
  const handled = useRef(false);

  const finish = (returnUrl: string) => {
    toast.success(tLogin("success"));
    router.replace(returnUrl);
    router.refresh();
  };

  useEffect(() => {
    if (handled.current) return;
    handled.current = true;
    window.history.replaceState(window.history.state, "", window.location.pathname);

    if (result.kind === "token") {
      try {
        signIn(result.token);
      } catch {
        router.replace(loginUrl());
        return;
      }
      toast.success(tLogin("success"));
      router.replace(result.returnUrl);
      router.refresh();
    }
  }, [result, router, signIn, tLogin]);

  if (result.kind === "twoFactor") {
    return (
      <TwoFactorForm
        twoFactorToken={result.twoFactorToken}
        onSuccess={() => finish(result.returnUrl)}
        onRestart={() => router.replace(loginUrl(result.returnUrl))}
      />
    );
  }

  if (result.kind === "error") {
    return (
      <StatePanel
        icon={ShieldAlert}
        tone="error"
        title={t("errorTitle")}
        description={t(`errors.${result.errorKey}`)}
        action={
          <Button asChild>
            <Link href="/login">{t("backToLogin")}</Link>
          </Button>
        }
      />
    );
  }

  return (
    <div role="status" className="flex flex-col items-center gap-4 py-16 text-center text-muted-foreground">
      <Loader2 className="size-8 animate-spin" aria-hidden="true" />
      <p>{t("signingIn")}</p>
    </div>
  );
}
