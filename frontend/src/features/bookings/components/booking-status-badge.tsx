import { Ban, CheckCircle2, CircleCheckBig, Clock, XCircle } from "lucide-react";
import { useTranslations } from "next-intl";

import { Badge } from "@/components/ui/badge";
import { BookingStatus } from "@/types/api";

/** Badge style per status; `labelKey` lives under `bookings.status` in messages/*.json. */
export const BOOKING_STATUS = {
  [BookingStatus.Reserved]: { labelKey: "reserved", variant: "warning", icon: Clock },
  [BookingStatus.Confirmed]: { labelKey: "confirmed", variant: "success", icon: CheckCircle2 },
  [BookingStatus.Completed]: { labelKey: "completed", variant: "info", icon: CircleCheckBig },
  [BookingStatus.Cancelled]: { labelKey: "cancelled", variant: "muted", icon: XCircle },
  [BookingStatus.Rejected]: { labelKey: "rejected", variant: "destructive", icon: Ban },
} as const;

export function BookingStatusBadge({ status }: { status: BookingStatus }) {
  const t = useTranslations("bookings.status");
  const config = BOOKING_STATUS[status] ?? { labelKey: "unknown", variant: "muted" as const, icon: Clock };
  const Icon = config.icon;
  return (
    <Badge variant={config.variant}>
      <Icon aria-hidden="true" />
      {t(config.labelKey)}
    </Badge>
  );
}
