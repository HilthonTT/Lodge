"use client";

import { FlaskConical, Lock } from "lucide-react";
import { useTranslations } from "next-intl";

import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

import { usePaymentsConfig } from "../hooks/use-payments";

/** "Test mode · no real payment" or "Secure payment by Stripe", from GET /payments/config. */
export function PaymentModeBadge({ className }: { className?: string }) {
  const t = useTranslations("payments");
  const config = usePaymentsConfig();
  if (!config.data) return null;
  return config.data.mode === "Simulated" ? (
    <Badge variant="warning" className={className} data-testid="payment-mode">
      <FlaskConical aria-hidden="true" /> {t("testMode")}
    </Badge>
  ) : (
    <Badge variant="outline" className={className} data-testid="payment-mode">
      <Lock aria-hidden="true" /> {t("stripeMode")}
    </Badge>
  );
}

/** One-line explanation of how the payment is processed. */
export function PaymentModeNote({ className }: { className?: string }) {
  const t = useTranslations("payments");
  const config = usePaymentsConfig();
  if (!config.data) return null;
  return (
    <p className={cn("flex flex-wrap items-center gap-2 text-sm text-muted-foreground", className)}>
      <PaymentModeBadge />
      {config.data.mode === "Simulated" ? t("simulatedNote") : t("stripeNote")}
    </p>
  );
}
