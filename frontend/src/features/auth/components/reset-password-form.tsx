"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2 } from "lucide-react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useMemo } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";

import { StatePanel } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { useSession } from "@/features/auth/session-provider";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";

import { useResetPassword } from "../hooks/use-auth-mutations";
import { createResetPasswordSchema, type ResetPasswordValues } from "../schemas";
import { FormError } from "./form-error";
import { PasswordInput } from "./password-input";

export function ResetPasswordForm() {
  const t = useTranslations("auth.resetPassword");
  const tAuth = useTranslations("auth");
  const tValidation = useTranslations("validation");
  const errorMessage = useApiErrorMessage();
  const router = useRouter();
  const token = useSearchParams().get("token")?.trim() ?? "";
  const { isAuthenticated, signOut } = useSession();
  const reset = useResetPassword();

  const schema = useMemo(() => createResetPasswordSchema(tValidation), [tValidation]);
  const form = useForm<ResetPasswordValues>({
    resolver: zodResolver(schema),
    defaultValues: { password: "" },
    mode: "onTouched",
  });
  const { errors } = form.formState;

  const onSubmit = form.handleSubmit(async ({ password }) => {
    try {
      await reset.mutateAsync({ token, password });
      toast.success(t("success"), { description: t("successDescription") });
      // A changed password should be used right away, also by someone who was signed in.
      if (isAuthenticated) signOut({ redirectTo: "/login" });
      else router.replace("/login");
    } catch {
      // Rendered below via reset.error
    }
  });

  if (!token) {
    return (
      <StatePanel
        title={t("missingTokenTitle")}
        description={t("missingTokenDescription")}
        action={
          <Button asChild>
            <Link href="/forgot-password">{t("requestNew")}</Link>
          </Button>
        }
      />
    );
  }

  const invalidToken = hasErrorCode(reset.error, "Authentication.InvalidToken");
  const busy = reset.isPending || reset.isSuccess;

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-5">
      <FormError message={errorMessage(reset.error)} />
      {invalidToken && (
        <Button asChild variant="outline">
          <Link href="/forgot-password">{t("requestNew")}</Link>
        </Button>
      )}
      <Field id="password" label={t("newPassword")} error={errors.password?.message} description={tAuth("passwordHint")}>
        {(control) => (
          <PasswordInput
            {...control}
            {...form.register("password")}
            autoComplete="new-password"
            autoFocus
            disabled={busy}
          />
        )}
      </Field>
      <Button type="submit" size="lg" disabled={busy} className="w-full">
        {busy && <Loader2 className="animate-spin" aria-hidden="true" />}
        {busy ? t("submitting") : t("submit")}
      </Button>
    </form>
  );
}
