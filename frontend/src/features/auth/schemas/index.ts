import type { useTranslations } from "next-intl";
import { z } from "zod";

/**
 * Translator for the `validation` namespace. Schemas are factories so their
 * messages follow the UI language: `useMemo(() => createLoginSchema(t), [t])`
 * with `const t = useTranslations("validation")`.
 */
export type ValidationTranslator = ReturnType<typeof useTranslations<"validation">>;

/** Mirrors Lodge.Domain.Users.Password rules so users see errors before submitting. */
export const createPasswordSchema = (t: ValidationTranslator) =>
  z
    .string()
    .min(6, t("passwordMin"))
    .regex(/[a-z]/, t("passwordLowercase"))
    .regex(/[A-Z]/, t("passwordUppercase"))
    .regex(/\d/, t("passwordDigit"))
    .regex(/[^a-zA-Z0-9]/, t("passwordSymbol"));

export const createEmailSchema = (t: ValidationTranslator) =>
  z
    .string()
    .trim()
    .min(1, t("emailRequired"))
    .max(256, t("emailTooLong"))
    .pipe(z.email(t("emailInvalid")));

export const createNameSchemas = (t: ValidationTranslator) => ({
  firstName: z.string().trim().min(1, t("firstNameRequired")).max(128, t("firstNameTooLong")),
  lastName: z.string().trim().min(1, t("lastNameRequired")).max(128, t("lastNameTooLong")),
});

/** Login only checks presence: complexity rules belong to registration. */
export const createLoginSchema = (t: ValidationTranslator) =>
  z.object({
    email: createEmailSchema(t),
    password: z.string().min(1, t("passwordRequired")),
  });

export const createRegisterSchema = (t: ValidationTranslator) =>
  z.object({
    ...createNameSchemas(t),
    email: createEmailSchema(t),
    password: createPasswordSchema(t),
  });

export const createForgotPasswordSchema = (t: ValidationTranslator) =>
  z.object({ email: createEmailSchema(t) });

export const createResetPasswordSchema = (t: ValidationTranslator) =>
  z.object({ password: createPasswordSchema(t) });

/** Authenticator apps show 6 digits (RFC 6238). */
export const ONE_TIME_CODE_LENGTH = 6;

export const createOneTimeCodeSchema = (t: ValidationTranslator) =>
  z
    .string()
    .trim()
    .min(1, t("codeRequired"))
    .regex(/^\d{6}$/, t("codeInvalid"));

export type LoginValues = z.infer<ReturnType<typeof createLoginSchema>>;
export type RegisterValues = z.infer<ReturnType<typeof createRegisterSchema>>;
export type ForgotPasswordValues = z.infer<ReturnType<typeof createForgotPasswordSchema>>;
export type ResetPasswordValues = z.infer<ReturnType<typeof createResetPasswordSchema>>;
