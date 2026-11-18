import "server-only";

import { notFound } from "next/navigation";
import { cache } from "react";

import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";
import { isUuid } from "@/lib/uuid";

import { experiencesApi } from "./experiences";

/** Loads an experience for a page (shared by `generateMetadata` and the page); unknown ids 404. */
export const loadExperience = cache(async (id: string) => {
  if (!isUuid(id)) notFound();
  const http = await getServerHttp();
  const result = await attempt(() => experiencesApi.get(http, id));
  if (!result.ok && (result.error.isNotFound || result.error.status === 400)) notFound();
  return result;
});
