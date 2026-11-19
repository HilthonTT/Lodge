import { z } from "zod";

import { createNameSchemas, createPasswordSchema, type ValidationTranslator } from "@/features/auth/schemas";

export const createNameFormSchema = (t: ValidationTranslator) => z.object(createNameSchemas(t));

export const createPasswordFormSchema = (t: ValidationTranslator) =>
  z
    .object({
      currentPassword: z.string().min(1, t("passwordRequired")),
      password: createPasswordSchema(t),
      confirmPassword: z.string().min(1, t("confirmPasswordRequired")),
    })
    .refine((v) => v.password === v.confirmPassword, {
      path: ["confirmPassword"],
      message: t("passwordsMismatch"),
    });

export const BIO_MAX_LENGTH = 500;

export const createBioFormSchema = (t: ValidationTranslator) =>
  z.object({ bio: z.string().trim().max(BIO_MAX_LENGTH, t("bioTooLong", { max: String(BIO_MAX_LENGTH) })) });

/** Avatar rules enforced by the API: JPEG/PNG/WebP up to 5 MB. */
export const AVATAR_TYPES = ["image/jpeg", "image/png", "image/webp"] as const;
export const AVATAR_MAX_MB = 5;

/** Returns the validation message key for an unacceptable avatar file, or null. */
export function avatarFileProblem(file: File): "avatarType" | "avatarSize" | null {
  if (!(AVATAR_TYPES as readonly string[]).includes(file.type)) return "avatarType";
  if (file.size > AVATAR_MAX_MB * 1024 * 1024) return "avatarSize";
  return null;
}

export type NameFormValues = z.infer<ReturnType<typeof createNameFormSchema>>;
export type BioFormValues = z.infer<ReturnType<typeof createBioFormSchema>>;
export type PasswordFormValues = z.infer<ReturnType<typeof createPasswordFormSchema>>;
