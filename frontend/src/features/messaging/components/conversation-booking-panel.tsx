"use client";

import { CalendarDays, ExternalLink, House, ReceiptText, Users } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { RemoteImage } from "@/components/shared/remote-image";
import { UserAvatar } from "@/components/shared/user-avatar";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { BookingStatusBadge } from "@/features/bookings/components/booking-status-badge";
import { useBooking } from "@/features/bookings/hooks/use-bookings";
import { formatDateRange, nightsBetween, parseDateOnly } from "@/lib/dates";
import type { Conversation } from "@/types/api";

import { toBookingStatus } from "../message-text";

/** Listing and latest booking of a conversation, with links to both. */
export function ConversationBookingPanel({ conversation }: { conversation: Conversation }) {
  const t = useTranslations("messaging.panel");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const booking = conversation.latestBooking;
  const details = useBooking(booking?.id);
  const iAmGuest = conversation.otherParticipant.role === "Host";
  const start = parseDateOnly(booking?.durationStart);
  const end = parseDateOnly(booking?.durationEnd);
  const nights = start && end ? nightsBetween(start, end) : 0;
  const bookingHref = booking
    ? iAmGuest
      ? `/bookings?highlight=${encodeURIComponent(booking.id)}`
      : `/hosting/reservations?highlight=${encodeURIComponent(booking.id)}`
    : null;

  return (
    <div className="grid content-start gap-5 p-5" data-testid="conversation-panel">
      <div className="relative aspect-[4/3] overflow-hidden rounded-2xl bg-muted">
        <RemoteImage src={conversation.apartment.imageUrl} alt="" fill sizes="288px" className="object-cover" />
      </div>
      <div className="grid gap-1">
        <h2 className="font-semibold">{conversation.apartment.name}</h2>
        <Link
          href={`/users/${conversation.otherParticipant.id}`}
          className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground"
        >
          <UserAvatar name={conversation.otherParticipant.firstName} src={conversation.otherParticipant.avatarUrl} className="size-6" fallbackClassName="text-[0.6rem]" />
          {iAmGuest
            ? t("hostedBy", { name: conversation.otherParticipant.firstName })
            : t("guestIs", { name: conversation.otherParticipant.firstName })}
        </Link>
      </div>

      <Separator />

      {booking ? (
        <div className="grid gap-3">
          <div className="flex items-center justify-between gap-2">
            <h3 className="text-sm font-semibold">{t("booking")}</h3>
            <BookingStatusBadge status={toBookingStatus(booking.status)} />
          </div>
          <dl className="grid gap-2 text-sm">
            <div className="flex items-center gap-2">
              <dt className="sr-only">{t("dates")}</dt>
              <CalendarDays className="size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
              <dd>
                {start && end ? formatDateRange(start, end, locale) : "—"}
                {nights > 0 && <span className="text-muted-foreground"> · {tCommon("nights", { count: nights })}</span>}
              </dd>
            </div>
            <div className="flex items-center gap-2">
              <dt className="sr-only">{t("guests")}</dt>
              <Users className="size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
              <dd>
                {details.isPending ? (
                  <Skeleton className="h-4 w-16" />
                ) : details.data ? (
                  tCommon("guests", { count: details.data.guestCount || 1 })
                ) : (
                  "—"
                )}
              </dd>
            </div>
          </dl>
          <div className="grid gap-2">
            {bookingHref && (
              <Button asChild variant="outline" size="sm">
                <Link href={bookingHref}>
                  <ReceiptText aria-hidden="true" /> {iAmGuest ? t("viewTrip") : t("viewReservation")}
                </Link>
              </Button>
            )}
            <Button asChild variant="ghost" size="sm">
              <Link href={`/apartments/${conversation.apartment.id}`}>
                <House aria-hidden="true" /> {t("viewListing")}
              </Link>
            </Button>
          </div>
        </div>
      ) : (
        <div className="grid gap-3 text-sm">
          <p className="text-muted-foreground">{iAmGuest ? t("noBookingGuest") : t("noBookingHost")}</p>
          <Button asChild variant="outline" size="sm">
            <Link href={`/apartments/${conversation.apartment.id}`}>
              <ExternalLink aria-hidden="true" /> {t("viewListing")}
            </Link>
          </Button>
        </div>
      )}
    </div>
  );
}
