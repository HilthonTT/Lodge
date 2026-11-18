"use client";

import { Loader2, MailWarning, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useCallback, useSyncExternalStore } from "react";
import { toast } from "sonner";

import { Container } from "@/components/shared/container";
import { Button } from "@/components/ui/button";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";
import { cn } from "@/lib/utils";

import { useRefreshSession, useResendVerification } from "../hooks/use-auth-mutations";
import { useSession } from "../session-provider";

/** "Resend e-mail" with friendly handling of the 60 second throttle. */
export function ResendVerificationButton({
  className,
  variant = "outline",
}: {
  className?: string;
  variant?: "outline" | "default" | "secondary";
}) {
  const t = useTranslations("verification");
  const errorMessage = useApiErrorMessage();
  const resend = useResendVerification();
  const refreshSession = useRefreshSession();

  const onClick = () =>
    resend.mutate(undefined, {
      onSuccess: () => toast.success(t("sent"), { description: t("sentDescription") }),
      onError: (error) => {
        if (hasErrorCode(error, "User.VerificationEmailThrottled")) {
          toast.info(t("throttled"));
        } else if (hasErrorCode(error, "User.EmailAlreadyVerified")) {
          // Verified elsewhere (another tab or device): pick up the new claim.
          toast.success(t("alreadyVerified"));
          void refreshSession();
        } else {
          toast.error(errorMessage(error));
        }
      },
    });

  return (
    <Button size="sm" variant={variant} className={className} onClick={onClick} disabled={resend.isPending}>
      {resend.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
      {resend.isPending ? t("resending") : t("resend")}
    </Button>
  );
}

// Dismissal lasts for the browser session (sessionStorage), per user.
const DISMISS_EVENT = "lodge:verify-banner";
const dismissKey = (userId: string) => `lodge:verify-banner-dismissed:${userId}`;

function subscribe(callback: () => void) {
  window.addEventListener(DISMISS_EVENT, callback);
  return () => window.removeEventListener(DISMISS_EVENT, callback);
}

function readDismissed(userId: string) {
  try {
    return window.sessionStorage.getItem(dismissKey(userId)) === "1";
  } catch {
    return false;
  }
}

/** Shown to signed-in users whose e-mail address isn't verified yet. */
export function EmailVerificationBanner() {
  const t = useTranslations("verification");
  const { session } = useSession();
  const userId = session?.userId ?? "";
  const dismissed = useSyncExternalStore(
    subscribe,
    useCallback(() => (userId ? readDismissed(userId) : false), [userId]),
    () => false,
  );

  if (!session || session.emailVerified || dismissed) return null;

  const dismiss = () => {
    try {
      window.sessionStorage.setItem(dismissKey(session.userId), "1");
    } catch {
      // Storage blocked: the banner just comes back on the next page load.
    }
    window.dispatchEvent(new Event(DISMISS_EVENT));
  };

  return (
    <div role="region" aria-label={t("bannerLabel")} className="border-b bg-accent/60 text-accent-foreground">
      <Container className="flex flex-wrap items-center gap-x-4 gap-y-2 py-3">
        <MailWarning className="size-5 shrink-0" aria-hidden="true" />
        <p className="min-w-0 flex-1 text-sm">
          <span className="font-medium">{t("bannerTitle")}</span>{" "}
          {t("bannerText", { email: session.email })}
        </p>
        <div className="flex items-center gap-1">
          <ResendVerificationButton className="bg-background" />
          <Button size="icon-sm" variant="ghost" onClick={dismiss} aria-label={t("dismiss")}>
            <X aria-hidden="true" />
          </Button>
        </div>
      </Container>
    </div>
  );
}

/** Explains why reserving was refused (403 `User.EmailNotVerified`) and offers a new link. */
export function EmailNotVerifiedNotice({ className }: { className?: string }) {
  const t = useTranslations("verification");
  const { session } = useSession();

  return (
    <div
      role="alert"
      className={cn("grid gap-3 rounded-2xl border border-primary/25 bg-accent/50 p-4 text-sm", className)}
    >
      <div className="flex items-start gap-3">
        <MailWarning className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden="true" />
        <div className="grid gap-1">
          <p className="font-semibold">{t("reserveTitle")}</p>
          <p className="text-muted-foreground">{t("reserveDescription", { email: session?.email ?? "" })}</p>
        </div>
      </div>
      <ResendVerificationButton className="justify-self-start" variant="default" />
    </div>
  );
}
