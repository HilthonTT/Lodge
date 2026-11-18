import "server-only";

import { getServerToken } from "@/lib/auth/session.server";
import { SERVER_API_URL } from "@/lib/env.server";

import { createHttpClient, type Http } from "./client";

/** HTTP client for Server Components / Route Handlers. Forwards the user's token when present. */
export async function getServerHttp(opts: { auth?: boolean } = {}): Promise<Http> {
  const token = opts.auth ? await getServerToken() : null;
  return createHttpClient(SERVER_API_URL, token);
}
