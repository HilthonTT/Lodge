"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, LogOut } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { FormError } from "@/features/auth/components/form-error";
import { PasswordInput } from "@/features/auth/components/password-input";
import { useSession } from "@/features/auth/session-provider";

import { useChangePassword } from "../hooks/use-user";
import { passwordFormSchema, type PasswordFormValues } from "../schemas";
import { SettingsRow } from "./settings-row";

export function LoginSecurity() {
  const { session, signOut } = useSession();
  const [editing, setEditing] = useState(false);
  const expires = session ? new Date(session.expiresAt) : null;

  return (
    <div className="grid">
      <SettingsRow
        title="Password"
        value="Use a strong password you don't use anywhere else."
        editLabel="Update"
        editing={editing}
        onEdit={() => setEditing(true)}
        onCancel={() => setEditing(false)}
      >
        <PasswordForm />
      </SettingsRow>

      <section className="flex flex-col gap-4 py-6 sm:flex-row sm:items-center sm:justify-between">
        <div className="grid gap-1">
          <h2 className="font-medium">Current session</h2>
          <p className="text-sm text-muted-foreground">
            Signed in as {session?.email}
            {expires && (
              <>
                {" "}
                · expires at{" "}
                <time dateTime={expires.toISOString()} suppressHydrationWarning>
                  {expires.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                </time>
              </>
            )}
          </p>
        </div>
        <Button variant="outline" onClick={() => signOut({ redirectTo: "/" })}>
          <LogOut aria-hidden="true" /> Sign out
        </Button>
      </section>
    </div>
  );
}

function PasswordForm() {
  const { signOut } = useSession();
  const changePassword = useChangePassword();
  const form = useForm<PasswordFormValues>({
    resolver: zodResolver(passwordFormSchema),
    defaultValues: { password: "", confirmPassword: "" },
    mode: "onTouched",
  });
  const { errors } = form.formState;

  const onSubmit = form.handleSubmit(async ({ password }) => {
    try {
      await changePassword.mutateAsync(password);
      toast.success("Password changed", { description: "Please sign in with your new password." });
      signOut({ redirectTo: "/login" });
    } catch {
      // inline
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="grid max-w-md gap-4">
      <FormError message={changePassword.error?.message} />
      <Field
        id="new-password"
        label="New password"
        error={errors.password?.message}
        description="At least 6 characters with upper- and lowercase letters, a digit and a symbol."
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
      <Field id="confirm-password" label="Confirm new password" error={errors.confirmPassword?.message}>
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
          Update password
        </Button>
      </div>
    </form>
  );
}
