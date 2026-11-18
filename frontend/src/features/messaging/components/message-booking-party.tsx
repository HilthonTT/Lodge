"use client";

import { Loader2, MessageCircle } from "lucide-react";
import { useRouter } from "next/navigation";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { useApiErrorMessage } from "@/i18n/api-errors";
import type { Booking } from "@/types/api";

import { useFindConversation } from "../hooks/use-messaging";
import { ContactHostDialog } from "./contact-host-dialog";

/**
 * "Message host" (guest) or "Message guest" (host) on a booking: opens the
 * conversation for that listing. A guest without one yet writes the first message.
 */
export function MessageBookingParty({
  booking,
  as,
  hostName,
  size = "default",
}: {
  booking: Booking;
  as: "guest" | "host";
  hostName?: string;
  size?: "default" | "sm";
}) {
  const t = useTranslations("messaging.entry");
  const router = useRouter();
  const errorMessage = useApiErrorMessage();
  const find = useFindConversation();
  const [busy, setBusy] = useState(false);
  const [composeOpen, setComposeOpen] = useState(false);

  const onClick = async () => {
    setBusy(true);
    try {
      const conversation = await find(booking.apartmentId, as === "host" ? booking.userId : undefined);
      if (conversation) router.push(`/messages/${conversation.id}`);
      else if (as === "guest") setComposeOpen(true);
      else toast.info(t("noConversation"), { description: t("noConversationText") });
    } catch (error) {
      toast.error(t("error"), { description: errorMessage(error as Error) });
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <Button variant="outline" size={size} onClick={onClick} disabled={busy} data-testid={as === "guest" ? "message-host" : "message-guest"}>
        {busy ? <Loader2 className="animate-spin" aria-hidden="true" /> : <MessageCircle aria-hidden="true" />}
        {as === "guest" ? t("messageHost") : t("messageGuest")}
      </Button>
      {as === "guest" && (
        <ContactHostDialog
          apartmentId={booking.apartmentId}
          listingName={booking.apartmentName}
          host={{ firstName: hostName ?? t("yourHost"), avatarUrl: null }}
          open={composeOpen}
          onOpenChange={setComposeOpen}
        />
      )}
    </>
  );
}
