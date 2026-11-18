import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { ExperienceSimulatedCheckout } from "@/features/experiences/components/experience-checkout";
import { SimulatedCheckout } from "@/features/payments/components/simulated-checkout";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("payments.checkout");
  return { title: t("metaTitle"), robots: { index: false, follow: false } };
}

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const first = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);

// Auth is enforced by src/proxy.ts. The API returns this page as the checkout URL in Simulated mode:
// `?bookingId=` for stays, `?experienceBookingId=` for experiences.
export default async function SimulatedCheckoutPage({ searchParams }: PageProps<"/checkout/simulated">) {
  const query = await searchParams;
  const experienceBookingId = first(query.experienceBookingId);
  if (experienceBookingId && UUID.test(experienceBookingId)) {
    return <ExperienceSimulatedCheckout bookingId={experienceBookingId} />;
  }
  const bookingId = first(query.bookingId);
  return <SimulatedCheckout bookingId={bookingId && UUID.test(bookingId) ? bookingId : null} />;
}
