import "server-only";

import { z } from "zod";

import { env } from "./env";

const internal = z
  .url()
  .optional()
  .catch(undefined)
  .parse(process.env.API_INTERNAL_BASE_URL || undefined);

/** Versioned API URL used by the Next.js server (may differ inside docker). */
export const SERVER_API_URL = `${(internal ?? env.NEXT_PUBLIC_API_BASE_URL).replace(/\/+$/, "")}/api/${env.NEXT_PUBLIC_API_VERSION}`;
