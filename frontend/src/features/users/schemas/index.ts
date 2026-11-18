import { z } from "zod";

import { nameSchema, passwordSchema } from "@/features/auth/schemas";

export const nameFormSchema = z.object({
  firstName: nameSchema("first name"),
  lastName: nameSchema("last name"),
});

export const passwordFormSchema = z
  .object({
    password: passwordSchema,
    confirmPassword: z.string().min(1, "Confirm your new password"),
  })
  .refine((v) => v.password === v.confirmPassword, {
    path: ["confirmPassword"],
    message: "Passwords don't match",
  });

export type NameFormValues = z.infer<typeof nameFormSchema>;
export type PasswordFormValues = z.infer<typeof passwordFormSchema>;
