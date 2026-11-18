"use client";

import { Loader2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { EmailNotVerifiedNotice } from "@/features/auth/components/email-verification";
import { FormError } from "@/features/auth/components/form-error";
import { useUserId } from "@/features/auth/session-provider";
import { useIdempotencyKey } from "@/hooks/use-idempotency-key";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";

import { useReserveBooking } from "../hooks/use-bookings";

export const MESSAGE_TO_HOST_MAX = 1000;

type ReserveButtonProps = {
  apartmentId: string;
  startDate: string;
  endDate: string;
  guestCount: number;
  /** Instant book creates a reservation; otherwise the guest sends a request the host answers within 24 hours. */
  instantBook: boolean;
  hostName: string;
};

export function ReserveButton({ apartmentId, startDate, endDate, guestCount, instantBook, hostName }: ReserveButtonProps) {
  const t = useTranslations("checkout");
  const errorMessage = useApiErrorMessage();
  const router = useRouter();
  const userId = useUserId();
  const reserve = useReserveBooking();
  // One key per distinct request: double clicks and retries can't create two bookings, and
  // changed dates, guests or message get a new key (the API replays a reused key's first result).
  const idempotencyKeyFor = useIdempotencyKey();
  const [message, setMessage] = useState("");
  const tooLong = message.length > MESSAGE_TO_HOST_MAX;

  const onReserve = async () => {
    if (!userId || tooLong) return;
    try {
      const body = {
        apartmentId,
        userId,
        startDate,
        endDate,
        guestCount,
        messageToHost: message.trim() || null,
      };
      const bookingId = await reserve.mutateAsync({ body, idempotencyKey: idempotencyKeyFor(body) });
      if (instantBook) {
        toast.success(t("successTitle"), { description: t("successDescription") });
      } else {
        toast.success(t("requestSentTitle"), { description: t("requestSentDescription", { name: hostName }) });
      }
      router.push(`/bookings?highlight=${encodeURIComponent(bookingId)}`);
    } catch {
      // Error rendered inline
    }
  };

  const busy = reserve.isPending || reserve.isSuccess;

  return (
    <div className="grid gap-5">
      <div className="grid gap-2">
        <label htmlFor="message-to-host" className="font-medium">
          {instantBook ? t("messageOptional", { name: hostName }) : t("messageLabel", { name: hostName })}
        </label>
        <p id="message-to-host-hint" className="text-sm text-muted-foreground">
          {instantBook ? t("messageHintInstant") : t("messageHint", { name: hostName })}
        </p>
        <Textarea
          id="message-to-host"
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          rows={4}
          placeholder={t("messagePlaceholder")}
          aria-describedby="message-to-host-hint message-to-host-count"
          aria-invalid={tooLong}
        />
        <p
          id="message-to-host-count"
          className={tooLong ? "text-right text-xs font-medium text-destructive" : "text-right text-xs text-muted-foreground"}
        >
          {message.length} / {MESSAGE_TO_HOST_MAX}
        </p>
      </div>
      {hasErrorCode(reserve.error, "User.EmailNotVerified") ? (
        <EmailNotVerifiedNotice />
      ) : (
        <FormError message={errorMessage(reserve.error)} />
      )}
      <Button
        size="lg"
        className="w-full sm:w-auto sm:min-w-56 sm:justify-self-start"
        onClick={onReserve}
        disabled={busy || !userId || tooLong}
      >
        {busy && <Loader2 className="animate-spin" aria-hidden="true" />}
        {busy ? (instantBook ? t("reserving") : t("requesting")) : instantBook ? t("confirm") : t("requestToBook")}
      </Button>
      {!instantBook && <p className="text-xs text-muted-foreground">{t("requestFootnote", { name: hostName })}</p>}
    </div>
  );
}
