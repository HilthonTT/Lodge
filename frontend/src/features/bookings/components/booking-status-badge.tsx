import { Ban, CheckCircle2, CircleCheckBig, Clock, XCircle } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { BookingStatus } from "@/types/api";

export const BOOKING_STATUS = {
  [BookingStatus.Reserved]: { label: "Awaiting payment", variant: "warning", icon: Clock },
  [BookingStatus.Confirmed]: { label: "Confirmed", variant: "success", icon: CheckCircle2 },
  [BookingStatus.Completed]: { label: "Completed", variant: "info", icon: CircleCheckBig },
  [BookingStatus.Cancelled]: { label: "Cancelled", variant: "muted", icon: XCircle },
  [BookingStatus.Rejected]: { label: "Rejected", variant: "destructive", icon: Ban },
} as const;

export function BookingStatusBadge({ status }: { status: BookingStatus }) {
  const config = BOOKING_STATUS[status] ?? { label: "Unknown", variant: "muted" as const, icon: Clock };
  const Icon = config.icon;
  return (
    <Badge variant={config.variant}>
      <Icon aria-hidden="true" />
      {config.label}
    </Badge>
  );
}
