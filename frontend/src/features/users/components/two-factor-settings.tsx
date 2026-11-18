"use client";

import { Loader2, ShieldCheck, ShieldOff } from "lucide-react";
import { useTranslations } from "next-intl";
import { QRCodeSVG } from "qrcode.react";
import { useState } from "react";
import { toast } from "sonner";

import { CopyButton } from "@/components/shared/copy-button";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Field } from "@/components/ui/field";
import { FormError } from "@/features/auth/components/form-error";
import { OneTimeCodeInput } from "@/features/auth/components/one-time-code-input";
import { PasswordInput } from "@/features/auth/components/password-input";
import { ONE_TIME_CODE_LENGTH } from "@/features/auth/schemas";
import { useApiErrorMessage } from "@/i18n/api-errors";
import type { TwoFactorSetupResponse, User } from "@/types/api";

import {
  useDisableTwoFactor,
  useEnableTwoFactor,
  useRegenerateRecoveryCodes,
  useSetupTwoFactor,
} from "../hooks/use-user";
import { RecoveryCodes } from "./recovery-codes";

type Phase = { kind: "idle" } | { kind: "setup"; setup: TwoFactorSetupResponse } | { kind: "codes"; codes: string[] };

/** Two-step verification: set up with an authenticator app, recovery codes, and turning it off. */
export function TwoFactorSettings({ user }: { user: User }) {
  const t = useTranslations("settings.security.twoFactor");
  const errorMessage = useApiErrorMessage();
  const setup = useSetupTwoFactor();
  const [phase, setPhase] = useState<Phase>({ kind: "idle" });
  const [dialog, setDialog] = useState<"regenerate" | "disable" | null>(null);

  const startSetup = () =>
    setup.mutate(undefined, { onSuccess: (data) => setPhase({ kind: "setup", setup: data }) });

  const enabled = user.twoFactorEnabled;

  return (
    <section className="grid gap-4 border-b py-6" aria-labelledby="two-factor-heading">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="grid max-w-xl gap-1">
          <h2 id="two-factor-heading" className="flex items-center gap-2 font-medium">
            {t("title")}
            <Badge variant={enabled ? "success" : "muted"}>{enabled ? t("on") : t("off")}</Badge>
          </h2>
          <p className="text-sm text-muted-foreground">{enabled ? t("descriptionOn") : t("descriptionOff")}</p>
        </div>
        {phase.kind === "idle" &&
          (enabled ? (
            <div className="flex flex-wrap gap-2">
              <Button size="sm" variant="outline" onClick={() => setDialog("regenerate")}>
                {t("regenerate")}
              </Button>
              <Button size="sm" variant="outline" onClick={() => setDialog("disable")}>
                <ShieldOff aria-hidden="true" /> {t("disable")}
              </Button>
            </div>
          ) : (
            <Button size="sm" onClick={startSetup} disabled={setup.isPending}>
              {setup.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <ShieldCheck aria-hidden="true" />}
              {setup.isPending ? t("settingUp") : t("setUp")}
            </Button>
          ))}
      </div>

      {phase.kind === "idle" && <FormError message={errorMessage(setup.error)} />}

      {phase.kind === "setup" && (
        <TwoFactorSetup
          setup={phase.setup}
          onCancel={() => setPhase({ kind: "idle" })}
          onEnabled={(codes) => {
            toast.success(t("enabled"));
            setPhase({ kind: "codes", codes });
          }}
        />
      )}

      {phase.kind === "codes" && (
        <RecoveryCodes codes={phase.codes} email={user.email} onDone={() => setPhase({ kind: "idle" })} />
      )}

      <RegenerateDialog
        open={dialog === "regenerate"}
        onOpenChange={(open) => setDialog(open ? "regenerate" : null)}
        onRegenerated={(codes) => {
          setDialog(null);
          toast.success(t("regenerated"));
          setPhase({ kind: "codes", codes });
        }}
      />
      <DisableDialog
        open={dialog === "disable"}
        hasPassword={user.hasPassword}
        onOpenChange={(open) => setDialog(open ? "disable" : null)}
        onDisabled={() => {
          setDialog(null);
          toast.success(t("disabled"));
        }}
      />
    </section>
  );
}

