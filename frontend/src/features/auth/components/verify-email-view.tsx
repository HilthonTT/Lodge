"use client";

import { CircleCheck, Loader2, MailX } from "lucide-react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useRef } from "react";

import { StatePanel } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { useSession } from "@/features/auth/session-provider";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";

import { useRefreshSession, useVerifyEmail } from "../hooks/use-auth-mutations";

/** Confirms the token from the e-mail link, then refreshes the session so the new claim sticks. */
export function VerifyEmailView() {
  const t = useTranslations("auth.verifyEmail");
  const errorMessage = useApiErrorMessage();
  const token = useSearchParams().get("token")?.trim() ?? "";
  const { isAuthenticated } = useSession();
  const refreshSession = useRefreshSession();
  const verify = useVerifyEmail();
  const { mutate } = verify;

  // Tokens are single-use: make sure Strict Mode's double effect doesn't send it twice.
  const started = useRef(false);
  useEffect(() => {
    if (!token || started.current) return;
    started.current = true;
    mutate(token, {
      onSuccess: () => {
        if (isAuthenticated) void refreshSession();
      },
    });
  }, [token, mutate, isAuthenticated, refreshSession]);

  const signInAction = (
    <Button asChild>
      <Link href={isAuthenticated ? "/" : "/login"}>{isAuthenticated ? t("continue") : t("signIn")}</Link>
    </Button>
  );

  if (!token || hasErrorCode(verify.error, "Authentication.InvalidToken")) {
    return (
      <StatePanel
        icon={MailX}
        tone="error"
        title={t("invalidTitle")}
        description={isAuthenticated ? t("invalidDescription") : t("invalidDescriptionSignedOut")}
        action={signInAction}
      />
    );
  }

  if (verify.isError) {
    return (
      <StatePanel
        icon={MailX}
        tone="error"
        title={t("errorTitle")}
        description={errorMessage(verify.error)}
        action={
          <Button variant="outline" onClick={() => verify.mutate(token)}>
            {t("retry")}
          </Button>
        }
      />
    );
  }

  if (verify.isSuccess) {
    return (
      <StatePanel
        icon={CircleCheck}
        title={t("successTitle")}
        description={t("successDescription")}
        action={signInAction}
      />
    );
  }

  return (
    <div role="status" className="flex flex-col items-center gap-4 py-16 text-center text-muted-foreground">
      <Loader2 className="size-8 animate-spin" aria-hidden="true" />
      <p>{t("verifying")}</p>
    </div>
  );
}
