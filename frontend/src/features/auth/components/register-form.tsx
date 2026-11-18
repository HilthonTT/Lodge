"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2 } from "lucide-react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useMemo } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { safeRedirectPath } from "@/lib/auth/token";

import { useRegister } from "../hooks/use-auth-mutations";
import { createRegisterSchema, type RegisterValues } from "../schemas";
import { FormError } from "./form-error";
import { PasswordInput } from "./password-input";
import { SocialLoginButtons } from "./social-login-buttons";

export function RegisterForm() {
  const t = useTranslations("auth");
  const tValidation = useTranslations("validation");
  const errorMessage = useApiErrorMessage();
  const router = useRouter();
  const searchParams = useSearchParams();
  const next = safeRedirectPath(searchParams.get("next"));
  const registerUser = useRegister();

  const schema = useMemo(() => createRegisterSchema(tValidation), [tValidation]);
  const form = useForm<RegisterValues>({
    resolver: zodResolver(schema),
    defaultValues: { firstName: "", lastName: "", email: "", password: "" },
    mode: "onTouched",
  });
  const { errors, isSubmitting } = form.formState;
  const busy = isSubmitting || registerUser.isPending;

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await registerUser.mutateAsync(values);
      // The account starts unverified: the API has just sent the verification e-mail.
      toast.success(t("register.successTitle"), {
        description: t("register.successDescription", { email: values.email }),
        duration: 10_000,
      });
      router.replace(next);
      router.refresh();
    } catch {
      // Rendered below via registerUser.error
    }
  });

  const loginHref = next !== "/" ? `/login?next=${encodeURIComponent(next)}` : "/login";

  return (
    <div className="grid gap-5">
      <SocialLoginButtons returnUrl={next} />
      <form onSubmit={onSubmit} noValidate className="grid gap-5">
        <FormError message={errorMessage(registerUser.error)} />

        <div className="grid gap-5 sm:grid-cols-2">
          <Field id="firstName" label={t("register.firstName")} error={errors.firstName?.message}>
            {(control) => (
              <Input {...control} {...form.register("firstName")} autoComplete="given-name" disabled={busy} />
            )}
          </Field>
          <Field id="lastName" label={t("register.lastName")} error={errors.lastName?.message}>
            {(control) => (
              <Input {...control} {...form.register("lastName")} autoComplete="family-name" disabled={busy} />
            )}
          </Field>
        </div>

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

        <Field id="password" label={t("password")} error={errors.password?.message} description={t("passwordHint")}>
          {(control) => (
            <PasswordInput
              {...control}
              {...form.register("password")}
              autoComplete="new-password"
              disabled={busy}
            />
          )}
        </Field>

        <Button type="submit" size="lg" disabled={busy} className="mt-1 w-full">
          {busy && <Loader2 className="animate-spin" aria-hidden="true" />}
          {busy ? t("register.submitting") : t("register.submit")}
        </Button>

        <p className="text-center text-sm text-muted-foreground">
          {t("register.haveAccount")}{" "}
          <Link href={loginHref} className="font-medium text-primary underline-offset-4 hover:underline">
            {t("register.signIn")}
          </Link>
        </p>
      </form>
    </div>
  );
}
