"use client";

import { MailWarning } from "lucide-react";
import { useTranslations } from "next-intl";

import { ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { ResendVerificationButton } from "@/features/auth/components/email-verification";
import { useRefreshSession } from "@/features/auth/hooks/use-auth-mutations";
import { useSession } from "@/features/auth/session-provider";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";

/** Explains that hosting needs a verified e-mail address and offers a new link. */
export function VerifyToHost() {
  const t = useTranslations("hosting.gate");
  const { session } = useSession();
  const refreshSession = useRefreshSession();
  return (
    <section className="mx-auto grid max-w-xl justify-items-center gap-5 rounded-3xl border bg-card p-8 text-center shadow-card sm:p-10">
      <span className="grid size-14 place-items-center rounded-2xl bg-accent text-accent-foreground">
        <MailWarning className="size-6" aria-hidden="true" />
      </span>
      <div className="grid gap-2">
        <h2 className="text-xl font-semibold">{t("title")}</h2>
        <p className="text-muted-foreground">{t("description", { email: session?.email ?? "" })}</p>
      </div>
      <div className="flex flex-wrap justify-center gap-2">
        <ResendVerificationButton variant="default" />
        <Button size="sm" variant="ghost" onClick={() => void refreshSession()}>
          {t("alreadyVerified")}
        </Button>
      </div>
    </section>
  );
}

/** Renders hosting pages only for verified users. */
export function HostingGate({ children }: { children: React.ReactNode }) {
  const { session } = useSession();
  if (session && !session.emailVerified) return <VerifyToHost />;
  return <>{children}</>;
}

/** Error state for hosting queries; a 403 `User.EmailNotVerified` shows the verify prompt instead. */
export function HostingError({
  error,
  title,
  onRetry,
}: {
  error: Error | null;
  title?: string;
  onRetry?: () => void;
}) {
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  if (hasErrorCode(error, "User.EmailNotVerified")) return <VerifyToHost />;
  return (
    <ErrorState
      title={title}
      description={errorMessage(error)}
      action={
        onRetry && (
          <Button variant="outline" onClick={onRetry}>
            {tCommon("tryAgain")}
          </Button>
        )
      }
    />
  );
}
