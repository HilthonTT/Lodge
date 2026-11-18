import { useTranslations } from "next-intl";
import { useMemo } from "react";

import { ApiError, type ApiErrorMessageKey } from "@/lib/api/client";

/**
 * Backend error codes the UI explains in the user's language, mapped to keys
 * under `errors.api.codes.*`. Any other server message is shown as returned.
 */
const ERROR_CODE_KEYS = {
  "Authentication.InvalidEmailOrPassword": "invalidEmailOrPassword",
  "Authentication.InvalidTwoFactorToken": "invalidTwoFactorToken",
  "Authentication.InvalidTwoFactorCode": "invalidTwoFactorCode",
  "Authentication.InvalidToken": "invalidToken",
  "User.VerificationEmailThrottled": "verificationEmailThrottled",
  "User.EmailAlreadyVerified": "emailAlreadyVerified",
  "User.EmailNotVerified": "emailNotVerified",
  "User.PasswordNotSet": "passwordNotSet",
  "User.InvalidAvatar": "invalidAvatar",
  "User.AvatarTooLarge": "avatarTooLarge",
  "User.BioTooLong": "bioTooLong",
  "TwoFactor.InvalidCode": "invalidTwoFactorCode",
  "TwoFactor.InvalidPassword": "invalidPassword",
  "TwoFactor.NotSetUp": "twoFactorNotSetUp",
  "TwoFactor.AlreadyEnabled": "twoFactorAlreadyEnabled",
  "TwoFactor.NotEnabled": "twoFactorNotEnabled",
} as const;

type ErrorCodeKey = (typeof ERROR_CODE_KEYS)[keyof typeof ERROR_CODE_KEYS];
type ApiErrorTranslator = (key: ApiErrorMessageKey | `codes.${ErrorCodeKey}`) => string;
type MaybeError = Error | null | undefined;

function codeKey(code: string | undefined): ErrorCodeKey | undefined {
  return code && Object.hasOwn(ERROR_CODE_KEYS, code)
    ? ERROR_CODE_KEYS[code as keyof typeof ERROR_CODE_KEYS]
    : undefined;
}

/** Whether the error (or one of its validation rules) has the given backend code. */
export function hasErrorCode(error: unknown, code: string): boolean {
  return error instanceof ApiError && (error.code === code || error.codes.includes(code));
}

/**
 * Text to show for an API error: a translated message for well-known backend
 * codes, otherwise the server's own message as returned, or the translated
 * frontend fallback (`errors.api.*`) when the server didn't send one.
 * Errors that aren't `ApiError`s get the generic fallback.
 *
 * Server components: `apiErrorMessage(error, await getTranslations("errors.api"))`.
 */
export function apiErrorMessage(error: Error, t: ApiErrorTranslator): string;
export function apiErrorMessage(error: MaybeError, t: ApiErrorTranslator): string | undefined;
export function apiErrorMessage(error: MaybeError, t: ApiErrorTranslator) {
  if (!error) return undefined;
  if (!(error instanceof ApiError)) return t("generic");
  const key = codeKey(error.code) ?? error.codes.map(codeKey).find(Boolean);
  if (key) return t(`codes.${key}`);
  return error.messageKey ? t(error.messageKey) : error.message;
}

/** Client/shared components: `const errorMessage = useApiErrorMessage(); errorMessage(query.error)`. */
export function useApiErrorMessage() {
  const t = useTranslations("errors.api");
  return useMemo(() => {
    function errorMessage(error: Error): string;
    function errorMessage(error: MaybeError): string | undefined;
    function errorMessage(error: MaybeError) {
      return apiErrorMessage(error, t);
    }
    return errorMessage;
  }, [t]);
}
