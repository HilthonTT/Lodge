"use client";

import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";

import type { Booking } from "@/types/api";

/** Fallback when an older API doesn't send `requestExpiresOnUtc`: 24 hours after the request. */
const REQUEST_TTL_MS = 24 * 60 * 60 * 1000;

/**
 * When a pending request expires: the API's `requestExpiresOnUtc` (the earlier
 * of 24 hours after the request and the end of the check-in day).
 */
export function requestExpiresAt(booking: Pick<Booking, "requestExpiresOnUtc" | "createdOnUtc">) {
  return booking.requestExpiresOnUtc
    ? new Date(booking.requestExpiresOnUtc).getTime()
    : new Date(booking.createdOnUtc).getTime() + REQUEST_TTL_MS;
}

/** "Expires in 21 h 14 min", refreshed every 30 seconds. Renders nothing until mounted. */
export function RequestCountdown({
  booking,
  className,
}: {
  booking: Pick<Booking, "requestExpiresOnUtc" | "createdOnUtc">;
  className?: string;
}) {
  const t = useTranslations("bookings.request");
  const [now, setNow] = useState<number | null>(null);

  useEffect(() => {
    const tick = () => setNow(Date.now());
    tick();
    const timer = window.setInterval(tick, 30_000);
    return () => window.clearInterval(timer);
  }, []);

  if (now === null) return null;
  const expiresAt = requestExpiresAt(booking);
  const remaining = expiresAt - now;
  if (remaining <= 0) return <span className={className}>{t("expiring")}</span>;
  const hours = Math.floor(remaining / 3_600_000);
  const minutes = Math.floor((remaining % 3_600_000) / 60_000);
  return (
    <time className={className} dateTime={new Date(expiresAt).toISOString()}>
      {t("expiresIn", { hours: String(hours), minutes: String(minutes) })}
    </time>
  );
}
