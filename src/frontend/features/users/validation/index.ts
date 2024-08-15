import { z } from "zod";

export const LegalNameValidation = z.object({
  firstName: z
    .string()
    .max(128, { message: "First name must be below 128 characters" }),
  lastName: z
    .string()
    .max(128, { message: "Last name must be below 128 characters" }),
});

export const PasswordValidation = z.object({
  password: z
    .string()
    .min(6, { message: "Password must be at least 6 characters" })
    .regex(/[a-z]/, {
      message: "Password must contain at least one lowercase letter",
    })
    .regex(/[A-Z]/, {
      message: "Password must contain at least one uppercase letter",
    })
    .regex(/\d/, { message: "Password must contain at least one digit" })
    .regex(/[^a-zA-Z0-9]/, {
      message: "Password must contain at least one special character",
    }),
});
