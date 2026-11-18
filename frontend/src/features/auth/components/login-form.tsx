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

import { useLogin } from "../hooks/use-auth-mutations";
import { loginSchema, type LoginValues } from "../schemas";
import { FormError } from "./form-error";
import { PasswordInput } from "./password-input";

export function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const next = safeRedirectPath(searchParams.get("next"));
  const login = useLogin();

  const form = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });
  const { errors, isSubmitting } = form.formState;
  const busy = isSubmitting || login.isPending;

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await login.mutateAsync(values);
      toast.success("Welcome back!");
      router.replace(next);
      router.refresh();
    } catch {
      // Rendered below via login.error
    }
  });

  const registerHref = next !== "/" ? `/register?next=${encodeURIComponent(next)}` : "/register";

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-5">
      <FormError message={login.error?.message} />

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

      <Field id="password" label="Password" error={errors.password?.message}>
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
        {busy ? "Signing in…" : "Sign in"}
      </Button>

      <p className="text-center text-sm text-muted-foreground">
        New to Lodge?{" "}
        <Link href={registerHref} className="font-medium text-primary underline-offset-4 hover:underline">
          Create an account
        </Link>
      </p>
    </form>
  );
}
