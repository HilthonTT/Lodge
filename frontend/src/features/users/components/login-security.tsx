"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, LogOut, Mail } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";

import { ErrorState } from "@/components/shared/state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Skeleton } from "@/components/ui/skeleton";
import { CheckInbox } from "@/features/auth/components/forgot-password-form";
import { FormError } from "@/features/auth/components/form-error";
import { PasswordInput } from "@/features/auth/components/password-input";
import { useForgotPassword } from "@/features/auth/hooks/use-auth-mutations";
import { useSession } from "@/features/auth/session-provider";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";
import { formatTime } from "@/lib/dates";
import type { ExternalProviderName, User } from "@/types/api";

import { useChangePassword, useCurrentUser } from "../hooks/use-user";
import { createPasswordFormSchema, type PasswordFormValues } from "../schemas";
import { SettingsRow } from "./settings-row";
import { TwoFactorSettings } from "./two-factor-settings";

const PROVIDERS = [
  { name: "google", label: "Google" },
  { name: "github", label: "GitHub" },
] as const satisfies readonly { name: ExternalProviderName; label: string }[];

export function LoginSecurity() {
  const t = useTranslations();
  const errorMessage = useApiErrorMessage();
  const { data: user, isPending, isError, error, refetch } = useCurrentUser();

  if (isPending) {
    return (
      <div className="grid gap-6" role="status" aria-label={t("settings.personalInfo.loading")}>
        {Array.from({ length: 4 }, (_, i) => (
          <div key={i} className="grid gap-2 border-b pb-6">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-4 w-64" />
          </div>
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <ErrorState
        title={t("settings.personalInfo.loadError")}
        description={errorMessage(error)}
        action={
          <Button variant="outline" onClick={() => refetch()}>
            {t("common.tryAgain")}
          </Button>
        }
      />
    );
  }

  return (
    <div className="grid">
      <PasswordRow user={user} />
      <TwoFactorSettings user={user} />
      <ConnectedAccounts user={user} />
      <CurrentSession />
    </div>
  );
}

function PasswordRow({ user }: { user: User }) {
  const t = useTranslations("settings.security");
  const [editing, setEditing] = useState(false);
  // Also switches to "set a password" when the API answers User.PasswordNotSet.
  const [passwordNotSet, setPasswordNotSet] = useState(false);
  const noPassword = !user.hasPassword || passwordNotSet;

  return (
    <SettingsRow
      title={t("password")}
      value={noPassword ? t("noPasswordValue") : t("passwordValue")}
      editLabel={noPassword ? t("setPassword") : t("update")}
      editing={editing}
      onEdit={() => setEditing(true)}
      onCancel={() => setEditing(false)}
    >
      {noPassword ? (
        <SetPasswordPanel email={user.email} />
      ) : (
        <PasswordForm onNoPassword={() => setPasswordNotSet(true)} />
      )}
    </SettingsRow>
  );
}

/** Social-only accounts get a password through the reset e-mail, which also proves they own the address. */
function SetPasswordPanel({ email }: { email: string }) {
  const t = useTranslations("settings.security");
  const errorMessage = useApiErrorMessage();
  const forgot = useForgotPassword();

  if (forgot.isSuccess) {
    return <CheckInbox title={t("linkSent")} description={t("linkSentDescription", { email })} />;
  }

  return (
    <div className="grid max-w-md gap-4">
      <p className="text-sm text-muted-foreground">{t("setPasswordDescription", { email })}</p>
      <FormError message={errorMessage(forgot.error)} />
      <div>
        <Button onClick={() => forgot.mutate(email)} disabled={forgot.isPending}>
          {forgot.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Mail aria-hidden="true" />}
          {forgot.isPending ? t("sendingLink") : t("sendLink")}
        </Button>
      </div>
    </div>
  );
}

function PasswordForm({ onNoPassword }: { onNoPassword: () => void }) {
  const t = useTranslations();
  const errorMessage = useApiErrorMessage();
  const { signOut } = useSession();
  const changePassword = useChangePassword();
  const tValidation = useTranslations("validation");
  const schema = useMemo(() => createPasswordFormSchema(tValidation), [tValidation]);
  const form = useForm<PasswordFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { currentPassword: "", password: "", confirmPassword: "" },
    mode: "onTouched",
  });
  const { errors } = form.formState;

  const onSubmit = form.handleSubmit(async ({ currentPassword, password }) => {
    try {
      await changePassword.mutateAsync({ currentPassword, password });
      toast.success(t("settings.security.changed"), { description: t("settings.security.changedDescription") });
      signOut({ redirectTo: "/login" });
    } catch (error) {
      if (hasErrorCode(error, "User.PasswordNotSet")) onNoPassword();
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="grid max-w-md gap-4">
      <FormError message={errorMessage(changePassword.error)} />
      <Field id="current-password" label={t("settings.security.currentPassword")} error={errors.currentPassword?.message}>
        {(control) => (
          <PasswordInput
            {...control}
            {...form.register("currentPassword")}
            autoComplete="current-password"
            disabled={changePassword.isPending}
          />
        )}
      </Field>
      <Field
        id="new-password"
        label={t("settings.security.newPassword")}
        error={errors.password?.message}
        description={t("auth.passwordHint")}
      >
        {(control) => (
          <PasswordInput
            {...control}
            {...form.register("password")}
            autoComplete="new-password"
            disabled={changePassword.isPending}
          />
        )}
      </Field>
      <Field id="confirm-password" label={t("settings.security.confirmPassword")} error={errors.confirmPassword?.message}>
        {(control) => (
          <PasswordInput
            {...control}
            {...form.register("confirmPassword")}
            autoComplete="new-password"
            disabled={changePassword.isPending}
          />
        )}
      </Field>
      <div>
        <Button type="submit" disabled={changePassword.isPending}>
          {changePassword.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
          {t("settings.security.updatePassword")}
        </Button>
      </div>
    </form>
  );
}

function ConnectedAccounts({ user }: { user: User }) {
  const t = useTranslations("settings.security");
  return (
    <section className="grid gap-4 border-b py-6" aria-labelledby="connected-heading">
      <div className="grid gap-1">
        <h2 id="connected-heading" className="font-medium">
          {t("connectedAccounts")}
        </h2>
        <p className="text-sm text-muted-foreground">{t("connectedAccountsDescription")}</p>
      </div>
      <ul className="grid gap-2">
        {PROVIDERS.map(({ name, label }) => {
          const connected = user.externalLogins.includes(name);
          return (
            <li key={name} className="flex items-center justify-between gap-4 rounded-xl border px-4 py-3">
              <span className="text-sm font-medium">{label}</span>
              <Badge variant={connected ? "success" : "muted"}>{connected ? t("connected") : t("notConnected")}</Badge>
            </li>
          );
        })}
      </ul>
    </section>
  );
}

function CurrentSession() {
  const t = useTranslations("settings.security");
  const locale = useLocale();
  const { session, signOut } = useSession();
  const expires = session ? new Date(session.expiresAt) : null;

  return (
    <section className="flex flex-col gap-4 py-6 sm:flex-row sm:items-center sm:justify-between">
      <div className="grid gap-1">
        <h2 className="font-medium">{t("currentSession")}</h2>
        <p className="text-sm text-muted-foreground">
          {t("signedInAs", { email: session?.email ?? "" })}
          {expires && (
            <>
              {" "}
              ·{" "}
              <time dateTime={expires.toISOString()} suppressHydrationWarning>
                {t("expiresAt", { time: formatTime(expires, locale) })}
              </time>
            </>
          )}
        </p>
      </div>
      <Button variant="outline" onClick={() => signOut({ redirectTo: "/" })}>
        <LogOut aria-hidden="true" /> {t("signOut")}
      </Button>
    </section>
  );
}
