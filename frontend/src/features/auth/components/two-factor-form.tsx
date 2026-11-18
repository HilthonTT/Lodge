"use client";

import { KeyRound, Loader2, ShieldCheck } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";

import { useLoginTwoFactor } from "../hooks/use-auth-mutations";
import { ONE_TIME_CODE_LENGTH } from "../schemas";
import { FormError } from "./form-error";
import { OneTimeCodeInput } from "./one-time-code-input";

type TwoFactorFormProps = {
  twoFactorToken: string;
  /** Called after the session cookie has been stored. */
  onSuccess: () => void;
  /** Back to the password step (or the login page), e.g. after the 5 minute token expired. */
  onRestart: () => void;
};

/** Second sign-in step: a code from the authenticator app, or a one-time recovery code. */
export function TwoFactorForm({ twoFactorToken, onSuccess, onRestart }: TwoFactorFormProps) {
  const t = useTranslations("auth.twoFactor");
  const tValidation = useTranslations("validation");
  const errorMessage = useApiErrorMessage();
  const verify = useLoginTwoFactor();
  const [mode, setMode] = useState<"code" | "recovery">("code");
  const [code, setCode] = useState("");
  const [recoveryCode, setRecoveryCode] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  const busy = verify.isPending || verify.isSuccess;
  const expired = hasErrorCode(verify.error, "Authentication.InvalidTwoFactorToken");

  const submit = async (value: string) => {
    if (busy) return;
    const trimmed = value.trim();
    if (mode === "code" && trimmed.length !== ONE_TIME_CODE_LENGTH) {
      setValidationError(trimmed ? tValidation("codeInvalid") : tValidation("codeRequired"));
      return;
    }
    if (mode === "recovery" && !trimmed) {
      setValidationError(tValidation("recoveryCodeRequired"));
      return;
    }
    setValidationError(null);
    try {
      await verify.mutateAsync(
        mode === "code" ? { twoFactorToken, code: trimmed } : { twoFactorToken, recoveryCode: trimmed },
      );
      onSuccess();
    } catch {
      // A wrong code clears the field so the next one can be typed straight away.
      if (mode === "code") setCode("");
    }
  };

  const switchMode = () => {
    verify.reset();
    setValidationError(null);
    setMode((m) => (m === "code" ? "recovery" : "code"));
  };

  return (
    <div className="grid gap-6">
      <div className="grid gap-3">
        <span className="grid size-12 place-items-center rounded-2xl bg-accent text-accent-foreground">
          {mode === "code" ? (
            <ShieldCheck className="size-6" aria-hidden="true" />
          ) : (
            <KeyRound className="size-6" aria-hidden="true" />
          )}
        </span>
        <h2 className="text-2xl font-semibold">{t("title")}</h2>
        <p className="text-sm text-muted-foreground">
          {mode === "code" ? t("description") : t("recoveryDescription")}
        </p>
      </div>

      <form
        noValidate
        className="grid gap-5"
        onSubmit={(event) => {
          event.preventDefault();
          void submit(mode === "code" ? code : recoveryCode);
        }}
      >
        <FormError message={errorMessage(verify.error)} />

        {mode === "code" ? (
          <Field id="two-factor-code" label={t("codeLabel")} error={validationError ?? undefined}>
            {(control) => (
              <OneTimeCodeInput
                {...control}
                autoFocus
                value={code}
                onValueChange={setCode}
                onComplete={(value) => void submit(value)}
                disabled={busy || expired}
              />
            )}
          </Field>
        ) : (
          <Field id="recovery-code" label={t("recoveryCodeLabel")} error={validationError ?? undefined}>
            {(control) => (
              <Input
                {...control}
                autoFocus
                autoComplete="off"
                spellCheck={false}
                placeholder="xxxxx-xxxxx"
                className="font-mono"
                value={recoveryCode}
                onChange={(event) => setRecoveryCode(event.target.value)}
                disabled={busy || expired}
              />
            )}
          </Field>
        )}

        {expired ? (
          <Button type="button" size="lg" className="w-full" onClick={onRestart}>
            {t("startOver")}
          </Button>
        ) : (
          <Button type="submit" size="lg" disabled={busy} className="w-full">
            {busy && <Loader2 className="animate-spin" aria-hidden="true" />}
            {busy ? t("verifying") : t("verify")}
          </Button>
        )}

        <div className="flex flex-wrap items-center justify-between gap-2 text-sm">
          <button
            type="button"
            onClick={switchMode}
            disabled={busy}
            className="font-medium text-primary underline-offset-4 hover:underline disabled:opacity-50"
          >
            {mode === "code" ? t("useRecoveryCode") : t("useAuthenticator")}
          </button>
          {!expired && (
            <button
              type="button"
              onClick={onRestart}
              disabled={busy}
              className="text-muted-foreground underline-offset-4 hover:text-foreground hover:underline disabled:opacity-50"
            >
              {t("startOver")}
            </button>
          )}
        </div>
      </form>
    </div>
  );
}
