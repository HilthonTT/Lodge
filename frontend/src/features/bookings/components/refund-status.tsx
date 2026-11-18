import { CircleCheck, Clock, HandCoins, Loader2, TriangleAlert } from "lucide-react";
import { useTranslations } from "next-intl";

import { Badge } from "@/components/ui/badge";
import type { RefundStatus } from "@/types/api";

const STYLE = {
  Pending: { variant: "info", icon: Loader2, spin: true },
  Succeeded: { variant: "success", icon: CircleCheck, spin: false },
  Failed: { variant: "destructive", icon: TriangleAlert, spin: false },
  Manual: { variant: "muted", icon: HandCoins, spin: false },
  None: { variant: "muted", icon: Clock, spin: false },
} as const;

/** Refund state of a cancelled booking (Pending turns Succeeded within seconds). */
export function RefundStatusBadge({ status }: { status: RefundStatus }) {
  const t = useTranslations("bookings.refundStatus");
  if (status === "None") return null;
  const style = STYLE[status] ?? STYLE.None;
  const Icon = style.icon;
  return (
    <Badge variant={style.variant} role="status">
      <Icon className={style.spin ? "animate-spin" : undefined} aria-hidden="true" />
      {t(status)}
    </Badge>
  );
}
