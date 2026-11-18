"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2 } from "lucide-react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { safeRedirectPath } from "@/lib/auth/token";

import { useLogin } from "../hooks/use-auth-mutations";
import { createLoginSchema, type LoginValues } from "../schemas";
import { FormError } from "./form-error";
import { PasswordInput } from "./password-input";
import { SocialLoginButtons } from "./social-login-buttons";
import { TwoFactorForm } from "./two-factor-form";

export function LoginForm() {
  const t = useTranslations("auth");
  const tValidation = useTranslations("validation");
  const errorMessage = useApiErrorMessage();
  const router = useRouter();
  const searchParams = useSearchParams();
  const next = safeRedirectPath(searchParams.get("next"));
  const login = useLogin();
  const [twoFactorToken, setTwoFactorToken] = useState<string | null>(null);

  const schema = useMemo(() => createLoginSchema(tValidation), [tValidation]);
  const form = useForm<LoginValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: "", password: "" },
  });
  const { errors, isSubmitting } = form.formState;
  const busy = isSubmitting || login.isPending;

  const finish = () => {
    toast.success(t("login.success"));
    router.replace(next);
    router.refresh();
  };

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      const result = await login.mutateAsync(values);
      if (result.requiresTwoFactor && result.twoFactorToken) {
        setTwoFactorToken(result.twoFactorToken);
        return;
      }
      finish();
    } catch {
      // Rendered below via login.error
    }
  });

  if (twoFactorToken) {
    return (
      <TwoFactorForm
        twoFactorToken={twoFactorToken}
        onSuccess={finish}
        onRestart={() => {
          login.reset();
          form.resetField("password");
          setTwoFactorToken(null);
        }}
      />
    );
  }

  const registerHref = next !== "/" ? `/register?next=${encodeURIComponent(next)}` : "/register";

  return (
    <div className="grid gap-5">
      <SocialLoginButtons returnUrl={next} />

      <form onSubmit={onSubmit} noValidate className="grid gap-5">
        <FormError message={errorMessage(login.error)} />

        <Field id="email" label={t("email")} error={errors.email?.message}>
          {(control) => (
            <Input
              {...control}
              {...form.register("email")}
              type="email"
              autoComplete="email"
              inputMode="email"
              placeholder={t("emailPlaceholder")}
              disabled={busy}
            />
          )}
        </Field>

        <Field
          id="password"
          label={t("password")}
          error={errors.password?.message}
          aside={
            <Link
              href="/forgot-password"
              className="text-sm font-medium text-primary underline-offset-4 hover:underline"
            >
              {t("login.forgotPassword")}
            </Link>
          }
        >
          {(control) => (
            <PasswordInput
              {...control}
              {...form.register("password")}
              autoComplete="current-password"
              disabled={busy}
            />
          )}
        </Field>

        <Button type="submit" size="lg" disabled={busy} className="mt-1 w-full">
          {busy && <Loader2 className="animate-spin" aria-hidden="true" />}
          {busy ? t("login.submitting") : t("login.submit")}
        </Button>

        <p className="text-center text-sm text-muted-foreground">
          {t("login.newHere")}{" "}
          <Link href={registerHref} className="font-medium text-primary underline-offset-4 hover:underline">
            {t("login.createAccount")}
          </Link>
        </p>
      </form>
    </div>
  );
}
