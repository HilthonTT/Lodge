"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, Loader2, MailCheck } from "lucide-react";
import Link from "next/link";
import { useTranslations } from "next-intl";
import type * as React from "react";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";

import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { useApiErrorMessage } from "@/i18n/api-errors";

import { useForgotPassword } from "../hooks/use-auth-mutations";
import { createForgotPasswordSchema, type ForgotPasswordValues } from "../schemas";
import { FormError } from "./form-error";

/** "Check your inbox" panel, also used by Settings when a social-only account sets a password. */
export function CheckInbox({ title, description, children }: { title: string; description: string; children?: React.ReactNode }) {
  return (
    <div role="status" className="grid gap-4 rounded-2xl border bg-card p-6">
      <span className="grid size-12 place-items-center rounded-2xl bg-accent text-accent-foreground">
        <MailCheck className="size-6" aria-hidden="true" />
      </span>
      <div className="grid gap-1.5">
        <h2 className="text-lg font-semibold">{title}</h2>
        <p className="text-sm text-muted-foreground">{description}</p>
      </div>
      {children}
    </div>
  );
}

export function ForgotPasswordForm() {
  const t = useTranslations("auth.forgotPassword");
  const tAuth = useTranslations("auth");
  const tValidation = useTranslations("validation");
  const errorMessage = useApiErrorMessage();
  const forgot = useForgotPassword();
  const [sentTo, setSentTo] = useState<string | null>(null);

  const schema = useMemo(() => createForgotPasswordSchema(tValidation), [tValidation]);
  const form = useForm<ForgotPasswordValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: "" },
  });
  const { errors } = form.formState;

  const onSubmit = form.handleSubmit(async ({ email }) => {
    try {
      // The API answers 202 whether or not the account exists, so this can't be used to probe e-mails.
      await forgot.mutateAsync(email);
      setSentTo(email);
    } catch {
      // Rendered below via forgot.error
    }
  });

  if (sentTo) {
    return (
      <CheckInbox title={t("sentTitle")} description={t("sentDescription", { email: sentTo })}>
        <div className="flex flex-wrap gap-2">
          <Button asChild>
            <Link href="/login">{t("backToLogin")}</Link>
          </Button>
          <Button
            variant="ghost"
            onClick={() => {
              forgot.reset();
              setSentTo(null);
            }}
          >
            {t("tryAnother")}
          </Button>
        </div>
      </CheckInbox>
    );
  }

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-5">
      <FormError message={errorMessage(forgot.error)} />
      <Field id="email" label={tAuth("email")} error={errors.email?.message}>
        {(control) => (
          <Input
            {...control}
            {...form.register("email")}
            type="email"
            autoComplete="email"
            inputMode="email"
            autoFocus
            placeholder={tAuth("emailPlaceholder")}
            disabled={forgot.isPending}
          />
        )}
      </Field>
      <Button type="submit" size="lg" disabled={forgot.isPending} className="w-full">
        {forgot.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
        {forgot.isPending ? t("submitting") : t("submit")}
      </Button>
      <Link
        href="/login"
        className="inline-flex items-center justify-center gap-1.5 text-sm font-medium text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t("backToLogin")}
      </Link>
    </form>
  );
}
