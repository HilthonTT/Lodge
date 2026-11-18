"use client";

import { Camera, Loader2, Trash2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";
import { toast } from "sonner";

import { UserAvatar } from "@/components/shared/user-avatar";
import { Button } from "@/components/ui/button";
import { FormError } from "@/features/auth/components/form-error";
import { useApiErrorMessage } from "@/i18n/api-errors";
import type { User } from "@/types/api";

import { useDeleteAvatar, useUploadAvatar } from "../hooks/use-user";
import { AVATAR_MAX_MB, AVATAR_TYPES, avatarFileProblem } from "../schemas";

/** Profile photo: pick a file (validated client-side), see it instantly while it uploads, or remove it. */
export function AvatarEditor({ user }: { user: User }) {
  const t = useTranslations("settings.personalInfo");
  const tValidation = useTranslations("validation");
  const errorMessage = useApiErrorMessage();
  const upload = useUploadAvatar();
  const remove = useDeleteAvatar();
  const inputRef = useRef<HTMLInputElement>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [problem, setProblem] = useState<string | null>(null);

  // Release the object URL when it's replaced or the editor unmounts.
  useEffect(() => {
    if (!preview) return;
    return () => URL.revokeObjectURL(preview);
  }, [preview]);

  const busy = upload.isPending || remove.isPending;
  const name = `${user.firstName} ${user.lastName}`;
  const src = preview ?? user.avatarUrl;

  const onFile = (file: File | undefined) => {
    if (inputRef.current) inputRef.current.value = "";
    if (!file) return;
    upload.reset();
    remove.reset();
    const issue = avatarFileProblem(file);
    if (issue) {
      setProblem(tValidation(issue, { max: String(AVATAR_MAX_MB) }));
      return;
    }
    setProblem(null);
    setPreview(URL.createObjectURL(file));
    upload.mutate(file, {
      onSuccess: () => toast.success(t("photoUpdated")),
      onError: () => setPreview(null),
    });
  };

  const onRemove = () => {
    upload.reset();
    setProblem(null);
    remove.mutate(undefined, {
      onSuccess: () => {
        setPreview(null);
        toast.success(t("photoRemoved"));
      },
    });
  };

  return (
    <section className="grid gap-4 border-b py-6 first:pt-0" aria-labelledby="photo-heading">
      <div className="flex flex-wrap items-center gap-5">
        <div className="relative">
          <UserAvatar name={name} src={src} className="size-24" fallbackClassName="text-2xl" />
          {upload.isPending && (
            <span className="absolute inset-0 grid place-items-center rounded-full bg-black/40 text-white">
              <Loader2 className="size-6 animate-spin" aria-hidden="true" />
            </span>
          )}
        </div>
        <div className="grid gap-2">
          <h2 id="photo-heading" className="font-medium">
            {t("photo")}
          </h2>
          <p className="text-xs text-muted-foreground">{t("photoHint", { max: String(AVATAR_MAX_MB) })}</p>
          <div className="flex flex-wrap gap-2">
            <input
              ref={inputRef}
              type="file"
              accept={AVATAR_TYPES.join(",")}
              className="sr-only"
              tabIndex={-1}
              aria-hidden="true"
              onChange={(event) => onFile(event.target.files?.[0])}
            />
            <Button size="sm" variant="outline" disabled={busy} onClick={() => inputRef.current?.click()}>
              {upload.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Camera aria-hidden="true" />}
              {upload.isPending ? t("uploading") : user.avatarUrl ? t("changePhoto") : t("uploadPhoto")}
            </Button>
            {(user.avatarUrl || preview) && (
              <Button size="sm" variant="ghost" disabled={busy} onClick={onRemove}>
                {remove.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <Trash2 aria-hidden="true" />}
                {t("removePhoto")}
              </Button>
            )}
          </div>
        </div>
      </div>
      <FormError message={problem ?? errorMessage(upload.error ?? remove.error)} />
    </section>
  );
}
