"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { BadgeCheck, ExternalLink, Loader2 } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { toast } from "sonner";

import { ErrorState } from "@/components/shared/state";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { FormError } from "@/features/auth/components/form-error";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatMonthYear } from "@/lib/dates";
import type { User } from "@/types/api";

import { useCurrentUser, useUpdateProfile } from "../hooks/use-user";
import {
  BIO_MAX_LENGTH,
  type BioFormValues,
  createBioFormSchema,
  createNameFormSchema,
  type NameFormValues,
} from "../schemas";
import { AvatarEditor } from "./avatar-editor";
import { SettingsRow } from "./settings-row";

export function PersonalInfo() {
  const t = useTranslations();
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const { data: user, isPending, isError, error, refetch } = useCurrentUser();
  const [editingName, setEditingName] = useState(false);
  const [editingBio, setEditingBio] = useState(false);

  if (isPending) {
    return (
      <div className="grid gap-6" role="status" aria-label={t("settings.personalInfo.loading")}>
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
      <AvatarEditor user={user} />

      <SettingsRow
        title={t("settings.personalInfo.legalName")}
        value={`${user.firstName} ${user.lastName}`}
        editing={editingName}
        onEdit={() => setEditingName(true)}
        onCancel={() => setEditingName(false)}
      >
        <NameForm user={user} onDone={() => setEditingName(false)} />
      </SettingsRow>

      <SettingsRow
        title={t("settings.personalInfo.bio")}
        value={
          user.bio ? (
            <span className="whitespace-pre-line">{user.bio}</span>
          ) : (
            t("settings.personalInfo.bioEmpty")
          )
        }
        editLabel={user.bio ? undefined : t("settings.personalInfo.add")}
        editing={editingBio}
        onEdit={() => setEditingBio(true)}
        onCancel={() => setEditingBio(false)}
      >
        <BioForm user={user} onDone={() => setEditingBio(false)} />
      </SettingsRow>

      <section className="grid gap-1 border-b py-6">
        <h2 className="font-medium">{t("settings.personalInfo.emailAddress")}</h2>
        <p className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
          {user.email}
          {user.emailVerified ? (
            <span className="inline-flex items-center gap-1 text-xs font-medium text-primary">
              <BadgeCheck className="size-3.5" aria-hidden="true" />
              {t("settings.personalInfo.emailVerified")}
            </span>
          ) : (
            <span className="text-xs font-medium text-destructive">{t("settings.personalInfo.emailNotVerified")}</span>
          )}
        </p>
        <p className="text-xs text-muted-foreground">{t("settings.personalInfo.emailHint")}</p>
      </section>

      <section className="flex flex-wrap items-end justify-between gap-4 py-6">
        <div className="grid gap-1">
          <h2 className="font-medium">{t("settings.personalInfo.memberSince")}</h2>
          <p className="text-sm text-muted-foreground">{formatMonthYear(new Date(user.createdOnUtc), locale)}</p>
        </div>
        <Button asChild variant="outline" size="sm">
          <Link href={`/users/${user.id}`}>
            <ExternalLink aria-hidden="true" /> {t("settings.personalInfo.viewProfile")}
          </Link>
        </Button>
      </section>
    </div>
  );
}

function NameForm({ user, onDone }: { user: User; onDone: () => void }) {
  const t = useTranslations();
  const errorMessage = useApiErrorMessage();
  const update = useUpdateProfile();
  const tValidation = useTranslations("validation");
  const schema = useMemo(() => createNameFormSchema(tValidation), [tValidation]);
  const form = useForm<NameFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { firstName: user.firstName, lastName: user.lastName },
  });
  const { errors, isDirty } = form.formState;

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await update.mutateAsync({ user, ...values });
      toast.success(t("settings.personalInfo.nameUpdated"));
      onDone();
    } catch {
      // inline
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="grid max-w-xl gap-4">
      <p className="text-sm text-muted-foreground">{t("settings.personalInfo.nameHint")}</p>
      <FormError message={errorMessage(update.error)} />
      <div className="grid gap-4 sm:grid-cols-2">
        <Field id="firstName" label={t("settings.personalInfo.firstName")} error={errors.firstName?.message}>
          {(control) => (
            <Input {...control} {...form.register("firstName")} autoComplete="given-name" disabled={update.isPending} />
          )}
        </Field>
        <Field id="lastName" label={t("settings.personalInfo.lastName")} error={errors.lastName?.message}>
          {(control) => (
            <Input {...control} {...form.register("lastName")} autoComplete="family-name" disabled={update.isPending} />
          )}
        </Field>
      </div>
      <div>
        <Button type="submit" disabled={update.isPending || !isDirty}>
          {update.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
          {t("common.save")}
        </Button>
      </div>
    </form>
  );
}

function BioForm({ user, onDone }: { user: User; onDone: () => void }) {
  const t = useTranslations();
  const errorMessage = useApiErrorMessage();
  const update = useUpdateProfile();
  const tValidation = useTranslations("validation");
  const schema = useMemo(() => createBioFormSchema(tValidation), [tValidation]);
  const form = useForm<BioFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { bio: user.bio ?? "" },
    mode: "onChange",
  });
  const { errors, isDirty } = form.formState;
  const length = useWatch({ control: form.control, name: "bio" }).length;

  const onSubmit = form.handleSubmit(async ({ bio }) => {
    try {
      // "" clears the bio; null would leave it unchanged.
      await update.mutateAsync({ user, bio });
      toast.success(t("settings.personalInfo.bioUpdated"));
      onDone();
    } catch {
      // inline
    }
  });

  return (
    <form onSubmit={onSubmit} noValidate className="grid max-w-xl gap-4">
      <FormError message={errorMessage(update.error)} />
      <Field
        id="bio"
        label={t("settings.personalInfo.bioLabel")}
        error={errors.bio?.message}
        aside={
          <span
            className={length > BIO_MAX_LENGTH ? "text-xs font-medium text-destructive" : "text-xs text-muted-foreground"}
            aria-live="polite"
          >
            {t("settings.personalInfo.bioCounter", { count: String(length), max: String(BIO_MAX_LENGTH) })}
          </span>
        }
      >
        {(control) => (
          <Textarea
            {...control}
            {...form.register("bio")}
            rows={5}
            placeholder={t("settings.personalInfo.bioPlaceholder")}
            disabled={update.isPending}
          />
        )}
      </Field>
      <div>
        <Button type="submit" disabled={update.isPending || !isDirty}>
          {update.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
          {t("common.save")}
        </Button>
      </div>
    </form>
  );
}
