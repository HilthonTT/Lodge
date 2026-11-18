import { z } from "zod";

/**
 * Validated environment. `NEXT_PUBLIC_*` values must be referenced literally so
 * Next.js can inline them into the client bundle. Defaults keep `next build`
 * working without a `.env` file and without a running backend.
 */
const schema = z.object({
  NEXT_PUBLIC_API_BASE_URL: z
    .url({ message: "NEXT_PUBLIC_API_BASE_URL must be an absolute URL" })
    .default("http://localhost:8000")
    .transform((v) => v.replace(/\/+$/, "")),
  NEXT_PUBLIC_API_VERSION: z
    .string()
    .regex(/^v\d+$/, { message: "NEXT_PUBLIC_API_VERSION must look like 'v1'" })
    .default("v1"),
});

const parsed = schema.safeParse({
  NEXT_PUBLIC_API_BASE_URL: process.env.NEXT_PUBLIC_API_BASE_URL || undefined,
  NEXT_PUBLIC_API_VERSION: process.env.NEXT_PUBLIC_API_VERSION || undefined,
});

if (!parsed.success) {
  throw new Error(
    `Invalid environment variables:\n${z.prettifyError(parsed.error)}`,
  );
}

export const env = parsed.data;

/** Base URL of the versioned API, e.g. http://localhost:8000/api/v1 */
export const API_URL = `${env.NEXT_PUBLIC_API_BASE_URL}/api/${env.NEXT_PUBLIC_API_VERSION}`;
