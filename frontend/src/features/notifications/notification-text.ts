"use client";

import {
  AlarmClock,
  CalendarCheck,
  CalendarRange,
  CalendarX2,
  CircleCheck,
  CircleX,
  DoorOpen,
  HandCoins,
  Inbox,
  Luggage,
  type LucideIcon,
  MessageSquareReply,
  Star,
  Ticket,
  TimerOff,
  Wallet,
} from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useCallback } from "react";

import { formatDateRange, formatMediumDate, formatSessionDateTime, parseDateOnly } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import type { AppNotification } from "@/types/api";

export const NOTIFICATION_ICONS: Record<string, LucideIcon> = {
  BookingRequested: Inbox,
  BookingAccepted: CircleCheck,
  BookingDeclined: CircleX,
  BookingExpired: TimerOff,
  BookingConfirmed: CalendarCheck,
  BookingCancelled: CalendarX2,
  BookingDatesChanged: CalendarRange,
  TripReminder: Luggage,
  ArrivalReminder: DoorOpen,
  ReviewReminder: Star,
  RefundSucceeded: HandCoins,
  ReviewReceived: Star,
  ReviewReplyReceived: MessageSquareReply,
  PayoutPaid: Wallet,
  ExperienceBooked: Ticket,
  ExperienceReminder: AlarmClock,
};

/**
 * Maps a notification link to a route this app has, so nothing 404s:
 * `/stays/{id}` without dates goes to the listing, and `?bookingId=` on the trips
 * page becomes the `highlight` parameter it understands.
 */
export function notificationHref(link: string | null | undefined): string {
  if (!link || !link.startsWith("/") || link.startsWith("//")) return "/notifications";
  const url = new URL(link, "http://lodge.local");
  const stay = /^\/stays\/([^/]+)\/?$/.exec(url.pathname);
  if (stay && !url.searchParams.get("startDate")) return `/apartments/${stay[1]}`;
  if (url.pathname === "/bookings") {
    const id = url.searchParams.get("bookingId");
    if (id) return `/bookings?highlight=${encodeURIComponent(id)}`;
  }
  if (url.pathname === "/hosting/reservations") {
    const id = url.searchParams.get("bookingId");
    if (id) return `/hosting/reservations?highlight=${encodeURIComponent(id)}`;
  }
  return url.pathname + url.search + url.hash;
}

/** Localized one-line text for a notification, built from its `type` and `data`. */
export function useNotificationText() {
  const t = useTranslations("notifications.types");
  const locale = useLocale();

  return useCallback(
    (n: AppNotification): string => {
      const d = n.data ?? {};
      const str = (key: string) => {
        const value = d[key];
        return value === null || value === undefined ? "" : String(value);
      };
      const num = (key: string) => {
        const value = Number(d[key]);
        return Number.isFinite(value) ? value : 0;
      };
      const range = (fromKey: string, toKey: string) => {
        const from = parseDateOnly(str(fromKey));
        const to = parseDateOnly(str(toKey));
        return from && to ? formatDateRange(from, to, locale) : "";
      };
      const day = (key: string) => {
        const date = parseDateOnly(str(key));
        return date ? formatMediumDate(date, locale) : "";
      };
      const instant = (key: string) => {
        const date = new Date(str(key));
        return Number.isNaN(date.getTime()) ? "" : formatSessionDateTime(date, locale);
      };
      const money = (key: string) => (d[key] === null || d[key] === undefined ? "" : formatPrice(num(key), str("currency") || "USD", locale));

      const listing = str("listingName");
      const guest = str("guestFirstName") || t("someone");
      const host = str("hostFirstName") || t("yourHost");
      const dates = range("checkIn", "checkOut");
      const count = num("guestCount") || 1;
      const role = str("role") === "Host" ? "Host" : "Guest";

      switch (n.type) {
        case "BookingRequested":
          return t("BookingRequested", { guest, listing, dates, count });
        case "BookingAccepted":
          return t("BookingAccepted", { host, listing, dates });
        case "BookingDeclined":
          return t("BookingDeclined", { host, listing, dates });
        case "BookingExpired":
          return t("BookingExpired", { listing, dates });
        case "BookingConfirmed":
          return t("BookingConfirmed", { role, guest, listing, dates, count });
        case "BookingCancelled": {
          const text = t("BookingCancelled", {
            cancelledBy: str("cancelledBy") === "Host" ? "Host" : "Guest",
            guest,
            host,
            listing,
            dates,
          });
          return num("refundAmount") > 0 ? `${text} ${t("refundNote", { amount: money("refundAmount") })}` : text;
        }
        case "BookingDatesChanged":
          return t("BookingDatesChanged", { guest, listing, previous: range("previousCheckIn", "previousCheckOut"), dates });
        case "TripReminder":
          return t("TripReminder", { listing, date: day("checkIn") });
        case "ArrivalReminder":
          return t("ArrivalReminder", { guest, listing, date: day("checkIn"), count });
        case "ReviewReminder":
          return t("ReviewReminder", { role, guest, listing, deadline: day("reviewDeadline") });
        case "RefundSucceeded":
          return t("RefundSucceeded", { amount: money("amount"), name: str("experienceTitle") || listing });
        case "ReviewReceived":
          return t("ReviewReceived", {
            kind: str("kind") === "Guest" ? "Guest" : "Listing",
            reviewer: str("reviewerFirstName") || t("someone"),
            listing,
            rating: num("rating"),
          });
        case "ReviewReplyReceived":
          return t("ReviewReplyReceived", { host, listing });
        case "PayoutPaid":
          return t("PayoutPaid", { amount: money("amount"), listing });
        case "ExperienceBooked":
          return t("ExperienceBooked", { role, guest, title: str("experienceTitle"), date: instant("startsAtUtc"), count });
        case "ExperienceReminder":
          return t("ExperienceReminder", {
            role,
            guest,
            title: str("experienceTitle"),
            date: instant("startsAtUtc"),
            meetingPoint: str("meetingPoint"),
          });
        default:
          return t("generic");
      }
    },
    [locale, t],
  );
}
