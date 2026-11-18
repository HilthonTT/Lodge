import "server-only";

import { notFound } from "next/navigation";
import { cache } from "react";

import { attempt } from "@/lib/api/client";
import { getServerHttp } from "@/lib/api/server";

import { apartmentsApi } from "./apartments";

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const isApartmentId = (value: string) => UUID.test(value);

/**
 * Loads an apartment for a page (deduplicated between `generateMetadata` and the
 * page). Unknown ids render the 404 page; other failures return the error so the
 * page can show a friendly message.
 */
export const loadApartment = cache(async (apartmentId: string) => {
  if (!isApartmentId(apartmentId)) notFound();
  const http = await getServerHttp();
  const result = await attempt(() => apartmentsApi.get(http, apartmentId));
  if (!result.ok && (result.error.isNotFound || result.error.status === 400)) notFound();
  return result;
});
