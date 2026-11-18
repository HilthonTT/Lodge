import { z } from "zod";

/** Mirrors Lodge.Domain.Users.Password rules so users see errors before submitting. */
export const passwordSchema = z
  .string()
  .min(6, "Use at least 6 characters")
  .regex(/[a-z]/, "Add a lowercase letter")
  .regex(/[A-Z]/, "Add an uppercase letter")
  .regex(/\d/, "Add a digit")
  .regex(/[^a-zA-Z0-9]/, "Add a symbol, e.g. ! or #");

export const emailSchema = z
  .string()
  .trim()
  .min(1, "Enter your email")
  .max(256, "Email must be 256 characters or fewer")
  .pipe(z.email("Enter a valid email address"));

export const nameSchema = (label: string) =>
  z.string().trim().min(1, `Enter your ${label}`).max(128, `${label} must be 128 characters or fewer`);

/** Login only checks presence: complexity rules belong to registration. */
export const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, "Enter your password"),
});

export const registerSchema = z.object({
  firstName: nameSchema("first name"),
  lastName: nameSchema("last name"),
  email: emailSchema,
  password: passwordSchema,
});

export type LoginValues = z.infer<typeof loginSchema>;
export type RegisterValues = z.infer<typeof registerSchema>;
