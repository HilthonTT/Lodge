import type { Http } from "@/lib/api/client";
import type { ExchangeRates, PaymentsConfig } from "@/types/api";

export const paymentsApi = {
  /** Anonymous: "Stripe" or "Simulated". */
  config: (http: Http) => http.get<PaymentsConfig>("/payments/config").then((r) => r.data),

  /** Anonymous: exchange rates relative to USD. */
  currencies: (http: Http) => http.get<ExchangeRates>("/currencies").then((r) => r.data),

  /** Guest only, Simulated mode: confirms the booking like the Stripe webhook would. */
  completeSimulated: (http: Http, bookingId: string) =>
    http.post<void>(`/payments/simulated/${encodeURIComponent(bookingId)}/complete`).then(() => undefined),
};
