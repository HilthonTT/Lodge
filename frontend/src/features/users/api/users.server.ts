import "server-only";

import { notFound } from "next/navigation";
import { cache } from "react";

import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";
import { isUuid } from "@/lib/uuid";

import { usersApi } from "./users";

/**
 * Loads a public profile for a page (deduplicated between `generateMetadata` and
 * the page). Unknown users render the 404 page; other failures return the error.
 */
export const loadPublicProfile = cache(async (userId: string) => {
  if (!isUuid(userId)) notFound();
  const http = await getServerHttp();
  const result = await attempt(() => usersApi.profile(http, userId));
  if (!result.ok && (result.error.isNotFound || result.error.status === 400)) notFound();
  return result;
});
