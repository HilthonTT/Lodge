"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { format } from "date-fns";
import { Loader2 } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";

import { ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { FormError } from "@/features/auth/components/form-error";
import type { User } from "@/types/api";

import { useCurrentUser, useUpdateName } from "../hooks/use-user";
import { nameFormSchema, type NameFormValues } from "../schemas";
import { SettingsRow } from "./settings-row";

export function PersonalInfo() {
  const { data: user, isPending, isError, error, refetch } = useCurrentUser();
  const [editingName, setEditingName] = useState(false);

  if (isPending) {
    return (
      <div className="grid gap-6" role="status" aria-label="Loading profile">
        {Array.from({ length: 3 }, (_, i) => (
          <div key={i} className="grid gap-2 border-b pb-6">
            <Skeleton className="h-4 w-24" />
            <Skeleton className="h-4 w-48" />
          </div>
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <ErrorState
        title="We couldn't load your profile"
        description={error.message}
        action={
          <Button variant="outline" onClick={() => refetch()}>
            Try again
          </Button>
        }
      />
    );
  }

  return (
    <div className="grid">
      <SettingsRow
        title="Legal name"
        value={`${user.firstName} ${user.lastName}`}
        editing={editingName}
        onEdit={() => setEditingName(true)}
        onCancel={() => setEditingName(false)}
      >
        <NameForm user={user} onDone={() => setEditingName(false)} />
      </SettingsRow>

      <section className="grid gap-1 border-b py-6">
        <h2 className="font-medium">Email address</h2>
        <p className="text-sm text-muted-foreground">{user.email}</p>
        <p className="text-xs text-muted-foreground">Your email is used to sign in and can&apos;t be changed here.</p>
      </section>

      <section className="grid gap-1 py-6">
        <h2 className="font-medium">Member since</h2>
        <p className="text-sm text-muted-foreground">{format(new Date(user.createdOnUtc), "MMMM yyyy")}</p>
      </section>
    </div>
  );
}

function NameForm({ user, onDone }: { user: User; onDone: () => void }) {
  const update = useUpdateName();
  const form = useForm<NameFormValues>({
    resolver: zodResolver(nameFormSchema),
    defaultValues: { firstName: user.firstName, lastName: user.lastName },
  });
  const { errors, isDirty } = form.formState;

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await update.mutateAsync({ user, ...values });
      toast.success("Name updated");
      onDone();
    } catch {
      // inline
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="grid max-w-xl gap-4">
      <p className="text-sm text-muted-foreground">Make sure this matches the name on your government ID.</p>
      <FormError message={update.error?.message} />
      <div className="grid gap-4 sm:grid-cols-2">
        <Field id="firstName" label="First name" error={errors.firstName?.message}>
          {(control) => (
            <Input {...control} {...form.register("firstName")} autoComplete="given-name" disabled={update.isPending} />
          )}
        </Field>
        <Field id="lastName" label="Last name" error={errors.lastName?.message}>
          {(control) => (
            <Input {...control} {...form.register("lastName")} autoComplete="family-name" disabled={update.isPending} />
          )}
        </Field>
      </div>
      <div>
        <Button type="submit" disabled={update.isPending || !isDirty}>
          {update.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
          Save
        </Button>
      </div>
    </form>
  );
}
