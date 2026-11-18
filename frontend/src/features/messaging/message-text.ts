"use client";

import { useTranslations } from "next-intl";
import { useCallback, useSyncExternalStore } from "react";

import { BookingStatus, type BookingStatusName, type SystemEvent, SYSTEM_EVENTS } from "@/types/api";

export function toBookingStatus(name: BookingStatusName): BookingStatus {
  return BookingStatus[name];
}

/** Localized text of a system message, falling back to the API's English body for unknown events. */
export function useSystemEventText() {
  const t = useTranslations("messaging.system");
  return useCallback(
    (event: SystemEvent | null, fallback: string) =>
      event && (SYSTEM_EVENTS as readonly string[]).includes(event) ? t(event) : fallback,
    [t],
  );
}

function subscribeVisibility(onChange: () => void) {
  document.addEventListener("visibilitychange", onChange);
  window.addEventListener("focus", onChange);
  return () => {
    document.removeEventListener("visibilitychange", onChange);
    window.removeEventListener("focus", onChange);
  };
}

/** Whether the tab is visible (false on the server). */
export function usePageVisible() {
  return useSyncExternalStore(
    subscribeVisibility,
    () => document.visibilityState === "visible",
    () => false,
  );
}