/** Groups the Base32 secret in fours so it's easier to type into an app. */
function formatSecret(secret: string) {
  return secret.replace(/(.{4})/g, "$1 ").trim();
}

function TwoFactorSetup({
  setup,
  onCancel,
  onEnabled,
}: {
  setup: TwoFactorSetupResponse;
  onCancel: () => void;
  onEnabled: (codes: string[]) => void;
}) {
  const t = useTranslations("settings.security.twoFactor");
  const tValidation = useTranslations("validation");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const enable = useEnableTwoFactor();
  const [code, setCode] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  const submit = (value: string) => {
    if (enable.isPending) return;
    if (value.length !== ONE_TIME_CODE_LENGTH) {
      setValidationError(value ? tValidation("codeInvalid") : tValidation("codeRequired"));
      return;
    }
    setValidationError(null);
    enable.mutate(value, {
      onSuccess: (data) => onEnabled(data.recoveryCodes),
      onError: () => setCode(""),
    });
  };

  return (
    <div className="grid gap-6 rounded-2xl border bg-card p-5 sm:p-6">
      <div className="grid gap-4 sm:grid-cols-[auto_1fr] sm:items-start sm:gap-6">
        <div className="justify-self-center rounded-xl border bg-white p-3 sm:justify-self-start">
          <QRCodeSVG
            value={setup.otpauthUri}
            size={168}
            marginSize={0}
            level="M"
            title={t("qrAlt")}
            role="img"
            aria-label={t("qrAlt")}
          />
        </div>
        <div className="grid gap-3">
          <div className="grid gap-1">
            <h3 className="font-semibold">{t("scanTitle")}</h3>
            <p className="text-sm text-muted-foreground">{t("scanDescription")}</p>
          </div>
          <div className="grid gap-2">
            <p className="text-sm text-muted-foreground">{t("manualEntry")}</p>
            <div className="flex flex-wrap items-center gap-2">
              <code data-testid="two-factor-secret" className="rounded-md bg-muted px-2.5 py-1.5 font-mono text-sm break-all">
                {formatSecret(setup.secret)}
              </code>
              <CopyButton value={setup.secret} label={t("copyKey")} />
            </div>
          </div>
        </div>
      </div>

      <form
        noValidate
        className="grid max-w-sm gap-4"
        onSubmit={(event) => {
          event.preventDefault();
          submit(code);
        }}
      >
        <div className="grid gap-1">
          <h3 className="font-semibold">{t("confirmTitle")}</h3>
          <p className="text-sm text-muted-foreground">{t("confirmDescription")}</p>
        </div>
        <FormError message={errorMessage(enable.error)} />
        <Field id="enable-two-factor-code" label={t("codeLabel")} error={validationError ?? undefined}>
          {(control) => (
            <OneTimeCodeInput
              {...control}
              value={code}
              onValueChange={setCode}
              onComplete={submit}
              disabled={enable.isPending}
            />
          )}
        </Field>
        <div className="flex flex-wrap gap-2">
          <Button type="submit" disabled={enable.isPending}>
            {enable.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
            {enable.isPending ? t("turningOn") : t("turnOn")}
          </Button>
          <Button type="button" variant="ghost" onClick={onCancel} disabled={enable.isPending}>
            {tCommon("cancel")}
          </Button>
        </div>
      </form>
    </div>
  );
}

function RegenerateDialog({
  open,
  onOpenChange,
  onRegenerated,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onRegenerated: (codes: string[]) => void;
}) {
  const t = useTranslations("settings.security.twoFactor");
  const tValidation = useTranslations("validation");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const regenerate = useRegenerateRecoveryCodes();
  const [code, setCode] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  const change = (next: boolean) => {
    if (!next) {
      setCode("");
      setValidationError(null);
      regenerate.reset();
    }
    onOpenChange(next);
  };

  const submit = (value: string) => {
    if (regenerate.isPending) return;
    if (value.length !== ONE_TIME_CODE_LENGTH) {
      setValidationError(value ? tValidation("codeInvalid") : tValidation("codeRequired"));
      return;
    }
    setValidationError(null);
    regenerate.mutate(value, {
      onSuccess: (data) => {
        change(false);
        onRegenerated(data.recoveryCodes);
      },
      onError: () => setCode(""),
    });
  };

  return (
    <Dialog open={open} onOpenChange={change}>
      <DialogContent>
        <form
          noValidate
          className="grid gap-5"
          onSubmit={(event) => {
            event.preventDefault();
            submit(code);
          }}
        >
          <DialogHeader>
            <DialogTitle>{t("regenerateTitle")}</DialogTitle>
            <DialogDescription>{t("regenerateDescription")}</DialogDescription>
          </DialogHeader>
          <FormError message={errorMessage(regenerate.error)} />
          <Field id="regenerate-code" label={t("codeLabel")} error={validationError ?? undefined}>
            {(control) => (
              <OneTimeCodeInput
                {...control}
                autoFocus
                value={code}
                onValueChange={setCode}
                onComplete={submit}
                disabled={regenerate.isPending}
              />
            )}
          </Field>
          <DialogFooter>
            <Button type="button" variant="ghost" onClick={() => change(false)}>
              {tCommon("cancel")}
            </Button>
            <Button type="submit" disabled={regenerate.isPending}>
              {regenerate.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
              {t("regenerateSubmit")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DisableDialog({
  open,
  hasPassword,
  onOpenChange,
  onDisabled,
}: {
  open: boolean;
  hasPassword: boolean;
  onOpenChange: (open: boolean) => void;
  onDisabled: () => void;
}) {
  const t = useTranslations("settings.security.twoFactor");
  const tValidation = useTranslations("validation");
  const tCommon = useTranslations("common");
  const errorMessage = useApiErrorMessage();
  const disable = useDisableTwoFactor();
  const [password, setPassword] = useState("");
  const [code, setCode] = useState("");
  const [errors, setErrors] = useState<{ password?: string; code?: string }>({});

  const change = (next: boolean) => {
    if (!next) {
      setPassword("");
      setCode("");
      setErrors({});
      disable.reset();
    }
    onOpenChange(next);
  };

  const submit = () => {
    const nextErrors: typeof errors = {};
    if (hasPassword && !password) nextErrors.password = tValidation("passwordRequired");
    if (code.length !== ONE_TIME_CODE_LENGTH) {
      nextErrors.code = code ? tValidation("codeInvalid") : tValidation("codeRequired");
    }
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length) return;
    disable.mutate(
      { password: hasPassword ? password : null, code },
      {
        onSuccess: () => {
          change(false);
          onDisabled();
        },
        onError: () => setCode(""),
      },
    );
  };

  return (
    <Dialog open={open} onOpenChange={change}>
      <DialogContent>
        <form
          noValidate
          className="grid gap-5"
          onSubmit={(event) => {
            event.preventDefault();
            submit();
          }}
        >
          <DialogHeader>
            <DialogTitle>{t("disableTitle")}</DialogTitle>
            <DialogDescription>
              {hasPassword ? t("disableDescription") : t("disableDescriptionNoPassword")}
            </DialogDescription>
          </DialogHeader>
          <FormError message={errorMessage(disable.error)} />
          {hasPassword && (
            <Field id="disable-password" label={t("currentPassword")} error={errors.password}>
              {(control) => (
                <PasswordInput
                  {...control}
                  autoFocus
                  autoComplete="current-password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  disabled={disable.isPending}
                />
              )}
            </Field>
          )}
          <Field id="disable-code" label={t("codeLabel")} error={errors.code}>
            {(control) => (
              <OneTimeCodeInput
                {...control}
                autoFocus={!hasPassword}
                value={code}
                onValueChange={setCode}
                disabled={disable.isPending}
              />
            )}
          </Field>
          <DialogFooter>
            <Button type="button" variant="ghost" onClick={() => change(false)}>
              {tCommon("cancel")}
            </Button>
            <Button type="submit" variant="destructive" disabled={disable.isPending}>
              {disable.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
              {t("disableSubmit")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
