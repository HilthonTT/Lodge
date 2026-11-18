"use client";

import { Loader2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { EmailNotVerifiedNotice } from "@/features/auth/components/email-verification";
import { FormError } from "@/features/auth/components/form-error";
import { useUserId } from "@/features/auth/session-provider";
import { hasErrorCode, useApiErrorMessage } from "@/i18n/api-errors";
import { uuid } from "@/lib/uuid";

import { useReserveBooking } from "../hooks/use-bookings";

type ReserveButtonProps = {
  apartmentId: string;
  startDate: string;
  endDate: string;
};

export function ReserveButton({ apartmentId, startDate, endDate }: ReserveButtonProps) {
  const t = useTranslations("checkout");
  const errorMessage = useApiErrorMessage();
  const router = useRouter();
  const userId = useUserId();
  const reserve = useReserveBooking();
  // One key per checkout page: double clicks and retries can't create two bookings.
  const [idempotencyKey] = useState(uuid);

  const onReserve = async () => {
    if (!userId) return;
    try {
      const bookingId = await reserve.mutateAsync({
        body: { apartmentId, userId, startDate, endDate },
        idempotencyKey,
      });
      toast.success(t("successTitle"), { description: t("successDescription") });
      router.push(`/bookings?highlight=${encodeURIComponent(bookingId)}`);
    } catch {
      // Error rendered inline
    }
  };

  const busy = reserve.isPending || reserve.isSuccess;

  return (
    <div className="grid gap-3">
      {hasErrorCode(reserve.error, "User.EmailNotVerified") ? (
        <EmailNotVerifiedNotice />
      ) : (
        <FormError message={errorMessage(reserve.error)} />
      )}
      <Button size="lg" className="w-full sm:w-auto sm:min-w-56" onClick={onReserve} disabled={busy || !userId}>
        {busy && <Loader2 className="animate-spin" aria-hidden="true" />}
        {busy ? t("reserving") : t("confirm")}
      </Button>
    </div>
  );
}
