"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2 } from "lucide-react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { safeRedirectPath } from "@/lib/auth/token";

import { useRegister } from "../hooks/use-auth-mutations";
import { registerSchema, type RegisterValues } from "../schemas";
import { FormError } from "./form-error";
import { PasswordInput } from "./password-input";

export function RegisterForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const next = safeRedirectPath(searchParams.get("next"));
  const registerUser = useRegister();

  const form = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { firstName: "", lastName: "", email: "", password: "" },
    mode: "onTouched",
  });
  const { errors, isSubmitting } = form.formState;
  const busy = isSubmitting || registerUser.isPending;

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await registerUser.mutateAsync(values);
      toast.success("Your account is ready", { description: "Welcome to Lodge!" });
      router.replace(next);
      router.refresh();
    } catch {
      // Rendered below via registerUser.error
    }
  });

  const loginHref = next !== "/" ? `/login?next=${encodeURIComponent(next)}` : "/login";

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-5">
      <FormError message={registerUser.error?.message} />

      <div className="grid gap-5 sm:grid-cols-2">
        <Field id="firstName" label="First name" error={errors.firstName?.message}>
          {(control) => (
            <Input {...control} {...form.register("firstName")} autoComplete="given-name" disabled={busy} />
          )}
        </Field>
        <Field id="lastName" label="Last name" error={errors.lastName?.message}>
          {(control) => (
            <Input {...control} {...form.register("lastName")} autoComplete="family-name" disabled={busy} />
          )}
        </Field>
      </div>

      <Field id="email" label="Email" error={errors.email?.message}>
        {(control) => (
          <Input
            {...control}
            {...form.register("email")}
            type="email"
            autoComplete="email"
            inputMode="email"
            placeholder="you@example.com"
            disabled={busy}
          />
        )}
      </Field>

      <Field
        id="password"
        label="Password"
        error={errors.password?.message}
        description="At least 6 characters with upper- and lowercase letters, a digit and a symbol."
      >
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
        {busy ? "Creating account…" : "Create account"}
      </Button>

      <p className="text-center text-sm text-muted-foreground">
        Already have an account?{" "}
        <Link href={loginHref} className="font-medium text-primary underline-offset-4 hover:underline">
          Sign in
        </Link>
      </p>
    </form>
  );
}
