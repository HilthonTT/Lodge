"use client";

import { Award, Loader2, TriangleAlert, XCircle } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { formatDateRange, parseDateOnly } from "@/lib/dates";
import { formatPrice } from "@/lib/format";
import type { Booking } from "@/types/api";

import { useHostCancelBooking } from "../hooks/use-hosting";

/** Host cancellation of a confirmed stay, behind a strong warning and an explicit acknowledgement. */
export function HostCancelButton({ booking }: { booking: Booking }) {
  const t = useTranslations("hosting.cancel");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const errorMessage = useApiErrorMessage();
  const cancel = useHostCancelBooking();
  const [open, setOpen] = useState(false);
  const [acknowledged, setAcknowledged] = useState(false);
  const guest = booking.guestName || booking.guestEmail;
  const start = parseDateOnly(booking.durationStart);
  const end = parseDateOnly(booking.durationEnd);
  const refund = formatPrice(booking.amountPaid ?? booking.totalPriceAmount, booking.totalPriceCurrency, locale);

  const onOpenChange = (next: boolean) => {
    setOpen(next);
    if (!next) {
      setAcknowledged(false);
      cancel.reset();
    }
  };

  const onConfirm = () =>
    cancel.mutate(booking.id, {
      onSuccess: () => {
        toast.success(t("success"), { description: t("successDescription", { guest, amount: refund }) });
        onOpenChange(false);
      },
    });

  return (
    <>
      <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setOpen(true)}>
        <XCircle aria-hidden="true" /> {t("button")}
      </Button>
      <AlertDialog open={open} onOpenChange={onOpenChange}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="flex items-center gap-2">
              <TriangleAlert className="size-5 text-destructive" aria-hidden="true" />
              {t("title")}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {t("description", {
                guest,
                listing: booking.apartmentName,
                dates: start && end ? formatDateRange(start, end, locale) : "",
              })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <ul className="grid gap-3 rounded-xl border border-destructive/30 bg-destructive/5 p-4 text-sm">
            <li className="flex items-start gap-2">
              <XCircle className="mt-0.5 size-4 shrink-0 text-destructive" aria-hidden="true" />
              <span>{t("fullRefund", { amount: refund })}</span>
            </li>
            <li className="flex items-start gap-2">
              <Award className="mt-0.5 size-4 shrink-0 text-destructive" aria-hidden="true" />
              <span>{t("superhost")}</span>
            </li>
            <li className="flex items-start gap-2">
              <TriangleAlert className="mt-0.5 size-4 shrink-0 text-destructive" aria-hidden="true" />
              <span>{t("irreversible")}</span>
            </li>
          </ul>
          <label className="flex items-start gap-3 text-sm">
            <Checkbox
              checked={acknowledged}
              onCheckedChange={(value) => setAcknowledged(value === true)}
              className="mt-0.5"
            />
            <span>{t("acknowledge")}</span>
          </label>
          {cancel.error && (
            <p role="alert" className="text-sm text-destructive">
              {errorMessage(cancel.error)}
            </p>
          )}
          <AlertDialogFooter>
            <AlertDialogCancel>{tCommon("back")}</AlertDialogCancel>
            <Button variant="destructive" onClick={onConfirm} disabled={!acknowledged || cancel.isPending}>
              {cancel.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
              {t("confirm")}
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
